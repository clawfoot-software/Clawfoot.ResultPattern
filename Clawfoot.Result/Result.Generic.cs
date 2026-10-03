using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Clawfoot.ResultPattern
{
    /// <summary>
    /// A generic version of a result (immutable).
    /// A successful <see cref="Result{T}"/> always carries a non-null <see cref="Value"/>; a failed one may or may not.
    /// Use a plain <see cref="Result"/> for success without a value.
    /// </summary>
    public class Result<T> : AbstractResult<Result<T>>
    {
        private readonly T? _value;
        private readonly bool _hasValue;

        /// <summary>
        /// Creates a successful result with the value and optional success message
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
        public Result([DisallowNull] T value, string? successMessage = null)
            : base(successMessage)
        {
            if (value is null) throw ValueRequiredException(nameof(value));
            _value = value;
            _hasValue = true;
        }

        /// <summary>
        /// Creates a failed result with initial errors and/or exceptions (no value)
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="errors"/> is null or empty</exception>
        public Result(
            IEnumerable<IError> errors,
            IEnumerable<Exception>? exceptions = null,
            string? successMessage = null)
            : base(errors, exceptions, successMessage)
        {
            if (_errors.Count == 0)
                throw new ArgumentException(
                    $"A Result<{typeof(T).Name}> without a value must contain at least one error. " +
                    "Use a plain Result for success without a value.",
                    nameof(errors));
        }

        /// <summary>
        /// Creates a result with initial errors, exceptions, success message, and value
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null and there are no errors</exception>
        public Result(
            IEnumerable<IError>? errors,
            IEnumerable<Exception>? exceptions,
            string? successMessage,
            [DisallowNull] T value)
            : this(errors, exceptions, successMessage, value, value is not null)
        { }

        internal Result(
            IEnumerable<IError>? errors,
            IEnumerable<Exception>? exceptions,
            string? successMessage,
            T? value,
            bool hasValue)
            : base(errors, exceptions, successMessage)
        {
            if (_errors.Count == 0 && !hasValue) throw ValueRequiredException(nameof(value));
            _value = hasValue ? value : default;
            _hasValue = hasValue;
        }

        /// <summary>
        /// The value. Never null when the result is successful (check <see cref="HasErrors"/>,
        /// <see cref="Success"/> or <see cref="TryGetValue"/> first and the compiler knows it).
        /// A failed result may still carry the value it had before the error was added.
        /// </summary>
        public T? Value => _value;

        /// <summary>
        /// If there are errors this is true. When false, <see cref="Value"/> is not null.
        /// </summary>
        [MemberNotNullWhen(false, nameof(Value))]
        public new bool HasErrors => base.HasErrors;

        /// <summary>
        /// If there are no errors this is true. When true, <see cref="Value"/> is not null.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Value))]
        public new bool Success => base.Success;

        /// <summary>
        /// If there are no errors this is true. When true, <see cref="Value"/> is not null.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Value))]
        public new bool IsOk => base.Success;

        /// <summary>
        /// True when the result is successful (and therefore carries a value).
        /// Default values such as <c>0</c> or <c>false</c> count as values. Returns false if there are errors.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Value))]
        public bool HasResult => !base.HasErrors && _hasValue;

        /// <summary>
        /// Gets the value if the result is successful.
        /// </summary>
        /// <returns>True and the non-null value when successful; otherwise false and default</returns>
        public bool TryGetValue([NotNullWhen(true)] out T? value)
        {
            if (base.HasErrors)
            {
                value = default;
                return false;
            }

            value = _value!;
            return true;
        }

        /// <summary>
        /// Returns the value, or throws if the result has errors.
        /// Intended for tests and startup code where a failure is a bug; use <see cref="HasErrors"/> or
        /// <see cref="TryGetValue"/> on normal code paths.
        /// </summary>
        /// <exception cref="InvalidOperationException">The result has errors. The message lists them; the inner exception
        /// is the result's exception (or an <see cref="AggregateException"/> of them) when there are any.</exception>
        [return: NotNull]
        public T GetValueOrThrow()
        {
            if (base.HasErrors)
            {
                Exception? inner = _exceptions.Count switch
                {
                    0 => null,
                    1 => _exceptions[0],
                    _ => new AggregateException(_exceptions)
                };
                throw new InvalidOperationException(
                    $"Result<{typeof(T).Name}> has {_errors.Count} error(s): {ToString("; ")}", inner);
            }

            return _value!;
        }

        /// <summary>
        /// Returns a new result with the same errors/exceptions/success message but the given value
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null and this result is successful</exception>
        public Result<T> WithValue([DisallowNull] T value)
        {
            return new Result<T>(_errors, _exceptions, _successMessage, value, value is not null);
        }

        protected override Result<T> CreateWith(
            IReadOnlyList<IError> errors,
            IReadOnlyList<Exception> exceptions,
            string successMessage)
        {
            return new Result<T>(errors, exceptions, successMessage, _value, _hasValue);
        }

        /// <summary>
        /// Converts this result to one with the provided value type (same errors/exceptions)
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null and this result is successful</exception>
        public Result<TResult> To<TResult>([DisallowNull] TResult value)
        {
            return new Result<TResult>(_errors, _exceptions, _successMessage, value, value is not null);
        }

        /// <summary>
        /// Creates a successful result carrying <paramref name="value"/>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
        public static implicit operator Result<T>([DisallowNull] T value)
        {
            return new Result<T>(value);
        }

        public static implicit operator Result(Result<T> generic)
        {
            return new Result(generic);
        }

        /// <summary>
        /// Propagates a <b>failed</b> <see cref="Result"/> as a <see cref="Result{T}"/>. See <see cref="AbstractResult{TConcrete}.As{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"><paramref name="value"/> is successful</exception>
        [return: NotNullIfNotNull(nameof(value))]
        public static implicit operator Result<T>?(Result? value)
        {
            return value?.As<T>();
        }

        public void Deconstruct(out Result result, out T? resultValue)
        {
            result = this;
            resultValue = Value;
        }

        public void Deconstruct(out Result result, out T? resultValue, out bool success)
        {
            result = this;
            resultValue = Value;
            success = Success;
        }

        internal static ArgumentNullException ValueRequiredException(string paramName)
        {
            return new ArgumentNullException(
                paramName,
                $"A successful Result<{typeof(T).Name}> must carry a non-null value. " +
                "Return a plain Result for success without a value, or (Result, T?) when the value is optional.");
        }

        internal static InvalidOperationException SuccessWithoutValueException(ResultBase source)
        {
            return new InvalidOperationException(
                $"Cannot convert a successful {DisplayName(source.GetType())} to Result<{typeof(T).Name}>: " +
                "a successful Result<T> must carry a value. Only failed results can be converted without one; " +
                "return the value (or Result.Ok(value) / SetResult(value)) instead.");
        }

        private static string DisplayName(Type type)
        {
            if (!type.IsGenericType) return type.Name;
            string name = type.Name.Substring(0, type.Name.IndexOf('`'));
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(DisplayName))}>";
        }
    }
}
