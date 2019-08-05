using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class ForceFieldRampTask : Task
    {
        public static ForceFieldRampTask Task = new ForceFieldRampTask();
        private Point2D IdlePos = null;
        int PreviousForceFieldFrame = -100;

        public ForceFieldRampTask() : base(11)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = Tyr.Bot.MapAnalyzer.GetMainRamp(), Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.SENTRY } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (IdlePos == null)
                IdlePos = new PotentialHelper(tyr.MapAnalyzer.GetMainRamp(), 6)
                    .To(tyr.MapAnalyzer.StartLocation)
                    .Get();

            Point2D ramp = tyr.MapAnalyzer.GetMainRamp();
            if (ramp == null)
                return;
            int enemyCount = 0;
            bool enemyAtRamp = false;
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.MARINE
                    && enemy.UnitType != UnitTypes.SCV
                    && enemy.UnitType != UnitTypes.ZEALOT
                    && enemy.UnitType != UnitTypes.STALKER
                    && enemy.UnitType != UnitTypes.ZERGLING)
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, ramp) > 40 * 40)
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, ramp) < 8 * 8)
                    enemyAtRamp = true;
                if (enemy.UnitType == UnitTypes.ZEALOT
                    || enemy.UnitType == UnitTypes.STALKER)
                    enemyCount += 2;
                else
                    enemyCount++;
            }
            if (enemyCount < 6 || !enemyAtRamp)
            {
                foreach (Agent agent in units)
                    agent.Order(Abilities.MOVE, IdlePos);
                return;
            }
            if (tyr.Frame - PreviousForceFieldFrame < 22.4 * 10)
                return;
            foreach (Agent agent in units)
            {
                agent.Order(1526, ramp);
                PreviousForceFieldFrame = tyr.Frame;
            }
        }
    }
}
