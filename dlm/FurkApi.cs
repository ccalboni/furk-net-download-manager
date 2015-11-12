using Newtonsoft.Json;
using RestSharp;

namespace dlm
{
    internal class FurkApi
    {
        private string _apiKey;
        private string _apiEndpoint;

        public FurkApi(string apiKey, string apiEndpoint)
        {
            this._apiKey = apiKey;
            this._apiEndpoint = apiEndpoint;
        }

        internal dynamic GetTorrentDetails(string infoHash)
        {
            var request = GetBaseRestRequest("file/get");
            request.AddParameter("t_files", 1);
            request.AddParameter("info_hash", infoHash);
            return DeserializeResponseContent(request);

        }

        internal dynamic GetReadyTorrents()
        {
            var request = GetBaseRestRequest("file/get");
            return DeserializeResponseContent(request);
        }

        private RestRequest GetBaseRestRequest(string requestUri)
        {
            var request = new RestRequest(requestUri, Method.GET);
            request.AddParameter("api_key", _apiKey);
            request.AddParameter("pretty", 1);
            return request;
        }

        private dynamic DeserializeResponseContent(RestRequest request)
        {
            var client = new RestClient(_apiEndpoint);
            IRestResponse response = client.Execute(request);
            var content = response.Content;
            dynamic deserializedResponse = JsonConvert.DeserializeObject(content);
            return deserializedResponse;
        }
    }
}
