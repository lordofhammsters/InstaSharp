using Newtonsoft.Json;

namespace InstaSharper.Classes
{
    internal class InstaLoginBaseResponse
    {
        #region InvalidCredentials

        [JsonProperty("invalid_credentials")]
        public bool InvalidCredentials { get; set; }

        [JsonProperty("error_type")]
        public string ErrorType { get; set; }

        #endregion

        #region 2 Factor Authentication

        [JsonProperty("two_factor_required")]
        public bool TwoFactorRequired { get; set; }

        [JsonProperty("two_factor_info")]
        public TwoFactorLoginInfo TwoFactorLoginInfo { get; set; }

        #endregion
    }


    public class ChallengeRequiredResponse : IInstaLoginResultError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("challenge")]
        public ChallengeRequiredResponseChallenge Challenge { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("error_type")]
        public string ErrorType { get; set; }

        public string Json { get; set; }
    }

    public class ChallengeRequiredResponseChallenge
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("api_path")]
        public string ApiPath { get; set; }

        [JsonProperty("hide_webview_header")]
        public bool HideWebviewHeader { get; set; }

        [JsonProperty("lock")]
        public bool Lock { get; set; }

        [JsonProperty("logout")]
        public bool Logout { get; set; }

        [JsonProperty("native_flow")]
        public bool NativeFlow { get; set; }


    }
}