using Microsoft.Extensions.DependencyInjection;
using NeuroCTF.Core.Abstractions;
using NeuroCTF.Modules.Modules;

namespace NeuroCTF.Modules;

public static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddBuiltInModules(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Base64TransformModule>();
        services.AddSingleton<IModule, Base32TransformModule>();
        services.AddSingleton<IModule, HexTransformModule>();
        services.AddSingleton<IModule, UrlDecodeTransformModule>();
        services.AddSingleton<IModule, CaesarTransformModule>();
        services.AddSingleton<IModule, XorTransformModule>();
        services.AddSingleton<IModule, GzipTransformModule>();
        services.AddSingleton<IModule, JwtInspectModule>();
        services.AddSingleton<IModule, ProtobufInspectorModule>();
        services.AddSingleton<IModule, Asn1InspectorModule>();
        services.AddSingleton<IModule, StegoLsbModule>();
        services.AddSingleton<IModule, StringsModule>();
        services.AddSingleton<IModule, HashModule>();
        services.AddSingleton<IModule, EntropyModule>();
        services.AddSingleton<IModule, FileTypeModule>();
        services.AddSingleton<IModule, FlagFinderModule>();
        services.AddSingleton<IModule, ArchivePasswordHeuristicModule>();
        services.AddSingleton<IModule, ArchiveTraversalModule>();
        services.AddSingleton<IModule, BinaryCarverModule>();
        services.AddSingleton<IModule, PeElfInspectorModule>();
        services.AddSingleton<IModule, DotNetBinaryInspectorModule>();
        services.AddSingleton<IModule, DisassemblyLiteModule>();
        services.AddSingleton<IModule, ControlFlowLiteModule>();
        services.AddSingleton<IModule, PatternCreateModule>();
        services.AddSingleton<IModule, PatternOffsetModule>();
        services.AddSingleton<IModule, ElfSecurityCheckModule>();
        services.AddSingleton<IModule, RopGadgetLiteModule>();
        services.AddSingleton<IModule, PcapInspectorModule>();
        services.AddSingleton<IModule, HttpRequestReplayModule>();
        services.AddSingleton<IModule, ParameterDiscoveryModule>();
        services.AddSingleton<IModule, UrlReconModule>();
        services.AddSingleton<IModule, DomainReconModule>();
        return services;
    }
}
