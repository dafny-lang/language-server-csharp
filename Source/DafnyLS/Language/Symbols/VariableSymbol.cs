﻿using Microsoft.Dafny;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;

namespace DafnyLS.Language.Symbols {
  internal class FieldSymbol : ISymbol {
    private readonly Field _node;

    public string Name => _node.Name;

    public FieldSymbol(Field field) {
      _node = field;
    }

    public DocumentSymbol AsLspSymbol(CancellationToken cancellationToken) {
      return new DocumentSymbol {
        Name = _node.Name,
        Kind = SymbolKind.Field,
        Range = _node.tok.GetLspRange(),
        SelectionRange = GetHoverRange(),
        Detail = GetDetailText(cancellationToken)
      };
    }

    public string GetDetailText(CancellationToken cancellationToken) {
      return $"{_node.Name} : {_node.Type}";
    }

    public Range GetHoverRange() {
      return _node.tok.GetLspRange();
    }
  }
}