using NeuroCTF.Core.Abstractions;
using NeuroCTF.Core.Models;

namespace NeuroCTF.Application.Services;

public sealed class ModuleCatalog : IModuleCatalog
{
    private readonly IReadOnlyList<IModule> _modules;
    private readonly IReadOnlyList<ModuleDescriptor> _descriptors;

    public ModuleCatalog(IEnumerable<IModule> builtInModules, PluginLoadResult? pluginLoadResult = null)
    {
        var builtIns = builtInModules.ToArray();
        var plugins = pluginLoadResult?.Modules ?? Array.Empty<IModule>();
        _modules = builtIns
            .Concat(plugins)
            .OrderBy(module => module.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _descriptors = builtIns
            .Select(module => new ModuleDescriptor(module.Name, module.Category, "built-in"))
            .Concat(pluginLoadResult?.Descriptors ?? Array.Empty<ModuleDescriptor>())
            .OrderBy(descriptor => descriptor.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(descriptor => descriptor.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<IModule> GetAll() => _modules;

    public IModule? GetByName(string name) =>
        _modules.FirstOrDefault(module => string.Equals(module.Name, name, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ModuleDescriptor> GetDescriptors() => _descriptors;
}
