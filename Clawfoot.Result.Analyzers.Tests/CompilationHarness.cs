using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shouldly;

namespace Clawfoot.Result.Analyzers.Tests;

/// <summary>
/// Compiles a snippet as a nullable-enabled consumer of Clawfoot.ResultPattern and returns its diagnostics.
/// </summary>
internal static class CompilationHarness
{
    private const string Usings = "using System;\nusing System.Threading.Tasks;\nusing Clawfoot.ResultPattern;\n";

    private static readonly Lazy<MetadataReference[]> References = new(BuildReferences);

    private static MetadataReference[] BuildReferences()
    {
        // Managed framework assemblies of the running .NET (TPA lists only managed ones)
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var framework = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => string.Equals(Path.GetDirectoryName(path), runtimeDir, StringComparison.OrdinalIgnoreCase));

        return framework
            .Append(typeof(Clawfoot.ResultPattern.Result).Assembly.Location)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    /// <summary>
    /// Compiler diagnostics (warnings and errors) for the snippet, without analyzers.
    /// </summary>
    public static ImmutableArray<Diagnostic> Compile(string source)
    {
        var compilation = CreateCompilation(source);
        return compilation.GetDiagnostics()
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToImmutableArray();
    }

    /// <summary>
    /// Diagnostics reported by <paramref name="analyzer"/>. Fails if the snippet itself doesn't compile.
    /// </summary>
    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(DiagnosticAnalyzer analyzer, string source)
    {
        var compilation = CreateCompilation(source);
        compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("the test snippet must compile");

        return await compilation
            .WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync();
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(Usings + source, new CSharpParseOptions(LanguageVersion.CSharp12));
        return CSharpCompilation.Create(
            "Consumer",
            new[] { tree },
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    public static string[] Ids(this IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Select(d => d.Id).ToArray();
}
