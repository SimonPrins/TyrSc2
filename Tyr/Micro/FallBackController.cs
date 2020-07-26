using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.CombatSim;

namespace SC2Sharp.Micro
{
    public class FallBackController : CustomController
    {
        public bool ReturnFire = false;
        public float MainDist = 0;
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (MainDist > 0 && agent.DistanceSq(Bot.Main.MapAnalyzer.StartLocation) < MainDist * MainDist)
                return false;
            if (agent.CombatSimulationDecision != CombatSimulationDecision.FallBack
                || Bot.Main.Frame - agent.CombatSimulationDecisionFrame >= 10)
                return false;

            return agent.FleeEnemies(ReturnFire);
        }
    }
}
