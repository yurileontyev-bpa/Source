﻿// <copyright file="SearchController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.ListSearch.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using System.Xml;
    using Microsoft.Teams.Apps.Common.Logging;
    using Microsoft.Teams.Apps.ListSearch.Common.Helpers;
    using Microsoft.Teams.Apps.ListSearch.Common.Models;
    using Microsoft.Teams.Apps.ListSearch.Filters;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Search controller that handles search flow.
    /// </summary>
    public class SearchController : Controller
    {
        private readonly JwtHelper jwtHelper;
        private readonly int topResultsToBeFetched;
        private readonly int minimumConfidenceScore;
        private readonly string tenantId;
        private readonly ILogProvider logProvider;
        private readonly KBInfoHelper kbInfoHelper;
        private readonly QnAMakerService qnaMakerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchController"/> class.
        /// </summary>
        /// <param name="jwtHelper">JWT Helper.</param>
        /// <param name="kbInfoHelper">KB helper to use</param>
        /// <param name="qnaMakerService">QnA Maker service to use</param>
        /// <param name="logProvider">Log provider to use</param>
        public SearchController(JwtHelper jwtHelper, KBInfoHelper kbInfoHelper, QnAMakerService qnaMakerService,  ILogProvider logProvider)
        {
            this.jwtHelper = jwtHelper;
            this.tenantId = ConfigurationManager.AppSettings["TenantId"];
            this.kbInfoHelper = kbInfoHelper;
            this.qnaMakerService = qnaMakerService;
            this.logProvider = logProvider;
            this.topResultsToBeFetched = Convert.ToInt32(ConfigurationManager.AppSettings["TopResultCount"]);
            this.minimumConfidenceScore = Convert.ToInt32(ConfigurationManager.AppSettings["MinimumConfidenceScore"]);
        }

        /// <summary>
        /// Search View
        /// </summary>
        /// <param name="token">jwt auth token.</param>
        /// <param name="sessionId">search session id.</param>
        /// <returns><see cref="ActionResult"/> representing Search view.</returns>
        [JwtExceptionFilter]
        public async Task<ActionResult> Index(string token, string sessionId)
        {
            this.jwtHelper.ValidateToken(token, this.tenantId);

            var kbList = await this.kbInfoHelper.GetAllKBs(
                new string[]
                {
                    nameof(KBInfo.KBName),
                    nameof(KBInfo.KBId),
                    nameof(KBInfo.QuestionField),
                    nameof(KBInfo.AnswerFields),
                });

            return this.View(kbList);
        }

        /// <summary>
        /// Search Result View
        /// </summary>
        /// <param name="searchedKeyword">Keyword searched by the user.</param>
        /// <param name="kbId">kb Id.</param>
        /// <returns>Task that resolves to <see cref="PartialViewResult"/> representing Search Results partial view.</returns>
        [JwtExceptionFilter(IsPartialView = true)]
        public async Task<PartialViewResult> SearchResults(string searchedKeyword, string kbId)
        {
            this.ValidateAuthorizationHeader();

            List<SelectedSearchResult> selectedSearchResults = new List<SelectedSearchResult>();

            if (!string.IsNullOrWhiteSpace(searchedKeyword))
            {
                searchedKeyword = searchedKeyword.Trim();

                var kbInfo = await this.kbInfoHelper.GetKBInfo(kbId);
                this.Session["SharePointUrl"] = kbInfo.SharePointUrl;

                var generateAnswerRequest = new GenerateAnswerRequest
                {
                    Question = searchedKeyword,
                    Top = this.topResultsToBeFetched,
                    ScoreThreshold = this.minimumConfidenceScore,
                    RankerType = kbInfo.RankerType ?? RankerTypes.QuestionOnly,     // Default to QuestionOnly for older KBs
                };
                var generateAnswerResponse = await this.qnaMakerService.GenerateAnswerAsync(kbId, generateAnswerRequest);

                var results = generateAnswerResponse?.Answers?.Where(a => a.Score > 0);
                if (results != null)
                {
                    foreach (QnAAnswer item in results)
                    {
                        List<ColumnInfo> answerFields = JsonConvert.DeserializeObject<List<ColumnInfo>>(kbInfo.AnswerFields);
                        JObject answerObj = JsonConvert.DeserializeObject<JObject>(item.Answer);
                        List<DeserializedAnswer> answers = this.DeserializeAnswers(answerObj, answerFields);

                        selectedSearchResults.Add(new SelectedSearchResult()
                        {
                            KBId = kbId,
                            Question = item.Questions[0],
                            Answers = answers,
                            ListItemId = answerObj["id"].ToString(),
                        });
                    }
                }
            }

            return this.PartialView(selectedSearchResults);
        }

        /// <summary>
        /// Result Card Partial View
        /// </summary>
        /// <param name="kbId">kd id</param>
        /// <param name="answer">answer string</param>
        /// <param name="question">question string</param>
        /// <param name="id">id of selected item</param>
        /// <returns><see cref="PartialViewResult"/> representing Result card partial view.</returns>
        [JwtExceptionFilter(IsPartialView = true)]
        public PartialViewResult ResultCardPartial(string kbId, string answer, string question, string id)
        {
            List<DeserializedAnswer> answers = JsonConvert.DeserializeObject<List<DeserializedAnswer>>(answer);

            SelectedSearchResult selectedSearchResult = new SelectedSearchResult()
            {
                KBId = kbId,
                Question = question,
                Answers = answers,
                ListItemId = id,
                SharePointListUrl = this.Session["SharePointUrl"].ToString(),
            };

            return this.PartialView(selectedSearchResult);
        }

        // Validate the incoming JWT
        private string ValidateAuthorizationHeader()
        {
            var token = string.Empty;
            var authHeader = this.Request.Headers["Authorization"];
            if (authHeader?.StartsWith("bearer") ?? false)
            {
                token = authHeader.Split(' ')[1];
            }

            this.jwtHelper.ValidateToken(token, this.tenantId);

            return token;
        }

        /// <summary>
        /// Deserializes Answers to <see cref="OrderedDictionary"/>
        /// </summary>
        /// <param name="qnaAnswer">qna Answer</param>
        /// <param name="answerFields">answer fields</param>
        /// <returns><see cref="System.Collections.Generic.List{String}"/> objects representing questions and answers.</returns>
        private List<DeserializedAnswer> DeserializeAnswers(JObject qnaAnswer, List<ColumnInfo> answerFields)
        {
            List<DeserializedAnswer> deserializedAnswers = new List<DeserializedAnswer>();

            foreach (var field in answerFields)
            {
                deserializedAnswers.Add(new DeserializedAnswer()
                {
                    Question = XmlConvert.DecodeName(field.DisplayName),
                    Answer = Convert.ToString(qnaAnswer[field.Name]),
                });
            }

            return deserializedAnswers;
        }
    }
}