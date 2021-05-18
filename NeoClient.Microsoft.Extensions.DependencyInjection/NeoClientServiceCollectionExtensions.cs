using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Neo4j.Driver.V1;
using NeoClient;
using NeoClient.Microsoft.Extensions.DependencyInjection.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class NeoClientServiceCollectionExtensions
    {
        public static void AddNeoClient(
            this IServiceCollection services,
            IConfiguration configuration,
            Config neoClientConfig = null,
            string sectionName = null)
        {
            var options = configuration.GetSection(sectionName ?? "NeoClient").Get<NeoClientOptions>();

            var client = new NeoClient.NeoClient(
                uri: options.Uri,
                userName: options.UserName,
                password: options.Password,
                config: neoClientConfig,
                options.StripHyphens
            );

            services.TryAddTransient<INeoClient>(service =>
            {
                client.Connect();

                return client;
            });
        }

        public static void AddNeoClient(
            this IServiceCollection services,
            string uri,
            string userName = null,
            string password = null,
            Config config = null,
            bool stripHyphens = false)
        {
            var client = new NeoClient.NeoClient(uri,
                userName,
                password,
                config,
                stripHyphens);

            services.TryAddTransient<INeoClient>(service =>
            {
                client.Connect();

                return client;
            });
        }
    }
}