using SC2APIProtocol;
using SC2Sharp.Agents;

namespace SC2Sharp.Micro
{
    public abstract class CustomController
    {
        public bool Stopped = false;

        public abstract bool DetermineAction(Agent agent, Point2D target);
    }
}
