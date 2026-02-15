using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clawfoot.Result;

namespace Clawfoot.Result
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
        /// <param name="successMessage">The default success message</param>
        public Result(string successMessage)
        {
            if (!String.IsNullOrWhiteSpace(successMessage))
            {
                _successMessage = successMessage;
            }
        }

        public Result MergeResults<T>(Result<T> result)
        {
            return base.MergeResults<Result, Result<T>>(result);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result"/> with no errors
        /// </summary>
        /// <param name="successMessage">The default success message</param>
        /// <returns></returns>
        public static Result Ok(string successMessage = null)
        {
            return new Result(successMessage);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result{T}"/> with a success message and a result
        /// </summary>
        /// <param name="result">The result of this generic</param>
        /// <param name="successMessage">The default success message</param>
        /// <returns></returns>
        public static Result<TResult> Ok<TResult>(TResult result, string successMessage = null)
        {
            return new Result<TResult>(result, successMessage);
        }

        /// <summary>
        /// Sugar to create a result from an error enum.
        /// </summary>
        /// <typeparam name="TErrorEnum">The error enum type</typeparam>
        /// <param name="errorEnum">The actual error enum value</param>
        /// <param name="errorParams">The string formatting params for the error message, if any</param>
        /// <returns></returns>
        public static Result FromError<TErrorEnum>(TErrorEnum errorEnum, params string[] errorParams)
             where TErrorEnum : Enum
        {
            IError error = Clawfoot.Result.Error.From(errorEnum, errorParams);
            return Result.Error(error);
        }

        /// <summary>
        /// Sugar to create a result from an error enum.
        /// </summary>
        /// <typeparam name="TErrorEnum">The error enum type</typeparam>
        /// <param name="errorEnum">The actual error enum value</param>
        /// <param name="message">The error message</param>
        /// <param name="userMessage">The user friendly error message</param>
        /// <returns></returns>
        public static Result FromError<TErrorEnum>(TErrorEnum errorEnum, string message, string userMessage = "")
             where TErrorEnum : Enum
        {
            IError error = Clawfoot.Result.Error.From(errorEnum, message, userMessage);
            return Result.Error(error);
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result"/> with an error message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="userMessage">The user friendly error message</param>
        /// <returns></returns>
        public static Result Error(string message, string userMessage = "")
        {
            Result result = new Result();
            result.AddError(message, userMessage);
            return result;
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result"/> with an error message
        /// </summary>
        /// <param name="error">The error model</param>
        /// <returns></returns>
        public static Result Error(IError error)
        {
            Result result = new Result();
            result.AddError(error);
            return result;
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result"/> with multiple error messages
        /// </summary>
        /// <param name="errors">The errors</param>
        /// <returns>A New Result</returns>
        public static Result Error(IEnumerable<IError> errors)
        {
            Result result = new Result();
            result.AddErrors(errors);
            return result;
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result{T}"/> with an error message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="userMessage">The user friendly error message</param>
        /// <returns></returns>
        public static Result<TResult> Error<TResult>(string message, string userMessage = "")
        {
            Result<TResult> result = new Result<TResult>();
            result.AddError(message, userMessage);
            return result;
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result{T}"/> with an error message
        /// </summary>
        /// <param name="error">The error model</param>
        /// <returns></returns>
        public static Result<TResult> Error<TResult>(IError error)
        {
            Result<TResult> result = new Result<TResult>();
            result.AddError(error);
            return result;
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result{T}"/> with multiple error messages
        /// </summary>
        /// <param name="errors">The errors</param>
        /// <returns>A New Result</returns>
        public static Result<TResult> Error<TResult>(IEnumerable<IError> errors)
        {
            Result<TResult> result = new Result<TResult>();
            result.AddErrors(errors);
            return result;
        }

        /// <summary>
        /// Helper method that creates a <see cref="Result"/> with the provided exception
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <returns></returns>
        public static Result Error(Exception ex)
        {
            Result result = new Result();
            result.AddException(ex);
            return result;
        }

        /// <summary>
        /// Helper method that creates a generic <see cref="Result{T}"/> with the provided exception
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <returns></returns>
        public static Result<TResult> Error<TResult>(Exception ex)
        {
            Result<TResult> result = new Result<TResult>();
            result.AddException(ex);
            return result;
        }
        
        public void Deconstruct(out bool success, out Result result)
        {
            success = Success;
            result = this;
        }
        
        public static implicit operator bool(Result result)
        {
            return result.Success;
        }
    }
}