using System;
using InstaSharper.Classes.Models;
using InstaSharper.Classes.ResponseWrappers;
using InstaSharper.Helpers;

namespace InstaSharper.Converters
{
    internal class InstaDirectThreadItemConverter : IObjectConverter<InstaDirectInboxItem, InstaDirectInboxItemResponse>
    {
        public InstaDirectInboxItemResponse SourceObject { get; set; }

        public InstaDirectInboxItem Convert()
        {
            Guid clientContext;

            if (!Guid.TryParse(SourceObject.ClientContext, out clientContext))
                clientContext = Guid.Empty;

            var threadItem = new InstaDirectInboxItem
            {
                ClientContext = clientContext,
                ItemId = SourceObject.ItemId
            };

            threadItem.TimeStamp = DateTimeHelper.UnixTimestampMilisecondsToDateTime(SourceObject.TimeStamp);
            threadItem.UserId = SourceObject.UserId;

            var truncatedItemType = SourceObject.ItemType.Trim().Replace("_", "");
            InstaDirectThreadItemType type;
            if (Enum.TryParse(truncatedItemType, true, out type))
                threadItem.ItemType = type;

            if (threadItem.ItemType == InstaDirectThreadItemType.Link)
            {
                threadItem.Text = SourceObject.Link?.LinkContext?.LinkUrl;
            }
            else if (threadItem.ItemType == InstaDirectThreadItemType.Like)
            {
                threadItem.Text = SourceObject.Like;
            }
            else if (threadItem.ItemType == InstaDirectThreadItemType.Media
                     && SourceObject.Media != null)
            {
                var converter = ConvertersFabric.Instance.GetInboxMediaConverter(SourceObject.Media);
                threadItem.Media = converter.Convert();
            }
            else if (threadItem.ItemType == InstaDirectThreadItemType.MediaShare
                     && SourceObject.MediaShare != null)
            {
                var converter = ConvertersFabric.Instance.GetSingleMediaConverter(SourceObject.MediaShare);
                threadItem.MediaShare = converter.Convert();
            }
            else
            {
                threadItem.Text = SourceObject.Text;
            }

            return threadItem;
        }
    }
}