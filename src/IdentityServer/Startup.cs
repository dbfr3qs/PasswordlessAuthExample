// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace IdentityServer
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // uncomment, if you want to add an MVC-based UI
            services.AddControllersWithViews().AddNewtonsoftJson();
            
            var builder = services.AddIdentityServer(options =>
                {
                    // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                    options.EmitStaticAudienceClaim = true;
                })
                .AddInMemoryIdentityResources(Config.IdentityResources)
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryClients(Config.Clients)
                .AddTestUsers(TestUsers.Users);
            
            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromMinutes(2);
                options.Cookie.HttpOnly = true;
                // Strict SameSite mode is required because the default mode used
                // by ASP.NET Core 3 isn't understood by the Conformance Tool
                // and breaks conformance testing
                //options.Cookie.SameSite = SameSiteMode.Unspecified;
            });
            services.AddFido2(options =>
                {
                    options.ServerDomain = Configuration["fido2:serverDomain"];
                    options.ServerName = "IdentityServerPasswordlessTest";
                    options.Origin = Configuration["fido2:origin"];
                    options.TimestampDriftTolerance = Configuration.GetValue<int>("fido2:timestampDriftTolerance");
                    options.MDSAccessKey = Configuration["fido2:MDSAccessKey"];
                    options.MDSCacheDirPath = Configuration["fido2:MDSCacheDirPath"];
                })
                .AddCachedMetadataService(config =>
                {
                    //They'll be used in a "first match wins" way in the order registered

                    if (!string.IsNullOrWhiteSpace(Configuration["fido2:MDSAccessKey"]))
                    {
                        config.AddFidoMetadataRepository(Configuration["fido2:MDSAccessKey"]);
                    }
                    config.AddStaticMetadataRepository();
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // uncomment if you want to add MVC
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseIdentityServer();

            // uncomment, if you want to add MVC
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
