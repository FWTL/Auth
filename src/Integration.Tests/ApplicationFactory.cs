﻿using FWTL.Management;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Threading.Tasks;

namespace Integration.Tests
{
    public class ApplicationFactory
    {
        public async Task<HttpClient> CreateAsync()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.UseStartup<Startup>();
                });

            var host = await hostBuilder.StartAsync();
            return host.GetTestClient();
        }
    }
}