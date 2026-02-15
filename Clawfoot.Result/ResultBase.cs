using System;
using System.Collections.Generic;
using System.Linq;

namespace Clawfoot.ResultPattern
{
    public abstract class ResultBase
    {
        protected internal const string DEFAULT_SUCCESS_MESSAGE = "Success";
        private protected readonly List<IError> _errors = new List<IError>();
        private protected readonly List<Exception> _exceptions = new List<Exception>();
        private protected string _successMessage = DEFAULT_SUCCESS_MESSAGE;

        /// <summary>
        /// Create a generic result
        /// </summary>
        public ResultBase() { }

        /// <summary>
        /// Create a result
        /// </summary>
        /// <param name="successMessage">The default success message</param>
        public ResultBase(string successMessage)
        {
            if (!String.IsNullOrWhiteSpace(successMessage))
            {
                _successMessage = successMessage;
            }
        }

        /// <summary>
        /// The list of errors of this result
        /// </summary>
        public IEnumerable<IError> Errors => _errors.AsEnumerable();

        /// <summary>
        /// The list of exceptions contained in this result
        /// </summary>
        public IEnumerable<Exception> Exceptions => _exceptions.AsEnumerable();

        /// <summary>
        /// If there are no errors this is true
        /// </summary>
        public bool Success => _errors.Count == 0;
        
        /// <summary>
        /// If there are no errors this is true
        /// </summary>
        public bool IsOk => Success;

        /// <summary>
        /// If there are errors this is true
        /// </summary>
        public bool HasErrors => _errors.Count > 0;

        /// <summary>
        /// If the result contains exceptions
        /// </summary>
        public bool HasExceptions => _exceptions.Count > 0;

        /// <summary>
        /// The message of this result, does not combine error messages. Use ToString() instead
        /// </summary>
        public string Message
        {
            get => Success
                ? _successMessage
                : $"Failed with {_errors.Count} error(s)";
            set => _successMessage = value;
        }

        /// <summary>
        /// Combines all error messages into a single string
        /// </summary>
        /// <param name="seperator"></param>
        /// <returns></returns>
        public string ToString(string seperator = "\n")
        {
            if (_errors.Count > 0)
            {
                return string.Join(seperator, _errors);
            }

            return string.Empty;
        }

        /// <summary>
        /// Combines all user-friendly error messages into a single string
        /// </summary>
        /// <param name="seperator"></param>
        /// <returns></returns>
        public string ToUserFriendlyString(string seperator = "\n")
        {
            if (_errors.Count > 0)
            {
                return string.Join(seperator, _errors.Select(x => x.ToUserString()).ToArray());
            }

            return string.Empty;
        }
        
        /// <summary>
        /// Will combine the errors and exceptions of the provided result with this result.
        /// If the provided result has a different success message, and no errors, replaces this results success message with the provided result.
        /// Returns this result
        /// </summary>
        /// <param name="result">The result to merge into this result</param>
        /// <returns>This result</returns>
        public virtual ResultBase MergeResults<TResultType>(TResultType result)
            where TResultType : ResultBase
        {
            _errors.AddRange(result.Errors);
            _exceptions.AddRange(result.Exceptions);

            if (!HasErrors)
            {
                _successMessage = result.Message;
            }

            return this;
        }
        
        // TODO: THIS MAY BE A MISTAKE
        public virtual TReturn MergeResults<TReturn, TResultType>(TResultType result)
            where TReturn : ResultBase
            where TResultType : ResultBase
        {
            return (TReturn)MergeResults(result);
        }
    }
    
