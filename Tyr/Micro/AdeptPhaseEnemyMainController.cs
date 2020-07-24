using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Micro
{
    public class AdeptPhaseEnemyMainController : CustomController
    {
        public static Point2D PhaseTarget = null;
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.ADEPT)
                return false;
            
            if (Bot.Bot.Frame % 44 >= 2)
                return false;

            bool secondChance = Bot.Bot.Frame % 44 == 1;
            if (secondChance && Bot.Bot.UnitManager.Count(UnitTypes.ADEPT_PHASE_SHIFT) == 0)
                return false;

            if (PhaseTarget == null)
            {
                if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count != 1)
                    return false;
                foreach (Base b in Bot.Bot.BaseManager.Bases)
                    if (SC2Util.DistanceSq(b.BaseLocation.Pos, Bot.Bot.TargetManager.PotentialEnemyStartLocations[0]) <= 2 * 2)
                    {
                        PhaseTarget = b.MineralLinePos;
                        break;
                    }
            }
            if (PhaseTarget == null)
                return false;

            bool closeEnemies = false;
            foreach (Unit enemy in Bot.Bot.Enemies())
                if ((enemy.UnitType == UnitTypes.IMMORTAL || enemy.UnitType == UnitTypes.STALKER)
                    && agent.DistanceSq(enemy) <= 15 * 15)
                {
                    closeEnemies = true;
                    break;
                }

            float maxDist;
            if (!closeEnemies)
                maxDist = secondChance ? 50 * 50 : 40 * 40;
            else
                maxDist = secondChance ? 100 * 100 : 90 * 90;

            float dist = agent.DistanceSq(PhaseTarget);
            if (dist <= maxDist && dist >= 15 * 15)
            {
                agent.Order(2544, PhaseTarget);
                return true;
            }

            return false;
        }
    }
}
