using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Clawfoot.Result.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ResultMustUseReturnValueAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CFRESULT001";

        private static readonly LocalizableString Title = "Result With* return value must be used";
        private static readonly LocalizableString MessageFormat = "The return value of '{0}' must be assigned, returned, or otherwise used. Discarding it is likely a bug.";
        private static readonly LocalizableString Description = "Methods like WithError, WithValue, etc. return a new result; the return value should not be discarded.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Only care when the invocation is used as a statement (return value discarded).
            if (!(invocation.Parent is ExpressionStatementSyntax))
                return;

            // Get method name from syntax first (works even when symbol isn't resolved, e.g. in IDE).
            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
                return;
            var syntaxName = memberAccess.Name.Identifier.ValueText;
            if (!syntaxName.StartsWith("With", System.StringComparison.Ordinal))
                return;

            string methodName;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
            var method = symbolInfo.Symbol as IMethodSymbol
                ?? (symbolInfo.CandidateSymbols.Length == 1 ? symbolInfo.CandidateSymbols[0] as IMethodSymbol : null);

            var isOurType = false;
            if (method != null)
            {
                var containingType = method.ContainingType;
                if (containingType != null)
                {
                    var ns = containingType.ContainingNamespace?.ToDisplayString();
                    isOurType = ns != null && ns.Contains("Clawfoot.ResultPattern");
                }
                methodName = method.Name;
            }
            else
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
                var receiverType = typeInfo.Type ?? typeInfo.ConvertedType;
                if (receiverType != null)
                {
                    var ns = receiverType.ContainingNamespace?.ToDisplayString();
                    isOurType = ns != null && ns.Contains("Clawfoot.ResultPattern");
                }
                methodName = syntaxName;
            }

            // Require namespace so we only warn for Clawfoot.ResultPattern With* methods.
            if (!isOurType)
                return;

            var diagnostic = Diagnostic.Create(
                Rule,
                invocation.GetLocation(),
                methodName);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
