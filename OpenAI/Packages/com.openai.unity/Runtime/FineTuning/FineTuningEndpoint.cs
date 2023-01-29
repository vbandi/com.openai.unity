﻿// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.FineTuning
{
    /// <summary>
    /// Manage fine-tuning jobs to tailor a model to your specific training data.
    /// <see href="https://beta.openai.com/docs/guides/fine-tuning"/>
    /// </summary>
    public class FineTuningEndpoint : BaseEndPoint
    {
        private class FineTuneList
        {
            [JsonProperty("object")]
            public string Object { get; set; }

            [JsonProperty("data")]
            public List<FineTuneJob> Data { get; set; }
        }

        private class FineTuneEventList
        {
            [JsonProperty("data")]
            public List<Event> Data { get; set; }
        }

        /// <inheritdoc />
        public FineTuningEndpoint(OpenAIClient api) : base(api) { }

        /// <inheritdoc />
        protected override string GetEndpoint()
            => $"{Api.BaseUrl}fine-tunes";

        /// <summary>
        /// Creates a job that fine-tunes a specified model from a given dataset.
        /// Response includes details of the enqueued job including job status and
        /// the name of the fine-tuned models once complete.
        /// </summary>
        /// <param name="jobRequest"><see cref="CreateFineTuneJobRequest"/>.</param>
        /// <returns><see cref="FineTuneJob"/>.</returns>
        /// <exception cref="HttpRequestException">.</exception>
        public async Task<FineTuneJobResponse> CreateFineTuneJobAsync(CreateFineTuneJobRequest jobRequest)
        {
            var jsonContent = JsonConvert.SerializeObject(jobRequest, Api.JsonSerializationOptions);
            var response = await Api.Client.PostAsync(GetEndpoint(), jsonContent.ToJsonStringContent());
            var responseAsString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{nameof(CreateFineTuneJobAsync)} Failed! HTTP status code: {response.StatusCode}. Request body: {responseAsString}");
            }

            var result = JsonConvert.DeserializeObject<FineTuneJobResponse>(responseAsString, Api.JsonSerializationOptions);
            result.SetResponseData(response.Headers);
            return result;
        }

        /// <summary>
        /// List your organization's fine-tuning jobs.
        /// </summary>
        /// <returns>List of <see cref="FineTuneJob"/>s.</returns>
        /// <exception cref="HttpRequestException">.</exception>
        public async Task<IReadOnlyList<FineTuneJob>> ListFineTuneJobsAsync()
        {
            var response = await Api.Client.GetAsync(GetEndpoint());
            var responseAsString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{nameof(ListFineTuneJobsAsync)} Failed! HTTP status code: {response.StatusCode}. Request body: {responseAsString}");
            }

            return JsonConvert.DeserializeObject<FineTuneList>(responseAsString, Api.JsonSerializationOptions)?.Data.OrderBy(job => job.CreatedAtUnixTime).ToArray();
        }

        /// <summary>
        /// Gets info about the fine-tune job.
        /// </summary>
        /// <param name="fineTuneJob"><see cref="FineTuneJob"/>.</param>
        /// <returns><see cref="FineTuneJobResponse"/>.</returns>
        /// <exception cref="HttpRequestException">.</exception>
        public async Task<FineTuneJobResponse> RetrieveFineTuneJobInfoAsync(FineTuneJob fineTuneJob)
            => await RetrieveFineTuneJobInfoAsync(fineTuneJob.Id);

        /// <summary>
        /// Gets info about the fine-tune job.
        /// </summary>
        /// <param name="jobId"><see cref="FineTuneJob.Id"/>.</param>
        /// <returns><see cref="FineTuneJobResponse"/>.</returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task<FineTuneJobResponse> RetrieveFineTuneJobInfoAsync(string jobId)
        {
            var response = await Api.Client.GetAsync($"{GetEndpoint()}/{jobId}");
            var responseAsString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{nameof(RetrieveFineTuneJobInfoAsync)} Failed! HTTP status code: {response.StatusCode}. Request body: {responseAsString}");
            }

            var result = JsonConvert.DeserializeObject<FineTuneJobResponse>(responseAsString, Api.JsonSerializationOptions);
            result.SetResponseData(response.Headers);
            return result;
        }

        /// <summary>
        /// Immediately cancel a fine-tune job.
        /// </summary>
        /// <param name="job"><see cref="FineTuneJob"/> to cancel.</param>
        /// <returns><see cref="FineTuneJobResponse"/>.</returns>
        /// <exception cref="HttpRequestException">.</exception>
        public async Task<bool> CancelFineTuneJobAsync(FineTuneJob job)
            => await CancelFineTuneJobAsync(job.Id);

        /// <summary>
        /// Immediately cancel a fine-tune job.
        /// </summary>
        /// <param name="jobId"><see cref="FineTuneJob.Id"/> to cancel.</param>
        /// <returns><see cref="FineTuneJobResponse"/>.</returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task<bool> CancelFineTuneJobAsync(string jobId)
        {
            var response = await Api.Client.PostAsync($"{GetEndpoint()}/{jobId}/cancel", null);
            var responseAsString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{nameof(CancelFineTuneJobAsync)} Failed! HTTP status code: {response.StatusCode}. Request body: {responseAsString}");
            }

            var result = JsonConvert.DeserializeObject<FineTuneJobResponse>(responseAsString, Api.JsonSerializationOptions);
            result.SetResponseData(response.Headers);
            return result.Status == "cancelled";
        }

        /// <summary>
        /// Get fine-grained status updates for a fine-tune job.
        /// </summary>
        /// <param name="job"><see cref="FineTuneJob"/>.</param>
        /// <returns>List of events for <see cref="FineTuneJob"/>.</returns>
        public async Task<IReadOnlyList<Event>> ListFineTuneEventsAsync(FineTuneJob job)
            => await ListFineTuneEventsAsync(job.Id);

        /// <summary>
        /// Get fine-grained status updates for a fine-tune job.
        /// </summary>
        /// <param name="jobId"><see cref="FineTuneJob.Id"/>.</param>
        /// <returns>List of events for <see cref="FineTuneJob"/>.</returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task<IReadOnlyList<Event>> ListFineTuneEventsAsync(string jobId)
        {
            var response = await Api.Client.GetAsync($"{GetEndpoint()}/{jobId}/events");
            var responseAsString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{nameof(ListFineTuneEventsAsync)} Failed! HTTP status code: {response.StatusCode}. Request body: {responseAsString}");
            }

            return JsonConvert.DeserializeObject<FineTuneEventList>(responseAsString, Api.JsonSerializationOptions)?.Data.OrderBy(@event => @event.CreatedAtUnixTime).ToArray();
        }

        /// <summary>
        /// Stream the fine-grained status updates for a fine-tune job.
        /// </summary>
        /// <param name="job"><see cref="FineTuneJob"/>.</param>
        /// <param name="fineTuneEventCallback">The event callback handler.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <exception cref="HttpRequestException"></exception>
        public async Task StreamFineTuneEventsAsync(FineTuneJob job, Action<Event> fineTuneEventCallback, CancellationToken cancellationToken = default)
            => await StreamFineTuneEventsAsync(job.Id, fineTuneEventCallback, cancellationToken);

        /// <summary>
        /// Stream the fine-grained status updates for a fine-tune job.
        /// </summary>
        /// <param name="jobId"><see cref="FineTuneJob.Id"/>.</param>
        /// <param name="fineTuneEventCallback">The event callback handler.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <exception cref="HttpRequestException"></exception>
        public async Task StreamFineTuneEventsAsync(string jobId, Action<Event> fineTuneEventCallback, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{GetEndpoint()}/{jobId}/events?stream=true");
            var response = await Api.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (await reader.ReadLineAsync() is { } line &&
                       !cancellationToken.IsCancellationRequested)
                {
                    if (line.StartsWith("data: "))
                    {
                        line = line["data: ".Length..];
                    }

                    if (line == "[DONE]")
                    {
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        fineTuneEventCallback(JsonConvert.DeserializeObject<Event>(line.Trim(), Api.JsonSerializationOptions));
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    var result = await CancelFineTuneJobAsync(jobId);

                    if (!result)
                    {
                        throw new Exception($"Failed to cancel {jobId}");
                    }
                }
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"{nameof(StreamFineTuneEventsAsync)} Failed! HTTP status code: {response.StatusCode}. Request body: {responseBody}");
            }
        }

        /// <summary>
        /// Stream the fine-grained status updates for a fine-tune job.
        /// </summary>
        /// <param name="jobId"><see cref="FineTuneJob.Id"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <exception cref="HttpRequestException"></exception>
        public async IAsyncEnumerable<Event> StreamFineTuneEventsEnumerableAsync(string jobId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{GetEndpoint()}/{jobId}/events?stream=true");
            var response = await Api.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (await reader.ReadLineAsync() is { } line &&
                       !cancellationToken.IsCancellationRequested)
                {
                    if (line.StartsWith("data: "))
                    {
                        line = line["data: ".Length..];
                    }

                    if (line == "[DONE]")
                    {
                        yield break;
                    }

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        yield return JsonConvert.DeserializeObject<Event>(line.Trim(), Api.JsonSerializationOptions);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    var result = await CancelFineTuneJobAsync(jobId);

                    if (!result)
                    {
                        throw new Exception($"Failed to cancel {jobId}");
                    }
                }
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"{nameof(StreamFineTuneEventsEnumerableAsync)} Failed! HTTP status code: {response.StatusCode}. Request body: {responseBody}");
            }
        }
    }
}