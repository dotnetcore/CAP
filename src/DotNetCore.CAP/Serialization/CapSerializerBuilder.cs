using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Serialization
{
    public static class CapSerializerBuilder
    {
        private static IMessageSerializerProvider _messageSerializerProvider;
        

        public static IMessageSerializerProvider MessageSerializerProvider { 

            get {
                return _messageSerializerProvider;  
            }
            set
            {
                if(_messageSerializerProvider == null )
                {
                    _messageSerializerProvider = value;
                }
                
            }

        }

        public static void AddMessageSerializer<TValue, IMessageSerializer>()
        {
            _messageSerializerProvider.AddMessageSerializer(typeof(TValue), typeof(IMessageSerializer));
        }

    }
}
