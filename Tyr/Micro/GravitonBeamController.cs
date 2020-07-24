using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class GravitonBeamController : CustomController
    {
        private static int LastGravitonFrame = 0;
        private static ulong LastGravitonUnitTag = 0;
        private static ulong TargetTag = 0;
        public float Delay = 22.4f * 7;

        public bool LiftReapers = false;
        public bool LiftMarauders = false;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.PHOENIX)
                return false;
            if (agent.Unit.Energy < 50)
                return false;

            bool stillExists = false;
            if (TargetTag != 0)
            {
                foreach (Unit enemy in Bot.Bot.Enemies())
                {
                    if (enemy.Tag == TargetTag)
                    {
                        stillExists = true;
                        break;
                    }
                }
            }
            if (!stillExists)
            {
                TargetTag = 0;
                LastGravitonFrame = 0;
            }

            if (LastGravitonUnitTag == agent.Unit.Tag
                && Bot.Bot.Frame - LastGravitonFrame < 22.4 * 5)
            {
                foreach (Unit enemy in Bot.Bot.Enemies())
                {
                    if (enemy.Tag == TargetTag)
                    {
                        agent.Order(173, enemy.Tag);
                        return true;
                    }
                }
            }

            if (Bot.Bot.Frame - LastGravitonFrame < Delay)
                return false;

            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.CYCLONE
                    && enemy.UnitType != UnitTypes.SIEGE_TANK
                    && enemy.UnitType != UnitTypes.SIEGE_TANK_SIEGED
                    && enemy.UnitType != UnitTypes.WIDOW_MINE
                    && enemy.UnitType != UnitTypes.WIDOW_MINE_BURROWED
                    && enemy.UnitType != UnitTypes.QUEEN
                    && enemy.UnitType != UnitTypes.HYDRALISK
                    && enemy.UnitType != UnitTypes.IMMORTAL
                    && (enemy.UnitType != UnitTypes.REAPER || !LiftReapers)
                    && (enemy.UnitType != UnitTypes.MARAUDER || !LiftMarauders || agent.Unit.Energy < 75))
                    continue;

                if (agent.DistanceSq(enemy) <= 10 * 10)
                {
                    agent.Order(173, enemy.Tag);
                    LastGravitonFrame = Bot.Bot.Frame;
                    LastGravitonUnitTag = agent.Unit.Tag;
                    TargetTag = enemy.Tag;
                    return true;
                }
            }
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.STALKER)
                    continue;

                if (agent.DistanceSq(enemy) <= 8 * 8)
                {
                    agent.Order(173, enemy.Tag);
                    LastGravitonFrame = Bot.Bot.Frame;
                    LastGravitonUnitTag = agent.Unit.Tag;
                    TargetTag = enemy.Tag;
                    return true;
                }
            }
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.DRONE)
                    continue;

                if (agent.DistanceSq(enemy) <= 10 * 10)
                {
                    agent.Order(173, enemy.Tag);
                    LastGravitonFrame = Bot.Bot.Frame;
                    LastGravitonUnitTag = agent.Unit.Tag;
                    TargetTag = enemy.Tag;
                    return true;
                }
            }

            return false;
        }
    }
}
