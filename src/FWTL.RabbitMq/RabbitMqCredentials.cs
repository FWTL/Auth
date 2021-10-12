﻿using FWTL.Common.Setup.Extensions;
using Microsoft.Extensions.Configuration;

namespace FWTL.RabbitMq
{
    public class RabbitMqCredentials
    {
        public RabbitMqCredentials(IConfiguration configuration)
        {
            UserName = configuration.Get("RabbitMq:UserName");
            Password = configuration.Get("RabbitMq:Password");
            Url = configuration.Get("RabbitMq:Url");
        }

        public string Password { get; }

        public string Url { get; }

        public string UserName { get; }
    }
}