using System.Runtime.Serialization;

namespace EasyTemplateXCS.Compilation
{
    [Serializable]
    public class MissingCloseDelimiterException : Exception
    {
        private object openTagText;

        public MissingCloseDelimiterException()
        {
        }

        public MissingCloseDelimiterException(object openTagText)
        {
            this.openTagText = openTagText;
        }

        public MissingCloseDelimiterException(string? message) : base(message)
        {
        }

        public MissingCloseDelimiterException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected MissingCloseDelimiterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class MissingStartDelimiterException : Exception
    {
        private object closeTagText;

        public MissingStartDelimiterException()
        {
        }

        public MissingStartDelimiterException(object closeTagText)
        {
            this.closeTagText = closeTagText;
        }

        public MissingStartDelimiterException(string? message) : base(message)
        {
        }

        public MissingStartDelimiterException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected MissingStartDelimiterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class UnclosedTagException : Exception
    {
        private object name;

        public UnclosedTagException()
        {
        }

        public UnclosedTagException(object name)
        {
            this.name = name;
        }

        public UnclosedTagException(string? message) : base(message)
        {
        }

        public UnclosedTagException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected UnclosedTagException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class UnknownContentTypeException : Exception
    {
        private string contentType;
        private string rawText;
        private string pathString;

        public UnknownContentTypeException()
        {
        }

        public UnknownContentTypeException(string? message) : base(message)
        {
        }

        public UnknownContentTypeException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        public UnknownContentTypeException(string contentType, string rawText, string pathString)
        {
            this.contentType = contentType;
            this.rawText = rawText;
            this.pathString = pathString;
        }

        protected UnknownContentTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class UnopenedTagException : Exception
    {
        private object name;

        public UnopenedTagException()
        {
        }

        public UnopenedTagException(object name)
        {
            this.name = name;
        }

        public UnopenedTagException(string? message) : base(message)
        {
        }

        public UnopenedTagException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected UnopenedTagException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}