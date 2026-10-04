using Microsoft.CodeAnalysis.Diagnostics;
using Shouldly;

namespace Clawfoot.Result.Analyzers.Tests;

public class AnalyzerTests
{
    private const string Types = """
        public class Principal { public string Name = ""; }
        public class Box { public Principal? Value; }

        public static class Lookup
        {
            public static Result<Principal> Load() => new Principal();
            public static Result Validate() => Result.Ok();
        }
        """;

    private static async Task<string[]> Report(DiagnosticAnalyzer analyzer, string body, params string[] enable) =>
        (await CompilationHarness.AnalyzeAsync(analyzer, Types + "\npublic static class Consumer {\n" + body + "\n}", enable)).Ids();

    // For rules that declare types: the snippet is compiled as-is, outside the Consumer class
    private static async Task<string[]> ReportDeclarations(DiagnosticAnalyzer analyzer, string declarations, params string[] enable) =>
        (await CompilationHarness.AnalyzeAsync(analyzer, Types + "\n" + declarations, enable)).Ids();

    public class CFRESULT001_DiscardedResult
    {
        private static readonly ResultMustUseReturnValueAnalyzer Analyzer = new();

        [Theory]
        [InlineData("Result.Ok().WithError(\"x\");")]
        [InlineData("Lookup.Load().WithValue(new Principal());")]
        [InlineData("Result.Combine(Result.Ok(), Result.Ok());")]
        [InlineData("Result.Ok().Invoke(() => { });")]
        public async Task Discarded_Reports(string statement)
        {
            (await Report(Analyzer, $"static void M() {{ {statement} }}"))
                .ShouldBe(new[] { ResultMustUseReturnValueAnalyzer.DiagnosticId });
        }

        [Fact]
        public async Task Used_DoesNotReport()
        {
            (await Report(Analyzer, "static Result M() { var r = Result.Ok().WithError(\"x\"); return r; }"))
                .ShouldBeEmpty();
        }
    }

    public class CFRESULT002_NullForgivingValue
    {
        private static readonly ResultValueNullForgivingAnalyzer Analyzer = new();

        [Theory]
        [InlineData("static string M() => Lookup.Load().Value!.Name;")]
        [InlineData("static Principal M() { var r = Lookup.Load(); if (r.HasErrors) return new Principal(); return r.Value!; }")]
        [InlineData("static Principal M() => (Lookup.Load().Value)!;")]
        [InlineData("static int M(Result<int> r) => r.Value!;")]
        public async Task NullForgivingOnValue_Reports(string body)
        {
            (await Report(Analyzer, body)).ShouldBe(new[] { ResultValueNullForgivingAnalyzer.DiagnosticId });
        }

        [Theory]
        [InlineData("static string M(Principal? p) => p!.Name;")]
        [InlineData("static Principal M(Box b) => b.Value!;")]
        [InlineData("static string M() { var r = Lookup.Load(); if (r.HasErrors) return \"\"; return r.Value.Name; }")]
        [InlineData("static Principal M(Result<Principal?> r) => r.Value!;")]
        // The '!' here also covers a null receiver, so it isn't redundant
        [InlineData("static Principal? M(Result<Principal>? r) => r?.Value!;")]
        public async Task OtherUses_DoNotReport(string body)
        {
            (await Report(Analyzer, body)).ShouldBeEmpty();
        }
    }

    public class CFRESULT003_SuccessfulResultToGeneric
    {
        private static readonly SuccessfulResultToGenericAnalyzer Analyzer = new();

        [Theory]
        [InlineData("static Result<Principal> M() => Result.Ok();")]
        [InlineData("static Result<Principal> M() => Result.Ok(\"done\");")]
        [InlineData("static Result<Principal> M() => new Result();")]
        [InlineData("static Result<Principal> M() => new Result(\"done\");")]
        [InlineData("static Result<Principal> M() => new GenericResult();")]
        [InlineData("static Result<Principal> M() => (Result<Principal>)Result.Ok();")]
        [InlineData("static Result<Principal> M() => Result.Ok().As<Principal>();")]
        [InlineData("static Result<Principal> M() { Result<Principal> r = Result.Ok(); return r; }")]
        [InlineData("static async Task<Result<Principal>> M() { await Task.Yield(); return Result.Ok(); }")]
        [InlineData("static Result<Principal> M(bool ok) => ok ? Result.Ok() : Result.Error(\"x\");")]
        public async Task KnownSuccessToGeneric_Reports(string body)
        {
            (await Report(Analyzer, body)).ShouldBe(new[] { SuccessfulResultToGenericAnalyzer.DiagnosticId });
        }

