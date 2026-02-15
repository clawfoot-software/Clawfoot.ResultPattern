using System;
using System.Collections.Generic;
using System.Text;

namespace Clawfoot.Result
{

    /// <summary>
    /// Attribute used to define error information for enum errors
    /// The message is not required, as the error may be system generated and provides it's own message
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ErrorAttribute : Attribute
    {
        public ErrorAttribute() { }

        public int Code { get; set; }
        public string GroupName { get; set; }
        public string Message { get; set; }
        public string UserMessage { get; set; }

        public string MemberName { get; set; }

        /// <summary>
        /// Formats the error message for this error
        /// If there are no values, returns an unformatted message
        /// </summary>
        public string GetFormattedMessage(params string[] values)
        {
            if(values is null || values.Length == 0)
            {
                return Message;
            }

            return String.Format(Message, values);
        }

        /// <summary>
        /// Formats the user friendly error message for this error
        /// If no <see cref="UserMessage"/> exists, than the <see cref="Message"/> will be returned
        /// If there are no values, returns an unformatted message
        /// </summary>
        public string GetFormattedUserMessage(params string[] values)
        {
            string message;

            if (String.IsNullOrEmpty(UserMessage))
            {
                message = Message;
            }
            else
            {
                message = UserMessage;
            }


            if (values is null || values.Length == 0)
            {
                return message;
            }

            return String.Format(message, values);
        }
    }
}
