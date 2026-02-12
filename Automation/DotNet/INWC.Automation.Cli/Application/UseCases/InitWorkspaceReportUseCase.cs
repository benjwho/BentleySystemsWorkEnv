using System.Text.RegularExpressions;
using INWC.Automation.Cli.Application.Interfaces;
using INWC.Automation.Cli.Catalog;
using INWC.Automation.Cli.Compatibility;
using INWC.Automation.Cli.Domain.Models;

namespace INWC.Automation.Cli.Application.UseCases;

internal sealed class InitWorkspaceReportUseCase : ICommandUseCase<InitWorkspaceReportOptions, CommandResult>
{
    private readonly IExitCodePolicy _exitCodePolicy;

    public InitWorkspaceReportUseCase(IExitCodePolicy exitCodePolicy)
    {
        _exitCodePolicy = exitCodePolicy;
    }

    public CommandResult Execute(CommandContext context, InitWorkspaceReportOptions options)
    {
        var checks = new List<CheckRecord>();
        var techRoot = context.Global.TechRoot;

        var directories = new[]
        {
            new { Path = Path.Combine(techRoot, "Configuration"), Desc = "Configuration root" },
            new { Path = Path.Combine(techRoot, "Configuration", "Standards"), Desc = "Standards folder" },
            new { Path = Path.Combine(techRoot, "Configuration", "Standards", "Seed"), Desc = "Seed DGN directory" },
            new { Path = Path.Combine(techRoot, "Configuration", "Standards", "Cells"), Desc = "Cell library directory" },
            new { Path = Path.Combine(techRoot, "Configuration", "Standards", "Dgnlib"), Desc = "DGN library directory" },
            new { Path = Path.Combine(techRoot, "Configuration", "Standards", "Fonts"), Desc = "Fonts directory" },
            new { Path = Path.Combine(techRoot, "Configuration", "Standards", "LineStyles"), Desc = "LineStyles directory" },
            new { Path = Path.Combine(techRoot, "Configuration", "Standards", "Reports"), Desc = "Reports directory" },
            new { Path = Path.Combine(techRoot, "Configuration", "Organization"), Desc = "Organization root" },
            new { Path = Path.Combine(techRoot, "Configuration", "Organization", "ByDiscipline", "Civil"), Desc = "Civil discipline folder" },
            new { Path = Path.Combine(techRoot, "Configuration", "Organization", "ByProject", "NWC_Rehab"), Desc = "NWC_Rehab project folder" },
            new { Path = Path.Combine(techRoot, "Configuration", "Organization", "ByFunction", "Plotting"), Desc = "Plotting standards folder" },
            new { Path = Path.Combine(techRoot, "Configuration", "Organization", "ByFunction", "Exports"), Desc = "Export standards folder" },
            new { Path = Path.Combine(techRoot, "Configuration", "WorkSpaces"), Desc = "Workspaces folder" },
            new { Path = Path.Combine(techRoot, "Configuration", "WorkSets"), Desc = "WorkSets folder" },
            new { Path = Path.Combine(techRoot, "Configuration", "Automation", "PowerShell"), Desc = "PowerShell automation" },
            new { Path = Path.Combine(techRoot, "Projects", "NWC_Rehab", "Models"), Desc = "NWC_Rehab Models folder" },
            new { Path = Path.Combine(techRoot, "Projects", "NWC_Rehab", "Sheets"), Desc = "NWC_Rehab Sheets folder" },
            new { Path = Path.Combine(techRoot, "Projects", "NWC_Rehab", "Exports"), Desc = "NWC_Rehab Exports folder" },
            new { Path = Path.Combine(techRoot, "Logs"), Desc = "Logs folder" }
        };

        foreach (var dir in directories)
        {
            CheckHelpers.Add(checks, "Directory", dir.Desc, dir.Path, Directory.Exists(dir.Path));
        }

        var configFiles = new[]
        {
            new { Path = Path.Combine(techRoot, "Configuration", "WorkSpaceSetup.cfg"), Variable = "MY_WORKSPACES_LOCATION" },
            new { Path = Path.Combine(techRoot, "Configuration", "WorkSpaces", "INWC_RH.cfg"), Variable = "INWC_TECH" },
            new { Path = Path.Combine(techRoot, "Configuration", "WorkSets", "NWC_Rehab.cfg"), Variable = "INWC_PROJECT_ROOT" },
            new { Path = Path.Combine(techRoot, "Configuration", "Organization", "ByProject", "NWC_Rehab", "NWC_Rehab_Organization.cfg"), Variable = "INWC_PROJECT_NAME" },
            new { Path = Path.Combine(techRoot, "Configuration", "Organization", "ByDiscipline", "Civil", "Civil_Standards.cfg"), Variable = "CIVIL_SEED_FILE" },
            new { Path = Path.Combine(techRoot, "Configuration", "Organization", "ByFunction", "Exports", "Export_Standards.cfg"), Variable = "EXPORT_PDF_ENABLED" }
        };

        foreach (var cfg in configFiles)
        {
            if (!File.Exists(cfg.Path))
            {
                CheckHelpers.Add(checks, "Config", "Config file exists", cfg.Path, false, cfg.Variable);
                continue;
            }

            var content = File.ReadAllText(cfg.Path);
            var hasVar = Regex.IsMatch(content, Regex.Escape(cfg.Variable) + "\\s*=");
            CheckHelpers.Add(checks, "Config", $"Variable '{cfg.Variable}' present", cfg.Path, hasVar);
        }

        foreach (var root in ProductConfigCatalog.DefaultRoots)
        {
            var cfgSetup = Path.Combine(root, "ConfigurationSetup.cfg");
            CheckHelpers.Add(checks, "ProgramData", "ConfigurationSetup.cfg present", cfgSetup, File.Exists(cfgSetup));
        }

        var hasFailure = checks.Any(c => !c.Ok && !c.Scope.Equals("ProgramData", StringComparison.OrdinalIgnoreCase));

        return new CommandResult
        {
            ExitCode = hasFailure ? _exitCodePolicy.Failure : _exitCodePolicy.Success,
            Message = hasFailure
                ? "Workspace report found validation issues."
                : "Workspace report completed successfully.",
            Checks = checks,
            Data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["WorkspaceRoot"] = "INWC_RH",
                ["WorkSet"] = "NWC_Rehab",
                ["ProjectsRoot"] = Path.Combine(techRoot, "Projects")
            }
        };
    }
}
