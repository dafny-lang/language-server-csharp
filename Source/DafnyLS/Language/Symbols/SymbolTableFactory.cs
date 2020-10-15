﻿using IntervalTree;
using Microsoft.Dafny;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using AstElement = System.Object;

namespace DafnyLS.Language.Symbols {
  internal class SymbolTableFactory : ISymbolTableFactory {
    private readonly ILogger _logger;

    public SymbolTableFactory(ILogger<SymbolTableFactory> logger) {
      _logger = logger;
    }

    public SymbolTable CreateFrom(Microsoft.Dafny.Program program, CompilationUnit compilationUnit, CancellationToken cancellationToken) {
      var declarations = CreateDeclarationDictionary(compilationUnit, cancellationToken);
      var designatorVisitor = new DesignatorVisitor(_logger, declarations, compilationUnit, cancellationToken);
      designatorVisitor.Visit(program);
      return new SymbolTable(designatorVisitor.SymbolLookup);
    }

    private static IDictionary<AstElement, ILocalizableSymbol> CreateDeclarationDictionary(CompilationUnit compilationUnit, CancellationToken cancellationToken) {
      return compilationUnit.GetAllDescendantsAndSelf()
        .WithCancellation(cancellationToken)
        .OfType<ILocalizableSymbol>()
        .ToDictionary(
          symbol => symbol.Node,
          symbol => symbol
        );
    }

    private class PositionComparer : Comparer<Position> {
      public override int Compare([AllowNull] Position x, [AllowNull] Position y) {
        if(x == null) {
          return y != null ? -1 : 0;
        } else if(y == null) {
          return 1;
        }
        int lineComparison = x.Line.CompareTo(y.Line);
        if(lineComparison != 0) {
          return lineComparison;
        }
        return x.Character.CompareTo(y.Character);
      }
    }

    private class DesignatorVisitor : SyntaxTreeVisitor {
      private readonly ILogger _logger;
      private readonly IDictionary<AstElement, ILocalizableSymbol> _declarations;
      private readonly CancellationToken _cancellationToken;

      private ISymbol _currentScope;

      public IIntervalTree<Position, ILocalizableSymbol> SymbolLookup { get; } = new IntervalTree<Position, ILocalizableSymbol>(new PositionComparer());

      public DesignatorVisitor(
          ILogger logger, IDictionary<AstElement, ILocalizableSymbol> declarations, ISymbol rootScope, CancellationToken cancellationToken
      ) {
        _logger = logger;
        _declarations = declarations;
        _currentScope = rootScope;
        _cancellationToken = cancellationToken;
      }

      public override void VisitUnknown(object node, Microsoft.Boogie.IToken token) {
        _logger.LogWarning("encountered unknown syntax node of type {} in {}@({},{})", node.GetType(), Path.GetFileName(token.filename), token.line, token.col);
      }

      public override void Visit(ModuleDefinition moduleDefinition) {
        _cancellationToken.ThrowIfCancellationRequested();
        ProcessNestedScope(moduleDefinition, () => base.Visit(moduleDefinition));
      }

      public override void Visit(ClassDecl classDeclaration) {
        _cancellationToken.ThrowIfCancellationRequested();
        ProcessNestedScope(classDeclaration, () => base.Visit(classDeclaration));
      }

      public override void Visit(Method method) {
        _cancellationToken.ThrowIfCancellationRequested();
        ProcessNestedScope(method, () => base.Visit(method));
      }

      public override void Visit(Function function) {
        _cancellationToken.ThrowIfCancellationRequested();
        ProcessNestedScope(function, () => base.Visit(function));
      }

      public override void Visit(ExprDotName expressionDotName) {
        base.Visit(expressionDotName);
      }

      public override void Visit(NameSegment nameSegment) {
        _cancellationToken.ThrowIfCancellationRequested();
        RegisterDisgnator(nameSegment.tok, nameSegment.Name);
      }

      private void RegisterDisgnator(Microsoft.Boogie.IToken token, string identifier) {
        var symbol = GetDeclaration(identifier);
        if(symbol != null) {
          var range = token.GetLspRange();
          SymbolLookup.Add(range.Start, range.End, symbol);
        } else {
          _logger.LogWarning("could not resolve the symbol of designator named {} in {}@({},{})", identifier, Path.GetFileName(token.filename), token.line, token.col);
        }
      }

      private void ProcessNestedScope(AstElement node, Action visit) {
        var oldScope = _currentScope;
        _currentScope = _declarations[node];
        visit();
        _currentScope = oldScope;
      }

      private ILocalizableSymbol? GetDeclaration(string identifier) {
        var scope = _currentScope;
        while(scope != null) {
          foreach(var child in scope.Children.OfType<ILocalizableSymbol>()) {
            _cancellationToken.ThrowIfCancellationRequested();
            if(child.Identifier == identifier) {
              return child;
            }
          }
          scope = scope.Scope;
        }
        return null;
      }
    }
  }
}