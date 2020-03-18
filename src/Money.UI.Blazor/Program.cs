﻿using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Money.Components;
using Money.Components.Bootstrap;
using Money.Models;
using Money.Models.Api;
using Money.Services;
using Neptuo.Events;
using Neptuo.Exceptions;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Money.UI.Blazor
{
    public class Program
    {
        private static Bootstrap.BootstrapTask bootstrapTask;

        public async static Task Main(string[] args)
        {

            // Configure.
            WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault();
            ConfigureServices(builder.Services);
            ConfigureComponents(builder.RootComponents);

            // Startup.
            WebAssemblyHost host = builder.Build();
            StartupServices(host.Services);

            // Run.
            await host.RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services
                .Configure<ApiClientConfiguration>(configuration =>
                {
#if DEBUG
                    configuration.ApiUrl = new Uri("http://localhost:63803", UriKind.Absolute);
#else
                    configuration.ApiUrl = new Uri("https://api.money.neptuo.com", UriKind.Absolute);
#endif
                })
                .AddAuthorizationCore()
                .AddBaseAddressHttpClient()
                .AddSingleton<ApiAuthenticationStateProvider>()
                .AddSingleton<AuthenticationStateProvider>(provider => provider.GetRequiredService<ApiAuthenticationStateProvider>())
                .AddSingleton<SignalRListener>()
                .AddSingleton<ApiHubService>()
                .AddTransient<Interop>()
                .AddSingleton<PwaInstallInterop>()
                .AddTransient<NetworkStateInterop>()
                .AddSingleton<NetworkState>()
                .AddTransient<CurrencyStorage>()
                .AddTransient<CategoryStorage>()
                .AddTransient<ProfileStorage>()
                .AddTransient<NavigatorUrl>()
                .AddSingleton<Navigator>()
                .AddSingleton<ApiClient>()
                .AddSingleton<ModalInterop>()
                .AddSingleton<TokenContainer>()
                .AddSingleton<QueryString>()
                .AddSingleton<CommandMapper>()
                .AddSingleton<QueryMapper>()
                .AddSingleton<ColorCollection>()
                .AddSingleton<IconCollection>();

            bootstrapTask = new Bootstrap.BootstrapTask(services);
            bootstrapTask.Initialize();
        }

        private static void ConfigureComponents(RootComponentMappingCollection rootComponents)
        {
            rootComponents.Add<App>("app");
        }

        private static void StartupServices(IServiceProvider services)
        {
            bootstrapTask.RegisterHandlers(services);

            services.GetService<IEventHandlerCollection>()
                .AddAll(services.GetService<SignalRListener>());
        }
    }
}
