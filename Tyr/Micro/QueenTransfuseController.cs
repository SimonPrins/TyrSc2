using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class QueenTransfuseController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.QUEEN)
                return false;

            if (agent.Unit.Energy < 50)
                return false;
            
            Agent transfuseTarget = null;
            float health = 10000;
            foreach (Agent ally in Bot.Main.UnitManager.Agents.Values)
            {
                if (!UnitTypes.CombatUnitTypes.Contains(ally.Unit.UnitType))
                    continue;

                if (ally.Unit.Tag == agent.Unit.Tag)
                    continue;

                if (ally.Unit.HealthMax - ally.Unit.Health < 125 && ally.Unit.UnitType != UnitTypes.HYDRALISK)
                    continue;

                if (ally.Unit.Health > 20 && ally.Unit.UnitType == UnitTypes.HYDRALISK)
                    continue;

                if (agent.DistanceSq(ally) > 7 * 7)
                    continue;

                float newHealth = ally.Unit.Health;
                if (newHealth < health)
                {
                    transfuseTarget = ally;
                    health = newHealth;
                }
            }

            if (transfuseTarget == null)
                return false;

            agent.Order(Abilities.TRANSFUSE, transfuseTarget.Unit.Tag);
            return true;
        }
    }
}
