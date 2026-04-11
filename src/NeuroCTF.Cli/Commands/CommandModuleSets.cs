namespace NeuroCTF.Cli.Commands;

public static class CommandModuleSets
{
    public static readonly IReadOnlyList<string> ScanModules =
    [
        "pcap",
        "http-replay",
        "param-discovery",
        "bin-inspect",
        "dotnet-inspect"
    ];

    public static readonly IReadOnlyList<string> ReconModules =
    [
        "url-recon",
        "domain-recon",
        "jwt"
    ];
}
