namespace SC2Sharp.Builds.BuildLists
{
    public interface BuildStep
    {
        StepResult Perform(BuildListState state);
    }
}
