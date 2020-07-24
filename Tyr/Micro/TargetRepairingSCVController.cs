using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class TargetRepairingSCVController : CustomController
    {
        private ulong TargetTag;
        private Unit ScvTarget;
        private int UpdateFrame = 0;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.STALKER)
                return false;

            Unit scvTarget = GetTarget(agent);
            if (scvTarget == null || agent.DistanceSq(scvTarget) >= 12 * 12)
                return false;

            Bot.Main.DrawLine(agent.Unit.Pos, scvTarget.Pos);
            agent.Order(Abilities.ATTACK, TargetTag);

            return true;
        }

        private Unit GetTarget(Agent agent)
        {
            if (UpdateFrame == Bot.Main.Frame)
                return ScvTarget;
            UpdateFrame = Bot.Main.Frame;
            if (TargetTag != 0)
            {
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.Tag != TargetTag)
                        continue;
                    if (BunkerInRange(enemy))
                    {
                        ScvTarget = enemy;
                        return enemy;
                    }
                }
                ScvTarget = null;
                TargetTag = 0;
            }


            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.SCV)
                    continue;
                if (agent.DistanceSq(enemy) >= 12 * 12)
                    continue;
                if (BunkerInRange(enemy))
                {
                    ScvTarget = enemy;
                    TargetTag = enemy.Tag;
                    return enemy;
                }
            }
            return null;
        }

        private bool BunkerInRange(Unit scv)
        {

            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BUNKER)
                    continue;
                if (SC2Util.DistanceSq(scv.Pos, enemy.Pos) <= 3 * 3)
                    return true;
            }
            return false;
        }
    }
}
