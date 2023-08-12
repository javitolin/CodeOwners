using CodeOwners.Entities;

namespace CodeOwners.IO.Parser
{
    public interface ICodeOwnersParser
    {
        CodeOwnerMap? Parse(string directoryName);
    }
}
