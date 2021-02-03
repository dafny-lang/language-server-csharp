﻿using Microsoft.Dafny.LanguageServer.Language;
using Microsoft.Dafny.LanguageServer.Language.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Dafny.LanguageServer.Workspace {
  public class TextDocumentLoader : ITextDocumentLoader {
    private readonly IDafnyParser _parser;
    private readonly ISymbolResolver _symbolResolver;
    private readonly IProgramVerifier _verifier;
    private readonly ISymbolTableFactory _symbolTableFactory;

    public TextDocumentLoader(IDafnyParser parser, ISymbolResolver symbolResolver, IProgramVerifier verifier, ISymbolTableFactory symbolTableFactory) {
      _parser = parser;
      _symbolResolver = symbolResolver;
      _verifier = verifier;
      _symbolTableFactory = symbolTableFactory;
    }

    public async Task<DafnyDocument> LoadAsync(TextDocumentItem textDocument, CancellationToken cancellationToken) {
      var errorReporter = new BuildErrorReporter();
      var program = await _parser.ParseAsync(textDocument, errorReporter, cancellationToken);
      var compilationUnit = await _symbolResolver.ResolveSymbolsAsync(textDocument, program, cancellationToken);
      var symbolTable = _symbolTableFactory.CreateFrom(program, compilationUnit, cancellationToken);
      return new DafnyDocument(textDocument, errorReporter, program, symbolTable);
    }

    public async Task<DafnyDocument> LoadAndVerifyAsync(TextDocumentItem textDocument, CancellationToken cancellationToken) {
      var document = await LoadAsync(textDocument, cancellationToken);
      var serializedCounterExamples = await _verifier.VerifyAsync(document.Program, cancellationToken);
      return new DafnyDocument(document, serializedCounterExamples);
    }
  }
}
