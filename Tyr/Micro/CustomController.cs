using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public interface CustomController
    {
        bool DetermineAction(Agent agent, Point2D target);
    }
}
