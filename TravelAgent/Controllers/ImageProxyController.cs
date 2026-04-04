using Microsoft.AspNetCore.Mvc;

namespace TravelAgent.Controllers
{
    [ApiController]
    [Route("proxy")]
    public class ImageProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ImageProxyController> _logger;

        // Only proxy images from trusted travel image CDNs
        private static readonly HashSet<string> _allowedHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "i.travelapi.com",
            "images.trvl-media.com",
            "media.travlr.com",
            "cdn.travlr.com"
        };

        public ImageProxyController(IHttpClientFactory httpClientFactory, ILogger<ImageProxyController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet("image")]
        [ResponseCache(Duration = 86400)] // cache for 24 h
        public async Task<IActionResult> Get([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("Missing url parameter");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http"))
                return BadRequest("Invalid url");

            if (!_allowedHosts.Contains(uri.Host))
            {
                _logger.LogWarning("Blocked proxy request for disallowed host: {Host}", uri.Host);
                return Forbid();
            }

            try
            {
                var http     = _httpClientFactory.CreateClient("ImageProxy");
                var response = await http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode);

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                var bytes       = await response.Content.ReadAsByteArrayAsync();

                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image proxy failed for {Url}", url);
                return StatusCode(502);
            }
        }
    }
}
