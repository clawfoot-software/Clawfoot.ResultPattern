using System;
using System.Threading.Tasks;

namespace Clawfoot.ResultPattern
{
    public static class InvokeExtensions
    {
        private static TResultType HandleInvokeException<TResultType>(TResultType result, Exception ex, bool keepException)
            where TResultType : AbstractResult<TResultType>
        {
            return keepException ? (TResultType)result.WithException(ex) : (TResultType)result.WithError(ex.Message);
        }

        /// <summary>
        /// Invokes the delegate; on exception returns a new result with the error. Returns result unchanged on success.
        /// </summary>
        public static TResultType Invoke<TResultType>(this TResultType result, Action action, bool keepException = false)
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
        /// Invokes the delegate; on exception returns a new result with the error. Returns result unchanged on success.
        /// </summary>
        public static async Task<TResultType> InvokeAsync<TResultType>(this TResultType result, Func<Task> action, bool keepException = false)
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
        /// Invokes the delegate; on exception returns a new result with the error. Returns result unchanged on success.
        /// </summary>
        public static TResultType Invoke<TParam, TResultType>(this TResultType result, Action<TParam> action, TParam obj, bool keepException = false)
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
        /// Invokes the delegate; on exception returns a new result with the error. Returns result unchanged on success.
        /// </summary>
        public static async Task<TResultType> InvokeAsync<TParam, TResultType>(this TResultType result,
            Func<TParam, Task> action,
            TParam obj,
            bool keepException = false)
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
        /// Invokes the delegate that returns a Result; returns a new Result combining this and the invoked result.
        /// </summary>
        public static Result Invoke<TResultType>(this TResultType result, Func<Result> func, bool keepException = false)
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
        /// Invokes the delegate that returns a Result; returns a new Result combining this and the invoked result.
        /// </summary>
        public static async Task<Result> InvokeAsync<TResultType>(this TResultType result, Func<Task<Result>> func, bool keepException = false)
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
        /// Invokes the delegate that returns Result&lt;TResult&gt;; returns a new Result&lt;TResult&gt; combining this result's errors with the invoked result (value from invoked).
        /// </summary>
        public static Result<TResult> InvokeResult<TResult, TResultType>(this TResultType result, Func<Result<TResult>> func, bool keepException = false)
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
        /// Invokes the delegate that returns Result&lt;TResult&gt;; returns a new Result&lt;TResult&gt; combining this result's errors with the invoked result (value from invoked).
        /// </summary>
        public static async Task<Result<TResult>> InvokeResultAsync<TResult, TResultType>(this TResultType result,
            Func<Task<Result<TResult>>> func,
            bool keepException = false)
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
        /// Invokes the delegate; on success returns a new Result&lt;TResult&gt; with the value. On exception returns a result with the error.
        /// </summary>
        public static Result<TResult> InvokeResult<TResult, TResultType>(this TResultType result, Func<TResult> func, bool keepException = false)
            where TResultType : AbstractResult<TResultType>
        {
            try
            {
                TResult value = func.Invoke();
                return result.SetResult(value);
            }
            catch (Exception ex)
            {
                return Result.Error<TResult>(ex);
            }
        }

        /// <summary>
        /// Invokes the delegate; on success returns a new Result&lt;TResult&gt; with the value. On exception returns a result with the error.
        /// </summary>
        public static async Task<Result<TResult>> InvokeResultAsync<TResult, TResultType>(this TResultType result,
            Func<Task<TResult>> func,
            bool keepException = false)
            where TResultType : AbstractResult<TResultType>
        {
            try
            {
                TResult value = await func.Invoke();
                return result.SetResult(value);
            }
            catch (Exception ex)
            {
                return Result.Error<TResult>(ex);
            }
        }
    }
}
