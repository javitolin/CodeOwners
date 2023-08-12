namespace CodeOwners.Entities
{
    public class PR
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string SourceBranch { get; set; } = string.Empty;
        public string DestinationBranch { get; set; } = string.Empty;
        public IEnumerable<Reviewer> Reviewers { get; set; } = new List<Reviewer>();
        public int Id { get; set; }

        public override string? ToString()
        {
            return $"Name: [{Name}], " +
                $"Description: [{Description}], " +
                $"Url: [{Url}], " +
                $"Repository: [{Repository}], " +
                $"SourceBranch: [{SourceBranch}], " +
                $"DestinationBranch: [{DestinationBranch}]";
        }
    }
}