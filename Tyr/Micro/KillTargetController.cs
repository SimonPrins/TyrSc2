using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class KillTargetController : CustomController
    {
        public uint TargetType;
        public bool NoEnemiesAround = false;
        public KillTargetController(uint targetType)
        {
            TargetType = targetType;
        }

        public KillTargetController(uint targetType, bool noEnemiesAround)
        {
            TargetType = targetType;
            NoEnemiesAround = noEnemiesAround;
        }

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            Unit killTarget = null;

            float dist = 15 * 15;
            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (unit.UnitType != TargetType)
                {
                    if (NoEnemiesAround)
                    {
                        if (!UnitTypes.WorkerTypes.Contains(unit.UnitType)
                            && UnitTypes.CombatUnitTypes.Contains(unit.UnitType)
                            && agent.DistanceSq(unit) <= 12 * 12)
                            return false;
                    }
                    continue;
                }

                float newDist = agent.DistanceSq(unit);
                if (newDist > dist)
                    continue;
                killTarget = unit;
                dist = newDist;
            }

            if (killTarget == null)
                return false;

            if (agent.Unit.WeaponCooldown > 0)
                agent.Order(Abilities.MOVE, SC2Util.To2D(killTarget.Pos));
            else
                agent.Order(Abilities.ATTACK, killTarget.Tag);

            return true;
        }
    }
}
