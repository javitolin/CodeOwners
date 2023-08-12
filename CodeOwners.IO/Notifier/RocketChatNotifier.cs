using CodeOwners.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;


namespace CodeOwners.IO.Notifier
{
    public class RocketChatNotifier : INotifier
    {
        private HttpClient _httpClient;
        private ILogger<RocketChatNotifier> _logger;
        string? _baseUrl;
        string? _username;
        string? _password;
        string? _loginUrl;
        string? _listUsersUrl;
        string? _sendMessageUrl;
        string? _messageFormat;

        public RocketChatNotifier(ILogger<RocketChatNotifier> logger, IConfiguration configuration,
            HttpClient httpClient)
        {
            _httpClient = httpClient;
            _logger = logger;

            var section = configuration.GetSection("rocketchat");
            _baseUrl = section.GetValue<string>("base_url");
            _username = section.GetValue<string>("username");
            _password = section.GetValue<string>("password");
            _loginUrl = section.GetValue<string>("login_url");
            _listUsersUrl = section.GetValue<string>("list_users_url");
            _sendMessageUrl = section.GetValue<string>("send_message_url");
            _messageFormat = section.GetValue<string>("message_format");
            CheckConfiguration();

            _httpClient.BaseAddress = new Uri(_baseUrl!);
        }

        private void CheckConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                throw new ArgumentNullException(nameof(_baseUrl));
            }

            if (string.IsNullOrWhiteSpace(_username))
            {
                throw new ArgumentNullException(nameof(_username));
            }

            if (string.IsNullOrWhiteSpace(_password))
            {
                throw new ArgumentNullException(nameof(_password));
            }

            if (string.IsNullOrWhiteSpace(_messageFormat))
            {
                throw new ArgumentNullException(nameof(_messageFormat));
            }
        }

        private async Task Login(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Logging in to RocketChat: [{_baseUrl}] - [{_loginUrl}]");
            var response = await _httpClient.PostAsync(_loginUrl, JsonContent.Create(new
            {
                user = _username,
                password = _password
            }), cancellationToken);

            JObject responseJson = await ParseResponse(response);
            var status = TryGetParameter<string>(responseJson, "status");

            if (status != "success")
            {
                _logger.LogError($"Login response is not success, response: [{responseJson}]");
                throw new HttpRequestException("Couldn't login");
            }

            var data = TryGetParameter<JObject>(responseJson, "data");
            var authToken = TryGetParameter<string>(data, "authToken");
            var me = TryGetParameter<JObject>(data, "me");
            var userId = TryGetParameter<string>(me, "_id");

            _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", authToken);
            _httpClient.DefaultRequestHeaders.Add("X-User-Id", userId);

            _logger.LogDebug("Logged in succesfully");
        }

        private T TryGetParameter<T>(JObject responseJson, string parameterToFind)
        {
            if (responseJson[parameterToFind] is null)
            {
                _logger.LogError($"Couldn't find parameter '{parameterToFind}' in response: [{responseJson}]");
                throw new Exception("Missing parameter '{parameterToFind}' in response");
            }

            return responseJson![parameterToFind]!.Value<T>();
        }

        private T TryGetParameter<T>(JObject responseJson, int indexToFind)
        {
            if (responseJson[indexToFind] is null)
            {
                _logger.LogError($"Couldn't find index '{indexToFind}' in response: [{responseJson}]");
                throw new Exception("Missing index '{parameterToFind}' in response");
            }

            return responseJson![indexToFind]!.Value<T>();
        }

        private static async Task<JObject> ParseResponse(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonConvert.DeserializeObject<JObject>(jsonString);
            if (responseJson is null)
            {
                throw new ArgumentNullException("Response is null");
            }

            return responseJson;
        }

        public async Task NotifyAsync(PR pullRequest, IEnumerable<string> usersToNotify, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Notifying users [{string.Join(",", usersToNotify)}] about PR [{pullRequest.Name}]");

            await Login(cancellationToken);

            usersToNotify = usersToNotify.Select(usersToNotify => usersToNotify.ToLower());

            var response = await _httpClient.GetAsync(_listUsersUrl);
            JObject responseJson = await ParseResponse(response);

            var numberOfUsers = TryGetParameter<int>(responseJson, "count");
            var users = TryGetParameter<JArray>(responseJson, "users");

            await SendMessageToUsersAsync(pullRequest, usersToNotify, numberOfUsers, users, cancellationToken);
            _logger.LogDebug("Users notified");
        }

        private async Task SendMessageToUsersAsync(PR pullRequest, IEnumerable<string> usersToNotify, int numberOfUsers, JArray users, CancellationToken cancellationToken)
        {
            for (int i = 0; i < numberOfUsers; i++)
            {
                var user = users[i];
                if (user is null || user["username"] is null)
                {
                    _logger.LogError($"User is null or doesn't contain field 'username'. User: [{user}]");
                    continue;
                }

                var username = user["username"]!.Value<string>();
                if (usersToNotify.Contains(username.ToLower()))
                {
                    var response = await _httpClient.PostAsync(_sendMessageUrl, JsonContent.Create(new
                    {
                        channel = $"@{username}",
                        text = FormatMessage(pullRequest, username)
                    }), cancellationToken);

                    response.EnsureSuccessStatusCode();
                }
            }
        }

        private string FormatMessage(PR pullRequest, string user)
        {
            _messageFormat = _messageFormat!.Replace("{username}", user);
            _messageFormat = _messageFormat.Replace("{pr_url}", pullRequest.Url);
            _messageFormat = _messageFormat.Replace("{pr_name}", pullRequest.Name);

            _logger.LogDebug($"Formatting message: [{_messageFormat}]");

            return _messageFormat;
        }
    }
}
