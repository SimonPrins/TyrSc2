using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public abstract class CustomController
    {
        public bool Stopped = false;

        public abstract bool DetermineAction(Agent agent, Point2D target);
    }
}
