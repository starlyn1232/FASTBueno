using System;
using System.Runtime.Serialization;

namespace FASTBueno.Classes
{
    [Serializable]
    public class FastException : Exception
    {
        public FastException() { }
        public FastException(string message) : base(message) { }
        public FastException(string message, Exception inner) : base(message, inner) { }
        protected FastException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
