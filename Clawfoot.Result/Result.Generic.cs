using System;
using System.Collections.Generic;

namespace Clawfoot.ResultPattern
{
    /// <summary>
    /// A generic version of a result (immutable)
    /// </summary>
    public class Result<T> : AbstractResult<Result<T>>
    {
        private readonly T _value;

        public Result() { _value = default; }

        /// <summary>
        /// Creates a result with the value and optional success message
        /// </summary>
        public Result(T value, string successMessage = null)
            : base(successMessage)
        {
            _value = value;
        }

        /// <summary>
        /// Creates a result with initial errors and/or exceptions (no value)
        /// </summary>
        public Result(
            IEnumerable<IError> errors,
            IEnumerable<Exception> exceptions = null,
            string successMessage = null)
            : base(errors, exceptions, successMessage)
        {
            _value = default;
        }

        /// <summary>
        /// Creates a result with initial errors, exceptions, success message, and value
        /// </summary>
        public Result(
            IEnumerable<IError> errors,
            IEnumerable<Exception> exceptions,
            string successMessage,
            T value)
            : base(errors, exceptions, successMessage)
        {
            _value = value;
        }

        /// <summary>
        /// The returned value (read-only)
        /// </summary>
        public T Value => _value;

        /// <summary>
        /// If this result has a value (non-default). Returns false if there are errors.
        /// </summary>
        public bool HasResult
        {
            get
            {
                if (HasErrors) return false;
                return !EqualityComparer<T>.Default.Equals(_value, default);
            }
        }

        /// <summary>
        /// Returns a new result with the same errors/exceptions/success message but the given value
        /// </summary>
        public Result<T> WithValue(T value)
        {
            return new Result<T>(_errors, _exceptions, _successMessage, value);
        }

        protected override Result<T> CreateWith(
            IReadOnlyList<IError> errors,
            IReadOnlyList<Exception> exceptions,
            string successMessage)
        {
            return new Result<T>(errors, exceptions, successMessage);
        }

        /// <summary>
        /// Converts this result to one with the provided value type (same errors/exceptions)
        /// </summary>
        public Result<TResult> To<TResult>(TResult value)
        {
            return new Result<TResult>(_errors, _exceptions, _successMessage, value);
        }

        public static implicit operator Result<T>(T value)
        {
            return new Result<T>(value);
        }

        public static implicit operator Result(Result<T> generic)
        {
            return new Result(generic);
        }

        public static implicit operator Result<T>(Result value)
        {
            return value?.As<T>();
        }

        public void Deconstruct(out Result result, out T resultValue)
        {
            result = this;
            resultValue = Value;
        }

        public void Deconstruct(out Result result, out T resultValue, out bool success)
        {
            result = this;
            resultValue = Value;
            success = Success;
        }
    }
}
