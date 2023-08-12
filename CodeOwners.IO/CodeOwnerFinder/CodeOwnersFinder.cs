using CodeOwners.Entities;
using Microsoft.Extensions.Logging;

namespace CodeOwners.IO.CodeOwnerFinder
{
    public class CodeOwnersFinder : ICodeOwnersFinder
    {
        private ILogger<CodeOwnersFinder> _logger;

        public CodeOwnersFinder(ILogger<CodeOwnersFinder> logger)
        {
            _logger = logger;
        }

        public OwnerChangedFiles FindOwners(CodeOwnerMap codeOwnerMap, IEnumerable<string> filesChanged)
        {
            OwnerChangedFiles ownersToNotify = new OwnerChangedFiles();
            var ownersMap = codeOwnerMap.OwnersMap.Reverse(); // Reversing the list to find the last match first

            foreach (string changedFile in filesChanged)
            {
                if (ownersMap.Any(o => changedFile.StartsWith(o.Key)))
                {
                    var match = ownersMap.First(o => changedFile.StartsWith(o.Key));
                    ownersToNotify.AddOwners(match.Value, match.Key);
                    continue;
                }

                if (!codeOwnerMap.DefaultOwners.Any())
                {
                    _logger.LogWarning($"Found a changed file with no specific owner and no default owners exist. File: [{changedFile}]. Add '*' in CODEOWNERS file for default owners on repo");
                    continue;
                }

                ownersToNotify.AddOwners(codeOwnerMap.DefaultOwners, CodeOwnerMap.DefaultPrefix);
            }

            return ownersToNotify;
        }
    }
}
