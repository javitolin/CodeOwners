using CodeOwners.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeOwners.IO.Parser
{
    public class CodeOwnersParser : ICodeOwnersParser
    {
        private ILogger<CodeOwnersParser> _logger;
        private string? _codeOwnersFilename;

        public CodeOwnersParser(ILogger<CodeOwnersParser> logger, IConfiguration configuration)
        {
            _logger = logger;
            _codeOwnersFilename = configuration.GetValue<string>("codeowners_filename");
        }

        public CodeOwnerMap? Parse(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(_codeOwnersFilename))
            {
                _logger.LogError($"'codeowners_filename' setting is empty");
                return null;
            }

            var filename = Path.Combine(directoryName, _codeOwnersFilename);
            if (!File.Exists(filename))
            {
                _logger.LogWarning($"No codeowners file found. Searched for: [{filename}]");
                return null;
            }

            CodeOwnerMap codeOwnerMap = new CodeOwnerMap();
            foreach (var line in File.ReadLines(filename))
            {
                if (line.Length == 0) continue;

                if (line.StartsWith(Constants.CODEOWNERS_COMMENT))
                {
                    continue;
                }

                var lineSplitted = line.Split(" ");
                var prefixPattern = lineSplitted[0];
                var owners = lineSplitted.Skip(1).ToList();

                if (prefixPattern == Constants.CODEOWNERS_DEFAULT)
                {
                    codeOwnerMap.DefaultOwners = owners;
                    continue;
                }

                codeOwnerMap.Upsert(prefixPattern, owners);
            }

            if (!codeOwnerMap.DefaultOwners.Any())
            {
                _logger.LogWarning("Finished parsing CODEOWNERS file. No default owners found");
            }

            return codeOwnerMap;
        }
    }
}
