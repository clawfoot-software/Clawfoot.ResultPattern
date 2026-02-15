using System;
using System.Threading.Tasks;

namespace Clawfoot.ResultPattern
{
    public static class InvokeExtensions
    {
        private static void HandleInvokeException<TResultType>(TResultType result, Exception ex, bool keepException)
            where TResultType: AbstractResult<TResultType>
        {
            if (!keepException)
            {
                result.AddError(ex.Message);
            }
            else
            {
                result.AddException(ex);
            }
        }
        

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result.
        /// Returns this result
        /// </summary>
        public static TResultType Invoke<TResultType>(this TResultType result, Action action, bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                HandleInvokeException(result, ex, keepException);
            }

            return result;
        }

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result.
        /// Returns this result
        /// </summary>
        public static TResultType Do<TResultType>(this TResultType result, Action action, bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            return Invoke(result, action, keepException);
        }
            

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result.
        /// Returns this result
        /// </summary>
        /// <param name="action"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static async Task<TResultType> InvokeAsync<TResultType>(this TResultType result, Func<Task> action, bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            try
            {
                await action.Invoke();
            }
            catch (Exception ex)
            {
                HandleInvokeException(result, ex, keepException);
            }

            return result;
        }

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result.
        /// Returns this result
        /// </summary>
        public static TResultType Invoke<TParam, TResultType>(this TResultType result, Action<TParam> action, TParam obj, bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            try
            {
                action.Invoke(obj);
            }
            catch (Exception ex)
            {
                HandleInvokeException(result, ex, keepException);
            }

            return result;
        }

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result.
        /// Returns this result
        /// </summary>
        public static TResultType Do<TParam, TResultType>(this TResultType result, Action<TParam> action, TParam obj, bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            return Invoke(result, action, obj, keepException);
        }
        
        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result.
        /// Returns this result
        /// </summary>
        /// <param name="action"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static async Task<TResultType> InvokeAsync<TParam, TResultType>(this TResultType result,
            Func<TParam, Task> action,
            TParam obj,
            bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            try
            {
                await action.Invoke(obj);
            }
            catch (Exception ex)
            {
                HandleInvokeException(result, ex, keepException);
            }

            return result;
        }

        /// <summary>
        /// Invokes the delegate that returns an <see cref="Result"/>, and merges that result into this result
        /// If an exception occurs, records that exception in this result.
        /// Returns this result
        /// </summary>
        public static TResultType Invoke<TResultType>(this TResultType result, Func<Result> func, bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            try
            {
                Result invokedResult = func.Invoke();
                invokedResult.MergeIntoResult(result);
            }
            catch (Exception ex)
            {
                HandleInvokeException(result, ex, keepException);
            }

            return result;
        }

        /// <summary>
        /// Invokes the delegate that returns an <see cref="Result"/>, and merges that result into this result
        /// If an exception occurs, records that exception in this result.
        /// Returns this result
        /// </summary>
        public static TResultType Do<TResultType>(this TResultType result, Func<Result> func, bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            return Invoke(result, func, keepException);
        }
        
        /// <summary>
        /// Invokes the delegate that returns an <see cref="Result"/>, and merges that result into this result
        /// If an exception occurs, records that exception in this result.
        /// Returns this result
        /// </summary>
        /// <param name="func"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static async Task<TResultType> InvokeAsync<TResultType>(this TResultType result, Func<Task<Result>> func, bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            try
            {
                Result invokedResult = await func.Invoke();
                invokedResult.MergeIntoResult(result);
            }
            catch (Exception ex)
            {
                HandleInvokeException(result, ex, keepException);
            }

            return result;
        }

        /// <summary>
        /// Invokes the delegate that returns an <see cref="Result{T}"/>, merges that result into this result, and returns the TResult result
        /// If an exception occurs, records that exception in this result and returns default(TResult).
        /// Returns this result
        /// </summary>
        /// <param name="func"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static TResult InvokeResult<TResult, TResultType>(this TResultType result, Func<Result<TResult>> func, bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            try
            {
                Result<TResult> invokedResult = func.Invoke();
                invokedResult.MergeIntoResult(result); //Merge errors into this
                return invokedResult.Value;
            }
            catch (Exception ex)
            {
                HandleInvokeException(result, ex, keepException);
            }

            return default(TResult);
        }

        /// <summary>
        /// Invokes the delegate that returns an <see cref="Result{T}"/>, merges that result into this result, and returns the TResult result
        /// If an exception occurs, records that exception in this result and returns default(TResult).
        /// Returns this result
        /// </summary>
        /// <param name="func"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static async Task<TResult> InvokeResultAsync<TResult, TResultType>(this TResultType result,
            Func<Task<Result<TResult>>> func,
            bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            try
            {
                Result<TResult> invokedResult = await func.Invoke();
                invokedResult.MergeIntoResult(result); //Merge errors into this
                return invokedResult.Value;
            }
            catch (Exception ex)
            {
                HandleInvokeException(result, ex, keepException);
            }

            return default(TResult);
        }

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result and returns default(TResult).
        /// If success, return the result of the delegate
        /// </summary>
        /// <typeparam name="TResult">The output type</typeparam>
        /// <param name="func">The delegate</param>
        /// <param name="keepException">To keep the exception in the result, or just record the error message</param>
        /// <returns></returns>
        public static TResult InvokeResult<TResult, TResultType>(this TResultType result, Func<TResult> func, bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            try
            {
                TResult resultValue = func.Invoke();
                return resultValue;
            }
            catch (Exception ex)
            {
                HandleInvokeException(result, ex, keepException);
            }

            return default(TResult);
        }

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result and returns default(TResult).
        /// If success, return the result of the delegate
        /// </summary>
        /// <typeparam name="TResult">The output type</typeparam>
        /// <param name="func">The delegate</param>
        /// <param name="keepException">To keep the exception in the result, or just record the error message</param>
        /// <returns></returns>
        public static async Task<TResult> InvokeResultAsync<TResult, TResultType>(this TResultType result,
            Func<Task<TResult>> func,
            bool keepException = false)
            where TResultType: AbstractResult<TResultType>
        {
            try
            {
                TResult resultValue = await func.Invoke();
                return resultValue;
            }
            catch (Exception ex)
            {
                HandleInvokeException(result, ex, keepException);
            }

            return default(TResult);
        }
    }
}