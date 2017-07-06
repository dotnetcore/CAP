﻿using DotNetCore.CAP.Abstractions;

namespace DotNetCore.CAP.RabbitMQ
{
    public class CapSubscribeAttribute : TopicAttribute
    {
        public CapSubscribeAttribute(string routingKey) : base(routingKey)
        {

        }
    }
}