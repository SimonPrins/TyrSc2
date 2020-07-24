using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class PhoenixHuntOverlordsTask : Task
    {
        public static PhoenixHuntOverlordsTask Task = new PhoenixHuntOverlordsTask();

        private Unit KillOverlord;

        public static void Enable()
        {
            Enable(Task);
        }

        public PhoenixHuntOverlordsTask() : base(4)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.PHOENIX;
        }

        public override bool IsNeeded()
        {
            DetermineTarget();
            return KillOverlord != null;
        }

        public override void OnFrame(Bot tyr)
        {
            DetermineTarget();
            if (KillOverlord == null)
            {
                Clear();
                return;
            }
            foreach (Agent agent in units)
            {
                agent.Order(Abilities.MOVE, SC2Util.To2D(KillOverlord.Pos));
            }
        }

        private void DetermineTarget()
        {
            float dist = 80 * 80;
            Unit target = null;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.OVERLORD && enemy.UnitType != UnitTypes.OVERSEER)
                    continue;
                float newDist = SC2Util.DistanceSq(enemy.Pos, Bot.Bot.MapAnalyzer.StartLocation);

                if (newDist < dist)
                {
                    target = enemy;
                    dist = newDist;
                }
            }
            KillOverlord = target;
        }
    }

}
