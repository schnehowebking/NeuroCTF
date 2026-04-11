using Microsoft.Extensions.Logging.Abstractions;
using NeuroCTF.Infrastructure.Plugins;
using NeuroCTF.Tests.Assertions;

namespace NeuroCTF.Tests.Cases;

public static class PluginTests
{
    public static async Task RunAsync()
    {
        var loader = new ReflectionPluginLoader(NullLogger<ReflectionPluginLoader>.Instance);
        var result = await loader.LoadModulesAsync(Path.GetFullPath("plugins"), CancellationToken.None);

        AssertEx.True(result.Modules.Any(module => module.Name == "rot13"), "Plugin loader should discover the sample plugin module");
        AssertEx.True(result.Descriptors.Any(descriptor => descriptor.Origin == "plugin"), "Plugin loader should expose plugin descriptors");
    }
}
