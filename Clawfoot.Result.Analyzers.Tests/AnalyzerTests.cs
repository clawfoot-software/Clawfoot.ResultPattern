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

    public class CFRESULT006_ErrorTextDecision
    {
        private static readonly ErrorTextDecisionAnalyzer Analyzer = new();
        private const string Id = ErrorTextDecisionAnalyzer.DiagnosticId;

        private const string Declarations = """
            public class AppError : Error { public AppError() : base("x", ErrorKind.NotFound) { } }

            public class PlainError : IError
            {
                public int Code => 0;
                public string GroupName => "";
                public string MemberName => "";
                public string Message => "";
                public string UserMessage => "";
                public string ToUserString() => Message;
            }

            public static class Log { public static void Write(string text) { } }
            public static class Assertions { public static void Contains(string expected, string actual) { } }
            """;

        private static Task<string[]> ReportBody(string body) =>
            ReportDeclarations(Analyzer, Declarations + "\npublic static class Consumer {\n" + body + "\n}");

        [Theory]
        // == / !=
        [InlineData("static bool M(IError e) => e.Message == \"not found\";")]
        [InlineData("static bool M(IError e) => \"not found\" != e.UserMessage;")]
        [InlineData("static bool M(Error e) => e.Message == \"not found\";")]
        [InlineData("static bool M(AppError e) => e.Message == \"not found\";")]
        [InlineData("static bool M(PlainError e) => e.Message == \"not found\";")]
        [InlineData("static bool M(IError e) => e.ToString() == \"not found\";")]
        [InlineData("static bool M(IError e) => e.ToUserString() == \"not found\";")]
        [InlineData("static bool M(Error e) => e.ToUserString() == \"not found\";")]
        [InlineData("static bool M(PlainError e) => e.ToString() == \"not found\";")]
        [InlineData("static bool M(Result r) => r.ToString() == \"not found\";")]
        [InlineData("static bool M(Result<Principal> r) => r.ToUserFriendlyString(\"; \") == \"not found\";")]
        [InlineData("static bool M(IError a, IError b) => a.Message == b.Message;")]
        [InlineData("static bool M(IError e) => e.Message == \"\";")]
        [InlineData("static bool M(Result r) => r.Errors.First().Message == \"not found\";")]
        [InlineData("static bool M(Result r) => r.Errors.Any(e => e.Message == \"not found\");")]
        // Equals
        [InlineData("static bool M(IError e) => e.Message.Equals(\"not found\");")]
        [InlineData("static bool M(IError e) => \"not found\".Equals(e.Message);")]
        [InlineData("static bool M(IError e) => string.Equals(e.Message, \"not found\", StringComparison.OrdinalIgnoreCase);")]
        [InlineData("static bool M(IError e) => Equals(e.Message, \"not found\");")]
        [InlineData("static bool M(IError e) => StringComparer.Ordinal.Equals(e.Message, \"not found\");")]
        // String searches
        [InlineData("static bool M(IError e) => e.Message.Contains(\"not found\");")]
        [InlineData("static bool M(IError e) => e.Message.StartsWith(\"Not\");")]
        [InlineData("static bool M(IError e) => e.UserMessage.EndsWith(\"found\");")]
        [InlineData("static bool M(IError e) => e.Message.IndexOf(\"found\") >= 0;")]
        [InlineData("static bool M(IError e) => string.Compare(e.Message, \"x\") == 0;")]
        [InlineData("static bool M(Result r) => r.ToString().Contains(\"not found\");")]
        [InlineData("static bool M(IError e, System.Collections.Generic.HashSet<string> known) => known.Contains(e.Message);")]
        [InlineData("static bool M(IError e, string[] known) => known.Contains(e.Message);")]
        [InlineData("static bool M(IError e) => System.Text.RegularExpressions.Regex.IsMatch(e.Message, \"not.*found\");")]
        [InlineData("static bool M(IError e, System.Text.RegularExpressions.Regex regex) => regex.IsMatch(e.Message);")]
        // Through normalizers, ?? , ?. and locals
        [InlineData("static bool M(IError e) => e.Message.ToLowerInvariant().Contains(\"not found\");")]
        [InlineData("static bool M(IError e) => e.Message.Trim() == \"not found\";")]
        [InlineData("static bool M(IError? e) => e?.Message.Contains(\"not found\") == true;")]
        [InlineData("static bool M(IError? e) => e?.Message == \"not found\";")]
        [InlineData("static bool M(IError? e) => (e?.Message ?? \"\") == \"not found\";")]
        [InlineData("static bool M(IError e) { var text = e.Message; return text == \"not found\"; }")]
        [InlineData("static bool M(IError e) { var text = e.Message; var lower = text.ToLower(); return lower.Contains(\"not found\"); }")]
        // switch and is
        [InlineData("static int M(IError e) { switch (e.Message) { case \"not found\": return 404; default: return 500; } }")]
        [InlineData("static int M(Result r) { switch (r.Errors.First().Message) { case \"a\": case \"b\": return 1; } return 0; }")]
        [InlineData("static int M(IError e) => e.Message switch { \"not found\" => 404, _ => 500 };")]
        [InlineData("static bool M(IError e) => e.Message is \"not found\";")]
        [InlineData("static bool M(IError e) => e.Message is \"a\" or \"b\";")]
        [InlineData("static bool M(IError e) => e.Message is not \"not found\";")]
        public async Task DecisionOnErrorText_Reports(string body)
        {
            (await ReportBody(body)).ShouldBe(new[] { Id });
        }

        [Theory]
        // Reading the text to log, format or display it
        [InlineData("static void M(IError e) => Log.Write(e.Message);")]
        [InlineData("static string M(IError e) => $\"Failed: {e.Message}\";")]
        [InlineData("static string M(IError e) => string.Format(\"Failed: {0}\", e.UserMessage);")]
        [InlineData("static string M(IError e) => \"Failed: \" + e.ToString();")]
        [InlineData("static string M(Result r) => r.ToString(\"; \");")]
        [InlineData("static string M(Result r) => string.Join(\", \", r.Errors.Select(e => e.ToUserString()));")]
        [InlineData("static int M(IError e) => e.Message.Length;")]
        [InlineData("static IError M(IError e) => new Error(e.Message, ErrorKind.BadRequest);")]
        [InlineData("static string M(IError e) => e.Message.Trim();")]
        [InlineData("static string M(IError e, System.Collections.Generic.Dictionary<string, string> translations) => translations[e.Message];")]
        // Null checks don't depend on the wording
        [InlineData("static bool M(IError e) => e.Message == null;")]
        [InlineData("static bool M(IError e) => e.Message != null;")]
        [InlineData("static bool M(IError e) => e.Message is null;")]
        [InlineData("static bool M(IError e) => e.Message is not null;")]
        [InlineData("static bool M(IError e) => e.Message is { Length: > 0 };")]
        [InlineData("static bool M(IError e) => string.IsNullOrEmpty(e.Message);")]
        // Deciding by kind or code
        [InlineData("static bool M(IError e) => e.Kind is ErrorKind.NotFound;")]
        [InlineData("static bool M(IError e) => e.Code == 404;")]
        [InlineData("static bool M(Result r) => r.HasErrorKind(ErrorKind.NotFound);")]
        // Other text: the result's own Message, exception messages, plain strings
        [InlineData("static bool M(Result r) => r.Message == \"Success\";")]
        [InlineData("static bool M(Exception ex) => ex.Message.Contains(\"timeout\");")]
        [InlineData("static bool M(string s) => s == \"x\";")]
        [InlineData("static bool M(IError e) { var text = \"x\"; return text == e.GroupName; }")]
        // A static Contains that isn't a collection lookup, e.g. an assertion
        [InlineData("static void M(IError e) => Assertions.Contains(\"found\", e.Message);")]
        public async Task OtherUses_DoNotReport(string body)
        {
            (await ReportBody(body)).ShouldBeEmpty();
        }

        [Fact]
        public async Task ReportsTheErrorText()
        {
            var diagnostics = await CompilationHarness.AnalyzeAsync(Analyzer,
                Types + "\npublic static class Consumer { static bool M(IError e) { var text = e.Message; return text == \"x\"; } }");
            diagnostics.Single().GetMessage().ShouldStartWith("'e.Message' decides by error text");
        }
    }

    public class CFRESULT007_ErrorKindNotEnum
    {
        private static readonly ErrorKindNotEnumAnalyzer Analyzer = new();
        private const string Id = ErrorKindNotEnumAnalyzer.DiagnosticId;

        [Theory]
        [InlineData("Kind = 404")]
        [InlineData("Kind = \"NotFound\"")]
        [InlineData("Kind = typeof(ErrorKind)")]
        [InlineData("Kind = 'x'")]
        [InlineData("Kind = new[] { ErrorKind.NotFound }")]
        [InlineData("Message = \"Not found\", Kind = 404.0")]
        public async Task NonEnumKind_Reports(string arguments)
        {
            var declarations = $$"""
                public enum UserErrors
                {
                    [Error({{arguments}})] NotFound,
                }
                """;
            (await ReportDeclarations(Analyzer, declarations)).ShouldBe(new[] { Id });
        }

        [Theory]
        [InlineData("Kind = ErrorKind.NotFound")]
        [InlineData("Kind = AppErrorKind.Conflict")]
        [InlineData("Kind = (object)AppErrorKind.Conflict")]
        [InlineData("Kind = Kinds.Missing")]
        [InlineData("Kind = null")]
        [InlineData("Message = \"Not found\"")]
        public async Task EnumOrNullKind_DoesNotReport(string arguments)
        {
            var declarations = $$"""
                public enum AppErrorKind { NotFound, Conflict }
                public static class Kinds { public const AppErrorKind Missing = AppErrorKind.NotFound; }
                public enum UserErrors
                {
                    [Error({{arguments}})] NotFound,
                }
                """;
            (await ReportDeclarations(Analyzer, declarations)).ShouldBeEmpty();
        }

        [Fact]
        public async Task PropertyAndFieldTargets_Report()
        {
            var declarations = """
                public class Errors
                {
                    [Error(Kind = 404)] public string? NotFound { get; set; }
                    [Error(Kind = "Conflict")] public string? Conflict;
                }
                """;
            (await ReportDeclarations(Analyzer, declarations)).ShouldBe(new[] { Id, Id });
        }
    }
}
