using System;
using System.Threading.Tasks;

namespace Clawfoot.ResultPattern
{
    public partial class Result
    {
        /// <summary>
        /// Helper method that invokes the delegate, and if it throws an exception, records it in a returned result
        /// Returns a new result
        /// </summary>
        /// <param name="action"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static Result Invoke(Action action, bool keepException = false)
        {
            Result result = new Result();

            return result.Invoke(action, keepException);
        }

        /// <summary>
        /// Helper method that invokes the delegate, and if it throws an exception, records it in a returned result
        /// Returns a new result
        /// </summary>
        /// <param name="action"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public async static Task<Result> InvokeAsync(Func<Task> action, bool keepException = false)
        {
            Result result = new Result();

            return await result.InvokeAsync(action, keepException);
        }

        /// <summary>
        /// Helper method that invokes the delegate, and if it throws an exception, records it in a returned result
        /// Returns a new result
        /// </summary>
        /// <param name="action"></param>
        /// <param name="keepException"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Result Invoke<TParam>(Action<TParam> action, TParam obj,
            bool keepException = false)
        {
            Result result = new Result();

            return result.Invoke(action, obj, keepException);
        }

        /// <summary>
        /// Helper method that invokes the delegate, and if it throws an exception, records it in a returned result
        /// Returns a new result
        /// </summary>
        /// <param name="action"></param>
        /// <param name="keepException"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async static Task<Result> InvokeAsync<TParam>(Func<TParam, Task> action, TParam obj,
            bool keepException = false)
        {
            Result result = new Result();

            return await result.InvokeAsync(action, obj, keepException);
        }
        
        /// <summary>
        /// Invokes the delegate, and returns a merged result based on the return or the failure of the delegate
        /// </summary>
        /// <param name="func"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static Result Invoke(Func<Result> func, bool keepException = false)
        {
            Result result = new Result();
            return result.Invoke(func, keepException);
        }

        /// <summary>
        /// Invokes the delegate, and returns a merged result based on the return or the failure of the delegate
        /// </summary>
        /// <param name="func"></param>
        /// <param name="keepException"></param>
        /// <returns></returns>
        public static async Task<Result> Invoke(Func<Task<Result>> func,
            bool keepException = false)
        {
            Result result = new Result();
            return await result.InvokeAsync(func, keepException);
        }

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in a new Result.
        /// If success, return the result of the delegate as a new Result
        /// </summary>
        /// <param name="func">The delegate</param>
        /// <param name="keepException">To keep the exception in the result, or just record the error message</param>
        /// <returns></returns>
        public static Result<TResult> InvokeResult<TResult>(Func<TResult> func,
            bool keepException = false)
        {
            try
            {
                TResult result = func.Invoke();
                return Ok<TResult>(result);
            }
            catch (Exception ex)
            {
                return Result.Error<TResult>(ex);
            }
        }

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in a new Result.
        /// If success, return the result of the delegate as a new Result
        /// </summary>
        /// <param name="func">The delegate</param>
        /// <param name="keepException">To keep the exception in the result, or just record the error message</param>
        /// <returns></returns>
        public static async Task<Result<TResult>> InvokeResultAsync<TResult>(Func<Task<TResult>> func,
            bool keepException = false)
        {
            try
            {
                TResult result = await func.Invoke();
                return Ok<TResult>(result);
            }
            catch (Exception ex)
            {
                return Result.Error<TResult>(ex);
            }
        }

        /// <summary>
        /// Invokes the delegate, and if it throws an exception, records it in a new Result.
        /// If success, return the result of the delegate
        /// </summary>
        /// <param name="func">The delegate</param>
        /// <param name="keepException">To keep the exception in the result, or just record the error message</param>
        /// <returns></returns>
        public static async Task<Result<TResult>> InvokeResultAsync<TResult>(Func<Task<Result<TResult>>> func,
            bool keepException = false)
        {
            try
            {
                return await func.Invoke();
            }
            catch (Exception ex)
            {
                return Result.Error<TResult>(ex);
            }
        }
    }
}