namespace CodeOwners.Entities
{
    public class Reviewer
    {
        public string UniqueName { get; set; } = string.Empty;
        public Guid Id { get; set; }

        public override string ToString()
        {
            return $"UniqueName: [{UniqueName}], Id: [{Id}]";
        }
    }
}