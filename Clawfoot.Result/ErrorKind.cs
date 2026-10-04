namespace Clawfoot.ResultPattern
{
    /// <summary>
    /// Built-in error kinds covering the HTTP 4xx and 5xx status codes. Each value is its status code,
    /// so <c>(int)ErrorKind.NotFound == 404</c>, and member names match <c>System.Net.HttpStatusCode</c>.
    /// </summary>
    /// <remarks>
    /// Optional: <see cref="IError.Kind"/> accepts any enum, so applications can use their own kinds instead of, or alongside, these.
    /// Every value is distinct (no aliases), so a kind always serializes to the same name.
    /// </remarks>
    public enum ErrorKind
    {
        // 4xx: the caller's request can't be fulfilled as sent

        /// <summary>400: the request is malformed or invalid</summary>
        BadRequest = 400,
        /// <summary>401: the caller isn't authenticated</summary>
        Unauthorized = 401,
        /// <summary>402: payment is required</summary>
        PaymentRequired = 402,
        /// <summary>403: the caller is authenticated but not allowed to do this</summary>
        Forbidden = 403,
        /// <summary>404: the resource doesn't exist</summary>
        NotFound = 404,
        /// <summary>405: the operation isn't supported on this resource</summary>
        MethodNotAllowed = 405,
        /// <summary>406: no acceptable representation can be produced</summary>
        NotAcceptable = 406,
        /// <summary>407: the caller must authenticate with a proxy</summary>
        ProxyAuthenticationRequired = 407,
        /// <summary>408: the request took too long to arrive</summary>
        RequestTimeout = 408,
        /// <summary>409: the request conflicts with the resource's current state</summary>
        Conflict = 409,
        /// <summary>410: the resource existed but is permanently gone</summary>
        Gone = 410,
        /// <summary>411: a length is required</summary>
        LengthRequired = 411,
        /// <summary>412: a precondition supplied by the caller failed</summary>
        PreconditionFailed = 412,
        /// <summary>413: the request content is too large</summary>
        RequestEntityTooLarge = 413,
        /// <summary>414: the request URI is too long</summary>
        RequestUriTooLong = 414,
        /// <summary>415: the content format isn't supported</summary>
        UnsupportedMediaType = 415,
        /// <summary>416: the requested range can't be satisfied</summary>
        RequestedRangeNotSatisfiable = 416,
        /// <summary>417: an expectation supplied by the caller can't be met</summary>
        ExpectationFailed = 417,
        /// <summary>421: the request was sent to a server that can't produce a response</summary>
        MisdirectedRequest = 421,
        /// <summary>422: the request is well-formed but semantically invalid (validation failures)</summary>
        UnprocessableEntity = 422,
        /// <summary>423: the resource is locked</summary>
        Locked = 423,
        /// <summary>424: the request depended on another request that failed</summary>
        FailedDependency = 424,
        /// <summary>425: the request might be replayed and won't be processed yet</summary>
        TooEarly = 425,
        /// <summary>426: the caller must switch to a different protocol</summary>
        UpgradeRequired = 426,
        /// <summary>428: the request must be conditional</summary>
        PreconditionRequired = 428,
        /// <summary>429: the caller is rate limited</summary>
        TooManyRequests = 429,
        /// <summary>431: request header fields are too large</summary>
        RequestHeaderFieldsTooLarge = 431,
        /// <summary>451: the resource is unavailable for legal reasons</summary>
        UnavailableForLegalReasons = 451,

        // 5xx: the failure is on our side

        /// <summary>500: an unexpected failure. Errors created from exceptions get this kind by default.</summary>
        InternalServerError = 500,
        /// <summary>501: the operation isn't implemented</summary>
        NotImplemented = 501,
        /// <summary>502: an upstream dependency returned an invalid response</summary>
        BadGateway = 502,
        /// <summary>503: the service is temporarily unavailable</summary>
        ServiceUnavailable = 503,
        /// <summary>504: an upstream dependency timed out</summary>
        GatewayTimeout = 504,
        /// <summary>505: the protocol version isn't supported</summary>
        HttpVersionNotSupported = 505,
        /// <summary>506: content negotiation is misconfigured</summary>
        VariantAlsoNegotiates = 506,
        /// <summary>507: there isn't enough storage to complete the request</summary>
        InsufficientStorage = 507,
        /// <summary>508: an infinite loop was detected while processing the request</summary>
        LoopDetected = 508,
        /// <summary>510: further extensions to the request are required</summary>
        NotExtended = 510,
        /// <summary>511: the caller must authenticate to gain network access</summary>
        NetworkAuthenticationRequired = 511,
    }
}
