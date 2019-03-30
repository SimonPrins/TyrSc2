
namespace Tyr.Builds.BuildLists
{
    /*
     * Considered harmful.
     */
    public class GotoStep : BuildStep
    {
        public int Pos;
        public GotoStep(int pos)
        {
            Pos = pos;
        }

        public StepResult Perform(BuildListState state)
        {
            if (state.BuiltThisFrame)
                return new NextList();
            return new ToLine(Pos);
        }

        public override string ToString()
        {
            return "Goto " + Pos;
        }
    }
}
