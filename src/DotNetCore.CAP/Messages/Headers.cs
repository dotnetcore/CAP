﻿namespace DotNetCore.CAP.Messages
{
    public static class Headers
    {
        /// <summary>
        /// Id of the message. Either set the ID explicitly when sending a message, or assign one to the message.
        /// </summary>
        public const string MessageId = "cap-msg-id";

        public const string MessageName = "cap-msg-name";

        public const string Group = "cap-msg-group";

        /// <summary>
        /// Message value .NET type
        /// </summary>
        public const string Type = "cap-msg-type";

        public const string CorrelationId = "cap-corr-id";

        public const string CorrelationSequence = "cap-corr-seq";

        public const string CallbackName = "cap-callback-name";

        public const string SentTime = "cap-senttime";

        public const string Exception = "cap-exception";
    }
}
