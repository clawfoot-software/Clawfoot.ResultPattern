using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clawfoot.ResultPattern
{
    public class Error : IError
    {
        public Error() { }
        public Error(string message, string? userMessage = "", int code = -1, string? groupName = "", string? memberName = "")
        {
            Message = message ?? string.Empty;
            GroupName = string.IsNullOrEmpty(groupName) ? String.Empty : groupName;
            UserMessage = string.IsNullOrEmpty(userMessage) ? String.Empty : userMessage;
            MemberName = string.IsNullOrEmpty(memberName) ? String.Empty : memberName;
            Code = code;
        }

        /// <inheritdoc/>
        public int Code { get; private set; }
        /// <inheritdoc/>
        public string GroupName { get; private set; } = string.Empty;
        /// <inheritdoc/>
        public string Message { get; private set; } = string.Empty;
        /// <inheritdoc/>
        public string UserMessage { get; private set; } = string.Empty;
        /// <inheritdoc/>
        public string MemberName { get; private set; } = string.Empty;

        /// <inheritdoc/>
        public override string ToString()
        {
            return Message;
        }

        /// <inheritdoc/>
        public string ToUserString()
        {
            if (string.IsNullOrEmpty(UserMessage))
            {
                return Message;
            }

            return UserMessage;
        }

        /// <summary>
        /// Gets the message string from the enum error
        /// </summary>
        /// <typeparam name="TErrorEnum"></typeparam>
        /// <param name="error"></param>
        /// <param name="errorParams"></param>
        /// <returns></returns>
        public static string GetMessage<TErrorEnum>(TErrorEnum error, params string[]? errorParams) where TErrorEnum : Enum
        {
            return From(error, errorParams).ToString();
        }

        /// <summary>
        /// Generates the error details from the enum value
        /// Requires that the enum value has the [Error] Attribute
        /// </summary>
        /// <param name="error"></param>
        /// <param name="errorParams">The parameters to format</param>
        /// <returns></returns>
        public static IError From<TErrorEnum>(TErrorEnum error, params string[]? errorParams) where TErrorEnum : Enum
        {
            ErrorAttribute attribute = GetErrorAttribute(error);

            return new Error()
            {
                Code = attribute.Code,
                GroupName = attribute.GroupName ?? string.Empty,
                Message = attribute.GetFormattedMessage(errorParams) ?? string.Empty,
                UserMessage = attribute.GetFormattedUserMessage(errorParams) ?? string.Empty,
                MemberName = attribute.MemberName ?? string.Empty
            };
        }

        /// <summary>
        /// Generates the error details from the enum value, using only the provided message
        /// Requires that the enum value has the [Error] Attribute
        /// </summary>
        /// <param name="error"></param>
        /// <param name="message">The message, replacing the attribute's</param>
        /// <param name="userMessage">The user-friendly message, replacing the attribute's</param>
        /// <returns></returns>
        public static IError From<TErrorEnum>(TErrorEnum error, string message, string? userMessage = "") where TErrorEnum : Enum
        {
            ErrorAttribute attribute = GetErrorAttribute(error);

            return new Error()
            {
                Code = attribute.Code,
                GroupName = attribute.GroupName ?? string.Empty,
                Message = message ?? string.Empty,
                UserMessage = userMessage ?? string.Empty,
                MemberName = attribute.MemberName ?? string.Empty
            };
        }

        private static ErrorAttribute GetErrorAttribute<TErrorEnum>(TErrorEnum error) where TErrorEnum : Enum
        {
            var enumType = typeof(TErrorEnum);
            var memberInfos = enumType.GetMember(error.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(x => x.DeclaringType == enumType);

            // Undefined enum values (e.g. (MyError)99) have no member to read an attribute from
            ErrorAttribute? attribute = enumValueMemberInfo is null
                ? null
                : (ErrorAttribute?)Attribute.GetCustomAttribute(enumValueMemberInfo, typeof(ErrorAttribute), false);

            if (attribute is null)
            {
                throw new InvalidOperationException("Error enum is expected to have an [Error] attribute to be used in Error<TErrorEnum>.From()");
            }

            return attribute;
        }
    }
}
