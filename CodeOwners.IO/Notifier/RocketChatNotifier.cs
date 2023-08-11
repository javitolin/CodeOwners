using CodeOwners.Entities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;


// TODO Refactor this, fix PR URL
namespace CodeOwners.IO.Notifier
{
    public class RocketChatNotifier : INotifier
    {
        private HttpClient _httpClient;
        private string? _username;
        private string? _password;
        private string? _messageFormat;

        public RocketChatNotifier(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            var section = configuration.GetSection("rocketchat");
            var baseUrl = section.GetValue<string>("base_url");
            _httpClient.BaseAddress = new Uri(baseUrl);

            _username = section.GetValue<string>("username");
            _password = section.GetValue<string>("password");
            _messageFormat = section.GetValue<string>("message_format");
        }
        private async Task Login(CancellationToken cancellationToken)
        {
            var response = await _httpClient.PostAsync("/api/v1/login", JsonContent.Create(new
            {
                user = _username,
                password = _password
            }), cancellationToken);

            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonConvert.DeserializeObject<JObject>(jsonString);
                        
            if (responseJson["status"].Value<string>() != "success")
            {
                // TODO Log error
                return;
            }

            var authToken = responseJson["data"]["authToken"].Value<string>();
            var userId = responseJson["data"]["me"]["_id"].Value<string>();

            _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", authToken);
            _httpClient.DefaultRequestHeaders.Add("X-User-Id", userId);
        }
        public async Task NotifyAsync(PR pullRequest, IEnumerable<string> usersToNotify, CancellationToken cancellationToken)
        {
            await Login(cancellationToken);

            usersToNotify = usersToNotify.Select(usersToNotify => usersToNotify.ToLower());

            var response = await _httpClient.GetAsync("/api/v1/users.list");
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonConvert.DeserializeObject<JObject>(jsonString);

            var numberOfUsers = responseJson["count"].Value<int>();
            for (int i = 0; i < numberOfUsers; i++)
            {
                var user = responseJson["users"][i];
                var username = user["username"].Value<string>();
                if (usersToNotify.Contains(username.ToLower()))
                {
                    response = await _httpClient.PostAsync("/api/v1/chat.postMessage", JsonContent.Create(new
                    {
                        channel = $"@{username}",
                        text = FormatMessage(pullRequest, username)
                    }));;

                    response.EnsureSuccessStatusCode();
                }
            }
        }

        private string FormatMessage(PR pullRequest, string user)
        {
            _messageFormat = _messageFormat.Replace("{username}", user);
            _messageFormat = _messageFormat.Replace("{pr_url}", pullRequest.Url);
            _messageFormat = _messageFormat.Replace("{pr_name}", pullRequest.Name);

            return _messageFormat;
        }
    }
}
