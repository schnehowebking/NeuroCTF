using NeuroCTF.Tests.Cases;

var tests = new List<(string Name, Func<Task> Run)>
{
    ("ModuleTests", ModuleTests.RunAsync),
    ("AdvancedModuleTests", AdvancedModuleTests.RunAsync),
    ("CliRouterTests", CliRouterTests.RunAsync),
    ("PerformanceGuardTests", PerformanceGuardTests.RunAsync),
    ("MultiDomainTests", MultiDomainTests.RunAsync),
    ("PipelineTests", PipelineTests.RunAsync),
    ("CarverTests", CarverTests.RunAsync),
    ("PluginTests", PluginTests.RunAsync)
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        await test.Run().ConfigureAwait(false);
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("Test failures:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

Console.WriteLine("All NeuroCTF tests passed.");
return 0;
