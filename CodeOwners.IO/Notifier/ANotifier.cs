using CodeOwners.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeOwners.IO.Notifier
{
    public abstract class ANotifier
    {
        private ILogger<ANotifier> _logger;
        string? _messageFormat;

        protected ANotifier(ILogger<ANotifier> logger, IConfiguration configuration)
        {
            _logger = logger;
            var section = configuration.GetSection("notifiers");
            _messageFormat = section.GetValue<string>("message_format");
        }

        protected string FormatMessage(PR pullRequest, string user)
        {
            if (string.IsNullOrWhiteSpace(_messageFormat))
            {
                throw new ArgumentNullException(nameof(_messageFormat));
            }

            var message = _messageFormat;

            message = message.Replace("{username}", user);
            message = message.Replace("{pr_url}", pullRequest.Url);
            message = message.Replace("{pr_name}", pullRequest.Name);
            message = message.Replace("{pr_description}", pullRequest.Description);
            message = message.Replace("{pr_id}", pullRequest.Id.ToString());
            message = message.Replace("{pr_reviewers}", string.Join(", ", pullRequest.Reviewers));
            message = message.Replace("{pr_destination_branch}", pullRequest.DestinationBranch);
            message = message.Replace("{pr_source_branch}", pullRequest.SourceBranch);
            message = message.Replace("{pr_repository}", pullRequest.Repository);

            _logger.LogDebug($"Formatting message: [{message}]");

            return message;
        }

        public abstract Task NotifyAsync(PR pullRequest, IEnumerable<string> usersToNotify, CancellationToken cancellationToken);
    }
}
