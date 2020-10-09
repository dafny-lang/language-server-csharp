﻿using System.Threading;
using System.Threading.Tasks;

namespace DafnyLS.Language {
  /// <summary>
  /// Implementations of this interface are responsible to verify the correctness of a program.
  /// </summary>
  internal interface IProgramVerifier {
    /// <summary>
    /// Applies the program verification to the specified dafny program.
    /// </summary>
    /// <param name="program">The dafny program to verify.</param>
    /// <param name="cancellationToken">A token to cancel the update operation before its completion.</param>
    /// <returns>The verification results.</returns>
    /// <exception cref="System.OperationCanceledException">Thrown when the cancellation was requested before completion.</exception>
    /// <exception cref="System.ObjectDisposedException">Thrown if the cancellation token was disposed before the completion.</exception>
    Task VerifyAsync(Microsoft.Dafny.Program program, CancellationToken cancellationToken);
  }
}