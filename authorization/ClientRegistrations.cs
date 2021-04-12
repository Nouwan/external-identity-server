using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace authorization
{
    public class ClientRegistrations : IHostedService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IOptions<AuthorizationConfig> config;

        public ClientRegistrations(IServiceProvider serviceProvider, IOptions<AuthorizationConfig> config)
        {
            this.serviceProvider = serviceProvider;
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<DbContext>();
            await context.Database.EnsureCreatedAsync(cancellationToken);

            ClientRegistration[] clientRegistrations;
            await using (var stream = File.OpenRead(config.Value.ClientRegistrationsPath))
            {
                clientRegistrations = await JsonSerializer
                   .DeserializeAsync<ClientRegistration[]>(stream,
                       cancellationToken: cancellationToken);
            }

            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            if (clientRegistrations == null) return;

            foreach (var clientRegistration in clientRegistrations)
            {
                if (await manager.FindByClientIdAsync(clientRegistration.ClientId, cancellationToken) == null)
                {
                    await manager.CreateAsync(new OpenIddictApplicationDescriptor
                    {
                        ClientId = clientRegistration.ClientId,
                        ClientSecret = clientRegistration.ClientSecret,
                        DisplayName = clientRegistration.DisplayName,
                        Permissions =
                        {
                            OpenIddictConstants.Permissions.Endpoints.Token,
                            OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                            OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                        }
                    }, cancellationToken);
                }
            }

            if (config.Value.Environment != Environment.Development)
            {
                File.Delete(config.Value.ClientRegistrationsPath);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class ClientRegistration
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string DisplayName { get; set; }
    }
}
