using System;
using System.Collections.Generic;
using System.Linq;
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
using InstaRecentActivityConverter = InstaSharper.Converters.Json.InstaRecentActivityConverter;

namespace InstaSharper.API.Processors
{
    public class FeedProcessor : IFeedProcessor
    {
        private readonly AndroidDevice _deviceInfo;
        private readonly IHttpRequestProcessor _httpRequestProcessor;
        private readonly IInstaLogger _logger;
        private readonly UserSessionData _user;

        public FeedProcessor(AndroidDevice deviceInfo, UserSessionData user, IHttpRequestProcessor httpRequestProcessor,
            IInstaLogger logger)
        {
            _deviceInfo = deviceInfo;
            _user = user;
            _httpRequestProcessor = httpRequestProcessor;
            _logger = logger;
        }

        public async Task<IResult<InstaTagFeed>> GetTagFeedAsync(string tag, PaginationParameters paginationParameters)
        {
            var tagFeed = new InstaTagFeed();
            try
            {
                var userFeedUri = UriCreator.GetTagFeedUri(tag, paginationParameters.NextId);
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, userFeedUri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaTagFeed>(response, json);
                var feedResponse = JsonConvert.DeserializeObject<InstaTagFeedResponse>(json,
                    new InstaTagFeedDataConverter());
                tagFeed = ConvertersFabric.Instance.GetTagFeedConverter(feedResponse).Convert();

                paginationParameters.NextId = feedResponse.NextMaxId;
                paginationParameters.PagesLoaded++;

                while (feedResponse.MoreAvailable
                       && !string.IsNullOrEmpty(paginationParameters.NextId)
                       && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextFeed = await GetTagFeedAsync(tag, paginationParameters);
                    if (!nextFeed.Succeeded)
                        return Result.Fail(nextFeed.Info, tagFeed);
                    tagFeed.NextId = paginationParameters.NextId = nextFeed.Value.NextId;
                    tagFeed.Medias.AddRange(nextFeed.Value.Medias);
                    tagFeed.Stories.AddRange(nextFeed.Value.Stories);
                }

                return Result.Success(tagFeed);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception, tagFeed);
            }
        }

