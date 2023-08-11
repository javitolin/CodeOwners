namespace CodeOwners.Entities
{
    public class OwnerChangedFiles
    {
        // Owner -> files changed
        public Dictionary<string, HashSet<string>> ChangedFilesMapping { get; private set; } = new Dictionary<string, HashSet<string>>();

        public IEnumerable<string> GetOwners()
        {
            return ChangedFilesMapping.Keys;
        }

        public void AddOwner(string owner, string prefixPattern)
        {
            if (ChangedFilesMapping.ContainsKey(owner))
            {
                ChangedFilesMapping[owner].Add(prefixPattern);
                return;
            }

            ChangedFilesMapping.Add(owner, new HashSet<string> { prefixPattern });
        }

        public void AddOwners(List<string> owners, string prefixPattern)
        {
            foreach (string owner in owners)
            {
                AddOwner(owner, prefixPattern);
            }
        }
    }
}
