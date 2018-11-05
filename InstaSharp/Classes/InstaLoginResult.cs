namespace InstaSharper.Classes
{
    public enum InstaLoginStatus
    {
        Success,
        BadPassword,
        InvalidUser,
        TwoFactorRequired,
        Exception,
        ChallengeRequired
    }

    public class InstaLoginResult
    {
        public InstaLoginStatus Status { get; private set; }
        public IInstaLoginResultError Error { get; set; }

        public InstaLoginResult(InstaLoginStatus status)
        {
            Status = status;
        }
    }

    public interface IInstaLoginResultError
    {

    }
}