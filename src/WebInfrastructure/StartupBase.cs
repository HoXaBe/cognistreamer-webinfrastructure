﻿using System;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Cors;
using Autofac;
using Autofac.Integration.WebApi;
using AutoMapper;
using Cognistreamer.WebInfrastructure.Services;
using Cognistreamer.WebInfrastructure.Startup;
using Microsoft.Owin.Cors;
using Newtonsoft.Json.Serialization;
using Owin;

// ReSharper disable UnusedMember.Global
namespace Cognistreamer.WebInfrastructure
{
    public abstract class StartupBase
    {
        private readonly Func<Assembly, bool> _registerApiControllerAssemblyPredicate;

        protected StartupBase(Func<Assembly, bool> registerApiControllerAssemblyPredicate = null)
        {
            _registerApiControllerAssemblyPredicate = registerApiControllerAssemblyPredicate;
        }

        public void Configuration(IAppBuilder app)
        {
            var serviceCollection = new ServiceCollection();

            if (_registerApiControllerAssemblyPredicate != null)
                serviceCollection.Builder.RegisterApiControllers(
                    AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Where(_registerApiControllerAssemblyPredicate)
                        .ToArray());

            ConfigureServices(serviceCollection);

            var mapper = new MapperConfiguration(serviceCollection.MapperConfiguration).CreateMapper();
            serviceCollection.Builder.RegisterInstance(mapper).As<IMapper>().SingleInstance();

            var config = new HttpConfiguration();
            var applicationBuilder = new WebApplicationBuilder(app, serviceCollection, config);
            app.UseAutofacMiddleware(applicationBuilder.Services);
            Configure(applicationBuilder);

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.UseDataContractJsonSerializer = false;
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;

            applicationBuilder.MapHttpAttributeRoutesForRegisteredApiControllers(config);
            applicationBuilder.MapRegisteredApiRoutes(config);
            app.UseAutofacWebApi(config);

            app.UseWebApi(config);

            ConfigureFinalMiddleware(app);
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected abstract void Configure(IWebApplicationBuilder app);

        protected virtual void ConfigureFinalMiddleware(IAppBuilder app) { }
    }
}
