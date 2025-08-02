using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;

namespace LlmTornado.Docs.Arcade.Roslyn.PersistentStorage;

[ExportWorkspaceService(typeof(IPersistentStorageConfiguration), ServiceLayer.Test), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
public sealed class NoOpPersistentStorageConfiguration() : IPersistentStorageConfiguration
{
    public bool ThrowOnFailure => false;

    string? IPersistentStorageConfiguration.TryGetStorageLocation(SolutionKey solutionKey) => null;
}

[ExportWorkspaceService(typeof(IGenerateTypeOptionsService), ServiceLayer.Test), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
public sealed class NoOpGenerateTypeOptionsService() : IGenerateTypeOptionsService
{
    GenerateTypeOptionsResult IGenerateTypeOptionsService.GetGenerateTypeOptions(string className, GenerateTypeDialogOptions generateTypeDialogOptions, Document document, INotificationService? notificationService, IProjectManagementService? projectManagementService, ISyntaxFactsService syntaxFactsService)
    {
        return GenerateTypeOptionsResult.Cancelled;
    }
}

[Export(typeof(IPythiaSignatureHelpProviderImplementation)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
public sealed class NoOpPythiaSignatureHelpProviderImplementation() : IPythiaSignatureHelpProviderImplementation
{
    Task<(ImmutableArray<PythiaSignatureHelpItemWrapper> items, int? selectedItemIndex)> IPythiaSignatureHelpProviderImplementation.GetMethodGroupItemsAndSelectionAsync(ImmutableArray<IMethodSymbol> accessibleMethods, Document document, InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, SymbolInfo currentSymbol, CancellationToken cancellationToken)
    {
        return Task.FromResult<(ImmutableArray<PythiaSignatureHelpItemWrapper>, int?)>(([], null));
    }
}

internal class WasmTemporaryStreamStorage : ITemporaryStreamStorageInternal
{
    private readonly MemoryStream _stream = new();

    public void Dispose()
    {
        _stream.Dispose();
    }

    // --- Synchronous Methods ---

    public Stream ReadStream(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _stream.Position = 0;
        return _stream;
    }

    public void WriteStream(Stream stream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _stream.SetLength(0);
        stream.CopyTo(_stream);
    }

    // --- Asynchronous Methods (Now fully implemented) ---

    public Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Since reading from a MemoryStream is instant, we just wrap the result in a completed task.
        _stream.Position = 0;
        return Task.FromResult<Stream>(_stream);
    }

    public async Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _stream.SetLength(0);
        // Use the built-in async method to copy the stream content.
        await stream.CopyToAsync(_stream, cancellationToken);
    }
}

internal class WasmTemporaryTextStorage : ITemporaryTextStorageInternal
{
    private SourceText? _sourceText;

    public void Dispose()
    {
        _sourceText = null;
    }

    // --- Synchronous Methods (Now fully implemented) ---

    public SourceText ReadText(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // This will throw a NullReferenceException if Read is called before Write,
        // which is the expected behavior for this kind of storage.
        return _sourceText!;
    }

    public void WriteText(SourceText text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sourceText = text;
    }

    // --- Asynchronous Methods (Already correct) ---

    public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_sourceText!);
    }

    public Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sourceText = text;
        return Task.CompletedTask;
    }
}

[ExportWorkspaceService(typeof(ITemporaryStorageServiceInternal), ServiceLayer.Host), Shared]
internal class WasmTemporaryStorageService : ITemporaryStorageServiceInternal
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WasmTemporaryStorageService() { }

    // --- Overloads with CancellationToken (Already correct) ---

    public ITemporaryStreamStorageInternal CreateTemporaryStreamStorage(CancellationToken cancellationToken = default)
    {
        return new WasmTemporaryStreamStorage();
    }

    public ITemporaryTextStorageInternal CreateTemporaryTextStorage(CancellationToken cancellationToken = default)
    {
        return new WasmTemporaryTextStorage();
    }

    // --- Overloads without CancellationToken (Now fully implemented) ---

    public ITemporaryStreamStorageInternal CreateTemporaryStreamStorage()
    {
        return new WasmTemporaryStreamStorage();
    }

    public ITemporaryTextStorageInternal CreateTemporaryTextStorage()
    {
        return new WasmTemporaryTextStorage();
    }
}