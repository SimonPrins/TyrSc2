using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class MoveWhenSafeController : CustomController
    {
        public static int DebugMessageFrame = 0;
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            DebugMessageFrame = Bot.Main.Frame;
            if (agent.DistanceSq(target) <= 8 * 8)
                return false;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType)
                    && !UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;
                if (agent.DistanceSq(enemy) <= 10 * 10)
                    return false;
            }
            agent.Order(Abilities.MOVE, target);
            return true;
        }
    }
}
