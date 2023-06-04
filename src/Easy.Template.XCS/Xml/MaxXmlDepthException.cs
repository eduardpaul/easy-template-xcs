using System.Runtime.Serialization;

namespace Easy.Template.XCS.Xml
{
    [Serializable]
    internal class MaxXmlDepthException : Exception
    {
        private int maxDepth;

        public MaxXmlDepthException()
        {
        }

        public MaxXmlDepthException(int maxDepth)
        {
            this.maxDepth = maxDepth;
        }

        public MaxXmlDepthException(string? message) : base(message)
        {
        }

        public MaxXmlDepthException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected MaxXmlDepthException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}