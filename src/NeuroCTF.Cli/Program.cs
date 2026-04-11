using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeuroCTF.Application.Services;
using NeuroCTF.Cli.Commands;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Infrastructure.Configuration;
using NeuroCTF.Modules;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables(prefix: "NEUROCTF_")
    .Build();

var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
    builder.SetMinimumLevel(LogLevel.Information);
});
services.AddNeuroCtfInfrastructure(configuration);
services.AddBuiltInModules();
services.AddSingleton<IModuleCatalog>(sp =>
{
    var builtIns = sp.GetServices<IModule>().ToArray();
    var pluginLoader = sp.GetRequiredService<IPluginLoader>();
    var runtimeOptions = sp.GetRequiredService<NeuroCTF.Core.Models.AnalysisOptions>();
    var pluginLoadResult = pluginLoader.LoadModulesAsync(runtimeOptions.PluginDirectory, CancellationToken.None).GetAwaiter().GetResult();
    return new ModuleCatalog(builtIns, pluginLoadResult);
});
services.AddSingleton<CliRouter>();

using var provider = services.BuildServiceProvider();
var router = provider.GetRequiredService<CliRouter>();
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

return await router.RouteAsync(args, cts.Token).ConfigureAwait(false);
