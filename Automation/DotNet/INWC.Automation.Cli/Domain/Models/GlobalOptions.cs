namespace INWC.Automation.Cli.Domain.Models;

internal sealed record GlobalOptions(
    string TechRoot,
    bool Json,
    bool WhatIf,
    bool Verbose);
