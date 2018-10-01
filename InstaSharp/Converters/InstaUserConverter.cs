﻿using System;
using InstaSharper.Classes.Models;
using InstaSharper.Classes.ResponseWrappers;
using InstaSharper.Converters;

namespace InstaSharperDirect.Converters
{
    internal class InstaUserConverter : IObjectConverter<InstaUser, InstaUserResponse>
    {
        public InstaUserResponse SourceObject { get; set; }

        public InstaUser Convert()
        {
            if (SourceObject == null) throw new ArgumentNullException($"Source object");
            var shortConverter = ConvertersFabric.Instance.GetUserShortConverter(SourceObject);
            var user = new InstaUser(shortConverter.Convert())
            {
                HasAnonymousProfilePicture = SourceObject.HasAnonymousProfilePicture,
                FollowersCount = SourceObject.FollowersCount,
                FollowersCountByLine = SourceObject.FollowersCountByLine,
                SearchSocialContext = SourceObject.SearchSocialContext,
                SocialContext = SourceObject.SocialContext
            };

            double mutualFollowers;
            if (double.TryParse(SourceObject.MulualFollowersCount, out mutualFollowers))
                user.MutualFollowers = System.Convert.ToInt16(mutualFollowers);

            if (SourceObject.FriendshipStatus != null)
            {
                var freindShipStatusConverter =
                    ConvertersFabric.Instance.GetFriendShipStatusConverter(SourceObject.FriendshipStatus);
                user.FriendshipStatus = freindShipStatusConverter.Convert();
            }

            return user;
        }
    }
}