        public async Task<IResult<InstaFeed>> GetUserTimelineFeedAsync(PaginationParameters paginationParameters)
        {
            var feed = new InstaFeed();
            try
            {
                var userFeedUri = UriCreator.GetUserFeedUri(paginationParameters.NextId);
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, userFeedUri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaFeed>(response, json);

                var feedResponse = JsonConvert.DeserializeObject<InstaFeedResponse>(json,
                    new InstaFeedResponseDataConverter());
                feed = ConvertersFabric.Instance.GetFeedConverter(feedResponse).Convert();
                paginationParameters.NextId = feed.NextId;
                paginationParameters.PagesLoaded++;

                while (feedResponse.MoreAvailable
                       && !string.IsNullOrEmpty(paginationParameters.NextId)
                       && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextFeed = await GetUserTimelineFeedAsync(paginationParameters);
                    if (!nextFeed.Succeeded)
                        return Result.Fail(nextFeed.Info, feed);

                    feed.Medias.AddRange(nextFeed.Value.Medias);
                    feed.Stories.AddRange(nextFeed.Value.Stories);

                    paginationParameters.NextId = nextFeed.Value.NextId;
                    paginationParameters.PagesLoaded++;
                }

                return Result.Success(feed);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception, feed);
            }
        }

        /// <summary>
        /// Get your "home screen" timeline feed.
        /// 
        /// An associative array with following keys (all of them are optional):
        /// "latest_story_pk" The media ID in Instagram's internal format (ie "3482384834_43294");
        /// 
        /// "seen_posts" One or more seen media IDs;
        /// 
        /// "unseen_posts" One or more unseen media IDs;
        /// 
        /// "is_pull_to_refresh" Whether this call was triggered by a refresh;
        /// 
        /// "push_disabled" Whether user has disabled PUSH;
        /// 
        /// "recovered_from_crash" Whether the app has recovered from a crash/was killed by Android
        /// memory manager/force closed by user/just installed for the first time;
        /// 
        /// "feed_view_info" DON'T USE IT YET.
        /// </summary>
        /// <returns></returns>
        public async Task<IResult<InstaFeed>> GetTimelineFeedAsync(Dictionary<string, string>  options = null)
        {
            var feed = new InstaFeed();
            try
            {
                
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, new Uri("https://i.instagram.com/api/v1/feed/timeline/"), _deviceInfo);

                request.Headers.Add("X-Ads-Opt-Out", "0");
                request.Headers.Add("X-Google-AD-ID", _httpRequestProcessor.RequestMessage.advertising_id);
                request.Headers.Add("X-DEVICE-ID", _httpRequestProcessor.RequestMessage.uuid);

                var fields = new Dictionary<string, string>
                {
                    {"_csrftoken", _user.CsrfToken},
                    {"_uuid", _httpRequestProcessor.RequestMessage.uuid},
                    {"is_prefetch", "0" },
                    {"phone_id", _httpRequestProcessor.RequestMessage.phone_id },
                    {"device_id", _httpRequestProcessor.RequestMessage.device_id },
                    {"client_session_id", _httpRequestProcessor.RequestMessage.session_id },
                    {"battery_level", "100" },
                    {"is_charging", "1" },
                    {"will_sound_on", "1" },
                    {"is_on_screen", "true" },
                    {"timezone_offset", InstaApiConstants.TIMEZONE_OFFSET.ToString() }, // date('Z')
                    {"is_async_ads", "0" },
                    {"is_async_ads_double_request", "0" },
                    {"is_async_ads_rti", "0" },
                    {"rti_delivery_backend", "" },

                };

                if (options != null && options.ContainsKey("latest_story_pk"))
                {
                    fields.Add("latest_story_pk", options["latest_story_pk"]);
                }

                /*
                 if ($maxId !== null) {
                    $request->addPost('reason', 'pagination');
                    $request->addPost('max_id', $maxId);
                    $request->addPost('is_pull_to_refresh', '0');

                } elseif (!empty($options['is_pull_to_refresh'])) {
                    $request->addPost('reason', 'pull_to_refresh');
                    $request->addPost('is_pull_to_refresh', '1');

                } elseif (isset($options['is_pull_to_refresh'])) {
                    $request->addPost('reason', 'warm_start_fetch');
                    $request->addPost('is_pull_to_refresh', '0');

                } else {
                    $request->addPost('reason', 'cold_start_fetch');
                    $request->addPost('is_pull_to_refresh', '0');
                }
                 */
                
                //if (options != null && options.ContainsKey("is_pull_to_refresh"))
                //{
                //    if (options["is_pull_to_refresh"] != null)
                //    {
                //        fields.Add("reason", "pull_to_refresh");
                //        fields.Add("is_pull_to_refresh", "1");
                //    }
                //    else
                //    {
                //        fields.Add("reason", "warm_start_fetch");
                //        fields.Add("is_pull_to_refresh", "0");
                //    }
                //}
                //else
                //{
                //    fields.Add("reason", "cold_start_fetch");
                //    fields.Add("is_pull_to_refresh", "0");
                //}

                fields.Add("unseen_posts", "");
                fields.Add("feed_view_info", "");

                if (options != null && options.ContainsKey("recovered_from_crash"))
                {
                    fields.Add("recovered_from_crash", "1");
                }

                request.Content = new FormUrlEncodedContent(fields);

                var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
                var json = await response.Content.ReadAsStringUnZipAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaFeed>(response, json);

                var feedResponse = JsonConvert.DeserializeObject<InstaFeedResponse>(json, new InstaFeedResponseDataConverter());
                feed = ConvertersFabric.Instance.GetFeedConverter(feedResponse).Convert();
                
                return Result.Success(feed);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception, feed);
            }
        }

        public async Task<IResult<InstaExploreFeed>> GetExploreFeedAsync(PaginationParameters paginationParameters)
        {
            var exploreFeed = new InstaExploreFeed();
            try
            {
                var exploreUri = UriCreator.GetExploreUri(paginationParameters.NextId);
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, exploreUri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaExploreFeed>(response, json);
                var feedResponse = JsonConvert.DeserializeObject<InstaExploreFeedResponse>(json,
                    new InstaExploreFeedDataConverter());
                exploreFeed = ConvertersFabric.Instance.GetExploreFeedConverter(feedResponse).Convert();
                var nextId = feedResponse.Items.Medias.LastOrDefault(media => !string.IsNullOrEmpty(media.NextMaxId))
                    ?.NextMaxId;
                exploreFeed.Medias.PageSize = feedResponse.ResultsCount;
                paginationParameters.NextId = nextId;
                exploreFeed.NextId = nextId;
                while (feedResponse.MoreAvailable
                       && !string.IsNullOrEmpty(paginationParameters.NextId)
                       && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextFeed = await GetExploreFeedAsync(paginationParameters);
                    if (!nextFeed.Succeeded)
                        return Result.Fail(nextFeed.Info, exploreFeed);
                    nextId = feedResponse.Items.Medias.LastOrDefault(media => !string.IsNullOrEmpty(media.NextMaxId))
                        ?.NextMaxId;
                    exploreFeed.NextId = paginationParameters.NextId = nextId;
                    paginationParameters.PagesLoaded++;
                    exploreFeed.Medias.AddRange(nextFeed.Value.Medias);
                }

                exploreFeed.Medias.Pages = paginationParameters.PagesLoaded;
                return Result.Success(exploreFeed);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception, exploreFeed);
            }
        }

        public async Task<IResult<InstaActivityFeed>> GetFollowingRecentActivityFeedAsync(
            PaginationParameters paginationParameters)
        {
            var uri = UriCreator.GetFollowingRecentActivityUri();
            return await GetRecentActivityInternalAsync(uri, paginationParameters);
        }

        public async Task<IResult<InstaActivityFeed>> GetRecentActivityFeedAsync(
            PaginationParameters paginationParameters)
        {
            var uri = UriCreator.GetRecentActivityUri();
            return await GetRecentActivityInternalAsync(uri, paginationParameters);
        }

        public async Task<IResult<InstaMediaList>> GetLikeFeedAsync(PaginationParameters paginationParameters)
        {
            var instaUri = UriCreator.GetUserLikeFeedUri(paginationParameters.NextId);
            var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, instaUri, _deviceInfo);
            var response = await _httpRequestProcessor.SendAsync(request);
            var json = await response.Content.ReadAsStringUnZipAsync();
            if (response.StatusCode != HttpStatusCode.OK)
                return Result.UnExpectedResponse<InstaMediaList>(response, json);
            var mediaResponse = JsonConvert.DeserializeObject<InstaMediaListResponse>(json,
                new InstaMediaListDataConverter());

            var mediaList = ConvertersFabric.Instance.GetMediaListConverter(mediaResponse).Convert();
            mediaList.NextId = paginationParameters.NextId = mediaResponse.NextMaxId;
            while (mediaResponse.MoreAvailable
                   && !string.IsNullOrEmpty(paginationParameters.NextId)
                   && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
            {
                var result = await GetLikeFeedAsync(paginationParameters);
                if (!result.Succeeded)
                    return Result.Fail(result.Info, mediaList);

                paginationParameters.PagesLoaded++;
                mediaList.NextId = paginationParameters.NextId = result.Value.NextId;
                mediaList.AddRange(result.Value);
            }

            mediaList.PageSize = mediaResponse.ResultsCount;
            mediaList.Pages = paginationParameters.PagesLoaded;
            return Result.Success(mediaList);
        }

        private async Task<IResult<InstaRecentActivityResponse>> GetFollowingActivityWithMaxIdAsync(string maxId)
        {
            var uri = UriCreator.GetFollowingRecentActivityUri(maxId);
            var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, uri, _deviceInfo);
            var response = await _httpRequestProcessor.SendAsync(request);
            var json = await response.Content.ReadAsStringUnZipAsync();
            if (response.StatusCode != HttpStatusCode.OK)
                return Result.UnExpectedResponse<InstaRecentActivityResponse>(response, json);
            var followingActivity = JsonConvert.DeserializeObject<InstaRecentActivityResponse>(json,
                new InstaRecentActivityConverter());
            return Result.Success(followingActivity);
        }

        private async Task<IResult<InstaActivityFeed>> GetRecentActivityInternalAsync(Uri uri,
            PaginationParameters paginationParameters)
        {
            var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, uri, _deviceInfo);
            var response = await _httpRequestProcessor.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            var activityFeed = new InstaActivityFeed();
            var json = await response.Content.ReadAsStringUnZipAsync();
            if (response.StatusCode != HttpStatusCode.OK)
                return Result.UnExpectedResponse<InstaActivityFeed>(response, json);
            var feedPage = JsonConvert.DeserializeObject<InstaRecentActivityResponse>(json,
                new InstaRecentActivityConverter());
            activityFeed.IsOwnActivity = feedPage.IsOwnActivity;
            var nextId = feedPage.NextMaxId;
            activityFeed.Items.AddRange(
                feedPage.Stories.Select(ConvertersFabric.Instance.GetSingleRecentActivityConverter)
                    .Select(converter => converter.Convert()));
            paginationParameters.PagesLoaded++;
            activityFeed.NextId = paginationParameters.NextId = feedPage.NextMaxId;
            while (!string.IsNullOrEmpty(nextId)
                   && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
            {
                var nextFollowingFeed = await GetFollowingActivityWithMaxIdAsync(nextId);
                if (!nextFollowingFeed.Succeeded)
                    return Result.Fail(nextFollowingFeed.Info, activityFeed);
                nextId = nextFollowingFeed.Value.NextMaxId;
                activityFeed.Items.AddRange(
                    feedPage.Stories.Select(ConvertersFabric.Instance.GetSingleRecentActivityConverter)
                        .Select(converter => converter.Convert()));
                paginationParameters.PagesLoaded++;
                activityFeed.NextId = paginationParameters.NextId = nextId;
            }

            return Result.Success(activityFeed);
        }
    }
}