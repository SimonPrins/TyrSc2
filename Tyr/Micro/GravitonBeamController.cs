using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class GravitonBeamController : CustomController
    {
        private int LastGravitonFrame = 0;
        private ulong LastGravitonUnitTag = 0;
        private ulong TargetTag = 0;
        public float Delay = 22.4f * 7;
        
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.PHOENIX)
                return false;
            if (agent.Unit.Energy < 50)
                return false;

            if (LastGravitonUnitTag == agent.Unit.Tag
                && Tyr.Bot.Frame - LastGravitonFrame < 22.4 * 5)
            {
                foreach (Unit enemy in Tyr.Bot.Enemies())
                {
                    if (enemy.Tag == TargetTag)
                    {
                        agent.Order(173, enemy.Tag);
                        return true;
                    }
                }
            }

            if (Tyr.Bot.Frame - LastGravitonFrame < Delay)
                return false;

            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.CYCLONE
                    && enemy.UnitType != UnitTypes.QUEEN
                    && enemy.UnitType != UnitTypes.HYDRALISK)
                    continue;

                if (agent.DistanceSq(enemy) <= 10 * 10)
                {
                    agent.Order(173, enemy.Tag);
                    LastGravitonFrame = Tyr.Bot.Frame;
                    LastGravitonUnitTag = agent.Unit.Tag;
                    TargetTag = enemy.Tag;
                    return true;
                }
            }
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.DRONE)
                    continue;

                if (agent.DistanceSq(enemy) <= 10 * 10)
                {
                    agent.Order(173, enemy.Tag);
                    LastGravitonFrame = Tyr.Bot.Frame;
                    LastGravitonUnitTag = agent.Unit.Tag;
                    TargetTag = enemy.Tag;
                    return true;
                }
            }

            return false;
        }
    }
}
