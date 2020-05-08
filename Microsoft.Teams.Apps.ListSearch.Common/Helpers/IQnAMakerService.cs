﻿// <copyright file="IQnAMakerService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.ListSearch.Common.Helpers
{
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.ListSearch.Common.Models;

    /// <summary>
    /// Interface of QnA Maker Service
    /// </summary>
    public interface IQnAMakerService
    {
        /// <summary>
        /// Gets Answer from QnA Maker API.
        /// </summary>
        /// <param name="kbId">knowledge base id</param>
        /// <param name="request"><see cref="GenerateAnswerRequest"/> request for GenerateAnswer API.</param>
        /// <returns>Task that resolves to <see cref="GenerateAnswerResponse"/> for the searched question.</returns>
        Task<GenerateAnswerResponse> GenerateAnswerAsync(string kbId, GenerateAnswerRequest request);

        /// <summary>
        /// Updates KB using QnA Maker API.
        /// </summary>
        /// <param name="kbId">knowledge base id</param>
        /// <param name="body"><see cref="UpdateKBRequest"/> request to be sent to QnA Maker API.</param>
        /// <returns>Task that resolves to <see cref="QnAMakerResponse"/>.</returns>
        Task<QnAMakerResponse> UpdateKB(string kbId, UpdateKBRequest body);

        /// <summary>
        /// Publishes KB.
        /// </summary>
        /// <param name="kbId">knowledge base id</param>
        /// <returns>Task that resolves to <see cref="bool"/> which represents success or failure of API call.</returns>
        Task<bool> PublishKB(string kbId);

        /// <summary>
        /// Creates KB using QnA Maker API.
        /// </summary>
        /// <param name="body"><see cref="CreateKBRequest"/> request to be sent to QnA Maker API.</param>
        /// <returns>Task that resolves to <see cref="QnAMakerResponse"/>.</returns>
        Task<QnAMakerResponse> CreateKB(CreateKBRequest body);

        /// <summary>
        /// Deletes a KB.
        /// </summary>
        /// <param name="kbId">knowledge base id</param>
        /// <returns>Task that resolves to <see cref="bool"/> which represents success or failure of API call.</returns>
        Task<bool> DeleteKB(string kbId);

        /// <summary>
        /// Gets operation status of QnA Maker Operation.
        /// </summary>
        /// <param name="operationId">Id of operation to retrieve status.</param>
        /// <returns>Task that resolves to <see cref="QnAMakerResponse"/>.</returns>
        Task<QnAMakerResponse> GetOperationDetails(string operationId);

        /// <summary>
        /// Gets Knowledge base details.
        /// </summary>
        /// <param name="kbId">knowledge base id</param>
        /// <returns>Task that resolves to <see cref="GetKnowledgeBaseDetailsResponse"/>.</returns>
        Task<GetKnowledgeBaseDetailsResponse> GetKnowledgeBaseDetails(string kbId);

        /// <summary>
        /// Wait for the QnAMaker operation to complete.
        /// </summary>
        /// <param name="response"><see cref="QnAMakerResponse"/> response to be awaited.</param>
        /// <returns>Operation state after completion.</returns>
        Task<string> AwaitOperationCompletionState(QnAMakerResponse response);

        /// <summary>
        /// Wait for the QnAMaker operation to complete.
        /// </summary>
        /// <param name="response"> response to be awaited.</param>
        /// <returns>Completed QnAMaker response.</returns>
        Task<QnAMakerResponse> AwaitOperationCompletionResponse(QnAMakerResponse response);

        /// <summary>
        /// Check if operation is successful.
        /// </summary>
        /// <param name="operationState">state of operation to be checked.</param>
        /// <returns><see cref="bool"/> that represents if operation is complete.</returns>
        bool IsOperationSuccessful(string operationState);
    }
}