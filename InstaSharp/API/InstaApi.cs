﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using InstaSharper.API.Processors;
using InstaSharper.Classes;
using InstaSharper.Classes.Android.DeviceInfo;
using InstaSharper.Classes.Models;
using InstaSharper.Classes.ResponseWrappers;
using InstaSharper.Classes.ResponseWrappers.BaseResponse;
using InstaSharper.Converters;
using InstaSharper.Helpers;
using InstaSharper.Logger;
using InstaSharperDirect.Classes.ResponseWrappers;
using Newtonsoft.Json;

namespace InstaSharper.API
{
    internal class InstaApi : IInstaApi
    {
        private readonly IHttpRequestProcessor _httpRequestProcessor;
        private readonly IInstaLogger _logger;
        private ICollectionProcessor _collectionProcessor;
        private ICommentProcessor _commentProcessor;
        private AndroidDevice _deviceInfo;
        private IFeedProcessor _feedProcessor;

        private IHashtagProcessor _hashtagProcessor;
        private ILocationProcessor _locationProcessor;
        private IMediaProcessor _mediaProcessor;
        private IMessagingProcessor _messagingProcessor;
        private IUserProfileProcessor _profileProcessor;
        private IStoryProcessor _storyProcessor;

        private TwoFactorLoginInfo _twoFactorInfo = null; // todo: ???
        private UserSessionData _user;
        private IUserProcessor _userProcessor;

        public InstaApi(UserSessionData user, IInstaLogger logger, AndroidDevice deviceInfo,
            IHttpRequestProcessor httpRequestProcessor)
        {
            _user = user;
            _logger = logger;
            _deviceInfo = deviceInfo;
            _httpRequestProcessor = httpRequestProcessor;
        }

        /// <summary>
        ///     Get user timeline feed (feed of recent posts from users you follow) asynchronously.
        /// </summary>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaFeed" />
        /// </returns>
        public async Task<IResult<InstaFeed>> GetUserTimelineFeedAsync(PaginationParameters paginationParameters)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _feedProcessor.GetUserTimelineFeedAsync(paginationParameters);
        }

