using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Clawfoot.Result.Analyzers
{
    /// <summary>
    /// CFRESULT006: flags decisions made on error text instead of the error's kind or code. Rewording a message then
    /// silently changes behaviour. Error text is IError.Message, IError.UserMessage, IError.ToString(),
    /// IError.ToUserString(), and a result's ToString() / ToUserFriendlyString(), including through Trim/ToLower/ToUpper,
    /// '??', '?.' and a local initialized from one of them. Reports error text used as an operand of:
    /// <list type="bullet">
    /// <item>== or != (except against null)</item>
    /// <item>Equals (instance or static, including string.Equals)</item>
    /// <item>string Contains, StartsWith, EndsWith, IndexOf, LastIndexOf, Compare, CompareOrdinal</item>
    /// <item>Contains on a collection, e.g. knownMessages.Contains(error.Message)</item>
    /// <item>Regex.IsMatch</item>
    /// <item>a switch statement or expression, or an 'is' pattern, that matches a constant</item>
    /// </list>
    /// Reading the text to log, format or display it is not reported.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ErrorTextDecisionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CFRESULT006";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Decision based on error text",
            "'{0}' decides by error text: rewording the message would silently change behaviour. Decide by the error's Kind (or Code) instead.",
            "Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Error messages are for people. Branching on them couples behaviour to wording: rewording a message changes what the code does, and services start rewording messages to steer it. Give the error a kind (or code) and decide by that.");

        // string methods whose receiver or arguments are compared
        private static readonly ImmutableHashSet<string> StringComparisons = ImmutableHashSet.Create(
            "Equals", "Contains", "StartsWith", "EndsWith", "IndexOf", "LastIndexOf", "Compare", "CompareOrdinal");

        // string methods that return a normalized copy of the text: deciding on the copy decides on the text
        private static readonly ImmutableHashSet<string> StringNormalizers = ImmutableHashSet.Create(
            "Trim", "TrimStart", "TrimEnd", "ToLower", "ToUpper", "ToLowerInvariant", "ToUpperInvariant", "Normalize");

        // How many locals to follow back to their initializer (var a = e.Message; var b = a; ...)
        private const int MaxLocalDepth = 4;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                var symbols = TextSymbols.TryCreate(start.Compilation);
                if (symbols == null)
                    return;
                start.RegisterOperationAction(ctx => AnalyzeBinary(ctx, symbols), OperationKind.Binary);
                start.RegisterOperationAction(ctx => AnalyzeInvocation(ctx, symbols), OperationKind.Invocation);
                start.RegisterOperationAction(ctx => AnalyzeSwitch(ctx, symbols), OperationKind.Switch);
                start.RegisterOperationAction(ctx => AnalyzeSwitchExpression(ctx, symbols), OperationKind.SwitchExpression);
                start.RegisterOperationAction(ctx => AnalyzeIsPattern(ctx, symbols), OperationKind.IsPattern);
            });
        }

        // error.Message == "x", "x" != error.ToString()
        private static void AnalyzeBinary(OperationAnalysisContext context, TextSymbols symbols)
        {
            var binary = (IBinaryOperation)context.Operation;
            if (binary.OperatorKind != BinaryOperatorKind.Equals && binary.OperatorKind != BinaryOperatorKind.NotEquals)
                return;
            // Null checks don't depend on the wording
            if (IsNullConstant(binary.LeftOperand) || IsNullConstant(binary.RightOperand))
                return;

            var text = FindText(binary.LeftOperand, symbols) ?? FindText(binary.RightOperand, symbols);
            if (text != null)
                Report(context, binary.Syntax, text);
        }

        // error.Message.Contains("x"), string.Equals(error.Message, "x"), known.Contains(error.Message), Regex.IsMatch(error.Message, "x")
        private static void AnalyzeInvocation(OperationAnalysisContext context, TextSymbols symbols)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;

            bool compares;
            if (method.ContainingType?.SpecialType == SpecialType.System_String)
                compares = StringComparisons.Contains(method.Name);
            else if (method.Name == "Equals")
                compares = true;
            // Instance or extension Contains is a collection lookup; a static one is something else, e.g. Assert.Contains
            else if (method.Name == "Contains")
                compares = !method.IsStatic || method.IsExtensionMethod;
            else
                compares = method.Name == "IsMatch" && SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.Regex);

            if (!compares)
                return;

            var text = invocation.Instance != null ? FindText(invocation.Instance, symbols) : null;
            foreach (var argument in invocation.Arguments)
            {
                if (text != null)
                    break;
                text = FindText(argument.Value, symbols);
            }

            if (text != null)
                Report(context, invocation.Syntax, text);
        }

        // switch (error.Message) { case "x": ... }
        private static void AnalyzeSwitch(OperationAnalysisContext context, TextSymbols symbols)
        {
            var switchOperation = (ISwitchOperation)context.Operation;
            var matchesText = switchOperation.Cases
                .SelectMany(c => c.Clauses)
                .Any(clause => clause switch
                {
                    ISingleValueCaseClauseOperation single => IsTextConstant(single.Value),
                    IPatternCaseClauseOperation pattern => MatchesTextConstant(pattern.Pattern),
                    _ => false
                });
            if (!matchesText)
                return;

            var text = FindText(switchOperation.Value, symbols);
            if (text != null)
                Report(context, switchOperation.Value.Syntax, text);
        }

        // error.Message switch { "x" => ..., _ => ... }
        private static void AnalyzeSwitchExpression(OperationAnalysisContext context, TextSymbols symbols)
        {
            var switchExpression = (ISwitchExpressionOperation)context.Operation;
            if (!switchExpression.Arms.Any(arm => MatchesTextConstant(arm.Pattern)))
                return;

            var text = FindText(switchExpression.Value, symbols);
            if (text != null)
                Report(context, switchExpression.Value.Syntax, text);
        }

        // error.Message is "x" or "y"
        private static void AnalyzeIsPattern(OperationAnalysisContext context, TextSymbols symbols)
        {
            var isPattern = (IIsPatternOperation)context.Operation;
            if (!MatchesTextConstant(isPattern.Pattern))
                return;

            var text = FindText(isPattern.Value, symbols);
            if (text != null)
                Report(context, isPattern.Syntax, text);
        }

        /// <summary>
        /// The error text operation that <paramref name="operation"/> evaluates to, or null when it isn't error text.
        /// Looks through conversions, '??', '?.', string normalizers and locals initialized from error text.
        /// </summary>
        private static IOperation? FindText(IOperation? operation, TextSymbols symbols, int localDepth = 0)
        {
            while (operation != null)
            {
                switch (operation)
                {
                    case IConversionOperation conversion:
                        operation = conversion.Operand;
                        continue;
                    case IParenthesizedOperation parenthesized:
                        operation = parenthesized.Operand;
                        continue;
                    case ICoalesceOperation coalesce:
                        operation = coalesce.Value;
                        continue;
                    case IConditionalAccessOperation conditionalAccess:
                        operation = conditionalAccess.WhenNotNull;
                        continue;
                    case IInvocationOperation normalizer
                        when normalizer.TargetMethod.ContainingType?.SpecialType == SpecialType.System_String
                             && !normalizer.TargetMethod.IsStatic
                             && StringNormalizers.Contains(normalizer.TargetMethod.Name):
                        operation = normalizer.Instance;
                        continue;
                    case ILocalReferenceOperation local when localDepth < MaxLocalDepth:
                        return FindText(LocalInitializer(local), symbols, localDepth + 1);
                    default:
                        return symbols.IsErrorText(operation) ? operation : null;
                }
            }
            return null;
        }

        /// <summary>
        /// The initializer of a local declared as 'var x = ...', or null
        /// </summary>
        private static IOperation? LocalInitializer(ILocalReferenceOperation local)
        {
            var model = local.SemanticModel;
            if (model == null)
                return null;
            foreach (var reference in local.Local.DeclaringSyntaxReferences)
            {
                if (reference.SyntaxTree == model.SyntaxTree
                    && reference.GetSyntax() is VariableDeclaratorSyntax declarator
                    && declarator.Initializer != null)
                {
                    return model.GetOperation(declarator.Initializer.Value);
                }
            }
            return null;
        }

        /// <summary>
        /// True when the pattern matches a non-null constant, directly or through and/or/not.
        /// Null checks, type patterns and property patterns (e.g. { Length: 0 }) don't depend on the wording.
        /// </summary>
        private static bool MatchesTextConstant(IPatternOperation pattern)
        {
            switch (pattern)
            {
                case IConstantPatternOperation constant:
                    return IsTextConstant(constant.Value);
                case INegatedPatternOperation negated:
                    return MatchesTextConstant(negated.Pattern);
                case IBinaryPatternOperation binary:
                    return MatchesTextConstant(binary.LeftPattern) || MatchesTextConstant(binary.RightPattern);
                default:
                    return false;
            }
        }

        private static bool IsTextConstant(IOperation value) =>
            value.ConstantValue.HasValue && value.ConstantValue.Value != null;

        private static bool IsNullConstant(IOperation value)
        {
            while (value is IConversionOperation conversion)
                value = conversion.Operand;
            return value.ConstantValue.HasValue && value.ConstantValue.Value == null;
        }

        private static void Report(OperationAnalysisContext context, SyntaxNode location, IOperation text) =>
            context.ReportDiagnostic(Diagnostic.Create(Rule, location.GetLocation(), text.Syntax.ToString()));

        private sealed class TextSymbols
        {
            private TextSymbols(INamedTypeSymbol iError, INamedTypeSymbol resultBase, INamedTypeSymbol? regex)
            {
                IError = iError;
                ResultBase = resultBase;
                Regex = regex;
            }

            public INamedTypeSymbol IError { get; }
            public INamedTypeSymbol ResultBase { get; }

            /// <summary>System.Text.RegularExpressions.Regex, when referenced</summary>
            public INamedTypeSymbol? Regex { get; }

            /// <summary>
            /// Returns null when the compilation doesn't reference Clawfoot.ResultPattern.
            /// </summary>
            public static TextSymbols? TryCreate(Compilation compilation)
            {
                var iError = compilation.GetTypeByMetadataName("Clawfoot.ResultPattern.IError");
                var resultBase = compilation.GetTypeByMetadataName("Clawfoot.ResultPattern.ResultBase");
                if (iError == null || resultBase == null)
                    return null;
                return new TextSymbols(iError, resultBase, compilation.GetTypeByMetadataName("System.Text.RegularExpressions.Regex"));
            }

            /// <summary>
            /// error.Message, error.UserMessage, error.ToString(), error.ToUserString(),
            /// result.ToString(), result.ToUserFriendlyString()
            /// </summary>
            public bool IsErrorText(IOperation operation)
            {
                switch (operation)
                {
                    case IPropertyReferenceOperation property:
                        var name = property.Property.Name;
                        return (name == "Message" || name == "UserMessage") && IsError(property.Property.ContainingType);
                    case IInvocationOperation invocation when invocation.Instance != null:
                        var method = invocation.TargetMethod.Name;
                        var receiver = invocation.Instance.Type;
                        return ((method == "ToString" || method == "ToUserString") && IsError(receiver))
                            || ((method == "ToString" || method == "ToUserFriendlyString") && IsResult(receiver));
                    default:
                        return false;
                }
            }

            private bool IsError(ITypeSymbol? type) =>
                type != null
                && (SymbolEqualityComparer.Default.Equals(type, IError)
                    || type.AllInterfaces.Contains(IError, SymbolEqualityComparer.Default));

            private bool IsResult(ITypeSymbol? type)
            {
                for (var current = type; current != null; current = current.BaseType)
                {
                    if (SymbolEqualityComparer.Default.Equals(current, ResultBase))
                        return true;
                }
                return false;
            }
        }
    }
}
