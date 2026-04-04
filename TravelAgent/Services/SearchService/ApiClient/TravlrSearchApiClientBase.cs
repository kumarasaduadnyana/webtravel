namespace Travlr.Search.Client
{
    /// <summary>
    /// Base class for TravlrSearchApiClient.
    /// Injects the API key into every outgoing request via the X-Api-Key header.
    /// </summary>
    public abstract class TravlrSearchApiClientBase
    {
        private string? _apiKey;

        /// <summary>Call this after construction to set the API key.</summary>
        public void SetApiKey(string apiKey) => _apiKey = apiKey;

        protected virtual System.Threading.Tasks.Task<System.Net.Http.HttpRequestMessage>
            CreateHttpRequestMessageAsync(System.Threading.CancellationToken cancellationToken)
        {
            var message = new System.Net.Http.HttpRequestMessage();
            if (!string.IsNullOrEmpty(_apiKey))
                message.Headers.Add("X-Api-Key", _apiKey);
            return System.Threading.Tasks.Task.FromResult(message);
        }
    }
}
