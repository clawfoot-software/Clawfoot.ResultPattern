using System;
using System.Collections.Generic;
using System.Text.Json;
using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

/// <summary>
/// System.Text.Json support for IError.Kind via ErrorKindJsonConverter
/// </summary>
public class ErrorKindJsonTests
{
    private static JsonSerializerOptions Registered(bool ignoreUnknownKinds = false)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ErrorKindJsonConverter(ignoreUnknownKinds, typeof(AppErrorKind)));
        return options;
    }

    // A custom IError whose kind is declared as Enum? without an attribute
    public sealed class CustomError : IError
    {
        public Enum? Kind { get; set; }
        public int Code { get; set; }
        public string GroupName { get; set; } = "";
        public string MemberName { get; set; } = "";
        public string Message { get; set; } = "";
        public string UserMessage { get; set; } = "";
        public string ToUserString() => Message;
        public override string ToString() => Message;
    }

    public static class Nested
    {
        // Same simple name as the top-level AppErrorKind
        public enum AppErrorKind { Other }
    }

    [Fact]
    public void Write_WithoutSetup_WritesTypeAndMemberName()
    {
        var json = JsonSerializer.Serialize(new Error("x", AppErrorKind.NotFound));
        json.ShouldContain("\"Kind\":\"AppErrorKind.NotFound\"");
    }

    [Fact]
    public void Write_NullKind_WritesNull()
    {
        JsonSerializer.Serialize(new Error("x")).ShouldContain("\"Kind\":null");
    }

    [Fact]
    public void Write_ThroughIErrorWithoutSetup_WritesKind()
    {
        // result.Errors is IEnumerable<IError>, so this is how a result's errors serialize
        IEnumerable<IError> errors = Result.Error("x", ErrorKind.Conflict).Errors;
        JsonSerializer.Serialize(errors).ShouldContain("\"Kind\":\"ErrorKind.Conflict\"");
    }

    [Fact]
    public void RoundTrip_RegisteredKind_RestoresError()
    {
        var options = Registered();
        var original = new Error("message", AppErrorKind.Conflict, "user", 7, "group", "member");

        var copy = JsonSerializer.Deserialize<Error>(JsonSerializer.Serialize(original, options), options)!;

        copy.Kind.ShouldBe(AppErrorKind.Conflict);
        copy.Message.ShouldBe("message");
        copy.UserMessage.ShouldBe("user");
        copy.Code.ShouldBe(7);
        copy.GroupName.ShouldBe("group");
        copy.MemberName.ShouldBe("member");
    }

    [Fact]
    public void RoundTrip_BuiltInKind_NeedsNoRegistration()
    {
        var json = JsonSerializer.Serialize(new Error("x", ErrorKind.TooManyRequests));
        JsonSerializer.Deserialize<Error>(json)!.Kind.ShouldBe(ErrorKind.TooManyRequests);
    }

    [Fact]
    public void RoundTrip_NullKind()
    {
        var json = JsonSerializer.Serialize(new Error("x"));
        JsonSerializer.Deserialize<Error>(json, Registered())!.Kind.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_UndefinedEnumValue()
    {
        var options = Registered();
        var json = JsonSerializer.Serialize(new Error("x", (AppErrorKind)99), options);
        json.ShouldContain("\"AppErrorKind.99\"");
        JsonSerializer.Deserialize<Error>(json, options)!.Kind.ShouldBe((AppErrorKind)99);
    }

    [Fact]
    public void RoundTrip_CustomIErrorWithRegisteredConverter()
    {
        var options = Registered();
        var json = JsonSerializer.Serialize(new CustomError { Kind = AppErrorKind.PaymentDeclined, Message = "x" }, options);
        json.ShouldContain("\"Kind\":\"AppErrorKind.PaymentDeclined\"");
        JsonSerializer.Deserialize<CustomError>(json, options)!.Kind.ShouldBe(AppErrorKind.PaymentDeclined);
    }

    [Fact]
    public void Read_UnregisteredType_Throws()
    {
        var json = JsonSerializer.Serialize(new Error("x", AppErrorKind.NotFound));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Error>(json))
            .Message.ShouldContain("typeof(AppErrorKind)");
    }

    [Fact]
    public void Read_UnknownMember_Throws()
    {
        const string json = """{"Message":"x","Kind":"AppErrorKind.RateLimited"}""";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Error>(json, Registered()))
            .Message.ShouldContain("RateLimited");
    }

    [Theory]
    [InlineData("""{"Message":"x","Kind":"AppErrorKind.RateLimited"}""")]
    [InlineData("""{"Message":"x","Kind":"OtherKind.Anything"}""")]
    public void Read_Unknown_WithIgnoreUnknownKinds_ReadsNull(string json)
    {
        var error = JsonSerializer.Deserialize<Error>(json, Registered(ignoreUnknownKinds: true))!;
        error.Kind.ShouldBeNull();
        error.Message.ShouldBe("x");
    }

    [Theory]
    [InlineData("""{"Message":"x","Kind":"NotFound"}""")]
    [InlineData("""{"Message":"x","Kind":".NotFound"}""")]
    [InlineData("""{"Message":"x","Kind":"ErrorKind."}""")]
    [InlineData("""{"Message":"x","Kind":404}""")]
    public void Read_Malformed_ThrowsEvenWhenIgnoringUnknownKinds(string json)
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Error>(json, Registered(ignoreUnknownKinds: true)));
    }

    [Fact]
    public void Constructor_NonEnumType_Throws()
    {
        Should.Throw<ArgumentException>(() => new ErrorKindJsonConverter(typeof(string)));
    }

    [Fact]
    public void Constructor_TypesWithSameName_Throws()
    {
        Should.Throw<ArgumentException>(() => new ErrorKindJsonConverter(typeof(AppErrorKind), typeof(Nested.AppErrorKind)));
    }

    [Fact]
    public void Constructor_RegisteringErrorKindAgain_IsAllowed()
    {
        Should.NotThrow(() => new ErrorKindJsonConverter(typeof(ErrorKind), typeof(AppErrorKind)));
    }
}
