namespace INWC.Automation.Cli.Catalog;

internal sealed record IntegrationDetectionRule(string Variable, string[] Paths, string? ForceValue = null);

internal static class IntegrationDetectionCatalog
{
    public static IReadOnlyList<IntegrationDetectionRule> Rules { get; } =
    [
        new IntegrationDetectionRule(
            "INWC_INTEROP_OPENFLOWS",
            [
                @"C:\Program Files\Bentley\FlowMaster\FlowMaster.exe",
                @"C:\Program Files\Bentley\OpenFlows Water\WaterCAD.exe",
                @"C:\Program Files\Bentley\OpenFlows Water\WaterGEMS.exe",
                @"C:\Program Files\Bentley\OpenFlows Water\Hamm.exe"
            ]),
        new IntegrationDetectionRule("INWC_INTEROP_SYNCHRO", [@"C:\Program Files\Bentley\SYNCHRO\4D Pro\Synchro4DPro.exe"]),
        new IntegrationDetectionRule(
            "INWC_INTEROP_STRUCTURAL",
            [
                @"C:\Program Files\Bentley\Engineering\STAAD.Pro 2025\STAAD\Bentley.Staad.exe",
                @"C:\Program Files\Bentley\AutoPIPE 2025\autopipe.exe",
                @"C:\Program Files\Bentley\Engineering\RCDC 2023\RCDC.exe",
                @"C:\Program Files\Bentley\Adina\25.00\bin\aui.exe"
            ]),
        new IntegrationDetectionRule("INWC_INTEROP_GEOTECH", [@"C:\Program Files\Bentley\Geotechnical\PLAXIS LE CONNECT Edition V21\PLAXISLE.exe"]),
        new IntegrationDetectionRule("INWC_INTEROP_PYTHON_AUTOMATION", [@"C:\ProgramData\Bentley\PowerPlatformPython\python\python.exe"]),
        new IntegrationDetectionRule(
            "INWC_INTEROP_OPENPLANT",
            [
                @"C:\Program Files\Bentley\OpenPlant 2024\OpenPlantModeler\OpenPlantModeler.exe",
                @"C:\Program Files\Bentley\OpenPlant 2024\IsometricsManager\OpenPlantIsoExtractor.exe"
            ]),
        new IntegrationDetectionRule("INWC_INTEROP_OPENCITIES", [@"C:\Program Files\Bentley\OpenCities Map Ultimate 2025\MapUltimate\MapUltimate.exe"]),
        new IntegrationDetectionRule("INWC_INTEROP_DESCARTES", [@"C:\Program Files\Bentley\Bentley Descartes 2025\DescartesStandAlone\DescartesStandAlone.exe"]),
        new IntegrationDetectionRule("INWC_INTEROP_ITWIN_CAPTURE", [@"C:\Program Files\Bentley\iTwin Capture Manage And Extract 25.00.04.01\program\bin\Orbit.exe"]),
        new IntegrationDetectionRule("INWC_INTEROP_PROJECTWISE_DRIVE", [@"C:\Program Files\Bentley\ProjectWise Drive\ProjectWise Drive.exe"])
    ];
}
