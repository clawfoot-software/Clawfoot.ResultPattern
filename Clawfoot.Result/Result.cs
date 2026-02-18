using System;
using System.Collections.Generic;
using System.Linq;

namespace Clawfoot.ResultPattern
{
    /// <summary>
    /// Exists to help with namespace concerns or locals conflicts
    /// </summary>
    public class GenericResult : Result { }

    public partial class Result : AbstractResult<Result>
    {
        /// <summary>
        /// Create a generic result
        /// </summary>
        public Result() { }

        /// <summary>
        /// Create a generic result
        /// </summary>
        public Result(string successMessage) : base(successMessage) { }

        /// <summary>
        /// Create a result with initial errors and/or exceptions
        /// </summary>
        public Result(
            IEnumerable<IError> errors,
            IEnumerable<Exception> exceptions = null,
            string successMessage = null)
            : base(errors, exceptions, successMessage)
        { }

        /// <summary>
        /// Create a result by copying state from another result
        /// </summary>
        public Result(ResultBase source)
            : base(
                source?.Errors ?? Array.Empty<IError>(),
                source?.Exceptions ?? Array.Empty<Exception>(),
                source?.Success == true ? source.Message : null)
        { }

        protected override Result CreateWith(
            IReadOnlyList<IError> errors,
            IReadOnlyList<Exception> exceptions,
            string successMessage)
        {
            return new Result(errors, exceptions, successMessage);
        }

        /// <summary>
        /// Combines multiple results into one. All errors and exceptions are combined.
        /// Success message is taken from the last result when combined result has no errors.
        /// </summary>
        public static Result Combine(Result r1, Result r2, params Result[] others)
        {
            var all = new List<Result> { r1, r2 };
            if (others != null) all.AddRange(others);
            return Combine(all);
        }

        /// <summary>
        /// Combines multiple results into one. All errors and exceptions are combined.
        /// </summary>
        public static Result Combine(IEnumerable<Result> results)
        {
            if (results == null) return new Result();
            var list = results.ToList();
            if (list.Count == 0) return new Result();
            var errors = list.SelectMany(r => r.Errors).ToArray();
            var exceptions = list.SelectMany(r => r.Exceptions).ToArray();
            var lastMessage = list[list.Count - 1].Success ? list[list.Count - 1].Message : null;
            return new Result(errors, exceptions, lastMessage);
        }

        /// <summary>
        /// Combines multiple results into one. All errors and exceptions are combined.
        /// Value is taken from the last result that has a value (last wins).
        /// </summary>
        public static Result<T> Combine<T>(Result<T> r1, Result<T> r2, params Result<T>[] others)
        {
            var all = new List<Result<T>> { r1, r2 };
            if (others != null) all.AddRange(others);
            return Combine(all);
        }

        /// <summary>
        /// Combines multiple results into one. All errors and exceptions are combined.
        /// Value is taken from the last result that has a value (last wins).
        /// </summary>
        public static Result<T> Combine<T>(IEnumerable<Result<T>> results)
        {
            if (results == null) return new Result<T>();
            var list = results.ToList();
            if (list.Count == 0) return new Result<T>();
            var errors = list.SelectMany(r => r.Errors).ToArray();
            var exceptions = list.SelectMany(r => r.Exceptions).ToArray();
            var lastMessage = list[list.Count - 1].Success ? list[list.Count - 1].Message : null;
            var lastWithValue = list.LastOrDefault(r => r.HasResult);
            var value = lastWithValue != null && lastWithValue.HasResult ? lastWithValue.Value : default;
            return new Result<T>(errors, exceptions, lastMessage, value);
        }

