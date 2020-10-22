﻿using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Threading;

namespace DafnyLS.Util {
  /// <summary>
  /// Extension methods related to LSP Positions.
  /// </summary>
  public static class PositionExtensions {
    /// <summary>
    /// Converts the given position to an absolute position within the given text.
    /// </summary>
    /// <param name="position">The position to get the absolute position of.</param>
    /// <param name="text">The text where the position should be resolved.</param>
    /// <param name="cancellationToken">A token to cancel the update operation before its completion.</param>
    /// <returns>The absolute position within the text.</returns>
    /// <exception cref="ArgumentException">Thrown if the specified position does not belong to the given text.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the cancellation was requested before completion.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the cancellation token was disposed before the completion.</exception>
    public static int ToAbsolutePosition(this Position position, string text, CancellationToken cancellationToken) {
      int line = 0;
      int character = 0;
      int absolutePosition = 0;
      do {
        cancellationToken.ThrowIfCancellationRequested();
        if(line == position.Line && character == position.Character) {
          return absolutePosition;
        }
        if(IsEndOfLine(text, absolutePosition)) {
          line++;
          character = 0;
        } else {
          character++;
        }
        absolutePosition++;
      } while(line <= position.Line && absolutePosition <= text.Length);
      throw new ArgumentException("could not resolve the absolute position");
    }

    private static bool IsEndOfLine(string text, int absolutePosition) {
      if(absolutePosition >= text.Length) {
        return false;
      }
      return text[absolutePosition] switch
      {
        '\n' => true,
        '\r' => absolutePosition + 1 == text.Length || text[absolutePosition + 1] != '\n',
        _ => false
      };
    }
  }
}