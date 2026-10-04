using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Clawfoot.ResultPattern
{
    public class Error : IError
    {
        public Error() { }
        public Error(string message, string? userMessage = "", int code = -1, string? groupName = "", string? memberName = "")
            : this(message, null, userMessage, code, groupName, memberName)
        { }

        /// <summary>
        /// Create an error with a kind
        /// </summary>
        /// <param name="message">The primary message</param>
        /// <param name="kind">The kind (category) of the error: any enum, e.g. an application's own kinds or <see cref="ErrorKind"/></param>
        /// <param name="userMessage">The optional user friendly message</param>
        /// <param name="code">The app-specific error code</param>
        /// <param name="groupName">The group or category name</param>
        /// <param name="memberName">The field or property name this error is for</param>
        [JsonConstructor]
        public Error(string message, Enum? kind, string? userMessage = "", int code = -1, string? groupName = "", string? memberName = "")
        {
            Message = message ?? string.Empty;
            Kind = kind;
            GroupName = string.IsNullOrEmpty(groupName) ? String.Empty : groupName;
            UserMessage = string.IsNullOrEmpty(userMessage) ? String.Empty : userMessage;
            MemberName = string.IsNullOrEmpty(memberName) ? String.Empty : memberName;
            Code = code;
        }

        /// <inheritdoc/>
        [JsonConverter(typeof(ErrorKindJsonConverter))]
        public Enum? Kind { get; private set; }
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
                Kind = GetKind(attribute, error),
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
                Kind = GetKind(attribute, error),
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

        private static Enum? GetKind<TErrorEnum>(ErrorAttribute attribute, TErrorEnum error) where TErrorEnum : Enum
        {
            switch (attribute.Kind)
            {
                case null:
                    return null;
                case Enum kind:
                    return kind;
                default:
                    throw new InvalidOperationException(
                        $"[Error(Kind = ...)] on {typeof(TErrorEnum).Name}.{error} must be an enum value, but was a {attribute.Kind.GetType().Name}");
            }
        }

        /// <summary>
        /// The error recorded for an exception. Exceptions are unexpected failures, so the kind defaults to <see cref="ErrorKind.InternalServerError"/>.
        /// </summary>
        internal static Error FromException(Exception ex, Enum? kind)
        {
            return new Error(ex.Message, kind);
        }

        internal static Error FromException(Exception ex)
        {
            return FromException(ex, ErrorKind.InternalServerError);
        }
    }
}
