using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Clawfoot.Result.Analyzers
{
    /// <summary>
    /// CFRESULT004: flags Result&lt;X?&gt;. A successful Result&lt;T&gt; always carries a non-null value,
    /// so a nullable type argument suggests an "optional value" that the type can't represent.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NullableResultTypeArgumentAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CFRESULT004";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Result<T> type argument should not be nullable",
            "'{0}' has a nullable type argument, but a successful Result<T> must carry a non-null value; use '{1}', or return (Result, {2}) when the value is optional",
            "Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A successful Result<T> always carries a non-null value: creating one with null throws ArgumentNullException. Result<X?> therefore can't represent \"succeeded but found nothing\". Use a non-nullable type argument, or return a (Result, X?) tuple when the value is optional.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                var symbols = ResultSymbols.TryCreate(start.Compilation);
                if (symbols == null)
                    return;
                start.RegisterSyntaxNodeAction(ctx => Analyze(ctx, symbols), SyntaxKind.GenericName);
            });
        }

        private static void Analyze(SyntaxNodeAnalysisContext context, ResultSymbols symbols)
        {
            var genericName = (GenericNameSyntax)context.Node;
            if (genericName.Identifier.ValueText != "Result" || genericName.TypeArgumentList.Arguments.Count != 1)
                return;
            if (!(genericName.TypeArgumentList.Arguments[0] is NullableTypeSyntax nullableArgument))
                return;

            var symbol = context.SemanticModel.GetSymbolInfo(genericName, context.CancellationToken).Symbol;
            if (!(symbol is INamedTypeSymbol type) || !symbols.IsGenericResult(type))
                return;

            var inner = nullableArgument.ElementType.ToString();
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                genericName.GetLocation(),
                genericName.ToString(),
                $"Result<{inner}>",
                $"{inner}?"));
        }
    }
}
