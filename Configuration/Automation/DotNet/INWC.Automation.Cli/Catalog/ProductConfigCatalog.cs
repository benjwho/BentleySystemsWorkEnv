namespace INWC.Automation.Cli.Catalog;

internal static class ProductConfigCatalog
{
    public static IReadOnlyList<string> DefaultRoots { get; } =
    [
        @"C:\ProgramData\Bentley\MicroStation 2025\Configuration",
        @"C:\ProgramData\Bentley\OpenRoads Designer 2025.00\Configuration",
        @"C:\ProgramData\Bentley\OpenSite Designer 2025.00\Configuration",
        @"C:\ProgramData\Bentley\OpenCities Map Ultimate 2025\Configuration",
        @"C:\ProgramData\Bentley\Bentley Descartes 2025\Configuration",
        @"C:\ProgramData\Bentley\OpenPlant 2024\Configuration"
    ];
}
