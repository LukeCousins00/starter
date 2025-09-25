namespace Starter.Infrastructure.Jobs;

public abstract class HangfireJob
{
    public abstract string JobName { get; }

    public abstract Task RunAsync();
}
