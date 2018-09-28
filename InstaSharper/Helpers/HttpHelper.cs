using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using InstaSharper.API;
using InstaSharper.Classes.Android.DeviceInfo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstaSharper.Helpers
{
    internal class HttpHelper
    {
        public static HttpRequestMessage GetDefaultRequest(HttpMethod method, Uri uri, AndroidDevice deviceInfo)
        {
            var request = new HttpRequestMessage(method, uri);

            var random = new Random();

            request.Headers.Add("X-IG-App-ID", InstaApiConstants.FACEBOOK_ANALYTICS_APPLICATION_ID);
            request.Headers.Add("X-IG-Capabilities", InstaApiConstants.IG_CAPABILITIES);
            request.Headers.Add("X-IG-Connection-Type", InstaApiConstants.IG_CONNECTION_TYPE);
            request.Headers.Add("X-IG-Connection-Speed", random.Next(1000, 3700) + "kbps");

            request.Headers.Add("X-IG-Bandwidth-Speed-KBPS", "-1.000");
            request.Headers.Add("X-IG-Bandwidth-TotalBytes-B", "0");
            request.Headers.Add("X-IG-Bandwidth-TotalTime-MS", "0");


            request.Headers.Add("User-Agent", deviceInfo.UserAgent);

            request.Headers.Add("Connection", "Keep-Alive"); // ?
            request.Headers.Add("X-FB-HTTP-Engine", "Liger");
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Accept-Encoding", "gzip,deflate");
            request.Headers.Add("Accept-Language", InstaApiConstants.ACCEPT_LANGUAGE);
            
            return request;
        }

        public static HttpRequestMessage GetSignedRequest(HttpMethod method, Uri uri, AndroidDevice deviceInfo, Dictionary<string, string> data)
        {
            var hash = CryptoHelper.CalculateHash(InstaApiConstants.IG_SIGNATURE_KEY, JsonConvert.SerializeObject(data));
            var payload = JsonConvert.SerializeObject(data);
            var signature = $"{hash}.{payload}";

            var fields = new Dictionary<string, string>
            {
                {InstaApiConstants.HEADER_IG_SIGNATURE, signature},
                {InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION, InstaApiConstants.IG_SIGNATURE_KEY_VERSION}
            };
            var request = GetDefaultRequest(HttpMethod.Post, uri, deviceInfo);
            request.Content = new FormUrlEncodedContent(fields);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE, signature);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION, InstaApiConstants.IG_SIGNATURE_KEY_VERSION);
            return request;
        }

        public static HttpRequestMessage GetSignedRequest(HttpMethod method, Uri uri, AndroidDevice deviceInfo, JObject data)
        {
            var hash = CryptoHelper.CalculateHash(InstaApiConstants.IG_SIGNATURE_KEY, data.ToString(Formatting.None));
            var payload = data.ToString(Formatting.None);
            var signature = $"{hash}.{payload}";

            var fields = new Dictionary<string, string>
            {
                {InstaApiConstants.HEADER_IG_SIGNATURE, signature},
                {InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION, InstaApiConstants.IG_SIGNATURE_KEY_VERSION}
            };
            var request = GetDefaultRequest(HttpMethod.Post, uri, deviceInfo);
            request.Content = new FormUrlEncodedContent(fields);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE, signature);
            request.Properties.Add(InstaApiConstants.HEADER_IG_SIGNATURE_KEY_VERSION, InstaApiConstants.IG_SIGNATURE_KEY_VERSION);
            return request;
        }

        public static HttpRequestMessage GetDefaultRequest(HttpMethod method, Uri uri, AndroidDevice deviceInfo, Dictionary<string, string> fields)
        {
            var request = GetDefaultRequest(method, uri, deviceInfo);

            if (fields != null)
                request.Content = new FormUrlEncodedContent(fields);

            return request;
        }
    }
}