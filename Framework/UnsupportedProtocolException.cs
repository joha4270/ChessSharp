using System;

namespace Framework
{
    class UnsupportedProtocolException : Exception
    {
        public UnsupportedProtocolException(string message) : base(message)
        {
            
        }
    }
}