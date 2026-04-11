using System.Reflection;
using Microsoft.Extensions.Logging;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Infrastructure.Plugins;

public sealed class ReflectionPluginLoader(ILogger<ReflectionPluginLoader> logger) : IPluginLoader
{
    public Task<PluginLoadResult> LoadModulesAsync(string pluginDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(pluginDirectory))
        {
            return Task.FromResult(new PluginLoadResult(Array.Empty<IModule>(), Array.Empty<ModuleDescriptor>()));
        }

        var modules = new List<IModule>();
        var descriptors = new List<ModuleDescriptor>();
        foreach (var file in Directory.EnumerateFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories)
                     .Where(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                         && !path.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                         && !path.Contains($"{Path.DirectorySeparatorChar}refint{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var assembly = Assembly.LoadFrom(Path.GetFullPath(file));
                var discovered = assembly.GetTypes()
                    .Where(type => !type.IsAbstract && typeof(IModule).IsAssignableFrom(type))
                    .Select(type => Activator.CreateInstance(type))
                    .OfType<IModule>();
                foreach (var module in discovered)
                {
                    modules.Add(module);
                    descriptors.Add(new ModuleDescriptor(module.Name, module.Category, "plugin", Path.GetFullPath(file)));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load plugin assembly {AssemblyPath}", file);
            }
        }

        return Task.FromResult(new PluginLoadResult(modules, descriptors));
    }
}
