using Newtonsoft.Json;

namespace InstaSharperDirect.Classes.ResponseWrappers
{

    /// <summary>
    ///   {
    ///     "step_name": "verify_email",
    ///     "step_data": {
    ///         "security_code": "None",
    ///         "resend_delay": 60,
    ///         "contact_point": "c*******6@y*****.com",
    ///         "form_type": "email"
    ///     },
    ///     "user_id": 7315612197,
    ///     "nonce_code": "YzU6HA0uFQ",
    ///     "status": "ok"
    /// }
    /// </summary>
    public class RequireChallengeCodeResponse
    {
        [JsonProperty("step_name")]
        public string StepName { get; set; }

        [JsonProperty("step_data")]
        public RequireChallengeCodeResponseStepData StepData { get; set; }
        
        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("nonce_code")]
        public string NonceCode { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class RequireChallengeCodeResponseStepData
    {
        [JsonProperty("security_code")]
        public string SecurityCode { get; set; }

        [JsonProperty("resend_delay")]
        public string ResendDelay { get; set; }

        [JsonProperty("contact_point")]
        public string ContactPoint { get; set; }

        [JsonProperty("form_type")]
        public string FormType { get; set; }
    }
}
