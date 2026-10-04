using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Clawfoot.Result.Analyzers
{
    /// <summary>
    /// CFRESULT007: flags [Error(Kind = ...)] set to something other than an enum value, e.g. Kind = 404 or
    /// Kind = "NotFound". ErrorAttribute.Kind is typed object (attribute properties can't be typed Enum), so these
    /// compile, but Error.From throws InvalidOperationException for them at runtime.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ErrorKindNotEnumAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CFRESULT007";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "[Error] Kind is not an enum value",
            "[Error] on '{0}' sets Kind to {1}, which is not an enum value, so Error.From throws InvalidOperationException. Use an enum value, e.g. Kind = ErrorKind.NotFound.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "ErrorAttribute.Kind is typed object because attribute properties can't be typed Enum. Any non-enum value compiles but makes Error.From throw at runtime.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                var errorAttribute = start.Compilation.GetTypeByMetadataName("Clawfoot.ResultPattern.ErrorAttribute");
                if (errorAttribute == null)
                    return;
                // [Error] targets enums, properties and fields
                start.RegisterSymbolAction(ctx => Analyze(ctx, errorAttribute), SymbolKind.Field, SymbolKind.Property, SymbolKind.NamedType);
            });
        }

        private static void Analyze(SymbolAnalysisContext context, INamedTypeSymbol errorAttribute)
        {
            var symbol = context.Symbol;
            foreach (var attribute in symbol.GetAttributes())
            {
                if (!IsErrorAttribute(attribute.AttributeClass, errorAttribute))
                    continue;

                foreach (var argument in attribute.NamedArguments)
                {
                    if (argument.Key != "Kind" || !IsInvalidKind(argument.Value))
                        continue;

                    var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? symbol.Locations[0];
                    var name = symbol.ContainingType != null ? symbol.ContainingType.Name + "." + symbol.Name : symbol.Name;
                    context.ReportDiagnostic(Diagnostic.Create(Rule, location, name, Describe(argument.Value)));
                }
            }
        }

        private static bool IsErrorAttribute(INamedTypeSymbol? type, INamedTypeSymbol errorAttribute)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, errorAttribute))
                    return true;
            }
            return false;
        }

        // Null is CFRESULT005's concern; Error is a value the compiler already rejected
        private static bool IsInvalidKind(TypedConstant value) =>
            !value.IsNull && value.Kind != TypedConstantKind.Enum && value.Kind != TypedConstantKind.Error;

        private static string Describe(TypedConstant value)
        {
            var type = value.Type?.ToDisplayString() ?? "unknown";
            return value.Kind == TypedConstantKind.Array
                ? "an array (" + type + ")"
                : "'" + value.ToCSharpString() + "' (" + type + ")";
        }
    }
}
