using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Clawfoot.ResultPattern
{
    /// <summary>
    /// Interface for a generic error
    /// </summary>
    public interface IError
    {
        /// <summary>
        /// The numeric error code for this error
        /// Defaults to -1
        /// </summary>
        int Code { get; }

        /// <summary>
        /// The group, category name, or type for this error
        /// </summary>
        string GroupName { get; }

        /// <summary>
        /// The field or property name this error is for
        /// </summary>
        string MemberName { get; }

        /// <summary>
        /// The primary message for this error
        /// </summary>
        string Message { get; }

        /// <summary>
        /// The optional user friendly message for this error.
        /// </summary>
        string UserMessage { get; }

        /// <summary>
        /// The kind (category) of this error, used to decide how to handle it, e.g. in a central <c>switch</c>.
        /// Any enum works: an application's own kinds, or the built-in <see cref="ErrorKind"/>.
        /// Null when the error has no kind.
        /// </summary>
        /// <remarks>
        /// Matching checks the enum type as well as the value, so <c>error.Kind is AppErrorKind.NotFound</c>
        /// never matches <c>ErrorKind.NotFound</c>, even when the underlying numbers are equal.
        /// Implementations that don't provide a kind inherit this default of null.
        /// </remarks>
        [JsonConverter(typeof(ErrorKindJsonConverter))]
        Enum? Kind => null;

        /// <summary>
        /// Prints out the error message for this error
        /// </summary>
        /// <returns></returns>
        string ToString();

        /// <summary>
        /// Prints out the user friendly error message
        /// If no <see cref="UserMessage"/> exists, than the <see cref="Message"/> will be returned
        /// </summary>
        string ToUserString();
    }
}
