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
}
