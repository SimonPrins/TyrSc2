using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class KillParasitedController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.WeaponCooldown > 0)
                return false;

            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;
                
                if (agent.DistanceSq(unit) <= 8 * 8)
                    return false;
            }

            foreach (ulong tag in InfestorController.NeuralControllers.Keys)
            {
                if (!Bot.Main.UnitManager.Agents.ContainsKey(tag))
                    continue;
                Agent killTarget = Bot.Main.UnitManager.Agents[tag];
                if (agent.DistanceSq(killTarget) <= 8 * 8)
                {
                    agent.Order(Abilities.ATTACK, tag);
                    return true;
                }
            }


            return false;
        }
    }
}
