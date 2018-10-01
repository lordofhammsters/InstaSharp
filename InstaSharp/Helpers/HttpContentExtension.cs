using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace InstaSharper.Helpers
{
    public static class HttpContentExtension
    {
        public static async Task<string> ReadAsStringUtf8Async(this HttpContent content)
        {
            return await content.ReadAsStringUnZipAsync(Encoding.UTF8);
        }

        public static async Task<string> ReadAsStringUnZipAsync(this HttpContent content, Encoding encoding)
        {
            using (var reader = new StreamReader((await content.ReadAsStreamAsync()), encoding))
            {
                return reader.ReadToEnd();
            }
        }

        // https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp/35580409
        public static async Task<string> ReadAsStringUnZipAsync(this HttpContent content)
        {
            try
            {
                if (content.Headers != null && content.Headers.ContentEncoding != null &&
                    content.Headers.ContentEncoding.Contains("gzip"))
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var stream = new GZipStream(await content.ReadAsStreamAsync(), CompressionMode.Decompress))
                            stream.CopyTo(ms);

                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                var a = ex.Message;
            }

            return await content.ReadAsStringAsync();
        }
    }
}
