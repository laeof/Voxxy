using Application.Abstractions.Services;
using Application.MeiliSearch;
using Meilisearch;
using Microsoft.Extensions.Configuration;
using SharedKernel.GlobalSearch;
using Index = Meilisearch.Index;

namespace Application.MeiliSearch;

public sealed class MeiliSearchIndexer : ISearchIndexer, IInfrastructureService
{
    private const string IndexName = "global_search";
    private readonly MeilisearchClient _client;

    public MeiliSearchIndexer(IConfiguration configuration, MeilisearchClient client) => _client = client;

    public async Task IndexAsync(SearchDocument document, CancellationToken cancellationToken)
    {
        Index index = _client.Index(IndexName);
        await index.AddDocumentsAsync([document], "id", cancellationToken: cancellationToken);
    }

    public async Task IndexManyAsync(IEnumerable<SearchDocument> documents, CancellationToken cancellationToken)
    {
        Index index = _client.Index(IndexName);
        await index.AddDocumentsAsync(documents, "id", cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string documentId, CancellationToken cancellationToken)
    {
        Index index = _client.Index(IndexName);
        await index.DeleteOneDocumentAsync(documentId, cancellationToken);
    }
}