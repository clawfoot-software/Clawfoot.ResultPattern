using Microsoft.CodeAnalysis;

namespace Clawfoot.Result.Analyzers
{
    /// <summary>
    /// Resolves the Clawfoot.ResultPattern types once per compilation.
    /// </summary>
    internal sealed class ResultSymbols
    {
        private ResultSymbols(INamedTypeSymbol result, INamedTypeSymbol genericResult)
        {
            Result = result;
            GenericResult = genericResult;
        }

        /// <summary>Clawfoot.ResultPattern.Result</summary>
        public INamedTypeSymbol Result { get; }

        /// <summary>Clawfoot.ResultPattern.Result&lt;T&gt; (unbound definition)</summary>
        public INamedTypeSymbol GenericResult { get; }

        /// <summary>
        /// Returns null when the compilation doesn't reference Clawfoot.ResultPattern.
        /// </summary>
        public static ResultSymbols? TryCreate(Compilation compilation)
        {
            var result = compilation.GetTypeByMetadataName("Clawfoot.ResultPattern.Result");
            var genericResult = compilation.GetTypeByMetadataName("Clawfoot.ResultPattern.Result`1");
            if (result == null || genericResult == null)
                return null;
            return new ResultSymbols(result, genericResult);
        }

        public bool IsGenericResult(ITypeSymbol? type)
        {
            return type is INamedTypeSymbol named
                && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, GenericResult);
        }

        /// <summary>
        /// True for Result and types deriving from it (e.g. GenericResult)
        /// </summary>
        public bool IsPlainResult(ITypeSymbol? type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, Result))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// True when the type argument is declared nullable: Result&lt;Foo?&gt; or Result&lt;int?&gt;
        /// </summary>
        public static bool HasNullableTypeArgument(INamedTypeSymbol genericResult)
        {
            if (genericResult.TypeArguments.Length != 1)
                return false;
            var typeArgument = genericResult.TypeArguments[0];
            return genericResult.TypeArgumentNullableAnnotations[0] == NullableAnnotation.Annotated
                || typeArgument.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }
    }
}
