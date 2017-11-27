using LunarParser;
using LunarParser.JSON;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace NeoLux.Core
{
    public enum RequestType
    {
        GET,
        POST
    }

    public static class RequestUtils
    {
        public static DataNode Request(RequestType kind, string url, DataNode data = null)
        {
            string contents;

            switch (kind) {
                case RequestType.GET: contents = GetWebRequest(url); break;
                case RequestType.POST:
                    {
                        var paramData = data != null ? JSONWriter.WriteToString(data): "{}";
                        contents = PostWebRequest(url, paramData);
                        break;
                    }
                default: return null;
            }


            File.WriteAllText("response.json", contents);

            var root = JSONReader.ReadFromString(contents);
            return root;
        }

        /*private static string ExecuteRequest(WebRequest webReq)
        {
            try
            {
                using (WebResponse response = webReq.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (WebException wex)
            {
                using (HttpWebResponse response = (HttpWebResponse)wex.Response)
                {
                    Stream str = response.GetResponseStream();
                    if (str == null)
                        throw;

                    using (StreamReader sr = new StreamReader(str))
                    {
                        if (response.StatusCode != HttpStatusCode.InternalServerError)
                            throw;
                        return sr.ReadToEnd();
                    }
                }
            }
        }*/

        public static string GetWebRequest(string url)
        {
            using (var  client = new WebClient { Encoding = System.Text.Encoding.UTF8 })
            {
                return client.DownloadString(url);
            }
        }

        // var r = PostWebRequest("http://seed2.antshares.org:10332", "{'jsonrpc': '2.0', 'method': 'getblockcount', 'params': [],  'id': 1}");

        public static string PostWebRequest(string url, string paramData)
        {
            using (var client = new WebClient { Encoding = System.Text.Encoding.UTF8 })
            {
                return client.UploadString(url, paramData);
            }
        }
    }
}
