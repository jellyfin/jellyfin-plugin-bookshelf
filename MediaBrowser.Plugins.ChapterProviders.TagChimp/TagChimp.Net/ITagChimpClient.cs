using System.Configuration;
using System;
using System.Net;
using System.IO;

namespace TagChimp
{
    public interface ITagChimpClient
    {
        SearchResults Search(SearchParameters paramaters);
    }

    public class TagChimpClient : ITagChimpClient
    {
        public SearchResults Search(SearchParameters parameters)
        {
            string baseUrl = "https://www.tagchimp.com/ape/search.php";
            string url = baseUrl + parameters.BuildQueryString();
            string xml = MakeRequest(new Uri(url));
            var results = SerializationHelper.DeserializeXml<SearchResults>(xml);
            if(results.TotalResults == 0 && !String.IsNullOrEmpty(results.Message.Error))
            {
                throw new TagChimpException(results.Message.Error, url, xml);
            }
            return results;
        }

        public string MakeRequest(Uri url)
        {
            var request = WebRequest.Create(url) as HttpWebRequest;
            if (request != null)
            {
                try
                {
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        if (response != null)
                        {
                            var reader = new StreamReader(response.GetResponseStream());
                            return reader.ReadToEnd();
                        }
                    }
                }
                catch (WebException e)
                {
                    Console.WriteLine(e);
                }
            }
            return String.Empty;
        }
    }
}