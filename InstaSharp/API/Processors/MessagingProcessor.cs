﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using InstaSharper.Classes;
using InstaSharper.Classes.Android.DeviceInfo;
using InstaSharper.Classes.Models;
using InstaSharper.Classes.ResponseWrappers;
using InstaSharper.Classes.ResponseWrappers.BaseResponse;
using InstaSharper.Converters;
using InstaSharper.Converters.Json;
using InstaSharper.Helpers;
using InstaSharper.Logger;
using Newtonsoft.Json;

namespace InstaSharper.API.Processors
{
    public class MessagingProcessor : IMessagingProcessor
    {
        private readonly AndroidDevice _deviceInfo;
        private readonly IHttpRequestProcessor _httpRequestProcessor;
        private readonly IInstaLogger _logger;
        private readonly UserSessionData _user;

        public MessagingProcessor(AndroidDevice deviceInfo, UserSessionData user,
            IHttpRequestProcessor httpRequestProcessor,
            IInstaLogger logger)
        {
            _deviceInfo = deviceInfo;
            _user = user;
            _httpRequestProcessor = httpRequestProcessor;
            _logger = logger;
        }

        public async Task<IResult<InstaDirectInboxContainer>> GetDirectInboxAsync()
        {
            try
            {
                var directInboxUri = UriCreator.GetDirectInboxUri();
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, directInboxUri, _deviceInfo);

                var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);

                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaDirectInboxContainer>(response, json);

                var inboxResponse = JsonConvert.DeserializeObject<InstaDirectInboxContainerResponse>(json);
                var converter = ConvertersFabric.Instance.GetDirectInboxConverter(inboxResponse);

                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxContainer>(exception.Message);
            }
        }

        public async Task<IResult<InstaDirectInboxContainer>> GetPendingDirectInboxAsync()
        {
            try
            {
                var directInboxUri = UriCreator.GetPendingDirectInboxUri();
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, directInboxUri, _deviceInfo);

                var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);

                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaDirectInboxContainer>(response, json);

                var inboxResponse = JsonConvert.DeserializeObject<InstaDirectInboxContainerResponse>(json);
                var converter = ConvertersFabric.Instance.GetDirectInboxConverter(inboxResponse);

                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxContainer>(exception.Message);
            }
        }

        public async Task<IResult<InstaDirectInboxThread>> GetDirectInboxThreadAsync(string threadId)
        {
            try
            {
                var directInboxUri = UriCreator.GetDirectInboxThreadUri(threadId);
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, directInboxUri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaDirectInboxThread>(response, json);
                var threadResponse = JsonConvert.DeserializeObject<InstaDirectInboxThreadResponse>(json,
                    new InstaThreadDataConverter());
                var converter = ConvertersFabric.Instance.GetDirectThreadConverter(threadResponse);
                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxThread>(exception.Message);
            }
        }

        public async Task<IResult<InstaDirectInboxThreadList>> SendDirectMessage(string recipients, string threadIds,
            string text)
        {
            var threads = new InstaDirectInboxThreadList();
            try
            {
                var directSendMessageUri = UriCreator.GetDirectSendMessageUri();
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, directSendMessageUri, _deviceInfo);
                var fields = new Dictionary<string, string> {{"text", text}};
                if (!string.IsNullOrEmpty(recipients))
                    fields.Add("recipient_users", "[[" + recipients + "]]");
                else
                    return Result.Fail<InstaDirectInboxThreadList>("Please provide at least one recipient.");
                if (!string.IsNullOrEmpty(threadIds))
                    fields.Add("thread_ids", "[" + threadIds + "]");

                request.Content = new FormUrlEncodedContent(fields);
                var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaDirectInboxThreadList>(response, json);
                var result = JsonConvert.DeserializeObject<InstaSendDirectMessageResponse>(json);
                if (!result.IsOk()) return Result.Fail<InstaDirectInboxThreadList>(result.Status);
                threads.AddRange(result.Threads.Select(thread =>
                    ConvertersFabric.Instance.GetDirectThreadConverter(thread).Convert()));
                return Result.Success(threads);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxThreadList>(exception);
            }
        }

        public async Task<IResult<InstaRecipients>> GetRecentRecipientsAsync()
        {
            try
            {
                var userUri = UriCreator.GetRecentRecipientsUri();
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, userUri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaRecipients>(response, json);
                var responseRecipients = JsonConvert.DeserializeObject<InstaRecentRecipientsResponse>(json);
                var converter = ConvertersFabric.Instance.GetRecipientsConverter(responseRecipients);
                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaRecipients>(exception.Message);
            }
        }

        public async Task<IResult<InstaRecipients>> GetRankedRecipientsAsync()
        {
            try
            {
                var userUri = UriCreator.GetRankedRecipientsUri();
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, userUri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaRecipients>(response, json);
                var responseRecipients = JsonConvert.DeserializeObject<InstaRankedRecipientsResponse>(json);
                var converter = ConvertersFabric.Instance.GetRecipientsConverter(responseRecipients);
                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaRecipients>(exception.Message);
            }
        }

        public async Task<IResult<BaseStatusResponse>> DeclineAllPendingDirectThreads()
        {
            try
            {
                var uri = UriCreator.GetDeclineAllPendingThreadsUri();
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, uri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<BaseStatusResponse>(response, json);

                var result = JsonConvert.DeserializeObject<BaseStatusResponse>(json);

                return !result.IsOk()
                    ? Result.Fail<BaseStatusResponse>(result.Status)
                    : Result.Success(result);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<BaseStatusResponse>(exception);
            }
        }

        public async Task<IResult<BaseStatusResponse>> ApprovePendingDirectThread(string threadId)
        {
            try
            {
                var uri = UriCreator.GetApproveThreadUri(threadId);
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, uri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<BaseStatusResponse>(response, json);

                var result = JsonConvert.DeserializeObject<BaseStatusResponse>(json);

                return !result.IsOk()
                    ? Result.Fail<BaseStatusResponse>(result.Status)
                    : Result.Success(result);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<BaseStatusResponse>(exception);
            }
        }

        public async Task<IResult<BaseStatusResponse>> ApprovePendingDirectThreads(List<string> threadIds)
        {
            try
            {
                var uri = UriCreator.GetApproveThreadsUri();
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, uri, _deviceInfo);

                var fields = new Dictionary<string, string>()
                {
                    {"thread_ids", JsonConvert.SerializeObject(threadIds)}
                };
                request.Content = new FormUrlEncodedContent(fields);

                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<BaseStatusResponse>(response, json);

                var result = JsonConvert.DeserializeObject<BaseStatusResponse>(json);

                return !result.IsOk()
                    ? Result.Fail<BaseStatusResponse>(result.Status)
                    : Result.Success(result);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<BaseStatusResponse>(exception);
            }
        }
    }
}