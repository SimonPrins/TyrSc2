
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

        public override string ToString()
        {
            return "Goto " + Pos;
        }
    }
}
