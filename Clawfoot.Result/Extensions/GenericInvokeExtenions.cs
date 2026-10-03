using System;
using System.Threading.Tasks;

namespace Clawfoot.ResultPattern
{
    public static class GenericInvokeExtenions
    {
        /// <summary>
        /// Invokes the delegate; on success returns a new result with the value. On exception returns a new result with the error (exception preserved by default).
        /// </summary>
        /// <exception cref="ArgumentNullException">The delegate returned null and <paramref name="result"/> is successful</exception>
        public static Result<T> InvokeResult<T>(this Result<T> result, Func<T> func, bool keepException = true)
        {
            T value;
            try
            {
                value = func.Invoke();
            }
            catch (Exception ex)
            {
                return keepException ? (Result<T>)result.WithException(ex) : (Result<T>)result.WithError(ex.Message);
            }

            // WithValue enforces the non-null contract at runtime
            return result.WithValue(value!);
        }

        /// <summary>
        /// Invokes the delegate that returns <see cref="Result{T}"/>; returns a new result combining this and the invoked result (last value wins). On exception, preserves the exception by default.
        /// </summary>
        public static Result<T> InvokeResult<T>(this Result<T> result, Func<Result<T>> func, bool keepException = true)
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
        /// Invokes the delegate; on success returns a new result with the value. On exception returns a new result with the error (exception preserved by default).
        /// </summary>
        /// <exception cref="ArgumentNullException">The delegate returned null and <paramref name="result"/> is successful</exception>
        public static async Task<Result<T>> InvokeResultAsync<T>(this Result<T> result, Func<Task<T>> func, bool keepException = true)
        {
            T value;
            try
            {
                value = await func.Invoke();
            }
            catch (Exception ex)
            {
                return keepException ? (Result<T>)result.WithException(ex) : (Result<T>)result.WithError(ex.Message);
            }

            // WithValue enforces the non-null contract at runtime
            return result.WithValue(value!);
        }

        /// <summary>
        /// Invokes the delegate that returns <see cref="Result{T}"/>; returns a new result combining this and the invoked result (last value wins). On exception, preserves the exception by default.
        /// </summary>
        public static async Task<Result<T>> InvokeResultAsync<T>(this Result<T> result, Func<Task<Result<T>>> func, bool keepException = true)
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
