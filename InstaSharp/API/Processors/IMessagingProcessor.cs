using System.Collections.Generic;
using System.Threading.Tasks;
using InstaSharper.Classes;
using InstaSharper.Classes.Models;
using InstaSharper.Classes.ResponseWrappers.BaseResponse;

namespace InstaSharper.API.Processors
{
    public interface IMessagingProcessor
    {
        Task<IResult<InstaDirectInboxContainer>> GetDirectInboxAsync();
        Task<IResult<InstaDirectInboxThread>> GetDirectInboxThreadAsync(string threadId);

        Task<IResult<InstaDirectInboxThreadList>> SendDirectMessage(string recipients, string threadIds, string text);

        Task<IResult<InstaRecipients>> GetRecentRecipientsAsync();
        Task<IResult<InstaRecipients>> GetRankedRecipientsAsync();

        Task<IResult<BaseStatusResponse>> DeclineAllPendingDirectThreads();
        Task<IResult<BaseStatusResponse>> ApprovePendingDirectThread(string threadId);

        Task<IResult<InstaDirectInboxContainer>> GetPendingDirectInboxAsync();
        Task<IResult<BaseStatusResponse>> ApprovePendingDirectThreads(List<string> threadIds);
    }
}