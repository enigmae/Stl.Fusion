using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.CommandR.Internal;

public class Commander : ICommander
{
    protected ICommandHandlerResolver HandlerResolver { get; }
    protected ILogger Log { get; }
    public IServiceProvider Services { get; }

    public Commander(
        IServiceProvider services,
        ILogger<Commander>? log = null)
    {
        Log = log ?? NullLogger<Commander>.Instance;
        Services = services;
        HandlerResolver = services.GetRequiredService<ICommandHandlerResolver>();
    }

    public Task Run(
        CommandContext context, bool isolate,
        CancellationToken cancellationToken = default)
    {
        // Task.Run is used to call RunInternal to make sure parent
        // task's ExecutionContext won't be "polluted" by temp.
        // change of CommandContext.Current (via AsyncLocal).
        if (!isolate)
            return Task.Run(() => RunInternal(context, cancellationToken), default);

        using var _ = ExecutionContextExt.SuppressFlow();
        return Task.Run(() => RunInternal(context, cancellationToken), default);
    }

    protected virtual async Task RunInternal(
        CommandContext context, CancellationToken cancellationToken = default)
    {
        var activationScope = context.Activate();
        try {
            var command = context.UntypedCommand;
            var handlers = HandlerResolver.GetCommandHandlers(command.GetType());
            context.ExecutionState = new CommandExecutionState(handlers);
            if (handlers!.Count == 0)
                await OnUnhandledCommand(command, context, cancellationToken).ConfigureAwait(false);
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) {
            context.SetResult(e);
        }
        finally {
            context.TryComplete(cancellationToken);
            activationScope.Dispose();
            await context.DisposeAsync().ConfigureAwait(false);
        }
    }

    protected virtual Task OnUnhandledCommand(
        ICommand command, CommandContext context,
        CancellationToken cancellationToken)
        => throw Errors.NoHandlerFound(command.GetType());
}
