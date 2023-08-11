using CodeOwners.Entities;
using CodeOwners.Entities.ExtensionMethods;

namespace CodeOwners.IO.CodeOwnerFinder
{
    public class CodeOwnersFinder : ICodeOwnersFinder
    {
        public OwnerChangedFiles FindOwners(CodeOwnerMap codeOwnerMap, IEnumerable<string> filesChanged)
        {
            OwnerChangedFiles ownersToNotify = new OwnerChangedFiles();
            var ownersMap = codeOwnerMap.OwnersMap.Reverse(); // Reversing the list to find the last match first

            foreach (string changedFile in filesChanged)
            {
                if (!ownersMap.Any(o => changedFile.StartsWith(o.Key)))
                {
                    ownersToNotify.AddOwners(codeOwnerMap.DefaultOwners, CodeOwnerMap.DefaultPrefix);
                    continue;
                }

                var match = ownersMap.First(o => changedFile.StartsWith(o.Key));
                ownersToNotify.AddOwners(match.Value, match.Key);
            }

            return ownersToNotify;
        }
    }
}
