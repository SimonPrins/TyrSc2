namespace Tyr.Builds.BuildLists
{
    public interface BuildStep
    {
        StepResult Perform(BuildListState state);
    }
}
