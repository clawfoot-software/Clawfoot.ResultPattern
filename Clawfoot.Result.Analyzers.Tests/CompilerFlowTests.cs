using Shouldly;

namespace Clawfoot.Result.Analyzers.Tests;

/// <summary>
/// What a nullable-enabled consumer's compiler reports, driven only by the library's annotations.
/// </summary>
public class CompilerFlowTests
{
    private const string Types = """
        public class Principal { public string Name = ""; }

        public static class Lookup
        {
            public static Result<Principal> Load() => new Principal();
        }
        """;

    private static string[] Warnings(string body) =>
        CompilationHarness.Compile(Types + "\npublic static class Consumer {\n" + body + "\n}").Ids();

    [Fact]
    public void ValueWithoutCheck_Warns()
    {
        Warnings("""
            static string M() { var r = Lookup.Load(); return r.Value.Name; }
            """).ShouldBe(new[] { "CS8602" });
    }

    [Fact]
    public void ValueAssignedToNonNullableWithoutCheck_Warns()
    {
        Warnings("""
            static Principal M() { var r = Lookup.Load(); Principal p = r.Value; return p; }
            """).ShouldContain("CS8600");
    }

    [Theory]
    [InlineData("if (r.HasErrors) return Result.Error(\"x\");")]
    [InlineData("if (!r.Success) return r;")]
    [InlineData("if (!r.IsOk) return r;")]
    [InlineData("if (!r.HasResult) return r;")]
    public void ValueAfterCheck_NoWarning(string check)
    {
        Warnings($$"""
            static Result M(out string name)
            {
                name = "";
                Result<Principal> r = Lookup.Load();
                {{check}}
                name = r.Value.Name;
                return Result.Ok();
            }
            """).ShouldBeEmpty();
    }

    [Fact]
    public void TryGetValue_NoWarning()
    {
        Warnings("""
            static string M() => Lookup.Load().TryGetValue(out var p) ? p.Name : "";
            """).ShouldBeEmpty();
    }

    [Fact]
    public void GetValueOrThrow_NoWarning()
    {
        Warnings("""
            static string M() => Lookup.Load().GetValueOrThrow().Name;
            """).ShouldBeEmpty();
    }

    [Fact]
    public void EnsureSuccess_NoWarning()
    {
        Warnings("""
            static string M() { var r = Lookup.Load(); r.EnsureSuccess(); return r.Value.Name; }
            """).ShouldBeEmpty();
    }

    [Fact]
    public void FailedResultPropagation_NoWarning()
    {
        Warnings("""
            static Result<Principal> A() => Result.Error("x");
            static Result<string> B() { var r = A(); if (r.HasErrors) return (Result)r; return r.Value.Name; }
            """).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("static Result<Principal> M(Principal? p) => p;")]
    [InlineData("static Result<Principal> M(Principal? p) => new Result<Principal>(p);")]
    [InlineData("static Result<Principal> M(Principal? p) => Result.Ok(p);")]
    [InlineData("static Result<Principal> M(Principal? p) => Result.Ok().SetResult(p);")]
    [InlineData("static Result<Principal> M(Principal? p) => Lookup.Load().WithValue(p);")]
    [InlineData("static Result<Principal> M(Principal? p) => new Result<int>(1).To(p);")]
    public void MaybeNullValueIntoSuccessProducer_Warns(string body)
    {
        Warnings(body).ShouldContain("CS8604");
    }

    [Fact]
    public void NonNullValueIntoSuccessProducer_NoWarning()
    {
        Warnings("""
            static Result<Principal> M(Principal? p) { if (p is null) return Result.Error("missing"); return p; }
            """).ShouldBeEmpty();
    }

    [Fact]
    public void Deconstruct_GivesMaybeNullValue()
    {
        Warnings("""
            static string M() { var (result, p) = Lookup.Load(); return p.Name; }
            """).ShouldBe(new[] { "CS8602" });
    }

    // Documented 4.1 source break: both Error constructors need default arguments, so neither is better
    [Fact]
    public void ErrorConstructor_NullSecondArgument_IsAmbiguous()
    {
        Warnings("static IError M() => new Error(\"m\", null);").ShouldContain("CS0121");
    }

    // The message-only overloads take every argument without defaults, so they win over the kind overloads
    [Theory]
    [InlineData("static Result M() => Result.Error(\"m\", null);")]
    [InlineData("static Result M() => Result.Ok().WithError(\"m\", null);")]
    [InlineData("static Result M() => Result.Ok().WithErrorIf(true, \"m\", null);")]
    [InlineData("static Result M() => Result.Error(\"m\", \"user\");")]
    [InlineData("static Result M() => Result.Error(\"m\", (string?)null);")]
    [InlineData("static Result M() => Result.Error(\"m\", ErrorKind.NotFound);")]
    [InlineData("static Result M() => Result.Error(\"m\", kind: null);")]
    [InlineData("static Result M(Exception ex) => Result.Error(ex, null);")]
    [InlineData("static IError M() => new Error(\"m\", userMessage: null);")]
    public void TypedOrNamedSecondArgument_Binds(string body)
    {
        Warnings(body).ShouldNotContain("CS0121");
    }
}
