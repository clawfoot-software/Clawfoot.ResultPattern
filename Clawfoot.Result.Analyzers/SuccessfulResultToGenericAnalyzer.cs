using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Clawfoot.Result.Analyzers
{
    /// <summary>
    /// CFRESULT003: flags a plain Result that is known to be successful (Result.Ok(), new Result())
    /// being converted to Result&lt;T&gt;. A successful Result&lt;T&gt; must carry a value, so the
    /// conversion throws at runtime. Converting failed results (error propagation) is not flagged.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SuccessfulResultToGenericAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CFRESULT003";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Successful Result converted to Result<T>",
            "'{0}' is a successful Result with no value and cannot become '{1}': return a value instead, or change the return type to Result",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A successful Result<T> must carry a value. Converting a successful plain Result (Result.Ok(), new Result()) to Result<T>, implicitly, by cast, or with As<T>(), throws InvalidOperationException at runtime. Only failed results can be converted to Result<T> without a value.");

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
                start.RegisterOperationAction(ctx => AnalyzeConversion(ctx, symbols), OperationKind.Conversion);
                start.RegisterOperationAction(ctx => AnalyzeInvocation(ctx, symbols), OperationKind.Invocation);
            });
        }

        // Result.Ok() → Result<T> via the implicit operator (return, assignment, argument) or an explicit cast
        private static void AnalyzeConversion(OperationAnalysisContext context, ResultSymbols symbols)
        {
            var conversion = (IConversionOperation)context.Operation;
            var method = conversion.OperatorMethod;
            if (method == null || !symbols.IsGenericResult(method.ContainingType) || !symbols.IsGenericResult(conversion.Type))
                return;
            if (method.Parameters.Length != 1 || !symbols.IsPlainResult(method.Parameters[0].Type))
                return;

            ReportKnownSuccesses(context, symbols, conversion.Operand, conversion.Type!);
        }

        // Result.Ok().As<T>()
        private static void AnalyzeInvocation(OperationAnalysisContext context, ResultSymbols symbols)
        {
            var invocation = (IInvocationOperation)context.Operation;
            if (invocation.TargetMethod.Name != "As" || invocation.Instance == null || !symbols.IsGenericResult(invocation.Type))
                return;

            ReportKnownSuccesses(context, symbols, invocation.Instance, invocation.Type!);
        }

        private static void ReportKnownSuccesses(OperationAnalysisContext context, ResultSymbols symbols, IOperation operand, ITypeSymbol target)
        {
            operand = Unwrap(operand);

            if (operand is IConditionalOperation conditional)
            {
                ReportKnownSuccesses(context, symbols, conditional.WhenTrue, target);
                if (conditional.WhenFalse != null)
                    ReportKnownSuccesses(context, symbols, conditional.WhenFalse, target);
                return;
            }

            if (!IsKnownSuccess(operand, symbols))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                operand.Syntax.GetLocation(),
                operand.Syntax.ToString(),
                target.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }

        private static IOperation Unwrap(IOperation operation)
        {
            while (true)
            {
                switch (operation)
                {
                    case IParenthesizedOperation parenthesized:
                        operation = parenthesized.Operand;
                        continue;
                    // e.g. GenericResult → Result, or a cast to Result
                    case IConversionOperation conversion when conversion.OperatorMethod == null:
                        operation = conversion.Operand;
                        continue;
                    default:
                        return operation;
                }
            }
        }

        /// <summary>
        /// Result.Ok(), Result.Ok("message"), new Result(), new Result("message") (and GenericResult)
        /// </summary>
        private static bool IsKnownSuccess(IOperation operation, ResultSymbols symbols)
        {
            switch (operation)
            {
                case IInvocationOperation invocation:
                    var method = invocation.TargetMethod;
                    return method.Name == "Ok"
                        && method.IsStatic
                        && !method.IsGenericMethod
                        && symbols.IsPlainResult(method.ContainingType)
                        && symbols.IsPlainResult(method.ReturnType);
                case IObjectCreationOperation creation:
                    var constructor = creation.Constructor;
                    if (constructor == null || !symbols.IsPlainResult(creation.Type))
                        return false;
                    return constructor.Parameters.Length == 0
                        || (constructor.Parameters.Length == 1 && constructor.Parameters[0].Type.SpecialType == SpecialType.System_String);
                default:
                    return false;
            }
        }
    }
}
