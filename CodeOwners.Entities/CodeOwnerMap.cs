using CodeOwners.Entities.ExtensionMethods;

namespace CodeOwners.Entities
{
    public class CodeOwnerMap
    {
        public static string DefaultPrefix { get; } = Constants.CODEOWNERS_DEFAULT;

        public void Upsert(string prefix, List<string> owners)
        {
            OwnersMap.Upsert(prefix, owners);
        }

        public Dictionary<string, List<string>> OwnersMap { get; set; } 
            = new Dictionary<string, List<string>>();

        public List<string> DefaultOwners { get; set; } = new List<string>();
    }
}
