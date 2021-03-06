﻿using AspNetCore.IdentityServer4.Auth.Events;
using AspNetCore.IdentityServer4.Auth.Utils.Config;
using AspNetCore.IdentityServer4.Auth.Utils.Extensions;
using AspNetCore.IdentityServer4.Auth.Utils.Service;
using AspNetCore.IdentityServer4.Core.Models.Config.Auth;
using AspNetCore.IdentityServer4.Mvc.OpenApiSpec;
using AspNetCore.IdentityServer4.Service.Ldap;
using IdentityServer.LdapExtension.Extensions;
using IdentityServer.LdapExtension.UserModel;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCore.IdentityServer4.Auth
{
    /// <summary>
    /// Startup
    /// </summary>
    public class Startup
    {
        private readonly AppSettings appSettings = null;

        private IConfiguration configuration { get; }
        private IWebHostEnvironment env { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">IConfiguration</param>
        /// <param name="env">IWebHostEnvironment</param>
        public Startup(
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            this.configuration = configuration;
            this.env = env;
            this.appSettings = new AppSettings();
            this.configuration.Bind(this.appSettings);
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services">Service collection</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                 .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddSession();

            #region Inject AppSetting configuration

            services.Configure<AppSettings>(this.configuration);
            
            // Set static AppSettingProvider
            var globalOptions = new GlobalOptions();
            configuration.GetSection("Global").Bind(globalOptions);
            AppSettingProvider.Global = globalOptions;
            #endregion

            #region OpenAPI specification (Swagger)
            services.AddOpenApiSpec<CustomSwaggerConfig>();
            #endregion

            #region IISOptions
            services.Configure<IISOptions>(options =>
            {
                options.AutomaticAuthentication = false;
                options.AuthenticationDisplayName = "Windows";
            });
            #endregion

            #region Identity Server

            var builder = services.AddIdentityServer(options =>
            {
                // options.PublicOrigin = "https://localhost:6001";
                // options.IssuerUri = "https://localhost:6001";
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                options.Discovery.ResponseCacheInterval = 60;
            });

            // Signing credential
            if (this.env.IsDevelopment())
            {
                builder.AddDeveloperSigningCredential();
            }
            else
            {
                // 1. Store in file (Support renew manually)
                // builder.AddSigningCredentialsByFile(this.appSettings);

                // 2. Store in Redis (Support renew automatically)
                builder.AddSigningCredentialByRedis(this.appSettings);

                // 3. Use cert
                // builder.AddSigningCredentialByCert(this.appSettings, isFromWindowsCertStore: true);
            }

            // Set in-memory, code config
            builder.AddInMemoryIdentityResources(InMemoryInitConfig.GetIdentityResources());
            builder.AddInMemoryApiResources(InMemoryInitConfig.GetApiResources());
            builder.AddInMemoryClients(InMemoryInitConfig.GetClients());
            builder.AddLdapUsers<OpenLdapAppUser>(this.configuration.GetSection("LdapServer"), UserStore.InMemory); // OpenLDAP
            //builder.AddLdapUsers<ActiveDirectoryAppUser>(this.configuration.GetSection("LdapServer"), UserStore.InMemory); // ActiveDirectory

            builder.AddProfileService<ProfileService>();

            #endregion

            #region  Inject Cache service
            services.AddMemoryCache();
            services.AddCacheServices();
            #endregion

            #region Custom sinks
            services.AddScoped<IEventSink, UserProfileCacheSink>();
            #endregion

            #region Custom services
            services.AddSingleton<LdapUserManager>();
            #endregion
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">IApplicationBuilder</param>
        /// <param name="env">IWebHostEnvironment</param>
        /// <param name="provider">IApiVersionDescriptionProvider</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable Swagger and Swagger UI
            app.UseCustomSwagger(provider);

            app.UseIdentityServer();

            app.UseSession();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}