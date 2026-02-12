using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Catalog;

internal static class AppCatalog
{
    public static IReadOnlyList<AppDefinition> Apps { get; } =
    [
        new AppDefinition(
            "MicroStation 2025",
            [
                @"C:\Program Files\Bentley\MicroStation 2025\MicroStation\microstation.exe",
                @"C:\Program Files\Bentley\MicroStation CONNECT Edition\MicroStation\microstation.exe"
            ],
            ["microstation.exe"],
            "MicroStation"),

        new AppDefinition(
            "OpenRoads Designer 2025.00",
            [
                @"C:\Program Files\Bentley\OpenRoads Designer 2025.00\OpenRoadsDesigner\OpenRoadsDesigner.exe",
                @"C:\Program Files\Bentley\OpenRoads Designer CONNECT Edition\OpenRoadsDesigner\OpenRoadsDesigner.exe"
            ],
            ["OpenRoadsDesigner.exe"],
            "OpenRoadsDesigner"),

        new AppDefinition(
            "OpenSite Designer 2025.00",
            [
                @"C:\Program Files\Bentley\OpenSite Designer 2025.00\OpenSiteDesigner\OpenSiteDesigner.exe",
                @"C:\Program Files\Bentley\OpenSite Designer CONNECT Edition\OpenSiteDesigner\OpenSiteDesigner.exe"
            ],
            ["OpenSiteDesigner.exe"],
            "OpenSiteDesigner"),

        new AppDefinition(
            "STAAD.Pro 2025",
            [
                @"C:\Program Files\Bentley\Engineering\STAAD.Pro 2025\STAAD\Bentley.Staad.exe",
                @"C:\Program Files\Bentley\Engineering\STAAD.Pro CONNECT Edition\STAAD\Bentley.Staad.exe"
            ],
            ["Bentley.Staad.exe"],
            "STAAD"),

        new AppDefinition(
            "OpenCities Map Ultimate 2025",
            [
                @"C:\Program Files\Bentley\OpenCities Map Ultimate 2025\MapUltimate\MapUltimate.exe",
                @"C:\Program Files\Bentley\OpenCities Map Ultimate CONNECT Edition\MapUltimate\MapUltimate.exe"
            ],
            ["MapUltimate.exe"],
            "OpenCities"),

        new AppDefinition(
            "Bentley Descartes 2025",
            [
                @"C:\Program Files\Bentley\Bentley Descartes 2025\DescartesStandAlone\DescartesStandAlone.exe",
                @"C:\Program Files\Bentley\Bentley Descartes CONNECT Edition\DescartesStandAlone\DescartesStandAlone.exe"
            ],
            ["DescartesStandAlone.exe"],
            "Descartes"),

        new AppDefinition(
            "OpenPlant Modeler 2024",
            [
                @"C:\Program Files\Bentley\OpenPlant 2024\OpenPlantModeler\OpenPlantModeler.exe",
                @"C:\Program Files\Bentley\OpenPlant Modeler CONNECT Edition\OpenPlantModeler\OpenPlantModeler.exe"
            ],
            ["OpenPlantModeler.exe"],
            "OpenPlant")
    ];
}
