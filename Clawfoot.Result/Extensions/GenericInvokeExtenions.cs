using System;
using System.Threading.Tasks;

namespace Clawfoot.ResultPattern
{
    public static class GenericInvokeExtenions
    {
        /// <summary>
        /// Invokes the delegate; on success returns a new result with the value. On exception returns a new result with the error.
        /// </summary>
        public static Result<T> InvokeResult<T>(this Result<T> result, Func<T> func, bool keepException = false)
        {
            try
            {
                T value = func.Invoke();
                return result.WithValue(value);
            }
            catch (Exception ex)
            {
                return keepException ? (Result<T>)result.WithException(ex) : (Result<T>)result.WithError(ex.Message);
            }
        }

        /// <summary>
        /// Invokes the delegate that returns Result&lt;T&gt;; returns a new result combining this and the invoked result (last value wins).
        /// </summary>
        public static Result<T> InvokeResult<T>(this Result<T> result, Func<Result<T>> func, bool keepException = false)
        {
            try
            {
                Result<T> invokedResult = func.Invoke();
                return Result.Combine(result, invokedResult);
            }
            catch (Exception ex)
            {
                return keepException ? (Result<T>)result.WithException(ex) : (Result<T>)result.WithError(ex.Message);
            }
        }

        /// <summary>
        /// Invokes the delegate; on success returns a new result with the value. On exception returns a new result with the error.
        /// </summary>
        public static async Task<Result<T>> InvokeResultAsync<T>(this Result<T> result, Func<Task<T>> func, bool keepException = false)
        {
            try
            {
                T value = await func.Invoke();
                return result.WithValue(value);
            }
            catch (Exception ex)
            {
                return keepException ? (Result<T>)result.WithException(ex) : (Result<T>)result.WithError(ex.Message);
            }
        }

        /// <summary>
        /// Invokes the delegate that returns Result&lt;T&gt;; returns a new result combining this and the invoked result (last value wins).
        /// </summary>
        public static async Task<Result<T>> InvokeResultAsync<T>(this Result<T> result, Func<Task<Result<T>>> func, bool keepException = false)
        {
            try
            {
                Result<T> invokedResult = await func.Invoke();
                return Result.Combine(result, invokedResult);
            }
            catch (Exception ex)
            {
                return keepException ? (Result<T>)result.WithException(ex) : (Result<T>)result.WithError(ex.Message);
            }
        }
    }
}
