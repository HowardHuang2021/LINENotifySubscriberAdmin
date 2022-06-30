using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json.Serialization;
using System.Text.Json;
using LINENotifySubscriberAdmin.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace LINENotifySubscriberAdmin.Controllers
{
    [Route("[controller]")]
    public class LINENotifyController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SubscriberContext _context;
        public LINENotifyController(IConfiguration configuration, IHttpClientFactory httpClientFactory, SubscriberContext context)
        {
            _config = configuration;
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        [HttpGet("authorize")]
        public IActionResult Authorize()
        {
            const string authorizeEndpoint = "https://notify-bot.line.me/oauth/authorize";

            var parameters = new Dictionary<string, string>()
            {
                ["response_type"] = "code",
                ["client_id"] = _config["Authentication:LineNotify:ClientId"],
                ["redirect_uri"] = this.Url.ActionLink(nameof(BindCallback)),
                ["scope"] = "notify",
                ["state"] = "abcd"
            };

            var requestUri = QueryHelpers.AddQueryString(authorizeEndpoint, parameters);

            return Redirect(requestUri);
        }

        [HttpGet("signin-line-notify")]
        public async Task<IActionResult> BindCallback(string code, string state)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest();
            }

            var keyValuePairs = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "authorization_code"),
                new("code", code),
                new("redirect_uri", this.Url.ActionLink(nameof(BindCallback))),
                new("client_id", _config["Authentication:LineNotify:ClientId"]),
                new("client_secret", _config["Authentication:LineNotify:ClientSecret"])
            };

            using var client = _httpClientFactory.CreateClient();

            const string tokenEndpoint = "https://notify-bot.line.me/oauth/token";
            var encodedContent = new FormUrlEncodedContent(keyValuePairs);

            using var httpResponseMessage = await client.PostAsync(tokenEndpoint, encodedContent);

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                string message = await httpResponseMessage.Content.ReadAsStringAsync();
                throw new Exception(message);
            }

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var tokenResponse = await httpResponseMessage.Content.ReadFromJsonAsync<TokenResponse>(jsonSerializerOptions);

            if (tokenResponse == null)
            {
                throw new Exception("token content failed");
            }

            var subscriber = new Subscriber()
            {
                Username = "test",
                AccessToken = tokenResponse.AccessToken,
            };
            await _context.Subscribers.AddAsync(subscriber);
            await _context.SaveChangesAsync();
            //return CreatedAtAction(nameof(GetSubscriberById),
            //    new { id = subscriber.Id },
            //    subscriber
            //    );
            return Ok();
        }

        [HttpPost("notify")]
        public async Task<IActionResult> Notify(string message)
        {
            var input = _context.Subscribers.Where(a => a.Username == "test").FirstOrDefault();
            if (input == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(input.AccessToken) || string.IsNullOrWhiteSpace(message))
            {
                return BadRequest();
            }

            using var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", input.AccessToken);

            var keyValuePairs = new List<KeyValuePair<string?, string?>>
            {
                new("message", message)
            };

            const string notifyEndpoint = "https://notify-api.line.me/api/notify";
            var encodedContent = new FormUrlEncodedContent(keyValuePairs);

            using var httpResponseMessage = await client.PostAsync(notifyEndpoint, encodedContent);

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                string responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
                throw new Exception(responseContent);
            }

            return Ok();
        }

        [HttpGet("{id}", Name = "GetSubscriberById")]
        public async Task<IActionResult> GetSubscriberById(int id)
        {
            var sub = await _context.Subscribers.FindAsync(id);
            if (sub == null)
            {
                return NotFound();
            }
            return Ok(sub);
        }
    }

    public class TokenResponse
    {
        public int Status { get; set; }

        public string Message { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
}
