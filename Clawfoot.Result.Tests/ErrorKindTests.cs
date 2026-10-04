using System;
using System.Threading.Tasks;
using Shouldly;

namespace Clawfoot.ResultPattern.Tests;

public enum AppErrorKind { NotFound = 404, Conflict = 409, PaymentDeclined = 1 }

public enum UserErrors
{
    [Error(Code = 1001, Kind = AppErrorKind.NotFound, Message = "User {0} not found")]
    NotFound,
    [Error(Code = 1002, Kind = ErrorKind.BadRequest, Message = "Invalid id")]
    InvalidId,
    [Error(Code = 1003, Message = "No kind")]
    NoKind,
#pragma warning disable CFRESULT007 // the runtime check for a non-enum kind is what's under test
    [Error(Kind = "not an enum", Message = "Bad kind")]
    NonEnumKind,
#pragma warning restore CFRESULT007
}

/// <summary>
/// IError.Kind: how errors get a kind, and how results expose it
/// </summary>
public class ErrorKindTests
{
    private sealed class LegacyError : IError
    {
        public int Code => -1;
        public string GroupName => "";
        public string MemberName => "";
        public string Message => "legacy";
        public string UserMessage => "";
        public string ToUserString() => Message;
        public override string ToString() => Message;
    }

    [Fact]
    public void ErrorKind_ValuesAreHttpStatusCodes()
    {
        ((int)ErrorKind.NotFound).ShouldBe(404);
        ((int)ErrorKind.UnprocessableEntity).ShouldBe(422);
        ((int)ErrorKind.TooManyRequests).ShouldBe(429);
        ((int)ErrorKind.InternalServerError).ShouldBe(500);
        ((int)ErrorKind.ServiceUnavailable).ShouldBe(503);
    }

    [Fact]
    public void ErrorKind_HasNoAliases()
    {
        var values = (int[])Enum.GetValues(typeof(ErrorKind));
        values.ShouldBeUnique();
        values.ShouldAllBe(v => v >= 400 && v <= 599);
    }

    [Fact]
    public void Error_WithoutKind_HasNullKind()
    {
        new Error("x").Kind.ShouldBeNull();
        new Error().Kind.ShouldBeNull();
    }

    [Fact]
    public void Error_WithKind_KeepsKindAndOtherFields()
    {
        var error = new Error("x", AppErrorKind.Conflict, "user", 7, "group", "member");
        error.Kind.ShouldBe(AppErrorKind.Conflict);
        error.Message.ShouldBe("x");
        error.UserMessage.ShouldBe("user");
        error.Code.ShouldBe(7);
        error.GroupName.ShouldBe("group");
        error.MemberName.ShouldBe("member");
    }

    [Fact]
    public void IError_WithoutKindMember_DefaultsToNull()
    {
        IError error = new LegacyError();
        error.Kind.ShouldBeNull();
    }

    [Fact]
    public void Kind_MatchesOnTypeAndValue()
    {
        // Same underlying number, different enum types
        IError error = new Error("x", ErrorKind.NotFound);
        (error.Kind is ErrorKind.NotFound).ShouldBeTrue();
        (error.Kind is AppErrorKind.NotFound).ShouldBeFalse();
    }

    [Fact]
    public void Kind_CanBeSwitchedOnAcrossEnums()
    {
        static int Status(IError error) => error.Kind switch
        {
            AppErrorKind.NotFound or ErrorKind.NotFound => 404,
            AppErrorKind.PaymentDeclined => 402,
            ErrorKind kind => (int)kind,
            null => 500,
            _ => 500,
        };

        Status(new Error("x", AppErrorKind.NotFound)).ShouldBe(404);
        Status(new Error("x", AppErrorKind.PaymentDeclined)).ShouldBe(402);
        Status(new Error("x", ErrorKind.Conflict)).ShouldBe(409);
        Status(new Error("x")).ShouldBe(500);
    }

    [Fact]
    public void ErrorFrom_CopiesKindFromAttribute()
    {
        // A single string argument binds to the message-replacing overload, so pass the params as an array
        var error = Error.From(UserErrors.NotFound, new[] { "42" });
        error.Kind.ShouldBe(AppErrorKind.NotFound);
        error.Message.ShouldBe("User 42 not found");
        error.Code.ShouldBe(1001);

        Error.From(UserErrors.InvalidId).Kind.ShouldBe(ErrorKind.BadRequest);
        Error.From(UserErrors.NotFound, "custom message").Kind.ShouldBe(AppErrorKind.NotFound);
        Result.FromError(UserErrors.InvalidId).GetErrorKind().ShouldBe(ErrorKind.BadRequest);
    }

    [Fact]
    public void ErrorFrom_AttributeWithoutKind_HasNullKind()
    {
        Error.From(UserErrors.NoKind).Kind.ShouldBeNull();
    }

    [Fact]
    public void ErrorFrom_NonEnumKind_Throws()
    {
        Should.Throw<InvalidOperationException>(() => Error.From(UserErrors.NonEnumKind))
            .Message.ShouldContain("UserErrors.NonEnumKind");
    }

