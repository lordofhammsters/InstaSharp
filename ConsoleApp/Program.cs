using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ConsoleApp
{
    class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(MainAsync);

            Console.WriteLine("Hello World!");
            Console.ReadKey();
        }

        public static async Task MainAsync()
        {
            try
            {
                var dataFromDbStr = "{\"User\":{\"UserName\":\"mikeadv08\",\"Password\":\"123qweasd\",\"LoggedInUder\":{\"IsVerified\":false,\"IsPrivate\":false,\"Pk\":7315612197,\"ProfilePicture\":\"https://instagram.fbhz2-1.fna.fbcdn.net/vp/74d4a001973ffb1c519909dc584b0316/5C328D7A/t51.2885-19/11906329_960233084022564_1448528159_a.jpg\",\"ProfilePictureId\":\"unknown\",\"UserName\":\"mikeadv08\",\"FullName\":\"mikeadv08\"},\"RankToken\":\"7315612197_9b92278f-77e1-4068-aa46-266e6a04fb30\",\"CsrfToken\":\"OeIFwmNkFvrtGWAvMLgvLRxVhcch8I4N\",\"LastLoginTime\":\"2018-09-14T15:13:01.5487324+04:00\"},\"DeviceInfo\":{\"PhoneGuid\":\"00000000-0000-0000-0000-000000000000\",\"DeviceGuid\":\"00000000-0000-0000-0000-000000000000\",\"GoogleAdId\":\"9f64c4c9-3d14-4ffa-8df0-0ec13d5ed4d4\",\"RankToken\":\"206e2698-19d9-47e9-ad69-6db54a7b17cc\",\"AndroidBoardName\":null,\"AndroidBootloader\":null,\"DeviceBrand\":null,\"DeviceId\":null,\"DeviceModel\":null,\"DeviceModelBoot\":null,\"DeviceModelIdentifier\":null,\"FirmwareBrand\":null,\"FirmwareFingerprint\":null,\"FirmwareTags\":null,\"FirmwareType\":null,\"HardwareManufacturer\":null,\"HardwareModel\":null,\"AndroidVersion\":\"24\",\"AndroidRelease\":\"7.0\",\"Dpi\":\"380dpi\",\"Resolution\":\"1080x1920\",\"Manufacturer\":\"OnePlus\",\"Brand\":null,\"Model\":\"ONEPLUS A3010\",\"Device\":\"OnePlus3T\",\"Cpu\":\"qcom\",\"UserAgent\":\"Instagram 42.0.0.19.95 Android (24/7.0; 380dpi; 1080x1920; OnePlus; ONEPLUS A3010; OnePlus3T; qcom; en_US; 104766893)\"},\"RequestMethod\":{\"device_id\":\"android-88ac32f01e3d42c3\",\"phone_id\":\"9b92278f-77e1-4068-aa46-266e6a04fb30\",\"uuid\":\"e77ac48d-cf34-4295-bddd-055b6725d3eb\",\"advertising_id\":\"a4068a61-7826-40ca-ac5e-1ae08bd318dc\",\"session_id\":\"081f24d3-4672-465b-8a98-0c6cb311edb5\",\"username\":\"mikeadv08\",\"guid\":\"00000000-0000-0000-0000-000000000000\",\"account_id\":\"7315612197\",\"password\":\"123qweasd\",\"login_attempt_count\":\"0\"},\"CookieContainer\":{\"Capacity\":300,\"Count\":12,\"MaxCookieSize\":4096,\"PerDomainCapacity\":20},\"Cookies\":[{\"Key\":\"i.instagram.com\",\"Value\":\"csrftoken=ISJPfx2IE2lKsOqVWM8k43cMkd2XtnJY\"},{\"Key\":\".instagram.com\",\"Value\":\"rur=FTW\"},{\"Key\":\".instagram.com\",\"Value\":\"mid=W5uXvAABAAFQgoTB6kUD-bfVFIVc\"},{\"Key\":\".instagram.com\",\"Value\":\"mcd=3\"},{\"Key\":\".instagram.com\",\"Value\":\"ds_user=mikeadv08\"},{\"Key\":\".instagram.com\",\"Value\":\"shbid=17793\"},{\"Key\":\".instagram.com\",\"Value\":\"shbts=1536923584.0798094\"},{\"Key\":\".instagram.com\",\"Value\":\"ds_user_id=7315612197\"},{\"Key\":\".instagram.com\",\"Value\":\"sessionid=IGSCd326c78fb0898c1c1da354547f9301b13b1e8622e9407554efb1d0e0fda4521e%3ASbgxl5M5677gmkYaGp0IzUgZwlAYItcl%3A%7B%22_auth_user_id%22%3A7315612197%2C%22_auth_user_backend%22%3A%22accounts.backends.CaseInsensitiveModelBackend%22%2C%22_auth_user_hash%22%3A%22%22%2C%22_platform%22%3A1%2C%22_token_ver%22%3A2%2C%22_token%22%3A%227315612197%3AfJS4j24NPosiXdXZz7aXzxea8HzgKA7q%3A5c1c325b84733b7441583ef0671824dd84cfca6997a9ba2d924f0a6dcab488d4%22%2C%22last_refreshed%22%3A1536923584.0811641216%7D\"},{\"Key\":\".instagram.com\",\"Value\":\"is_starred_enabled=yes\"},{\"Key\":\".instagram.com\",\"Value\":\"igfl=mikeadv08\"},{\"Key\":\".instagram.com\",\"Value\":\"urlgen=\\\"{\\\\\\\"91.239.16.66\\\\\\\": 50716}:1g0m2D:JHjmhAbBi8FrDqWLtIkuelY3s5Q\\\"\"}]}";
                var apiData = JsonConvert.DeserializeObject<InstaApiData>(dataFromDbStr) ?? new InstaApiData();

                var user = apiData.User != null ? apiData.User : new UserSessionData() { UserName = "", Password = "" };

                var api =
                    InstaApiBuilder.CreateBuilder()
                        .SetUser(user)
                        .SetDevice(apiData.DeviceInfo)
                        .SetApiRequestMessage(apiData.RequestMethod)
                        .SetCookie(apiData.CookieContainer, apiData.Cookies)
                        .SetRequestDelay(RequestDelay.FromSeconds(5, 5))
                        .Build();

                var login = await api.LoginAsync();
                if (!login.Succeeded)
                    throw new Exception(login.Info.Message);

                // save data
                var data = api.GetData();

                //var strData = JsonConvert.SerializeObject(data);

                //var a = api.GetStateDataAsStream();

                //var login2 = await api.LoginAsync();
                //if (!login2.Succeeded)
                //    throw new Exception(login2.Info.Message);

                var directInbox = await api.GetDirectInboxAsync();
                if (!directInbox.Succeeded)
                    throw new Exception(directInbox.Info.Message);

                var threads = directInbox.Value.Inbox.Threads;

                foreach (var thread in threads.Where(x => !x.IsSpam))
                {
                    var th = await api.GetDirectInboxThreadAsync(thread.ThreadId);
                    if (!th.Succeeded)
                        continue;

                    foreach (var message in th.Value.Items)
                    {
                        Console.WriteLine(message.Text);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }


            /*
            //var user = await api.GetCurrentUserAsync();
            //if (!user.Succeeded)
            //    throw new Exception(user.Info.Message);

            var directInbox = await api.GetDirectInboxAsync();
            if (!directInbox.Succeeded)
                throw new Exception(directInbox.Info.Message);

            //var threads = directInbox.Value.Inbox.Threads;

            //if (threads == null || threads.Count == 0)
            //    return;

            //foreach (var thread in threads.Where(x => !x.IsSpam))
            //{
            //    var th = await api.GetDirectInboxThreadAsync(thread.ThreadId);
            //    if (!th.Succeeded)
            //        continue;

            //    foreach (var message in th.Value.Items)
            //    {
            //        Console.WriteLine(message.Text);
            //    }
            //}

            //var medias = await api.GetUserMediaByPkAsync("pk", 1);
            //if (!medias.Succeeded)
            //    return;

            //var result = await api.SendDirectMessage("6157611363", null, "test123");
            //if (result.Succeeded)
            //{
            //    var thread = result.Value[0];

            //    //thread.ThreadId
            //    //thread.Title
            //    //thread.Items[0].ItemId
            //}
            */
        }
    }

    //public class NonPublicPropertiesResolver : DefaultContractResolver
    //{
    //    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    //    {
    //        var prop = base.CreateProperty(member, memberSerialization);
    //        if (member is PropertyInfo pi)
    //        {
    //            prop.Readable = (pi.GetMethod != null);
    //            prop.Writable = (pi.SetMethod != null);
    //        }
    //        return prop;
    //    }
    //}
}
