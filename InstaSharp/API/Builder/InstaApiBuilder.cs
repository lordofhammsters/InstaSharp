using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using InstaSharper.Classes;
using InstaSharper.Classes.Android.DeviceInfo;
using InstaSharper.Logger;

namespace InstaSharper.API.Builder
{
    public class InstaApiBuilder : IInstaApiBuilder
    {
        private IRequestDelay _delay = RequestDelay.Empty();
        private AndroidDevice _device;
        private HttpClient _httpClient;
        private HttpClientHandler _httpHandler = new HttpClientHandler();
        private IHttpRequestProcessor _httpRequestProcessor;
        private IInstaLogger _logger;
        private ApiRequestMessage _requestMessage;
        private UserSessionData _user;

        private CookieContainer _cookieContainer;
        private List<KeyValuePair<string, string>> _cookies;

        private InstaApiBuilder()
        {
        }

        /// <summary>
        ///     Create new API instance
        /// </summary>
        /// <returns>
        ///     API instance
        /// </returns>
        /// <exception cref="ArgumentNullException">User auth data must be specified</exception>
        public IInstaApi Build()
        {
            if (_user == null)
                throw new ArgumentNullException("User auth data must be specified");

            if (_httpClient == null)
                _httpClient = new HttpClient(_httpHandler) {BaseAddress = new Uri(InstaApiConstants.INSTAGRAM_URL)};

            if (_device == null)
                _device = AndroidDeviceGenerator.GetRandomGoodDevice();

            if (_requestMessage == null)
            {
                _requestMessage = new ApiRequestMessage
                {
                    device_id = ApiRequestMessage.GenerateDeviceId(),
                    phone_id = Guid.NewGuid().ToString(),
                    uuid = Guid.NewGuid().ToString(), // "1365863f-bb91-4e9b-9c32-d3d6e5fda31f",
                    advertising_id = Guid.NewGuid().ToString(),
                    session_id = Guid.NewGuid().ToString()
                };
            }

            if (string.IsNullOrEmpty(_requestMessage.password))
                _requestMessage.password = _user?.Password;

            if (string.IsNullOrEmpty(_requestMessage.username))
                _requestMessage.username = _user?.UserName;

            if (_cookieContainer != null)
                _httpHandler.CookieContainer = _cookieContainer;

            if (_cookies != null)
            {
                var str = "";
                foreach (var cookie in _cookies)
                {
                    str += (str == "" ? "" : ",") + cookie.Value;
                }
                _httpHandler.CookieContainer.SetCookies(new Uri("https://i.instagram.com"), str);
            }

            if (_httpRequestProcessor == null)
                _httpRequestProcessor = new HttpRequestProcessor(_delay, _httpClient, _httpHandler, _requestMessage, _logger);
            
            var instaApi = new InstaApi(_user, _logger, _device, _httpRequestProcessor);
            return instaApi;
        }

        /// <summary>
        ///     Use custom logger
        /// </summary>
        /// <param name="logger">IInstaLogger implementation</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder UseLogger(IInstaLogger logger)
        {
            _logger = logger;
            return this;
        }

        /// <summary>
        ///     Set specific HttpClient
        /// </summary>
        /// <param name="httpClient">HttpClient</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder UseHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            return this;
        }

        /// <summary>
        ///     Set custom HttpClientHandler to be able to use certain features, e.g Proxy and so on
        /// </summary>
        /// <param name="handler">HttpClientHandler</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder UseHttpClientHandler(HttpClientHandler handler)
        {
            _httpHandler = handler;
            return this;
        }

        /// <summary>
        ///     Specify user login, password from here
        /// </summary>
        /// <param name="user">User auth data</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder SetUser(UserSessionData user)
        {
            _user = user;
            return this;
        }
        
        /// <summary>
        ///     Set custom request message. Used to be able to customize device info.
        /// </summary>
        /// <param name="requestMessage">Custom request message object</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        /// <remarks>
        ///     Please, do not use if you don't know what you are doing
        /// </remarks>
        public IInstaApiBuilder SetApiRequestMessage(ApiRequestMessage requestMessage)
        {
            _requestMessage = requestMessage;
            return this;
        }

        /// <summary>
        ///     Set delay between requests. Useful when API supposed to be used for mass-bombing.
        /// </summary>
        /// <param name="delay">Timespan delay</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder SetRequestDelay(IRequestDelay delay)
        {
            _delay = delay;
            return this;
        }

        public IInstaApiBuilder SetDevice(AndroidDevice device)
        {
            _device = device;
            return this;
        }

        public IInstaApiBuilder SetCookie(CookieContainer cookieContainer, List<KeyValuePair<string, string>> cookies)
        {
            _cookieContainer = cookieContainer;
            _cookies = cookies;
            return this;
        }

        /// <summary>
        ///     Creates the builder.
        /// </summary>
        /// <returns></returns>
        public static IInstaApiBuilder CreateBuilder()
        {
            return new InstaApiBuilder();
        }
    }
}