        /// <summary>
        ///     Get user story reel feed. Contains user info last story including all story items.
        /// </summary>
        /// <param name="userId">User identifier (PK)</param>
        /// <returns></returns>
        public async Task<IResult<InstaReelFeed>> GetUserStoryFeedAsync(long userId)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _storyProcessor.GetUserStoryFeedAsync(userId);
        }


        /// <summary>
        ///     Get user explore feed (Explore tab info) asynchronously
        /// </summary>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaExploreFeed" />&gt;
        /// </returns>
        public async Task<IResult<InstaExploreFeed>> GetExploreFeedAsync(PaginationParameters paginationParameters)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _feedProcessor.GetExploreFeedAsync(paginationParameters);
        }

        /// <summary>
        ///     Get all user media by username asynchronously
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaMediaList" />
        /// </returns>
        public async Task<IResult<InstaMediaList>> GetUserMediaAsync(string username,
            PaginationParameters paginationParameters)
        {
            ValidateUser();
            ValidateLoggedIn();
            var user = await GetUserAsync(username);
            if (!user.Succeeded)
                return Result.Fail<InstaMediaList>("Unable to get user to load media");
            return await _userProcessor.GetUserMediaAsync(user.Value.Pk, paginationParameters);
        }

        public async Task<IResult<InstaMediaList>> GetUserMediaAsync(long userId, PaginationParameters paginationParameters)
        {
            return await _userProcessor.GetUserMediaAsync(userId, paginationParameters);
        }

        /// <summary>
        ///     Get media by its id asynchronously
        /// </summary>
        /// <param name="mediaId">Maximum count of pages to retrieve</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaMedia" />
        /// </returns>
        public async Task<IResult<InstaMedia>> GetMediaByIdAsync(string mediaId)
        {
            ValidateUser();
            return await _mediaProcessor.GetMediaByIdAsync(mediaId);
        }

        /// <summary>
        ///     Get user info by its user name asynchronously
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaUser" />
        /// </returns>
        public async Task<IResult<InstaUser>> GetUserAsync(string username)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _userProcessor.GetUserAsync(username);
        }

        /// <summary>
        ///     Get currently logged in user info asynchronously
        /// </summary>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaCurrentUser" />
        /// </returns>
        public async Task<IResult<InstaCurrentUser>> GetCurrentUserAsync()
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _userProcessor.GetCurrentUserAsync();
        }

        /// <summary>
        ///     Get tag feed by tag value asynchronously
        /// </summary>
        /// <param name="tag">Tag value</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaTagFeed" />
        /// </returns>
        public async Task<IResult<InstaTagFeed>> GetTagFeedAsync(string tag, PaginationParameters paginationParameters)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _feedProcessor.GetTagFeedAsync(tag, paginationParameters);
        }

        /// <summary>
        /// Decline ALL pending threads
        /// </summary>
        /// <returns>Status response</returns>
        public async Task<IResult<BaseStatusResponse>> DeclineAllPendingDirectThreads()
        {
            return await _messagingProcessor.DeclineAllPendingDirectThreads();
        }

        /// <summary>
        /// Approve single thread by id
        /// </summary>
        /// <param name="threadId">Thread id, e.g. "111182366841710300949128137443944311111"</param>
        /// <returns>Status response</returns>
        public async Task<IResult<BaseStatusResponse>> ApprovePendingDirectThread(string threadId)
        {
            return await _messagingProcessor.ApprovePendingDirectThread(threadId);
        }

        public async Task<IResult<BaseStatusResponse>> ApprovePendingDirectThreads(List<string> threadIds)
        {
            return await _messagingProcessor.ApprovePendingDirectThreads(threadIds);
        }

        /// <summary>
        ///     Get followers list by username asynchronously
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <param name="searchQuery">Search string to locate specific followers</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaUserShortList" />
        /// </returns>
        public async Task<IResult<InstaUserShortList>> GetUserFollowersAsync(string username,
            PaginationParameters paginationParameters, string searchQuery = "")
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _userProcessor.GetUserFollowersAsync(username, paginationParameters, searchQuery);
        }

        /// <summary>
        ///     Get following list by username asynchronously
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <param name="searchQuery">Search string to locate specific followings</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaUserShortList" />
        /// </returns>
        public async Task<IResult<InstaUserShortList>> GetUserFollowingAsync(string username,
            PaginationParameters paginationParameters, string searchQuery = "")
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _userProcessor.GetUserFollowingAsync(username, paginationParameters, searchQuery);
        }

        /// <summary>
        ///     Gets the user extended information (followers count, following count, bio, etc) by user identifier.
        /// </summary>
        /// <param name="pk">User Id, like "123123123"</param>
        /// <returns></returns>
        public async Task<IResult<InstaUserInfo>> GetUserInfoByIdAsync(long pk)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _userProcessor.GetUserInfoByIdAsync(pk);
        }

        /// <summary>
        ///     Gets the user extended information (followers count, following count, bio, etc) by username.
        /// </summary>
        /// <param name="username">Username, like "instagram"</param>
        /// <returns></returns>
        public async Task<IResult<InstaUserInfo>> GetUserInfoByUsernameAsync(string username)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _userProcessor.GetUserInfoByUsernameAsync(username);
        }

        /// <summary>
        ///     Get followers list for currently logged in user asynchronously
        /// </summary>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaUserShortList" />
        /// </returns>
        public async Task<IResult<InstaUserShortList>> GetCurrentUserFollowersAsync(
            PaginationParameters paginationParameters)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _userProcessor.GetCurrentUserFollowersAsync(paginationParameters);
        }

        /// <summary>
        ///     Get user tags by username asynchronously
        ///     <remarks>Returns media list containing tags</remarks>
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaMediaList" />
        /// </returns>
        public async Task<IResult<InstaMediaList>> GetUserTagsAsync(string username,
            PaginationParameters paginationParameters)
        {
            ValidateUser();
            ValidateLoggedIn();
            var user = await GetUserAsync(username);
            if (!user.Succeeded)
                return Result.Fail($"Unable to get user {username} to get tags", (InstaMediaList)null);
            return await _userProcessor.GetUserTagsAsync(user.Value.Pk, paginationParameters);
        }


        /// <summary>
        ///     Get direct inbox threads for current user asynchronously
        /// </summary>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaDirectInboxContainer" />
        /// </returns>
        public async Task<IResult<InstaDirectInboxContainer>> GetDirectInboxAsync()
        {
            return await _messagingProcessor.GetDirectInboxAsync();
        }

        public async Task<IResult<InstaDirectInboxContainer>> GetPendingDirectInboxAsync()
        {
            return await _messagingProcessor.GetPendingDirectInboxAsync();
        }

        /// <summary>
        ///     Get direct inbox thread by its id asynchronously
        /// </summary>
        /// <param name="threadId">Thread id</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaDirectInboxThread" />
        /// </returns>
        public async Task<IResult<InstaDirectInboxThread>> GetDirectInboxThreadAsync(string threadId)
        {
            return await _messagingProcessor.GetDirectInboxThreadAsync(threadId);
        }

        /// <summary>
        ///     Send direct message to provided users and threads
        /// </summary>
        /// <param name="recipients">Comma-separated users PK</param>
        /// <param name="threadIds">Message thread ids</param>
        /// <param name="text">Message text</param>
        /// <returns>
        ///     List of threads
        /// </returns>
        public async Task<IResult<InstaDirectInboxThreadList>> SendDirectMessage(string recipients, string threadIds,
            string text)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _messagingProcessor.SendDirectMessage(recipients, threadIds, text);
        }

        /// <summary>
        ///     Get recent recipients (threads and users) asynchronously
        /// </summary>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaRecipientThreads" />
        /// </returns>
        public async Task<IResult<InstaRecipients>> GetRecentRecipientsAsync()
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _messagingProcessor.GetRecentRecipientsAsync();
        }

        /// <summary>
        ///     Get ranked recipients (threads and users) asynchronously
        /// </summary>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaRecipientThreads" />
        /// </returns>
        public async Task<IResult<InstaRecipients>> GetRankedRecipientsAsync()
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _messagingProcessor.GetRankedRecipientsAsync();
        }

        /// <summary>
        ///     Get recent activity info asynchronously
        /// </summary>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaActivityFeed" />
        /// </returns>
        public async Task<IResult<InstaActivityFeed>> GetRecentActivityAsync(PaginationParameters paginationParameters)
        {
            return await _feedProcessor.GetRecentActivityFeedAsync(paginationParameters);
        }

        /// <summary>
        ///     Get activity of following asynchronously
        /// </summary>
        /// <param name="paginationParameters"></param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaActivityFeed" />
        /// </returns>
        public async Task<IResult<InstaActivityFeed>> GetFollowingRecentActivityAsync(
            PaginationParameters paginationParameters)
        {
            return await _feedProcessor.GetFollowingRecentActivityFeedAsync(paginationParameters);
        }


        /// <summary>
        ///     Like media (photo or video)
        /// </summary>
        /// <param name="mediaId">Media id</param>
        /// <returns></returns>
        public async Task<IResult<bool>> LikeMediaAsync(string mediaId)
        {
            return await _mediaProcessor.LikeMediaAsync(mediaId);
        }

        /// <summary>
        ///     Remove like from media (photo or video)
        /// </summary>
        /// <param name="mediaId">Media id</param>
        /// <returns></returns>
        public async Task<IResult<bool>> UnLikeMediaAsync(string mediaId)
        {
            return await _mediaProcessor.UnLikeMediaAsync(mediaId);
        }


        /// <summary>
        ///     Get media comments
        /// </summary>
        /// <param name="mediaId">Media id</param>
        /// <param name="paginationParameters">Maximum amount of pages to load and start id</param>
        /// <returns></returns>
        public async Task<IResult<InstaCommentList>> GetMediaCommentsAsync(string mediaId,
            PaginationParameters paginationParameters)
        {
            ValidateUser();
            ValidateLoggedIn();

            return await _commentProcessor.GetMediaCommentsAsync(mediaId, paginationParameters);
        }

        /// <summary>
        ///     Get users (short) who liked certain media. Normaly it return around 1000 last users.
        /// </summary>
        /// <param name="mediaId">Media id</param>
        /// <returns></returns>
        public async Task<IResult<InstaLikersList>> GetMediaLikersAsync(string mediaId)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _mediaProcessor.GetMediaLikersAsync(mediaId);
        }

        /// <summary>
        ///     Follow user
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns></returns>
        public async Task<IResult<InstaFriendshipStatus>> FollowUserAsync(long userId)
        {
            return await _userProcessor.FollowUserAsync(userId);
        }

        /// <summary>
        ///     Stop follow user
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns></returns>
        public async Task<IResult<InstaFriendshipStatus>> UnFollowUserAsync(long userId)
        {
            return await _userProcessor.UnFollowUserAsync(userId);
        }


        /// <summary>
        ///     Block user
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns></returns>
        public async Task<IResult<InstaFriendshipStatus>> BlockUserAsync(long userId)
        {
            return await _userProcessor.BlockUserAsync(userId);
        }

        /// <summary>
        ///     Stop Block user
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns></returns>
        public async Task<IResult<InstaFriendshipStatus>> UnBlockUserAsync(long userId)
        {
            return await _userProcessor.UnBlockUserAsync(userId);
        }

        /// <summary>
        ///     Set current account private
        /// </summary>
        /// <returns></returns>
        public async Task<IResult<InstaUserShort>> SetAccountPrivateAsync()
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _profileProcessor.SetAccountPrivateAsync();
        }

        /// <summary>
        ///     Set current account public
        /// </summary>
        /// <returns></returns>
        public async Task<IResult<InstaUserShort>> SetAccountPublicAsync()
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _profileProcessor.SetAccountPublicAsync();
        }


        /// <summary>
        ///     Comment media
        /// </summary>
        /// <param name="mediaId">Media id</param>
        /// <param name="text">Comment text</param>
        /// <returns></returns>
        public async Task<IResult<InstaComment>> CommentMediaAsync(string mediaId, string text)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _commentProcessor.CommentMediaAsync(mediaId, text);
        }

        /// <summary>
        ///     Delete comment from media
        /// </summary>
        /// <param name="mediaId">Media id</param>
        /// <param name="commentId">Comment id</param>
        /// <returns></returns>
        public async Task<IResult<bool>> DeleteCommentAsync(string mediaId, string commentId)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _commentProcessor.DeleteCommentAsync(mediaId, commentId);
        }
        /// <summary>
        ///     Upload video
        /// </summary>
        /// <param name="video">Video to upload</param>
        /// <param name="imageThumbnail">Image thumbnail</param>
        /// <param name="caption">Caption</param>
        /// <returns></returns>
        public async Task<IResult<InstaMedia>> UploadVideoAsync(InstaVideo video, InstaImage imageThumbnail, string caption)
        {
            ValidateUser();
            ValidateLoggedIn();

            return await _mediaProcessor.UploadVideoAsync(video, imageThumbnail, caption);
        }
        /// <summary>
        ///     Upload photo
        /// </summary>
        /// <param name="image">Photo to upload</param>
        /// <param name="caption">Caption</param>
        /// <returns></returns>
        public async Task<IResult<InstaMedia>> UploadPhotoAsync(InstaImage image, string caption)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _mediaProcessor.UploadPhotoAsync(image, caption);
        }

        /// <summary>
        ///     Upload photo
        /// </summary>
        /// <param name="images">Array of photos to upload</param>
        /// <param name="caption">Caption</param>
        /// <returns></returns>
        public async Task<IResult<InstaMedia>> UploadPhotosAlbumAsync(InstaImage[] images, string caption)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _mediaProcessor.UploadPhotosAlbumAsync(images, caption);
        }

        /// <summary>
        ///     Configure photo
        /// </summary>
        /// <param name="image">Photo to configure</param>
        /// <param name="uploadId">Upload id</param>
        /// <param name="caption">Caption</param>
        /// <returns></returns>
        public async Task<IResult<InstaMedia>> ConfigurePhotoAsync(InstaImage image, string uploadId, string caption)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _mediaProcessor.ConfigurePhotoAsync(image, uploadId, caption);
        }

        /// <summary>
        ///     Configure photos for Album
        /// </summary>
        /// <param name="uploadIds">Array of upload IDs to configure</param>
        /// ///
        /// <param name="caption">Caption</param>
        /// <returns></returns>
        public async Task<IResult<InstaMedia>> ConfigureAlbumAsync(string[] uploadIds, string caption)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _mediaProcessor.ConfigureAlbumAsync(uploadIds, caption);
        }


        /// <summary>
        ///     Get user story feed (stories from users followed by current user).
        /// </summary>
        /// <returns></returns>
        public async Task<IResult<InstaStoryFeed>> GetStoryFeedAsync()
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _storyProcessor.GetStoryFeedAsync();
        }

        /// <summary>
        ///     Get the story by userId
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <returns></returns>
        public async Task<IResult<InstaStory>> GetUserStoryAsync(long userId)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _storyProcessor.GetUserStoryAsync(userId);
        }

        /// <summary>
        ///     Upload story photo
        /// </summary>
        /// <param name="image">Photo to upload</param>
        /// <param name="caption">Caption</param>
        /// <returns></returns>
        public async Task<IResult<InstaStoryMedia>> UploadStoryPhotoAsync(InstaImage image, string caption)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _storyProcessor.UploadStoryPhotoAsync(image, caption);
        }

        /// <summary>
        ///     Configure story photo
        /// </summary>
        /// <param name="image">Photo to configure</param>
        /// <param name="uploadId">Upload id</param>
        /// <param name="caption">Caption</param>
        /// <returns></returns>
        public async Task<IResult<InstaStoryMedia>> ConfigureStoryPhotoAsync(InstaImage image, string uploadId,
            string caption)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _storyProcessor.ConfigureStoryPhotoAsync(image, uploadId, caption);
        }

        /// <summary>
        ///     Change password
        /// </summary>
        /// <param name="oldPassword">The old password</param>
        /// <param name="newPassword">
        ///     The new password (shouldn't be the same old password, and should be a password you never used
        ///     here)
        /// </param>
        /// <returns>
        ///     Return true if the password is changed
        /// </returns>
        public async Task<IResult<bool>> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _profileProcessor.ChangePasswordAsync(oldPassword, newPassword);
        }

        /// <summary>
        ///     Delete a media (photo or video)
        /// </summary>
        /// <param name="mediaId">The media ID</param>
        /// <param name="mediaType">The type of the media</param>
        /// <returns>
        ///     Return true if the media is deleted
        /// </returns>
        public async Task<IResult<bool>> DeleteMediaAsync(string mediaId, InstaMediaType mediaType)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _mediaProcessor.DeleteMediaAsync(mediaId, mediaType);
        }

        /// <summary>
        ///     Edit the caption of the media (photo/video)
        /// </summary>
        /// <param name="mediaId">The media ID</param>
        /// <param name="caption">The new caption</param>
        /// <returns>
        ///     Return true if everything is ok
        /// </returns>
        public async Task<IResult<bool>> EditMediaAsync(string mediaId, string caption)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _mediaProcessor.EditMediaAsync(mediaId, caption);
        }

        /// <summary>
        ///     Get feed of media your liked.
        /// </summary>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaMediaList" />
        /// </returns>
        public async Task<IResult<InstaMediaList>> GetLikeFeedAsync(PaginationParameters paginationParameters)
        {
            ValidateUser();
            return await _feedProcessor.GetLikeFeedAsync(paginationParameters);
        }

        /// <summary>
        ///     Get friendship status for given user id.
        /// </summary>
        /// <param name="userId">User identifier (PK)</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaFriendshipStatus" />
        /// </returns>
        public async Task<IResult<InstaFriendshipStatus>> GetFriendshipStatusAsync(long userId)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _userProcessor.GetFriendshipStatusAsync(userId);
        }

        /// <summary>
        ///     Get your collection for given collection id
        /// </summary>
        /// <param name="collectionId">Collection ID</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaCollectionItem" />
        /// </returns>
        public async Task<IResult<InstaCollectionItem>> GetCollectionAsync(long collectionId)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _collectionProcessor.GetCollectionAsync(collectionId);
        }


        /// <summary>
        ///     Get your collections
        /// </summary>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaCollections" />
        /// </returns>
        public async Task<IResult<InstaCollections>> GetCollectionsAsync()
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _collectionProcessor.GetCollectionsAsync();
        }

        /// <summary>
        ///     Create a new collection
        /// </summary>
        /// <param name="collectionName">The name of the new collection</param>
        /// <returns>
        ///     <see cref="T:InstaSharper.Classes.Models.InstaCollectionItem" />
        /// </returns>
        public async Task<IResult<InstaCollectionItem>> CreateCollectionAsync(string collectionName)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _collectionProcessor.CreateCollectionAsync(collectionName);
        }

        public async Task<IResult<InstaCollectionItem>> AddItemsToCollectionAsync(long collectionId,
            params string[] mediaIds)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _collectionProcessor.AddItemsToCollectionAsync(collectionId, mediaIds);
        }

        /// <summary>
        ///     Delete your collection for given collection id
        /// </summary>
        /// <param name="collectionId">Collection ID to delete</param>
        /// <returns>true if succeed</returns>
        public async Task<IResult<bool>> DeleteCollectionAsync(long collectionId)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _collectionProcessor.DeleteCollectionAsync(collectionId);
        }

        /// <summary>
        ///     Get media ID from an url (got from "share link")
        /// </summary>
        /// <param name="uri">Uri to get media ID</param>
        /// <returns>Media ID</returns>
        public async Task<IResult<string>> GetMediaIdFromUrlAsync(Uri uri)
        {
            ValidateLoggedIn();
            ValidateRequestMessage();
            return await _mediaProcessor.GetMediaIdFromUrlAsync(uri);
        }

        /// <summary>
        ///     Get share link from media Id
        /// </summary>
        /// <param name="mediaId">media ID</param>
        /// <returns>Share link as Uri</returns>
        public async Task<IResult<Uri>> GetShareLinkFromMediaIdAsync(string mediaId)
        {
            return await _mediaProcessor.GetShareLinkFromMediaIdAsync(mediaId);
        }

        /// <summary>
        ///     Searches for specific location by provided geo-data or search query.
        /// </summary>
        /// <param name="latitude">Latitude</param>
        /// <param name="longitude">Longitude</param>
        /// <param name="query">Search query</param>
        /// <returns>
        ///     List of locations (short format)
        /// </returns>
        public async Task<IResult<InstaLocationShortList>> SearchLocation(double latitude, double longitude,
            string query)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _locationProcessor.Search(latitude, longitude, query);
        }

        /// <summary>
        ///     Gets the feed of particular location.
        /// </summary>
        /// <param name="locationId">Location identifier</param>
        /// <param name="paginationParameters">Pagination parameters: next id and max amount of pages to load</param>
        /// <returns>
        ///     Location feed
        /// </returns>
        public async Task<IResult<InstaLocationFeed>> GetLocationFeed(long locationId,
            PaginationParameters paginationParameters)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _locationProcessor.GetFeed(locationId, paginationParameters);
        }

        /// <summary>
        ///     Searches for specific hashtag by search query.
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="excludeList">Array of numerical hashtag IDs (ie "17841562498105353") to exclude from the response, allowing you to skip tags from a previous call to get more results</param>
        /// <param name="rankToken">The rank token from the previous page's response</param>
        /// <returns>
        ///     List of hashtags
        /// </returns>
        public async Task<IResult<InstaHashtagSearch>> SearchHashtag(string query, IEnumerable<long> excludeList, string rankToken)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _hashtagProcessor.Search(query, excludeList, rankToken);
        }

        /// <summary>
        ///     Gets the hashtag information by user tagname.
        /// </summary>
        /// <param name="tagname">Tagname</param>
        /// <returns>Hashtag information</returns>
        public async Task<IResult<InstaHashtag>> GetHashtagInfo(string tagname)
        {
            ValidateUser();
            ValidateLoggedIn();
            return await _hashtagProcessor.GetHashtagInfo(tagname);
        }


        #region Authentication/State data

        /// <summary>
        ///     Indicates whether user authenticated or not
        /// </summary>
        public bool IsUserAuthenticated { get; private set; }
        /// <summary>
        ///     Create a new instagram account
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="email">Email</param>
        /// <param name="firstName">First name (optional)</param>
        /// <returns></returns>
        public async Task<IResult<CreationResponse>> CreateNewAccount(string username, string password, string email, string firstName)
        {
            CreationResponse createResponse = new CreationResponse();
            try
            {
                var postData = new Dictionary<string, string>
                {
                    {"email",     email },
                    {"username",    username},
                    {"password",    password},
                    {"device_id",   ApiRequestMessage.GenerateDeviceId()},
                    {"guid",        _deviceInfo.DeviceGuid.ToString()},
                    {"first_name",  firstName}
                };

                var instaUri = UriCreator.GetCreateAccountUri();
                var request = HttpHelper.GetSignedRequest(HttpMethod.Post, instaUri, _deviceInfo, postData);
                var response = await _httpRequestProcessor.SendAsync(request);
                var result = await response.Content.ReadAsStringUnZipAsync();

                return Result.Success(JsonConvert.DeserializeObject<CreationResponse>(result));
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<CreationResponse>(exception);
            }
        }

        /// <summary>
        ///     Login using given credentials asynchronously
        /// </summary>
        /// <returns>
        ///     Success --> is succeed
        ///     TwoFactorRequired --> requires 2FA login.
        ///     BadPassword --> Password is wrong
        ///     InvalidUser --> User/phone number is wrong
        ///     Exception --> Something wrong happened
        /// </returns>
        public async Task<IResult<InstaLoginResult>> LoginAsync()
        {
            //ValidateUser();
            //ValidateRequestMessage();

            try
            {
                var needReLogin = false;

                if (_user != null && _user.LoggedInUder != null)
                {
                    // check last time loggin

                    var isSessionExpired = _user.LastLoginTime == null ||
                                           (DateTime.Now - _user.LastLoginTime.Value).Minutes > 4 * 60;

                    InvalidateProcessors();

                    // Act like a real logged in app client refreshing its news timeline.
                    // This also lets us detect if we're still logged in with a valid session.
                    try
                    {
                        var result = await _feedProcessor.GetTimelineFeedAsync(new Dictionary<string, string>() {{"is_pull_to_refresh", isSessionExpired ? null : "1"}});
                        if (!result.Succeeded)
                            needReLogin = true;
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);

                        _user = null;

                        needReLogin = true;
                    }

                    if (!needReLogin)
                    {
                        // Perform the "user has returned to their already-logged in app,
                        // so refresh all feeds to check for news" API flow.
                        if (isSessionExpired)
                        {
                            /*
                            $this->settings->set('last_login', time());
    
                            // Generate and save a new application session ID.
                            $this->session_id = Signatures::generateUUID(true);
                            $this->settings->set('session_id', $this->session_id);
    
                            // Do the rest of the "user is re-opening the app" API flow...
                            $this->people->getBootstrapUsers();
                            $this->story->getReelsTrayFeed();
                            $this->direct->getRankedRecipients('reshare', true);
                            $this->direct->getRankedRecipients('raven', true);
                            $this->_registerPushChannels();
                            //$this->internal->getMegaphoneLog();
                            $this->direct->getInbox();
                            $this->direct->getPresences();
                            $this->people->getRecentActivityInbox();
                            $this->internal->getProfileNotice();
                            $this->discover->getExploreFeed();
                            */

                        }

                        IsUserAuthenticated = true;

                        return Result.Success(new InstaLoginResult(InstaLoginStatus.Success));
                    }
                }

                await SendPreLoginFlow();

                var fields = new Dictionary<string, string>()
                {
                    {"phone_id", _httpRequestProcessor.RequestMessage.phone_id},
                    {"_csrftoken", _user.CsrfToken},
                    {"username", _user.UserName},
                    {"adid", _httpRequestProcessor.RequestMessage.advertising_id},
                    {"guid", _httpRequestProcessor.RequestMessage.uuid},
                    {"device_id", _httpRequestProcessor.RequestMessage.device_id},
                    {"password", _user.Password},
                    {"login_attempt_count", "0"}
                };

                var request = HttpHelper.GetSignedRequest(HttpMethod.Post, new Uri("https://i.instagram.com/api/v1/accounts/login/"), _deviceInfo, fields);

                var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
                var json = await response.Content.ReadAsStringUnZipAsync();


                if (response.StatusCode != HttpStatusCode.OK) //If the password is correct BUT 2-Factor Authentication is enabled, it will still get a 400 error (bad request)
                {
                    var loginFailReason = JsonConvert.DeserializeObject<InstaLoginBaseResponse>(json);

                    if (loginFailReason.InvalidCredentials)
                        return Result.Fail("Invalid Credentials", new InstaLoginResult(loginFailReason.ErrorType == "bad_password" ? InstaLoginStatus.BadPassword : InstaLoginStatus.InvalidUser));

                    if (loginFailReason.TwoFactorRequired)
                    {
                        _twoFactorInfo = loginFailReason.TwoFactorLoginInfo;
                        return Result.Fail("Two Factor Authentication is required", new InstaLoginResult(InstaLoginStatus.TwoFactorRequired));
                    }

                    var challengeRequiredReason = JsonConvert.DeserializeObject<ChallengeRequiredResponse>(json);

                    if (challengeRequiredReason.ErrorType == "checkpoint_challenge_required")
                    {
                        challengeRequiredReason.Json = json;
                        return Result.Fail("checkpoint_challenge_required", new InstaLoginResult(InstaLoginStatus.ChallengeRequired){ Error = challengeRequiredReason});
                    }

                    return Result.UnExpectedResponse<InstaLoginResult>(response, json);
                }

                var loginInfo = JsonConvert.DeserializeObject<InstaLoginResponse>(json);
                var converter = ConvertersFabric.Instance.GetUserShortConverter(loginInfo.User);

                SetAuthorizedUser(converter.Convert());

                // Updates the internal state after a successful login.
                InvalidateProcessors();

                await SendLoginFlow(true);
            }
            catch (Exception exception)
            {
                LogException(exception);

                InvalidateProcessors();

                return Result.Fail(exception, new InstaLoginResult(InstaLoginStatus.Exception));
            }

            return Result.Success(new InstaLoginResult(InstaLoginStatus.Success));
        }

        private void SetAuthorizedUser(InstaUserShort user)
        {
            _user.LoggedInUder = user;
            _user.RankToken = $"{_user.LoggedInUder.Pk}_{_httpRequestProcessor.RequestMessage.phone_id}";
            _user.LastLoginTime = DateTime.Now;

            _httpRequestProcessor.RequestMessage.account_id = _user.LoggedInUder.Pk.ToString();

            IsUserAuthenticated = true;
        }


        /// <summary>
        /// Require challenge code by sms or email
        /// </summary>
        /// <param name="challengeApiPath">challenge api path</param>
        /// <param name="choiceMethod">0 = SMS, 1 = Email</param>
        /// <returns></returns>
        public async Task<bool> RequireChallengeCode(string challengeApiPath, int choiceMethod = 1)
        {
            try
            {
                var uri = new Uri("https://i.instagram.com/api/v1" + challengeApiPath);
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, uri, _deviceInfo);

                var fields = new Dictionary<string, string>
                {
                    {"choice", choiceMethod.ToString()},
                    {"_uuid", _httpRequestProcessor.RequestMessage.uuid},
                    {"guid", _httpRequestProcessor.RequestMessage.uuid},
                    {"device_id", _httpRequestProcessor.RequestMessage.device_id},
                    {"_uid", challengeApiPath.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries)[1]},
                    {"_csrftoken", _user.CsrfToken}
                };
                request.Content = new FormUrlEncodedContent(fields);

                var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
                var json = await response.Content.ReadAsStringUnZipAsync();

                var result = JsonConvert.DeserializeObject<RequireChallengeCodeResponse>(json);

                return result != null && result.Status == "ok";
            }
            catch (Exception exception)
            {
                LogException(exception);
            }
            return false;
        }

        /// <summary>
        /// Send challenge code
        /// </summary>
        /// <param name="challengeApiPath">challenge api path</param>
        /// <param name="code">code from sms or email</param>
        /// <returns></returns>
        public async Task<IResult<SendChallengeCodeResponse>> SendChallengeCode(string challengeApiPath, string code)
        {
            try
            {
                var uri = new Uri("https://i.instagram.com/api/v1" + challengeApiPath);
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Post,  uri, _deviceInfo);

                var fields = new Dictionary<string, string>
                {
                    {"security_code", code},
                    {"_uuid", _httpRequestProcessor.RequestMessage.uuid},
                    {"guid", _httpRequestProcessor.RequestMessage.uuid},
                    {"device_id", _httpRequestProcessor.RequestMessage.device_id},
                    {"_uid", challengeApiPath.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries)[1]},
                    {"_csrftoken", _user.CsrfToken}
                };
                request.Content = new FormUrlEncodedContent(fields);

                var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
                var json = await response.Content.ReadAsStringUnZipAsync();

                var result = JsonConvert.DeserializeObject<SendChallengeCodeResponse>(json);

                if (result.Status == "ok" && result.LoggedInUser != null)
                {
                    SetAuthorizedUser(new InstaUserShort()
                    {
                        UserName = result.LoggedInUser.Username,
                        FullName = result.LoggedInUser.FullName,
                        ProfilePicture = result.LoggedInUser.ProfilePicUrl,
                        IsPrivate = result.LoggedInUser.IsPrivate,
                        IsVerified = result.LoggedInUser.IsVerified,
                        Pk = result.LoggedInUser.Pk
                    });
                }

                return Result.Success(result);
            }
            catch (Exception exception)
            {
                LogException(exception);
                return Result.Fail<SendChallengeCodeResponse>(exception);
            }
        }


        #region PreLogin Flow

        private async Task SendPreLoginFlow()
        {
            // Calling this non-token API will put a csrftoken in our cookie
            // jar. We must do this before any functions that require a token.
            await GetMsisdn();
            await SyncDeviceFeatures();
            await SendLauncherSync();
            await LogAttribution();

            // We must fetch new token here, because it updates rewrite rules.
            await FetchZeroRatingToken();

            // It must be at the end, because it's called when a user taps on login input.
            await SetContactPointPrefill();
        }

        //private async Task GetCsrfToken()
        //{
        //    var firstResponse = await _httpRequestProcessor.GetAsyncWithoutDelay(_httpRequestProcessor.Client.BaseAddress);
        //    var cookies = _httpRequestProcessor.HttpHandler.CookieContainer.GetCookies(_httpRequestProcessor.Client.BaseAddress);
        //    _logger?.LogResponse(firstResponse);

        //    var csrftoken = cookies[InstaApiConstants.CSRFTOKEN]?.Value ?? String.Empty;
        //    _user.CsrfToken = csrftoken;
        //}

        private async Task GetMsisdn()
        {
            var url = new Uri("https://i.instagram.com/api/v1/accounts/read_msisdn_header/");

            var fields = new Dictionary<string, string>()
            {
                { "mobile_subno_usage", "ig_select_app" },
                { "device_id", _httpRequestProcessor.RequestMessage.uuid }
            };
            
            var request = HttpHelper.GetSignedRequest(HttpMethod.Post, url, _deviceInfo, fields);

            request.Headers.Add("X-DEVICE-ID", _httpRequestProcessor.RequestMessage.uuid);
            
            var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
            var json = await response.Content.ReadAsStringUnZipAsync();

            var cookies = _httpRequestProcessor.HttpHandler.CookieContainer.GetCookies(new Uri("https://i.instagram.com"));

            var csrftoken = cookies[InstaApiConstants.CSRFTOKEN]?.Value ?? String.Empty;
            _user.CsrfToken = csrftoken;
        }

        /// <summary>
        /// Perform an Instagram "feature synchronization" call for account.
        /// </summary>
        /// <returns></returns>
        private async Task SyncDeviceFeatures()
        {
            var url = new Uri("https://i.instagram.com/api/v1/qe/sync/");

            var fields = new Dictionary<string, string>()
            {
                { "id", _httpRequestProcessor.RequestMessage.uuid },
                { "experiments", InstaApiConstants.LOGIN_EXPERIMENTS }
            };

            var request = HttpHelper.GetSignedRequest(HttpMethod.Post, url, _deviceInfo, fields);

            request.Headers.Add("X-DEVICE-ID", _httpRequestProcessor.RequestMessage.uuid);

            var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
            var json = await response.Content.ReadAsStringUnZipAsync();
        }

        /// <summary>
        /// Send launcher sync.
        /// </summary>
        /// <returns></returns>
        private async Task SendLauncherSync(bool preLogin = true)
        {
            var url = new Uri("https://i.instagram.com/api/v1/launcher/sync/");

            var fields =
                preLogin
                    ? new Dictionary<string, string>()
                    {
                        {"_csrftoken", _user.CsrfToken},
                        {"configs", ""},
                        {"id", _httpRequestProcessor.RequestMessage.uuid},
                    }
                    : new Dictionary<string, string>()
                    {
                        {"configs", ""},
                        {"id", _httpRequestProcessor.RequestMessage.account_id},
                        {"_uuid", _httpRequestProcessor.RequestMessage.uuid},
                        {"_uid", _httpRequestProcessor.RequestMessage.account_id},
                        {"_csrftoken", _user.CsrfToken},
                    };

            var request = HttpHelper.GetSignedRequest(HttpMethod.Post, url, _deviceInfo, fields);
            
            var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
            var json = await response.Content.ReadAsStringUnZipAsync();
        }


        /// <summary>
        /// Registers advertising identifier.
        /// </summary>
        /// <returns></returns>
        private async Task LogAttribution()
        {
            var url = new Uri("https://i.instagram.com/api/v1/attribution/log_attribution/");

            var fields = new Dictionary<string, string>()
            {
                { "adid", _httpRequestProcessor.RequestMessage.advertising_id },
            };

            var request = HttpHelper.GetSignedRequest(HttpMethod.Post, url, _deviceInfo, fields);

            var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
            var json = await response.Content.ReadAsStringUnZipAsync();
        }

        /// <summary>
        /// Get zero rating token hash result.
        /// </summary>
        /// <param name="fetch_reason">"token_expired", "mqtt_token_push", "token_stale", "provisioning_time_mismatch"</param>
        /// <returns></returns>
        private async Task FetchZeroRatingToken(string fetch_reason = "token_expired")
        {
            var url = new Uri("https://i.instagram.com/api/v1/zr/token/result/");

            var fields = new Dictionary<string, string>()
            {
                { "custom_device_id", _httpRequestProcessor.RequestMessage.uuid },
                { "device_id", _httpRequestProcessor.RequestMessage.device_id },
                {"fetch_reason", fetch_reason },
                {"token_hash", "" }
            };

            var request = HttpHelper.GetSignedRequest(HttpMethod.Get, url, _deviceInfo, fields);

            var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
            var json = await response.Content.ReadAsStringUnZipAsync();

            // save zr_token zr_expires
        }

        /// <summary>
        /// Set contact point prefill.
        /// </summary>
        /// <param name="usage">Either "prefill" or "auto_confirmation".</param>
        /// <returns></returns>
        private async Task SetContactPointPrefill(string usage = "prefill")
        {
            var url = new Uri("https://i.instagram.com/api/v1/accounts/contact_point_prefill/");

            var fields = new Dictionary<string, string>()
            {
                {"phone_id", _httpRequestProcessor.RequestMessage.phone_id},
                {"usage", usage},
                {"_csrftoken", _user.CsrfToken}
            };

            var request = HttpHelper.GetSignedRequest(HttpMethod.Post, url, _deviceInfo, fields);

            var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
            var json = await response.Content.ReadAsStringUnZipAsync();
        }

        #endregion

        #region SendLoginFlow

        private async Task SendLoginFlow(bool justLoggedIn)
        {
            // EVERY time the user presses their device's home button to leave the
            // app and then comes back to the app, Instagram does ALL of these things
            // to refresh its internal app state. We MUST emulate that perfectly,
            // otherwise Instagram will silently detect you as a "fake" client
            // after a while!
            //
            // You can configure the login's $appRefreshInterval in the function
            // parameter above, but you should keep it VERY frequent (definitely
            // NEVER longer than 6 hours), so that Instagram sees you as a real
            // client that keeps quitting and opening their app like a REAL user!
            //
            // Otherwise they WILL detect you as a bot and silently BLOCK features
            // or even ban you.
            //
            // You have been warned.

            if (justLoggedIn)
            {
                // Perform the "user has just done a full login" API flow.
                await SendLauncherSync(false);
                await SyncUserFeatures();
                await _feedProcessor.GetTimelineFeedAsync(new Dictionary<string, string>() {{"recovered_from_crash", "1"}});

                await _storyProcessor.GetReelsTrayFeed();

                // We must fetch new token here, because it updates rewrite rules.
                await FetchZeroRatingToken();

            }
        }

        /// <summary>
        /// Perform an Instagram "feature synchronization" call for account.
        /// </summary>
        /// <returns></returns>
        private async Task SyncUserFeatures()
        {
            var url = new Uri("https://i.instagram.com/api/v1/qe/sync/");

            var fields = new Dictionary<string, string>()
            {
                {"_uuid", _httpRequestProcessor.RequestMessage.uuid},
                {"_uid", _httpRequestProcessor.RequestMessage.account_id},
                {"_csrftoken", _user.CsrfToken},
                {"id", _httpRequestProcessor.RequestMessage.account_id},
                {"experiments", InstaApiConstants.EXPERIMENTS},
            };

            var request = HttpHelper.GetSignedRequest(HttpMethod.Post, url, _deviceInfo, fields);

            request.Headers.Add("X-DEVICE-ID", _httpRequestProcessor.RequestMessage.uuid);

            var response = await _httpRequestProcessor.SendAsyncWithoutDelay(request);
            var json = await response.Content.ReadAsStringUnZipAsync();

            // Save the updated experiments for this user.
            // TODO: $this->_saveExperiments($result);
            /*
                // Save the experiments and the last time we refreshed them.
                $this->ig->experiments = $this->ig->settings->setExperiments($experiments);
                $this->ig->settings->set('last_experiments', time());
            */
        }

        #endregion

        /// <summary>
        ///     2-Factor Authentication Login using a verification code
        ///     Before call this method, please run LoginAsync first.
        /// </summary>
        /// <param name="verificationCode">Verification Code sent to your phone number</param>
        /// <returns>
        ///     Success --> is succeed
        ///     InvalidCode --> The code is invalid
        ///     CodeExpired --> The code is expired, please request a new one.
        ///     Exception --> Something wrong happened
        /// </returns>
        public async Task<IResult<InstaLoginTwoFactorResult>> TwoFactorLoginAsync(string verificationCode)
        {
            if (_twoFactorInfo == null)
                return Result.Fail<InstaLoginTwoFactorResult>("Run LoginAsync first");

            try
            {
                var twoFactorRequestMessage = new ApiTwoFactorRequestMessage(verificationCode,
                    _httpRequestProcessor.RequestMessage.username,
                    _httpRequestProcessor.RequestMessage.device_id,
                    _twoFactorInfo.TwoFactorIdentifier);

                var instaUri = UriCreator.GetTwoFactorLoginUri();
                var signature =
                    $"{twoFactorRequestMessage.GenerateSignature(InstaApiConstants.IG_SIGNATURE_KEY)}.{twoFactorRequestMessage.GetMessageString()}";
                var fields = new Dictionary<string, string>
                {
                    {InstaApiConstants.HEADER_IG_SIGNATURE, signature},
                    {
                        InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION,
                        InstaApiConstants.IG_SIGNATURE_KEY_VERSION
                    }
                };
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, instaUri, _deviceInfo);
                request.Content = new FormUrlEncodedContent(fields);
                request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE, signature);
                request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION,
                    InstaApiConstants.IG_SIGNATURE_KEY_VERSION);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var loginInfo =
                        JsonConvert.DeserializeObject<InstaLoginResponse>(json);
                    IsUserAuthenticated = IsUserAuthenticated =
                        loginInfo.User != null && loginInfo.User.UserName.ToLower() == _user.UserName.ToLower();
                    var converter = ConvertersFabric.Instance.GetUserShortConverter(loginInfo.User);
                    _user.LoggedInUder = converter.Convert();
                    _user.RankToken = $"{_user.LoggedInUder.Pk}_{_httpRequestProcessor.RequestMessage.phone_id}";

                    return Result.Success(InstaLoginTwoFactorResult.Success);
                }

                var loginFailReason = JsonConvert.DeserializeObject<InstaLoginTwoFactorBaseResponse>(json);

                if (loginFailReason.ErrorType == "sms_code_validation_code_invalid")
                    return Result.Fail("Please check the security code.", InstaLoginTwoFactorResult.InvalidCode);
                return Result.Fail("This code is no longer valid, please, call LoginAsync again to request a new one",
                    InstaLoginTwoFactorResult.CodeExpired);
            }
            catch (Exception exception)
            {
                LogException(exception);
                return Result.Fail(exception, InstaLoginTwoFactorResult.Exception);
            }
        }

        /// <summary>
        ///     Get Two Factor Authentication details
        /// </summary>
        /// <returns>
        ///     An instance of TwoFactorInfo if success.
        ///     A null reference if not success; in this case, do LoginAsync first and check if Two Factor Authentication is
        ///     required, if not, don't run this method
        /// </returns>
        public async Task<IResult<TwoFactorLoginInfo>> GetTwoFactorInfoAsync()
        {
            return await Task.Run(() =>
                _twoFactorInfo != null
                    ? Result.Success(_twoFactorInfo)
                    : Result.Fail<TwoFactorLoginInfo>("No Two Factor info available."));
        }

        /// <summary>
        ///     Logout from instagram asynchronously
        /// </summary>
        /// <returns>
        ///     True if logged out without errors
        /// </returns>
        public async Task<IResult<bool>> LogoutAsync()
        {
            ValidateUser();
            ValidateLoggedIn();
            try
            {
                var instaUri = UriCreator.GetLogoutUri();
                var request = HttpHelper.GetDefaultRequest(HttpMethod.Get, instaUri, _deviceInfo);
                var response = await _httpRequestProcessor.SendAsync(request);
                var json = await response.Content.ReadAsStringUnZipAsync();
                if (response.StatusCode != HttpStatusCode.OK) return Result.UnExpectedResponse<bool>(response, json);
                var logoutInfo = JsonConvert.DeserializeObject<BaseStatusResponse>(json);
                if (logoutInfo.Status == "ok")
                    IsUserAuthenticated = false;
                return Result.Success(!IsUserAuthenticated);
            }
            catch (Exception exception)
            {
                LogException(exception);
                return Result.Fail(exception, false);
            }
        }

        /// <summary>
        ///     Get current state info as Memory stream
        /// </summary>
        /// <returns>
        ///     State data
        /// </returns>
        public Stream GetStateDataAsStream()
        {
            var state = new StateData
            {
                DeviceInfo = _deviceInfo,
                IsAuthenticated = IsUserAuthenticated,
                UserSession = _user,
                Cookies = _httpRequestProcessor.HttpHandler.CookieContainer
            };
            return SerializationHelper.SerializeToStream(state);
        }

        /// <summary>
        ///     Loads the state data from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void LoadStateDataFromStream(Stream stream)
        {
            var data = SerializationHelper.DeserializeFromStream<StateData>(stream);
            _deviceInfo = data.DeviceInfo;
            _user = data.UserSession;
            _httpRequestProcessor.HttpHandler.CookieContainer = data.Cookies;
            IsUserAuthenticated = data.IsAuthenticated;
            InvalidateProcessors();
        }

        #endregion


        #region private part

        private void InvalidateProcessors()
        {
            if (_hashtagProcessor == null)
                _hashtagProcessor = new HashtagProcessor(_deviceInfo, _user, _httpRequestProcessor, _logger);
            if (_locationProcessor == null)
                _locationProcessor = new LocationProcessor(_deviceInfo, _user, _httpRequestProcessor, _logger);
            if (_collectionProcessor == null)
                _collectionProcessor = new CollectionProcessor(_deviceInfo, _user, _httpRequestProcessor, _logger);
            if (_mediaProcessor == null)
                _mediaProcessor = new MediaProcessor(_deviceInfo, _user, _httpRequestProcessor, _logger);
            if (_userProcessor == null)
                _userProcessor = new UserProcessor(_deviceInfo, _user, _httpRequestProcessor, _logger);
            if (_storyProcessor == null)
                _storyProcessor = new StoryProcessor(_deviceInfo, _user, _httpRequestProcessor, _logger);
            if (_commentProcessor == null)
                _commentProcessor = new CommentProcessor(_deviceInfo, _user, _httpRequestProcessor, _logger);
            if (_profileProcessor == null)
                _profileProcessor = new UserProfileProcessor(_deviceInfo, _user, _httpRequestProcessor, _logger);
            if (_messagingProcessor == null)
                _messagingProcessor = new MessagingProcessor(_deviceInfo, _user, _httpRequestProcessor, _logger);
            if (_feedProcessor == null)
                _feedProcessor = new FeedProcessor(_deviceInfo, _user, _httpRequestProcessor, _logger);
        }

        private void ValidateUser()
        {
            if (string.IsNullOrEmpty(_user.UserName) || string.IsNullOrEmpty(_user.Password))
                throw new ArgumentException("user name and password must be specified");
        }

        private void ValidateLoggedIn()
        {
            if (!IsUserAuthenticated)
                throw new ArgumentException("user must be authenticated");
        }

        private void ValidateRequestMessage()
        {
            if (_httpRequestProcessor.RequestMessage == null || _httpRequestProcessor.RequestMessage.IsEmpty())
                throw new ArgumentException("API request message null or empty");
        }

        private void LogException(Exception exception)
        {
            _logger?.LogException(exception);
        }

        #endregion


        public InstaApiData GetData()
        {
            InvalidateProcessors();

            var list = new List<KeyValuePair<string, string>>();
            var cookies = _httpRequestProcessor.HttpHandler.CookieContainer.GetCookies(new Uri("https://i.instagram.com"));
            
            foreach (Cookie cookie in cookies)
            {
                list.Add(new KeyValuePair<string, string>(cookie.Domain, cookie.Name +"=" + cookie.Value));
            }

            return new InstaApiData()
            {
                User = _user,
                DeviceInfo = _deviceInfo,
                RequestMethod = _httpRequestProcessor.RequestMessage,
                CookieContainer = _httpRequestProcessor.HttpHandler.CookieContainer,
                Cookies = list
            };
        }
    }
}