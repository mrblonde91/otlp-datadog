using Orleans;
using Orleans.Hosting;
using Orleans.Statistics;

internal static class OrleansSiloModule
{
    internal static WebApplicationBuilder SetupOrleansSilo(this WebApplicationBuilder builder)
    {
        builder.Host.UseOrleans((siloBuilder) =>
        {
            siloBuilder.UseLocalhostClustering();

            siloBuilder.AddActivityPropagation();

            siloBuilder.UseLinuxEnvironmentStatistics();

        });

        return builder;
    }
}
