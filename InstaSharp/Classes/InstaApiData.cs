using System.Collections.Generic;
using System.Net;
using InstaSharper.Classes.Android.DeviceInfo;

namespace InstaSharper.Classes
{
    public class InstaApiData
    {
        public UserSessionData User { get; set; }
        public AndroidDevice DeviceInfo { get; set; }
        public ApiRequestMessage RequestMethod { get; set; }
        public CookieContainer CookieContainer { get; set; }
        public List<KeyValuePair<string, string>> Cookies { get; set; }
    }
}
