using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Clawfoot.Result.Analyzers
{
    /// <summary>
    /// CFRESULT005 (opt-in): flags errors created without a kind, for codebases that dispatch on IError.Kind and want
    /// every error to have one. Reports:
    /// <list type="bullet">
    /// <item>new Error(...) without a kind</item>
    /// <item>Result.Error / WithError / WithErrorIf / WithErrorIfNull / WithErrorIfNullOrDefault called with a message but no kind</item>
    /// <item>a null kind passed to any of these, or to WithException / Result.Error(exception, kind)</item>
    /// <item>[Error] on an enum member without Kind, since Error.From copies the kind from the attribute</item>
    /// <item>a concrete IError implementation that doesn't implement Kind, so it inherits the default of null</item>
    /// </list>
    /// Errors created from exceptions without an explicit kind are not flagged: they get ErrorKind.InternalServerError.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ErrorWithoutKindAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CFRESULT005";

        private const string KindParameter = "kind";
        private const string MessageParameter = "message";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Error has no kind",
            "{0}",
            "Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: "Opt-in rule for codebases that handle errors by IError.Kind and want every error to have one. Enable it with 'dotnet_diagnostic.CFRESULT005.severity = warning' (or error) in .editorconfig.");

        // Library methods that create an error from a message (and, in their kind overloads, a kind)
        private static readonly ImmutableHashSet<string> ErrorFactories = ImmutableHashSet.Create(
            "Error", "WithError", "WithErrorIf", "WithErrorIfNull", "WithErrorIfNullOrDefault", "WithException");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                var symbols = ErrorSymbols.TryCreate(start.Compilation);
                if (symbols == null)
                    return;
                start.RegisterOperationAction(ctx => AnalyzeCreation(ctx, symbols), OperationKind.ObjectCreation);
                start.RegisterOperationAction(ctx => AnalyzeInvocation(ctx, symbols), OperationKind.Invocation);
                start.RegisterSymbolAction(ctx => AnalyzeEnumMember(ctx, symbols), SymbolKind.Field);
                start.RegisterSymbolAction(ctx => AnalyzeImplementation(ctx, symbols), SymbolKind.NamedType);
            });
        }

        // new Error("message") / new Error("message", kind: null)
        private static void AnalyzeCreation(OperationAnalysisContext context, ErrorSymbols symbols)
        {
            var creation = (IObjectCreationOperation)context.Operation;
            if (creation.Constructor == null || !SymbolEqualityComparer.Default.Equals(creation.Type, symbols.Error))
                return;

            var kind = creation.Arguments.FirstOrDefault(a => a.Parameter?.Name == KindParameter);
            if (kind == null || IsNull(kind.Value))
            {
                Report(context, creation.Syntax.GetLocation(),
                    $"'{creation.Syntax}' creates an error without a kind: pass one, e.g. new Error(message, ErrorKind.BadRequest)");
            }
        }

        // Result.Error("message"), result.WithError("message"), result.WithErrorIf(condition, "message", null), ...
        private static void AnalyzeInvocation(OperationAnalysisContext context, ErrorSymbols symbols)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;
            if (!ErrorFactories.Contains(method.Name) || !symbols.IsLibrary(method))
                return;

            var kind = invocation.Arguments.FirstOrDefault(a => a.Parameter?.Name == KindParameter);
            var createsKindlessError = kind != null
                ? IsNull(kind.Value)
                : method.Parameters.Any(p => p.Name == MessageParameter && p.Type.SpecialType == SpecialType.System_String);

            if (createsKindlessError)
            {
                Report(context, invocation.Syntax.GetLocation(),
                    $"'{invocation.Syntax}' creates an error without a kind: use the {method.Name} overload that takes a kind");
            }
        }

        // [Error(Message = "...")] on an enum member: Error.From(member) produces an error without a kind
        private static void AnalyzeEnumMember(SymbolAnalysisContext context, ErrorSymbols symbols)
        {
            var field = (IFieldSymbol)context.Symbol;
            if (field.ContainingType.TypeKind != TypeKind.Enum)
                return;

            foreach (var attribute in field.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.ErrorAttribute))
                    continue;

                var kind = attribute.NamedArguments.FirstOrDefault(a => a.Key == "Kind");
                if (kind.Key != null && !kind.Value.IsNull)
                    continue;

                var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? field.Locations[0];
                Report(context, location,
                    $"[Error] on '{field.ContainingType.Name}.{field.Name}' has no Kind, so Error.From creates an error without a kind: set one, e.g. [Error(Kind = ErrorKind.NotFound)]");
            }
        }

        // class MyError : IError { ... } without a Kind property inherits IError's default of null
        private static void AnalyzeImplementation(SymbolAnalysisContext context, ErrorSymbols symbols)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.IsAbstract || (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct))
                return;
            if (!type.AllInterfaces.Contains(symbols.IError, SymbolEqualityComparer.Default))
                return;

            var implementation = type.FindImplementationForInterfaceMember(symbols.Kind);
            if (implementation != null && implementation.ContainingType.TypeKind != TypeKind.Interface)
                return;

            Report(context, type.Locations[0],
                $"'{type.Name}' implements IError without a Kind property, so its errors have no kind: add 'public Enum? Kind {{ get; }}' (or 'Enum? IError.Kind => ...')");
        }

        private static bool IsNull(IOperation value)
        {
            // Unwrap the conversion to Enum? (e.g. from a null literal or default)
            while (value is IConversionOperation conversion && conversion.IsImplicit)
                value = conversion.Operand;
            return value.ConstantValue.HasValue && value.ConstantValue.Value == null;
        }

        private static void Report(OperationAnalysisContext context, Location location, string message) =>
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, message));

        private static void Report(SymbolAnalysisContext context, Location location, string message) =>
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, message));

        private sealed class ErrorSymbols
        {
            private ErrorSymbols(INamedTypeSymbol error, INamedTypeSymbol iError, INamedTypeSymbol errorAttribute, IPropertySymbol kind)
            {
                Error = error;
                IError = iError;
                ErrorAttribute = errorAttribute;
                Kind = kind;
            }

            public INamedTypeSymbol Error { get; }
            public INamedTypeSymbol IError { get; }
            public INamedTypeSymbol ErrorAttribute { get; }

            /// <summary>IError.Kind</summary>
            public IPropertySymbol Kind { get; }

            /// <summary>
            /// Returns null when the compilation doesn't reference a Clawfoot.ResultPattern version with error kinds.
            /// </summary>
            public static ErrorSymbols? TryCreate(Compilation compilation)
            {
                var error = compilation.GetTypeByMetadataName("Clawfoot.ResultPattern.Error");
                var iError = compilation.GetTypeByMetadataName("Clawfoot.ResultPattern.IError");
                var errorAttribute = compilation.GetTypeByMetadataName("Clawfoot.ResultPattern.ErrorAttribute");
                var kind = iError?.GetMembers("Kind").OfType<IPropertySymbol>().FirstOrDefault();
                if (error == null || iError == null || errorAttribute == null || kind == null)
                    return null;
                return new ErrorSymbols(error, iError, errorAttribute, kind);
            }

            /// <summary>
            /// True for methods declared in Clawfoot.ResultPattern itself
            /// </summary>
            public bool IsLibrary(IMethodSymbol method) =>
                SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, Error.ContainingAssembly);
        }
    }
}