        [Theory]
        [InlineData("static Result<Principal> M() => Result.Error(\"x\");")]
        [InlineData("static Result<Principal> M() { var v = Lookup.Validate(); if (v.HasErrors) return v; return new Principal(); }")]
        [InlineData("static Result<string> M() { var r = Lookup.Load(); if (r.HasErrors) return (Result)r; return r.Value.Name; }")]
        [InlineData("static Result<Principal> M() => Result.Ok(new Principal());")]
        [InlineData("static Result<Principal> M() => Result.Combine(Result.Ok(), Lookup.Load());")]
        [InlineData("static Result<Principal> M() => Result.Ok().SetResult(new Principal());")]
        [InlineData("static Result M() => Result.Ok();")]
        // Not statically known to succeed; the runtime check covers it
        [InlineData("static Result<Principal> M() => Result.Combine(Result.Ok(), Result.Ok());")]
        public async Task OtherConversions_DoNotReport(string body)
        {
            (await Report(Analyzer, body)).ShouldBeEmpty();
        }
    }

    public class CFRESULT004_NullableTypeArgument
    {
        private static readonly NullableResultTypeArgumentAnalyzer Analyzer = new();

        [Theory]
        [InlineData("static Result<Principal?> M() => Result.Error(\"x\");")]
        [InlineData("static Result<int?> M() => Result.Error(\"x\");")]
        [InlineData("static void M(Result<Principal?> r) { }")]
        public async Task NullableTypeArgument_Reports(string body)
        {
            (await Report(Analyzer, body)).ShouldBe(new[] { NullableResultTypeArgumentAnalyzer.DiagnosticId });
        }

        [Theory]
        [InlineData("static Result<Principal> M() => Result.Error(\"x\");")]
        [InlineData("static (Result, Principal?) M() => (Result.Ok(), null);")]
        [InlineData("static System.Collections.Generic.List<Principal?> M() => new();")]
        public async Task OtherTypes_DoNotReport(string body)
        {
            (await Report(Analyzer, body)).ShouldBeEmpty();
        }
    }

    public class CFRESULT005_ErrorWithoutKind
    {
        private static readonly ErrorWithoutKindAnalyzer Analyzer = new();
        private const string Id = ErrorWithoutKindAnalyzer.DiagnosticId;

        private const string Kinds = """
            public enum AppErrorKind { NotFound, Conflict }
            """;

        [Theory]
        [InlineData("static IError M() => new Error(\"x\");")]
        [InlineData("static IError M() => new Error(\"x\", \"user\", 5);")]
        [InlineData("static IError M() => new Error();")]
        [InlineData("static IError M() => new Error(\"x\", kind: null);")]
        [InlineData("static Result M() => Result.Error(\"x\");")]
        [InlineData("static Result<Principal> M() => Result.Error<Principal>(\"x\", \"user\");")]
        [InlineData("static Result M() => Result.Error(\"x\", (Enum?)null);")]
        [InlineData("static Result M() => Result.Ok().WithError(\"x\");")]
        [InlineData("static Result<Principal> M(Result<Principal> r) => r.WithError(\"x\");")]
        [InlineData("static Result M(bool b) => Result.Ok().WithErrorIf(b, \"x\");")]
        [InlineData("static Result M(Principal? p) => Result.Ok().WithErrorIfNull(p, \"x\");")]
        [InlineData("static Result M(int? i) => Result.Ok().WithErrorIfNullOrDefault(i, \"x\");")]
        [InlineData("static Result M(Exception ex) => Result.Ok().WithException(ex, null);")]
        [InlineData("static Result M(Exception ex) => Result.Error(ex, default(Enum));")]
        public async Task KindlessError_Reports(string body)
        {
            (await Report(Analyzer, Kinds + "\n" + body, Id)).ShouldBe(new[] { Id });
        }

