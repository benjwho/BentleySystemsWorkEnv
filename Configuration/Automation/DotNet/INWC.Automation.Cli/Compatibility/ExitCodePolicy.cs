namespace INWC.Automation.Cli.Compatibility;

internal interface IExitCodePolicy
{
    int Success { get; }
    int Failure { get; }
    int NeedsFix { get; }
    int MissingDependency { get; }
}

internal sealed class ExitCodePolicy : IExitCodePolicy
{
    public int Success => 0;
    public int Failure => 1;
    public int NeedsFix => 2;
    public int MissingDependency => 127;
}