    [Fact]
    public void KindOverloads_SetKind()
    {
        Result.Error("x", AppErrorKind.Conflict).GetErrorKind().ShouldBe(AppErrorKind.Conflict);
        Result.Error<int>("x", AppErrorKind.Conflict, "user").GetErrorKind().ShouldBe(AppErrorKind.Conflict);
        Result.Ok().WithError("x", ErrorKind.Gone).GetErrorKind().ShouldBe(ErrorKind.Gone);
        Result.Ok(5).WithError("x", ErrorKind.Gone).GetErrorKind().ShouldBe(ErrorKind.Gone);
        Result.Ok().WithErrorIf(true, "x", AppErrorKind.NotFound).GetErrorKind().ShouldBe(AppErrorKind.NotFound);
        Result.Ok().WithErrorIf(false, "x", AppErrorKind.NotFound).Success.ShouldBeTrue();
        Result.Ok().WithErrorIfNull((string?)null, "x", AppErrorKind.NotFound).GetErrorKind().ShouldBe(AppErrorKind.NotFound);
        Result.Ok().WithErrorIfNull((int?)null, "x", AppErrorKind.NotFound).GetErrorKind().ShouldBe(AppErrorKind.NotFound);
        Result.Ok().WithErrorIfNullOrDefault((int?)0, "x", AppErrorKind.NotFound).GetErrorKind().ShouldBe(AppErrorKind.NotFound);
        Result.Ok().WithErrorIfNullOrDefault((string?)null, "x", AppErrorKind.NotFound).GetErrorKind().ShouldBe(AppErrorKind.NotFound);
    }

    [Fact]
    public void MessageOnlyOverloads_HaveNoKind()
    {
        Result.Error("x").GetErrorKind().ShouldBeNull();
        Result.Ok().WithError("x", "user").GetErrorKind().ShouldBeNull();
    }

    [Fact]
    public void ExceptionErrors_DefaultToInternalServerError()
    {
        var ex = new InvalidOperationException("boom");
        Result.Error(ex).GetErrorKind().ShouldBe(ErrorKind.InternalServerError);
        Result.Error<int>(ex).GetErrorKind().ShouldBe(ErrorKind.InternalServerError);
        Result.Ok().WithException(ex).GetErrorKind().ShouldBe(ErrorKind.InternalServerError);
        Result.Ok().Invoke(new Action(() => throw ex)).GetErrorKind().ShouldBe(ErrorKind.InternalServerError);
        Result.Ok().Invoke(new Action(() => throw ex), keepException: false).GetErrorKind().ShouldBe(ErrorKind.InternalServerError);
        Result.Ok(1).InvokeResult(new Func<int>(() => throw ex), keepException: false).GetErrorKind().ShouldBe(ErrorKind.InternalServerError);
    }

    [Fact]
    public async Task ExceptionErrors_FromAsyncInvoke_DefaultToInternalServerError()
    {
        var result = await Result.Ok().InvokeAsync(new Func<Task>(() => throw new InvalidOperationException("boom")));
        result.GetErrorKind().ShouldBe(ErrorKind.InternalServerError);
    }

    [Fact]
    public void ExceptionErrors_KindCanBeOverridden()
    {
        var ex = new TimeoutException("slow");
        Result.Error(ex, ErrorKind.GatewayTimeout).GetErrorKind().ShouldBe(ErrorKind.GatewayTimeout);
        Result.Error<int>(ex, ErrorKind.GatewayTimeout).GetErrorKind().ShouldBe(ErrorKind.GatewayTimeout);
        var withException = Result.Ok().WithException(ex, AppErrorKind.Conflict);
        withException.GetErrorKind().ShouldBe(AppErrorKind.Conflict);
        withException.Exceptions.ShouldHaveSingleItem().ShouldBeSameAs(ex);
    }

    [Fact]
    public void GetErrorKind_ReturnsFirstNonNullKind()
    {
        var result = Result.Ok()
            .WithError("no kind")
            .WithError("second", AppErrorKind.Conflict)
            .WithError("third", ErrorKind.NotFound);
        result.GetErrorKind().ShouldBe(AppErrorKind.Conflict);
        Result.Ok().GetErrorKind().ShouldBeNull();
        Result.Error("x").GetErrorKind().ShouldBeNull();
    }

    [Fact]
    public void TryGetErrorKind_ReturnsFirstKindOfThatType()
    {
        var result = Result.Ok()
            .WithError("builtin", ErrorKind.NotFound)
            .WithError("app", AppErrorKind.PaymentDeclined)
            .WithError("app2", AppErrorKind.Conflict);

        result.TryGetErrorKind<AppErrorKind>(out var app).ShouldBeTrue();
        app.ShouldBe(AppErrorKind.PaymentDeclined);
        result.TryGetErrorKind<ErrorKind>(out var builtIn).ShouldBeTrue();
        builtIn.ShouldBe(ErrorKind.NotFound);
        Result.Error("x").TryGetErrorKind<AppErrorKind>(out _).ShouldBeFalse();
    }

    [Fact]
    public void HasErrorKind_MatchesAnyErrorOnTypeAndValue()
    {
        var result = Result.Ok()
            .WithError("a", ErrorKind.NotFound)
            .WithError("b", AppErrorKind.Conflict);

        result.HasErrorKind(AppErrorKind.Conflict).ShouldBeTrue();
        result.HasErrorKind(ErrorKind.NotFound).ShouldBeTrue();
        result.HasErrorKind(AppErrorKind.NotFound).ShouldBeFalse();
        result.HasErrorKind(ErrorKind.Conflict).ShouldBeFalse();
    }
}
