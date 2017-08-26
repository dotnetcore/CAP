using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public class MessageData
    {
        public string State { get; set; }
        public Message Message { get; set; }
        public DateTime CreatedAt { get; set; }       
    }
}