        /// <summary>
        /// Combines a ResultBase with a Result<T> into a single Result<T> (errors combined, value from typed result)
        /// </summary>
        public static Result<T> Combine<T>(ResultBase baseResult, Result<T> typedResult)
        {
            if (baseResult == null && typedResult == null) return new Result<T>();
            var errors = (baseResult?.Errors ?? Array.Empty<IError>()).Concat(typedResult?.Errors ?? Array.Empty<IError>()).ToArray();
            var exceptions = (baseResult?.Exceptions ?? Array.Empty<Exception>()).Concat(typedResult?.Exceptions ?? Array.Empty<Exception>()).ToArray();
            var msg = typedResult?.Success == true ? typedResult.Message : null;
            var value = typedResult != null && typedResult.HasResult ? typedResult.Value : default;
            return new Result<T>(errors, exceptions, msg, value);
        }

        /// <summary>
        /// Combines any mix of Result/Result<T> into a single Result (no value)
        /// </summary>
        public static Result Combine(params ResultBase[] results)
        {
            if (results == null || results.Length == 0) return new Result();
            var errors = results.SelectMany(r => r.Errors).ToArray();
            var exceptions = results.SelectMany(r => r.Exceptions).ToArray();
            var lastMessage = results[results.Length - 1].Success ? results[results.Length - 1].Message : null;
            return new Result(errors, exceptions, lastMessage);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result"/> with no errors
        /// </summary>
        public static Result Ok(string successMessage = null)
        {
            return new Result(successMessage);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result{T}"/> with a success message and a result
        /// </summary>
        public static Result<TResult> Ok<TResult>(TResult result, string successMessage = null)
        {
            return new Result<TResult>(result, successMessage);
        }

        /// <summary>
        /// Sugar to create a result from an error enum.
        /// </summary>
        public static Result FromError<TErrorEnum>(TErrorEnum errorEnum, params string[] errorParams)
            where TErrorEnum : Enum
        {
            return Result.Error(Clawfoot.ResultPattern.Error.From(errorEnum, errorParams));
        }

        /// <summary>
        /// Sugar to create a result from an error enum.
        /// </summary>
        public static Result FromError<TErrorEnum>(TErrorEnum errorEnum, string message, string userMessage = "")
            where TErrorEnum : Enum
        {
            return Result.Error(Clawfoot.ResultPattern.Error.From(errorEnum, message, userMessage));
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result"/> with an error message
        /// </summary>
        public static Result Error(string message, string userMessage = "")
        {
            return new Result(new[] { new Error(message, userMessage) }, null, null);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result"/> with an error
        /// </summary>
        public static Result Error(IError error)
        {
            return new Result(new[] { error }, null, null);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result"/> with multiple errors
        /// </summary>
        public static Result Error(IEnumerable<IError> errors)
        {
            return new Result(errors, null, null);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result{T}"/> with an error message
        /// </summary>
        public static Result<TResult> Error<TResult>(string message, string userMessage = "")
        {
            return new Result<TResult>(new[] { new Error(message, userMessage) }, null, null);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result{T}"/> with an error
        /// </summary>
        public static Result<TResult> Error<TResult>(IError error)
        {
            return new Result<TResult>(new[] { error }, null, null);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result{T}"/> with multiple errors
        /// </summary>
        public static Result<TResult> Error<TResult>(IEnumerable<IError> errors)
        {
            return new Result<TResult>(errors, null, null);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result"/> with the provided exception
        /// </summary>
        public static Result Error(Exception ex)
        {
            var errors = new[] { (IError)new Error(ex.Message) };
            var exceptions = new[] { ex };
            return new Result(errors, exceptions, null);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result{T}"/> with the provided exception
        /// </summary>
        public static Result<TResult> Error<TResult>(Exception ex)
        {
            var errors = new[] { (IError)new Error(ex.Message) };
            var exceptions = new[] { ex };
            return new Result<TResult>(errors, exceptions, null);
        }

        public void Deconstruct(out bool success, out Result result)
        {
            success = Success;
            result = this;
        }

        public static implicit operator bool(Result result)
        {
            return result?.Success ?? false;
        }
    }
}
