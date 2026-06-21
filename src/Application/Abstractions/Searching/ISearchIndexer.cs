using SharedKernel.GlobalSearch;

namespace Application.MeiliSearch;

public interface ISearchIndexer
{
    Task IndexAsync(SearchDocument document, CancellationToken cancellationToken);
    Task IndexManyAsync(IEnumerable<SearchDocument> documents, CancellationToken cancellationToken);
    Task DeleteAsync(string documentId, CancellationToken cancellationToken);
}