        [Theory]
        [InlineData("static IError M() => new Error(\"x\", AppErrorKind.NotFound);")]
        [InlineData("static IError M() => new Error(\"x\", kind: ErrorKind.NotFound, userMessage: \"u\");")]
        [InlineData("static Result M() => Result.Error(\"x\", AppErrorKind.Conflict);")]
        [InlineData("static Result<Principal> M() => Result.Error<Principal>(\"x\", ErrorKind.BadRequest, \"user\");")]
        [InlineData("static Result M() => Result.Ok().WithError(\"x\", AppErrorKind.NotFound);")]
        [InlineData("static Result M(bool b) => Result.Ok().WithErrorIf(b, \"x\", AppErrorKind.Conflict);")]
        [InlineData("static Result M(Principal? p) => Result.Ok().WithErrorIfNull(p, \"x\", ErrorKind.NotFound);")]
        [InlineData("static Result M(Enum kind) => Result.Error(\"x\", kind);")]
        // Exception errors default to ErrorKind.InternalServerError
        [InlineData("static Result M(Exception ex) => Result.Error(ex);")]
        [InlineData("static Result M(Exception ex) => Result.Ok().WithException(ex);")]
        [InlineData("static Result M() => Result.Ok().Invoke(() => { });")]
        // Adds an existing error rather than creating one
        [InlineData("static Result M(IError e) => Result.Ok().WithError(e);")]
        [InlineData("static Result M(IError e) => Result.Error(e);")]
        public async Task ErrorWithKind_DoesNotReport(string body)
        {
            (await Report(Analyzer, Kinds + "\n" + body, Id)).ShouldBeEmpty();
        }

        [Fact]
        public async Task DisabledByDefault()
        {
            (await Report(Analyzer, "static Result M() => Result.Error(\"x\");")).ShouldBeEmpty();
        }

        [Fact]
        public async Task ErrorEnumMemberWithoutKind_Reports()
        {
            var declarations = Kinds + """

                public enum UserErrors
                {
                    [Error(Message = "Not found", Kind = AppErrorKind.NotFound)] NotFound,
                    [Error(Message = "Invalid", Kind = ErrorKind.BadRequest)] Invalid,
                    [Error(Message = "No kind")] NoKind,
                    [Error(Message = "Null kind", Kind = null)] NullKind,
                    Undecorated,
                }
                """;
            (await ReportDeclarations(Analyzer, declarations, Id)).ShouldBe(new[] { Id, Id });
        }

        [Fact]
        public async Task ErrorImplementationWithoutKind_Reports()
        {
            var declarations = Kinds + """

                public class NoKindError : IError
                {
                    public int Code => 0;
                    public string GroupName => "";
                    public string MemberName => "";
                    public string Message => "";
                    public string UserMessage => "";
                    public string ToUserString() => Message;
                }
                """;
            (await ReportDeclarations(Analyzer, declarations, Id)).ShouldBe(new[] { Id });
        }

        [Theory]
        [InlineData("public Enum? Kind => AppErrorKind.NotFound;")]
        [InlineData("Enum? IError.Kind => Kind; public AppErrorKind Kind => AppErrorKind.Conflict;")]
        public async Task ErrorImplementationWithKind_DoesNotReport(string kindMember)
        {
            var declarations = Kinds + $$"""

                public class AppError : IError
                {
                    {{kindMember}}
                    public int Code => 0;
                    public string GroupName => "";
                    public string MemberName => "";
                    public string Message => "";
                    public string UserMessage => "";
                    public string ToUserString() => Message;
                }

                // Inherits Error's Kind
                public class DerivedError : Error
                {
                    public DerivedError() : base("x", AppErrorKind.NotFound) { }
                }
                """;
            (await ReportDeclarations(Analyzer, declarations, Id)).ShouldBeEmpty();
        }
    }
}
