using System;
using System.Collections.Generic;
using System.Linq;

namespace Clawfoot.ResultPattern
{
    public abstract class ResultBase
    {
        protected internal const string DEFAULT_SUCCESS_MESSAGE = "Success";
        private protected readonly IReadOnlyList<IError> _errors;
        private protected readonly IReadOnlyList<Exception> _exceptions;
        private protected readonly string _successMessage;

        /// <summary>
        /// Create a generic result
        /// </summary>
        public ResultBase()
        {
            _errors = Array.Empty<IError>();
            _exceptions = Array.Empty<Exception>();
            _successMessage = DEFAULT_SUCCESS_MESSAGE;
        }

        /// <summary>
        /// Create a result
        /// </summary>
        /// <param name="successMessage">The default success message</param>
        public ResultBase(string successMessage)
        {
            _errors = Array.Empty<IError>();
            _exceptions = Array.Empty<Exception>();
            _successMessage = !string.IsNullOrWhiteSpace(successMessage) ? successMessage : DEFAULT_SUCCESS_MESSAGE;
        }

        /// <summary>
        /// Create a result with initial errors and/or exceptions
        /// </summary>
        public ResultBase(
            IEnumerable<IError> errors,
            IEnumerable<Exception> exceptions = null,
            string successMessage = null)
        {
            _errors = errors?.ToArray() ?? Array.Empty<IError>();
            _exceptions = exceptions?.ToArray() ?? Array.Empty<Exception>();
            _successMessage = !string.IsNullOrWhiteSpace(successMessage) ? successMessage : DEFAULT_SUCCESS_MESSAGE;
        }

        /// <summary>
        /// The list of errors of this result
        /// </summary>
        public IEnumerable<IError> Errors => _errors;

        /// <summary>
        /// The list of exceptions contained in this result
        /// </summary>
        public IEnumerable<Exception> Exceptions => _exceptions;

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
        public string Message =>
            Success
                ? _successMessage
                : $"Failed with {_errors.Count} error(s)";

        /// <summary>
        /// Combines all error messages into a single string
        /// </summary>
        public string ToString(string seperator = "\n")
        {
            if (_errors.Count > 0)
                return string.Join(seperator, _errors);
            return string.Empty;
        }

        /// <summary>
        /// Combines all user-friendly error messages into a single string
        /// </summary>
        public string ToUserFriendlyString(string seperator = "\n")
        {
            if (_errors.Count > 0)
                return string.Join(seperator, _errors.Select(x => x.ToUserString()).ToArray());
            return string.Empty;
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
        public AbstractResult(string successMessage) : base(successMessage) { }

        /// <summary>
        /// Create a result with initial errors and/or exceptions
        /// </summary>
        public AbstractResult(
            IEnumerable<IError> errors,
            IEnumerable<Exception> exceptions = null,
            string successMessage = null)
            : base(errors, exceptions, successMessage)
        { }

        /// <summary>
        /// Converts this result into a <see cref="Result{T}"/> (same errors/exceptions, no value)
        /// </summary>
        public virtual Result<T> As<T>()
        {
            return new Result<T>(_errors, _exceptions, _successMessage);
        }

        /// <summary>
        /// Returns a new <see cref="Result{T}"/> with this result's errors/exceptions and the given value
        /// </summary>
        public Result<T> SetResult<T>(T value)
        {
            return new Result<T>(_errors, _exceptions, _successMessage, value);
        }

        /// <summary>
        /// Returns a new result with an additional exception (and its message as error)
        /// </summary>
        public TConcrete WithException(Exception ex)
        {
            var errors = _errors.Concat(new[] { new Error(ex.Message) }).ToArray();
            var exceptions = _exceptions.Concat(new[] { ex }).ToArray();
            return CreateWith(errors, exceptions, _successMessage);
        }

        /// <summary>
        /// Returns a new result with an additional error
        /// </summary>
        public TConcrete WithError(string message, string userMessage = "")
        {
            var errors = _errors.Concat(new[] { (IError)new Error(message, userMessage) }).ToArray();
            return CreateWith(errors, _exceptions, _successMessage);
        }

        /// <summary>
        /// Returns a new result with an additional error
        /// </summary>
        public TConcrete WithError(IError error)
        {
            var errors = _errors.Concat(new[] { error }).ToArray();
            return CreateWith(errors, _exceptions, _successMessage);
        }

        /// <summary>
        /// Returns a new result with additional errors
        /// </summary>
        public TConcrete WithErrors(IEnumerable<IError> errors)
        {
            var newErrors = _errors.Concat(errors ?? Array.Empty<IError>()).ToArray();
            return CreateWith(newErrors, _exceptions, _successMessage);
        }

        /// <summary>
        /// Returns a new result with an error if the condition is true
        /// </summary>
        public TConcrete WithErrorIf(bool condition, string message, string userMessage = "")
        {
            if (!condition) return (TConcrete)this;
            return WithError(message, userMessage);
        }

        /// <summary>
        /// Returns a new result with an error if the value is null (reference types)
        /// </summary>
        public TConcrete WithErrorIfNull<T>(T value, string message, string userMessage = "") where T : class
        {
            if (value is null) return WithError(message, userMessage);
            return (TConcrete)this;
        }

        /// <summary>
        /// Returns a new result with an error if the nullable value is null
        /// </summary>
        public TConcrete WithErrorIfNull<T>(T? value, string message, string userMessage = "") where T : struct
        {
            if (value is null) return WithError(message, userMessage);
            return (TConcrete)this;
        }

        /// <summary>
        /// Returns a new result with an error if the value is null or default (reference types)
        /// </summary>
        public TConcrete WithErrorIfNullOrDefault<T>(T value, string message, string userMessage = "") where T : class
        {
            if (value is null || value == default) return WithError(message, userMessage);
            return (TConcrete)this;
        }

        /// <summary>
        /// Returns a new result with an error if the nullable value is null or default
        /// </summary>
        public TConcrete WithErrorIfNullOrDefault<T>(T? value, string message, string userMessage = "") where T : struct
        {
            if (value is null || value.Value.Equals(default(T))) return WithError(message, userMessage);
            return (TConcrete)this;
        }

        /// <summary>
        /// Override in concrete types to create a new instance with the given state
        /// </summary>
        protected abstract TConcrete CreateWith(
            IReadOnlyList<IError> errors,
            IReadOnlyList<Exception> exceptions,
            string successMessage);
    }
}
