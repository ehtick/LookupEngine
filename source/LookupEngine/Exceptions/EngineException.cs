#if NET
using System.Diagnostics.CodeAnalysis;
#else
using JetBrains.Annotations;
#endif

namespace LookupEngine.Exceptions;

/// <summary>
///     The exception that is thrown when engine fails with an unexpected error
/// </summary>
public sealed class EngineException(string message) : Exception(message)
{
#if NET
    [DoesNotReturn]
#else
    [ContractAnnotation("=> halt")]
#endif
    internal static void ThrowIfEngineNotInitialized(string propertyName)
    {
        throw new EngineException($"LookupEngine internal error. {propertyName} must be initialized before accessing it.");
    }
}