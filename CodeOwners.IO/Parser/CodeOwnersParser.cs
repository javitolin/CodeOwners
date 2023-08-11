using CodeOwners.Entities;

namespace CodeOwners.IO.Parser
{
    public class CodeOwnersParser : ICodeOwnersParser
    {
        public CodeOwnerMap Parse(string filename)
        {
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

            return codeOwnerMap;
        }
    }
}
