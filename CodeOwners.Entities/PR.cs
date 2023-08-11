namespace CodeOwners.Entities
{
    public class PR
    {
        public string Name { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string SourceBranch { get; set; } = string.Empty;
        public string DestinationBranch { get; set; } = string.Empty;
        public IEnumerable<Reviewer> Reviewers { get;set; } = new List<Reviewer>();
        public int Id { get; set; }
    }
}