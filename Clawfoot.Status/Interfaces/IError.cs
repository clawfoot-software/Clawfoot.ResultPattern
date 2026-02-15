using System;
using System.Collections.Generic;
using System.Text;

namespace Clawfoot.Result
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
