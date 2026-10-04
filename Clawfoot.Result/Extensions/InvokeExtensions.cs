using System;
using System.Threading.Tasks;

namespace Clawfoot.ResultPattern
{
    public static class InvokeExtensions
    {
        private static TResultType HandleInvokeException<TResultType>(TResultType result, Exception ex, bool keepException)
            where TResultType : AbstractResult<TResultType>
        {
            return keepException
                ? (TResultType)result.WithException(ex)
                : (TResultType)result.WithError(ex.Message, ErrorKind.InternalServerError);
        }

        /// <summary>
        /// Invokes the delegate; on exception returns a new result with the error (exception preserved by default). Returns result unchanged on success.
        /// </summary>
        public static TResultType Invoke<TResultType>(this TResultType result, Action action, bool keepException = true)
            where TResultType : AbstractResult<TResultType>
        {
            try
            {
                action.Invoke();
                return result;
            }
            catch (Exception ex)
            {
                return HandleInvokeException(result, ex, keepException);
            }
        }

        /// <summary>
        /// Invokes the delegate; on exception returns a new result with the error (exception preserved by default). Returns result unchanged on success.
        /// </summary>
        public static async Task<TResultType> InvokeAsync<TResultType>(this TResultType result, Func<Task> action, bool keepException = true)
            where TResultType : AbstractResult<TResultType>
        {
            try
            {
                await action.Invoke();
                return result;
            }
            catch (Exception ex)
            {
                return HandleInvokeException(result, ex, keepException);
            }
        }

        /// <summary>
        /// Invokes the delegate; on exception returns a new result with the error (exception preserved by default). Returns result unchanged on success.
        /// </summary>
        public static TResultType Invoke<TParam, TResultType>(this TResultType result, Action<TParam> action, TParam obj, bool keepException = true)
            where TResultType : AbstractResult<TResultType>
        {
            try
            {
                action.Invoke(obj);
                return result;
            }
            catch (Exception ex)
            {
                return HandleInvokeException(result, ex, keepException);
            }
        }

        /// <summary>
        /// Invokes the delegate; on exception returns a new result with the error (exception preserved by default). Returns result unchanged on success.
        /// </summary>
        public static async Task<TResultType> InvokeAsync<TParam, TResultType>(this TResultType result,
            Func<TParam, Task> action,
            TParam obj,
            bool keepException = true)
            where TResultType : AbstractResult<TResultType>
        {
            try
            {
                await action.Invoke(obj);
                return result;
            }
            catch (Exception ex)
            {
                return HandleInvokeException(result, ex, keepException);
            }
        }

        /// <summary>
        /// Invokes the delegate that returns a Result; returns a new Result combining this and the invoked result. On exception, preserves the exception by default.
        /// </summary>
        public static Result Invoke<TResultType>(this TResultType result, Func<Result> func, bool keepException = true)
            where TResultType : AbstractResult<TResultType>
        {
            try
            {
                Result invokedResult = func.Invoke();
                return Result.Combine(result, invokedResult);
            }
            catch (Exception ex)
            {
                var withError = HandleInvokeException(result, ex, keepException);
                return new Result(withError);
            }
        }

        /// <summary>
        /// Invokes the delegate that returns a Result; returns a new Result combining this and the invoked result. On exception, preserves the exception by default.
        /// </summary>
        public static async Task<Result> InvokeAsync<TResultType>(this TResultType result, Func<Task<Result>> func, bool keepException = true)
            where TResultType : AbstractResult<TResultType>
        {
            try
            {
                Result invokedResult = await func.Invoke();
                return Result.Combine(result, invokedResult);
            }
            catch (Exception ex)
            {
                var withError = HandleInvokeException(result, ex, keepException);
                return new Result(withError);
            }
        }

        /// <summary>
        /// Invokes the delegate that returns <see cref="Result{T}"/>; returns a new <see cref="Result{T}"/> combining this result's errors with the invoked result (value from invoked).
        /// </summary>
        public static Result<TResult> InvokeResult<TResult, TResultType>(this TResultType result, Func<Result<TResult>> func, bool keepException = true)
            where TResultType : AbstractResult<TResultType>
        {
            try
            {
                Result<TResult> invokedResult = func.Invoke();
                return Result.Combine(result, invokedResult);
            }
            catch (Exception ex)
            {
                return Result.Error<TResult>(ex);
            }
        }

        /// <summary>
        /// Invokes the delegate that returns <see cref="Result{T}"/>; returns a new <see cref="Result{T}"/> combining this result's errors with the invoked result (value from invoked).
        /// </summary>
        public static async Task<Result<TResult>> InvokeResultAsync<TResult, TResultType>(this TResultType result,
            Func<Task<Result<TResult>>> func,
            bool keepException = true)
            where TResultType : AbstractResult<TResultType>
        {
            try
            {
                Result<TResult> invokedResult = await func.Invoke();
                return Result.Combine(result, invokedResult);
            }
            catch (Exception ex)
            {
                return Result.Error<TResult>(ex);
            }
        }

        /// <summary>
        /// Invokes the delegate; on success returns a new <see cref="Result{T}"/> with the value. On exception returns a result with the error (exception preserved by default).
        /// </summary>
        /// <exception cref="ArgumentNullException">The delegate returned null and <paramref name="result"/> is successful</exception>
        public static Result<TResult> InvokeResult<TResult, TResultType>(this TResultType result, Func<TResult> func, bool keepException = true)
            where TResultType : AbstractResult<TResultType>
        {
            TResult value;
            try
            {
                value = func.Invoke();
            }
            catch (Exception ex)
            {
                return Result.Error<TResult>(ex);
            }

            // SetResult enforces the non-null contract at runtime
            return result.SetResult(value!);
        }

        /// <summary>
        /// Invokes the delegate; on success returns a new <see cref="Result{T}"/> with the value. On exception returns a result with the error (exception preserved by default).
        /// </summary>
        public static async Task<Result<TResult>> InvokeResultAsync<TResult, TResultType>(this TResultType result,
            Func<Task<TResult>> func,
            bool keepException = true)
            where TResultType : AbstractResult<TResultType>
        {
            TResult value;
            try
            {
                value = await func.Invoke();
            }
            catch (Exception ex)
            {
                return Result.Error<TResult>(ex);
            }

            // SetResult enforces the non-null contract at runtime
            return result.SetResult(value!);
        }
    }
}
