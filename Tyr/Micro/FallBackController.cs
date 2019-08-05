using SC2APIProtocol;
using Tyr.Agents;
using Tyr.CombatSim;

namespace Tyr.Micro
{
    public class FallBackController : CustomController
    {
        public bool ReturnFire = false;
        public float MainDist = 0;
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (MainDist > 0 && agent.DistanceSq(Tyr.Bot.MapAnalyzer.StartLocation) < MainDist * MainDist)
                return false;
            if (agent.CombatSimulationDecision != CombatSimulationDecision.FallBack
                || Tyr.Bot.Frame - agent.CombatSimulationDecisionFrame >= 10)
                return false;

            return agent.FleeEnemies(ReturnFire);
        }
    }
}
