﻿using Microsoft.Dafny;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DafnyLS.Language.Symbols {
  internal class FunctionSymbol : Symbol, ILocalizableSymbol {
    private readonly Function _node;

    public object Node => _node;

    public ISet<ISymbol> Parameters { get; } = new HashSet<ISymbol>();

    public override IEnumerable<ISymbol> Children => Parameters;

    public FunctionSymbol(ISymbol? scope, Function function) : base(scope, function.Name) {
      _node = function;
    }

    public DocumentSymbol AsLspSymbol(CancellationToken cancellationToken) {
      return new DocumentSymbol {
        Name = _node.Name,
        Kind = SymbolKind.Method,
        Range = new Range(_node.tok.GetLspPosition(), _node.BodyEndTok.GetLspPosition()),
        SelectionRange = GetHoverRange(),
        Detail = GetDetailText(cancellationToken),
        Children = Parameters.WithCancellation(cancellationToken).OfType<ILocalizableSymbol>().Select(child => child.AsLspSymbol(cancellationToken)).ToArray()
      };
    }

    public string GetDetailText(CancellationToken cancellationToken) {
      return $"function {_node.Name}({_node.Formals.AsCommaSeperatedText()}) : {_node.ResultType.AsText()}";
    }

    public Range GetHoverRange() {
      return _node.tok.GetLspRange();
    }
  }
}