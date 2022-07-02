using LINENotifySubscriberAdmin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LINENotifySubscriberAdmin.Controllers
{
    [Route("[controller]")]
    public class LINELoginController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SubscriberContext _context;
        public LINELoginController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            SubscriberContext context)
        {
            _config = configuration;
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        [HttpGet("authorize")]
        public IActionResult Authorize()
        {
            const string authorizeEndpoint = "https://access.line.me/oauth2/v2.1/authorize";

            var parameters = new Dictionary<string, string>()
            {
                ["response_type"] = "code",
                ["client_id"] = _config["Authentication:LineLogin:ChannelId"],
                ["redirect_uri"] = this.Url.ActionLink(nameof(Callback)),
                ["scope"] = "profile openid email",
                ["state"] = "abcd"
            };

            var requestUri = QueryHelpers.AddQueryString(authorizeEndpoint, parameters);

            return Redirect(requestUri);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string code, string state)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest();
            }

            var keyValuePairs = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "authorization_code"),
                new("code", code),
                new("redirect_uri", this.Url.ActionLink(nameof(Callback))),
                new("client_id", _config["Authentication:LineLogin:ChannelId"]),
                new("client_secret", _config["Authentication:LineLogin:ChannelSecret"])
            };

            using var client = _httpClientFactory.CreateClient();

            const string tokenEndpoint = "https://api.line.me/oauth2/v2.1/token";
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

            var tokenResponse = await httpResponseMessage.Content.ReadFromJsonAsync<LoginTokenResponse>(jsonSerializerOptions);

            if (tokenResponse == null)
            {
                throw new Exception("token content failed");
            }

            keyValuePairs = new List<KeyValuePair<string, string>>
            {
                new("id_token", tokenResponse.IdToken),
                new("client_id", _config["Authentication:LineLogin:ChannelId"])
            };

            const string verifyEndpoint = "https://api.line.me/oauth2/v2.1/verify";
            encodedContent = new FormUrlEncodedContent(keyValuePairs);

            var response = await client.PostAsync(verifyEndpoint, encodedContent);

            if (!response.IsSuccessStatusCode)
            {
                string message = await response.Content.ReadAsStringAsync();
                throw new Exception(message);
            }

            var verifyResponse = await response.Content.ReadFromJsonAsync<LoginVerifyResponse>(jsonSerializerOptions);

            if (verifyResponse == null)
            {
                throw new Exception("ID token verify failed");
            }

            var input = _context.Subscribers.Where(a => a.UserId == verifyResponse.UserID).FirstOrDefault();
            if (input == null)
            {
                var subscriber = new Subscriber()
                {
                    UserId = verifyResponse.UserID,
                    Username = verifyResponse.UserName,
                    Email = verifyResponse.UserEmail,
                    LINELoginAccessToken = tokenResponse.AccessToken,
                    LINELoginIdToken = tokenResponse.IdToken,
                };
                await _context.Subscribers.AddAsync(subscriber);
                await _context.SaveChangesAsync();
            }
            else
            {
                input.Username = verifyResponse.UserName;
                input.Email = verifyResponse.UserEmail;
                input.LINELoginAccessToken = tokenResponse.AccessToken;
                input.LINELoginIdToken = tokenResponse.IdToken;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
    }
    public class LoginTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class LoginVerifyResponse
    {
        [JsonPropertyName("sub")]
        public string UserID { get; set; }

        [JsonPropertyName("name")]
        public string UserName { get; set; }

        [JsonPropertyName("picture")]
        public string UserPic { get; set; }

        [JsonPropertyName("email")]
        public string UserEmail { get; set; }
    }
}
