﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using InstaSharper.Classes;
using InstaSharper.Classes.Android.DeviceInfo;
using InstaSharper.Classes.Models;
using InstaSharper.Classes.ResponseWrappers;
using InstaSharper.Converters;
using InstaSharper.Converters.Json;
using InstaSharper.Helpers;
using InstaSharper.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstaSharper.API.Processors
{
    public class StoryProcessor : IStoryProcessor
    {
        private readonly AndroidDevice _deviceInfo;
        private readonly IHttpRequestProcessor _httpRequestProcessor;
        private readonly IInstaLogger _logger;
        private readonly UserSessionData _user;

        public StoryProcessor(AndroidDevice deviceInfo, UserSessionData user,
            IHttpRequestProcessor httpRequestProcessor, IInstaLogger logger)
        {
            _deviceInfo = deviceInfo;
            _user = user;
            _httpRequestProcessor = httpRequestProcessor;
            _logger = logger;
        }

        public async Task<IResult<InstaStoryFeed>> GetStoryFeedAsync()
        {
            try
            {
                var storyFeedUri = UriCreator.GetStoryFeedUri();
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, storyFeedUri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK) return Result.Fail("", (InstaStoryFeed) null);
                var storyFeedResponse = JsonConvert.DeserializeObject<InstaStoryFeedResponse>(json);
                var instaStoryFeed = ConvertersFabric.Instance.GetStoryFeedConverter(storyFeedResponse).Convert();
                return Result.Success(instaStoryFeed);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaStoryFeed>(exception.Message);
            }
        }

        public async Task<IResult<InstaStory>> GetUserStoryAsync(long userId)
        {
            try
            {
                var userStoryUri = UriCreator.GetUserStoryUri(userId);
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, userStoryUri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();

                if (response.StatusCode != HttpStatusCode.OK) Result.UnExpectedResponse<InstaStory>(response, json);
                var userStoryResponse = JsonConvert.DeserializeObject<InstaStoryResponse>(json);
                var userStory = ConvertersFabric.Instance.GetStoryConverter(userStoryResponse).Convert();
                return Result.Success(userStory);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaStory>(exception.Message);
            }
        }

        public async Task<IResult<InstaStoryMedia>> UploadStoryPhotoAsync(InstaImage image, string caption)
        {
            try
            {
                var instaUri = UriCreator.GetUploadPhotoUri();
                var uploadId = ApiRequestMessage.GenerateUploadId();
                var requestContent = new MultipartFormDataContent(uploadId)
                {
                    {new StringContent(uploadId), "\"upload_id\""},
                    {new StringContent(_deviceInfo.DeviceGuid.ToString()), "\"_uuid\""},
                    {new StringContent(_user.CsrfToken), "\"_csrftoken\""},
                    {
                        new StringContent("{\"lib_name\":\"jt\",\"lib_version\":\"1.3.0\",\"quality\":\"87\"}"),
                        "\"image_compression\""
                    }
                };
                var imageContent = new ByteArrayContent(File.ReadAllBytes(image.URI));
                imageContent.Headers.Add("Content-Transfer-Encoding", "binary");
                imageContent.Headers.Add("Content-Type", "application/octet-stream");
                requestContent.Add(imageContent, "photo", $"pending_media_{ApiRequestMessage.GenerateUploadId()}.jpg");
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, instaUri, _deviceInfo);
                request.Content = requestContent;
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.IsSuccessStatusCode)
                    return await ConfigureStoryPhotoAsync(image, uploadId, caption);
                return Result.UnExpectedResponse<InstaStoryMedia>(response, json);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaStoryMedia>(exception.Message);
            }
        }

        public async Task<IResult<InstaStoryMedia>> ConfigureStoryPhotoAsync(InstaImage image, string uploadId,
            string caption)
        {
            try
            {
                var instaUri = UriCreator.GetStoryConfigureUri();
                var data = new JObject
                {
                    {"_uuid", _deviceInfo.DeviceGuid.ToString()},
                    {"_uid", _user.LoggedInUder.Pk},
                    {"_csrftoken", _user.CsrfToken},
                    {"source_type", "1"},
                    {"caption", caption},
                    {"upload_id", uploadId},
                    {"edits", new JObject()},
                    {"disable_comments", false},
                    {"configure_mode", 1},
                    {"camera_position", "unknown"}
                };
                var request = HttpHelper.GetSignedRequest(HttpMethod.Post, instaUri, _deviceInfo, data);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.IsSuccessStatusCode)
                {
                    var mediaResponse = JsonConvert.DeserializeObject<InstaStoryMediaResponse>(json);
                    var converter = ConvertersFabric.Instance.GetStoryMediaConverter(mediaResponse);
                    return Result.Success(converter.Convert());
                }

                return Result.UnExpectedResponse<InstaStoryMedia>(response, json);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaStoryMedia>(exception.Message);
            }
        }

        public async Task<IResult<InstaReelFeed>> GetUserStoryFeedAsync(long userId)
        {
            var feed = new InstaReelFeed();
            try
            {
                var userFeedUri = UriCreator.GetUserReelFeedUri(userId);
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, userFeedUri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaReelFeed>(response, json);
                var feedResponse = JsonConvert.DeserializeObject<InstaReelFeedResponse>(json);
                feed = ConvertersFabric.Instance.GetReelFeedConverter(feedResponse).Convert();
                return Result.Success(feed);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception.Message, feed);
            }
        }

        /// <summary>
        /// Get the global story feed which contains everyone you follow.
        /// Note that users will eventually drop out of this list even though they
        /// still have stories.So it's always safer to call getUserStoryFeed() if
        /// a specific user's story feed matters to you.
        /// </summary>
        /// <returns></returns>
        public async Task<IResult<InstaFeed>> GetReelsTrayFeed()
        {
            var feed = new InstaFeed();
            try
            {
                var fields = new Dictionary<string, string>
                {
                    {"is_prefetch", "0"},
                    {
                        "supported_capabilities_new",
                        JsonConvert.SerializeObject(InstaApiConstants.SUPPORTED_CAPABILITIES)
                    },
                    {"_csrftoken", _user.CsrfToken},
                    {"_uuid", _httpRequestProcessor.RequestMessage.uuid},
                };

                var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, new Uri("https://i.instagram.com/api/v1/feed/reels_tray/"), _deviceInfo, fields);
                
                var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
                var json = await response.Content.ReadAsStringUnZipAsync();

                //if (response.StatusCode != HttpStatusCode.OK)
                //    return Result.UnExpectedResponse<InstaFeed>(response, json);

                //var feedResponse = JsonConvert.DeserializeObject<InstaFeedResponse>(json, new InstaFeedResponseDataConverter());
                //feed = ConvertersFabric.Instance.GetFeedConverter(feedResponse).Convert();

                return Result.Success(feed);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception, feed);
            }
        }
    }
}