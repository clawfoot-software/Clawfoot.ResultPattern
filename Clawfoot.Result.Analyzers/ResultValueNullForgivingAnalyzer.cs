using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Clawfoot.Result.Analyzers
{
    /// <summary>
    /// CFRESULT002: flags the null-forgiving operator on Result&lt;T&gt;.Value.
    /// Value is annotated so the compiler already knows it's non-null after a HasErrors/Success check;
    /// a '!' either does nothing or hides a missing check.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ResultValueNullForgivingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CFRESULT002";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Do not use '!' on Result<T>.Value",
            "Remove '!' from '{0}': check HasErrors or Success first and the compiler knows Value is not null; in tests, use GetValueOrThrow()",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Result<T>.Value is non-null whenever the result is successful, and the compiler tracks this after a HasErrors, Success, IsOk, HasResult or TryGetValue check. The null-forgiving operator suppresses the warning that a missing check would produce. Use the check, TryGetValue, or GetValueOrThrow() instead.");

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
                start.RegisterSyntaxNodeAction(
                    ctx => Analyze(ctx, symbols),
                    SyntaxKind.SuppressNullableWarningExpression);
            });
        }

        private static void Analyze(SyntaxNodeAnalysisContext context, ResultSymbols symbols)
        {
            var suppression = (PostfixUnaryExpressionSyntax)context.Node;
            var operand = suppression.Operand;
            while (operand is ParenthesizedExpressionSyntax parenthesized)
                operand = parenthesized.Expression;

            SimpleNameSyntax? name = operand switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
                _ => null
            };
            if (name == null || name.Identifier.ValueText != "Value")
                return;

            if (!(context.SemanticModel.GetSymbolInfo(operand, context.CancellationToken).Symbol is IPropertySymbol property))
                return;
            if (!symbols.IsGenericResult(property.ContainingType))
                return;

            // Result<X?> is reported by CFRESULT004; there the '!' may be the author's only option
            if (ResultSymbols.HasNullableTypeArgument(property.ContainingType))
                return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, suppression.GetLocation(), suppression.ToString()));
        }
    }
}
