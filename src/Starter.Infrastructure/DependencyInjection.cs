using Hangfire;
using Hangfire.Common;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Starter.Infrastructure.Jobs;

namespace Starter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfireServices(configuration);

        return services;
    }

    public static IApplicationBuilder UseInfrastructureServices(this IApplicationBuilder app)
    {
        app.UseHangfire();

        return app;
    }

    private static void AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseFilter(new AutomaticRetryAttribute
            {
                Attempts = 0
            })
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("db"),
                new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromMinutes(1),
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                })
            .UseFilter(new AutomaticRetryAttribute { Attempts = 0 }));

        services.AddHangfireServer();
        
        AddHangfireJobServices(services);

        return;

        void AddHangfireJobServices(IServiceCollection services)
        {
            var hangfireJobs =
                typeof(IInfrastructureMarker)
                .Assembly
                .GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsSubclassOf(typeof(HangfireJob)));

            foreach (var job in hangfireJobs)
            {
                services.AddScoped(typeof(HangfireJob), job);
            }
        }
    }

    private static void UseHangfire(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var scopedProvider = scope.ServiceProvider;
        var jobManager = scopedProvider.GetRequiredService<IRecurringJobManager>();
        var configuration = scopedProvider.GetRequiredService<IConfiguration>();

        AddHangfireJobs(scopedProvider.GetServices<HangfireJob>());
        app.UseHangfireDashboard();

        return;

        void AddHangfireJobs(IEnumerable<HangfireJob> jobs)
        {
            foreach (var job in jobs)
            {
                jobManager.AddOrUpdate(
                    job.JobName,
                    Job.FromExpression(() => job.RunAsync()),
                    configuration[$"CronExpressions:{job.JobName}"]);
            }
        }
    }
}