    public abstract class AbstractResult<TConcrete> : ResultBase
        where TConcrete : AbstractResult<TConcrete>
    {
        
        /// <summary>
        /// Create a generic result
        /// </summary>
        public AbstractResult() { }

        /// <summary>
        /// Create a result
        /// </summary>
        /// <param name="successMessage">The default success message</param>
        public AbstractResult(string successMessage) : base(successMessage) { }
        
        /// <summary>
        /// Converts this <see cref="Result"/> into an <see cref="Result{T}"/>
        /// </summary>
        /// <typeparam name="T">The Generic Type for the returned result</typeparam>
        /// <returns></returns>
        public virtual Result<T> As<T>()
        {
            Result<T> result = new Result<T>();
            switch (this)
            {
                case Result thisResult:
                    result.MergeResults(thisResult);
                    break;
                case Result<T> thisResultT:
                    result.MergeResults(thisResultT);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Creates a <see cref="Result{T}"/>, merges this result into it, and sets the result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// /// <param name="result"></param>
        /// <returns>A new result</returns>
        public Result<T> SetResult<T>(T result)
        {
            Result<T> resultObj = new Result<T>();
            switch (this)
            {
                case Result thisResult:
                    resultObj.MergeResults(thisResult);
                    break;
                case Result<T> thisResultT:
                    resultObj.MergeResults(thisResultT);
                    break;
            }

            resultObj.SetResult(result);
            return resultObj;
        }
        
        /// <summary>
        /// Will combine the errors and exceptions of this result into the provided result.
        /// Returns the provided result
        /// </summary>
        /// <param name="result">The result to merge into</param>
        /// <returns>The provided result</returns>
        public TResultType MergeIntoResult<TResultType>(TResultType result)
            where TResultType : ResultBase
        {
            return (TResultType)result.MergeResults(this);
        }

        /// <summary>
        /// Adds the provided exception to the result.
        /// This also adds the exception message as <see langword="abstract"/>new error
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public TConcrete AddException(Exception ex)
        {
            _exceptions.Add(ex);
            AddError(ex.Message);
            return (TConcrete)this;
        }

        /// <summary>
        /// Adds a new error to the result
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="userMessage">The user friendly error message</param>
        /// <returns></returns>
        public TConcrete AddError(string message, string userMessage = "")
        {
            _errors.Add(new Error(message, userMessage));
            return (TConcrete)this;
        }

        /// <summary>
        /// Adds a new error to the result
        /// </summary>
        /// <param name="error"></param>
        /// <returns>This result</returns>
        public TConcrete AddError(IError error)
        {
            _errors.Add(error);
            return (TConcrete)this;
        }

        /// <summary>
        /// Adds multiple errors to the result
        /// </summary>
        /// <param name="errors"></param>
        /// <returns>This result</returns>
        public TConcrete AddErrors(IEnumerable<IError> errors)
        {
            foreach (IError error in errors)
            {
                _errors.Add(error);
            }

            return (TConcrete)this;
        }

        /// <summary>
        /// Adds a new error to the result if the item is null
        /// </summary>
        /// <remarks>This only accepts reference types</remarks>
        /// <param name="value">>The value that is checked</param>
        /// <param name="message">The error message</param>
        /// <param name="userMessage">The user friendly error message</param>
        /// <returns></returns>
        public TConcrete AddErrorIfNull<T>(T value, string message, string userMessage = "") where T : class
        {
            if (value is null)
            {
                return AddError(message, userMessage);
            }

            return (TConcrete)this;
        }

        /// <summary>
        /// Adds a new error to the result if the item is null
        /// </summary>
        /// <remarks>This only accepts structs that implement <see cref="Nullable{T}"/></remarks>
        /// <param name="value">>The nullable value that is checked</param>
        /// <param name="message">The error message</param>
        /// <param name="userMessage">The user friendly error message</param>
        /// <returns></returns>
        public TConcrete AddErrorIfNull<T>(T? value, string message, string userMessage = "") where T : struct
        {
            if (value is null)
            {
                return AddError(message, userMessage);
            }

            return (TConcrete)this;
        }

        /// <summary>
        /// Adds a new error to the result if the item is null or is default(T)
        /// </summary>
        /// <remarks>This only accepts reference types</remarks>
        /// <param name="value">>The value that is checked</param>
        /// <param name="message">The error message</param>
        /// <param name="userMessage">The user friendly error message</param>
        /// <returns></returns>
        public TConcrete AddErrorIfNullOrDefault<T>(T value, string message, string userMessage = "") where T : class
        {
            if (value is null || value == default(T))
            {
                return AddError(message, userMessage);
            }

            return (TConcrete)this;
        }

        /// <summary>
        /// Adds a new error to the result if the item is null or is default(T)
        /// </summary>
        /// <remarks>This only accepts structs that implement <see cref="Nullable{T}"/></remarks>
        /// <param name="value">>The nullable value that is checked</param>
        /// <param name="message">The error message</param>
        /// <param name="userMessage">The user friendly error message</param>
        /// <returns></returns>
        public TConcrete AddErrorIfNullOrDefault<T>(T? value, string message, string userMessage = "") where T : struct
        {
            if (value is null || value.Value.Equals(default(T)))
            {
                return AddError(message, userMessage);
            }

            return (TConcrete)this;
        }
    }
}