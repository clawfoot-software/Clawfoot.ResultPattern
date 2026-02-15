using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Clawfoot.Result
{
    /// <summary>
    /// A generic version of a result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Result<T> : AbstractResult<Result<T>>
    {
        private T _result;


        public Result() { }

        /// <summary>
        /// Creates a <see cref="Result{T}"/> with the result and success message
        /// </summary>
        /// <param name="result"></param>
        /// <param name="successMessage"></param>
        public Result(T result, string successMessage = null)
            : base(successMessage)
        {
            _result = result;
        }

        /// <summary>
        /// The returned value
        /// </summary>
        public T Value
        {
            get => _result;
            set => _result = value;
        }

        /// <summary>
        /// If this result has a result.
        /// Returns false if there are errors, even if a result has been set
        /// </summary>
        public bool HasResult
        {
            get
            {
                if (EqualityComparer<T>.Default.Equals(_result, default(T))) return false;

                return true;
            }
        }

        /// <summary>
        /// Sets the result of the result
        /// </summary>
        /// <param name="result"></param>
        public Result<T> SetResult(T result)
        {
            Value = result;
            return this;
        }

        public Result<T> MergeResults(Result result)
        {
            return base.MergeResults<Result<T>, Result>(result);
        }

        /// <summary>
        /// Will combine the result, errors, and exceptions of the provided result with this result.
        /// If the provided result has a different success message, and no errors, replaces this results success message with the provided result.
        /// Will prioritize keeping the result that exists. If this result doesn't have a result, and the provided result does, will keep the provided result.
        /// If both results have a result, this will prioritize the current results result over the provided result.
        /// Returns this result
        /// </summary>
        /// <param name="result">The result to merge into this result</param>
        /// <returns>This result</returns>
        public Result<T> MergeResults(Result<T> result)
        {
            base.MergeResults(result);

            // If this doesn't have a result, and result does, keep the provided result
            // This also implicitly means that an existing result on this result is maintained either way
            if (!HasResult && result.HasResult)
            {
                SetResult(result.Value);
            }

            return this;
        }

        /// <summary>
        /// Will combine the errors, exceptions, and result of this result into the provided result using <see cref="MergeResults(Result{T})"/>.
        /// Returns the provided result
        /// </summary>
        /// <param name="result">The result to merge into</param>
        /// <returns>The provided result</returns>
        public Result<T> MergeIntoResult(Result<T> result)
        {
            return result.MergeResults(this);
        }

        /// <summary>
        /// Will combine the errors and exceptions of this result into the provided result using <see cref="Result.MergeResults(Result)"/>.
        /// Returns the result of this result
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T MergeIntoResultAndReturnResult(Result result)
        {
            result.MergeResults((ResultBase)this);
            return this.Value;
        }

        /// <summary>
        /// Will combine the errors, exceptions, and result of this result into the provided result using <see cref="MergeResults(Result{T})"/>.
        /// Returns the result of this result
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public T MergeIntoResultAndReturnResult(Result<T> result)
        {
            result.MergeResults(this);
            return this.Value;
        }

        /// <summary>
        /// Converts this generic result to one with the provided result type
        /// Used by the MapTo extension method
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="result"></param>
        public Result<TResult> To<TResult>(TResult result)
        {
            Result<TResult> resultObj = new Result<TResult>();
            resultObj.SetResult(result);

            resultObj.MergeResults(this);
            return resultObj;

        }
        
        public static implicit operator Result<T>(T value)
        {
            return new Result<T>(value);
        }
        
        public static implicit operator Result(Result<T> generic)
        {
            return (Result)new Result().MergeResults(generic);
        }
        
        public static implicit operator Result<T>(Result value)
        {
            return value.As<T>();
        }
        
        /// <summary>
        /// Deconstructs the result into its parts
        /// </summary>
        /// <param name="result"></param>
        /// <param name="resultValue"></param>
        public void Deconstruct(out Result result, out T resultValue)
        {
            result = this;
            resultValue = Value;
        }
        
        /// <summary>
        ///  Deconstructs the result into its parts
        /// </summary>
        /// <param name="result"></param>
        /// <param name="resultValue"></param>
        /// <param name="success"></param>
        public void Deconstruct(out Result result, out T resultValue, out bool success)
        {
            result = this;
            resultValue = Value;
            success = Success;
        }
    }
}


