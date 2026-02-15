using System;
using System.Threading.Tasks;

namespace Clawfoot.Result
{
    public static class GenericInvokeExtenions
    {
        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result and returns null.
        /// If success, sets the result value, and returns the result of the delegate
        /// </summary>
        /// <param name="func"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static T InvokeResult<T>(this Result<T> result, Func<T> func, bool keepException = false)
        {
            try
            {
                T resultValue = func.Invoke();
                result.SetResult(resultValue);
                return resultValue;
            }
            catch (Exception ex)
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

            return default(T);
        }
        
        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result and returns null.
        /// If success, unwraps the nested result, sets the result value, and returns the result of the delegate
        /// </summary>
        /// <param name="func"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static T InvokeResult<T>(this Result<T> result, Func<Result<T>> func, bool keepException = false)
        {
            try
            {
                Result<T> invokedResult = func.Invoke();
                result.MergeResults(invokedResult);
                result.SetResult(invokedResult.Value);
                
                return invokedResult.Value;
            }
            catch (Exception ex)
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
        
            return default(T);
        }
        

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result and returns null.
        /// If success, sets the result value, and returns the result of the delegate
        /// </summary>
        /// <param name="func"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static async Task<T> InvokeResultAsync<T>(this Result<T> result, Func<Task<T>> func,
            bool keepException = false)
        {
            try
            {
                T resultValue = await func.Invoke();
                result.SetResult(resultValue);
                return resultValue;
            }
            catch (Exception ex)
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

            return default(T);
        }
        
        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in the current result and returns null.
        /// If success, unwraps the nested result, sets the result value, and returns the result of the delegate
        /// </summary>
        /// <param name="func"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static async Task<T> InvokeResultAsync<T>(this Result<T> result, Func<Task<Result<T>>> func,
            bool keepException = false)
        {
            try
            {
                Result<T> invokedResult = await func.Invoke();
                result.MergeResults(invokedResult);
                result.SetResult(invokedResult.Value);
                
                return invokedResult.Value;
            }
            catch (Exception ex)
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

            return default(T);
        }
    }
}