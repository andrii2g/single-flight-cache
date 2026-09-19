using SingleFlightCacheStampedeLab.Cli;
using SingleFlightCacheStampedeLab.Composition;

CliOptions options;
try
{
    options = CliOptions.Parse(args);
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine($"Usage error: {exception.Message}");
    Console.Error.WriteLine("Run 'help' for supported commands and options.");
    return 2;
}

if (options.Command == "help")
{
    Console.WriteLine(HelpText.Value);
    return 0;
}

using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
Console.CancelKeyPress += cancelHandler;
try
{
    await LabComposition.RunAsync(options, args, cancellation.Token);
    return 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("Experiment canceled.");
    return 1;
}
catch (Exception exception)
{
    // Convert the experiment failure into the CLI's documented nonzero result.
    Console.Error.WriteLine($"Experiment failed: {exception.Message}");
    return 1;
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}
