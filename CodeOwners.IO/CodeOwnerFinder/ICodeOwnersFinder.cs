using CodeOwners.Entities;

namespace CodeOwners.IO.CodeOwnerFinder
{
    public interface ICodeOwnersFinder
    {
        OwnerChangedFiles FindOwners(CodeOwnerMap codeOwnerMap, IEnumerable<string> filesChanged);
    }
}
