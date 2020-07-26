using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
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
            return agent.Unit.Energy >= 50;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = Bot.Main.MapAnalyzer.GetMainRamp(), Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.SENTRY } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            for (int i = Units.Count - 1; i >= 0; i--)
                if (Units[i].Unit.Energy < 50)
                    ClearAt(i);

            if (IdlePos == null)
                IdlePos = new PotentialHelper(bot.MapAnalyzer.GetMainRamp(), 6)
                    .To(bot.MapAnalyzer.StartLocation)
                    .Get();

            Point2D ramp = bot.MapAnalyzer.GetMainRamp();
            if (ramp == null)
                return;
            int enemyCount = 0;
            bool enemyAtRamp = false;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.MARINE
                    && enemy.UnitType != UnitTypes.SCV
                    && enemy.UnitType != UnitTypes.ZEALOT
                    && enemy.UnitType != UnitTypes.STALKER
                    && enemy.UnitType != UnitTypes.ZERGLING
                    && enemy.UnitType != UnitTypes.ROACH
                    && enemy.UnitType != UnitTypes.HYDRALISK
                    && enemy.UnitType != UnitTypes.MARAUDER)
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, ramp) > 40 * 40)
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, ramp) < 8 * 8)
                    enemyAtRamp = true;
                if (enemy.UnitType == UnitTypes.ZEALOT
                    || enemy.UnitType == UnitTypes.STALKER
                    || enemy.UnitType == UnitTypes.ROACH
                    || enemy.UnitType == UnitTypes.HYDRALISK
                    || enemy.UnitType == UnitTypes.MARAUDER)
                    enemyCount += 2;
                else
                    enemyCount++;
            }
            if (enemyCount < 6 || !enemyAtRamp)
            {
                foreach (Agent agent in units)
                {
                    if (agent.DistanceSq(IdlePos) >= 4 * 4)
                        agent.Order(Abilities.MOVE, IdlePos);
                }
                return;
            }
            if (bot.Frame - PreviousForceFieldFrame < 22.4 * 10)
                return;
            foreach (Agent agent in units)
            {
                agent.Order(1526, ramp);
                PreviousForceFieldFrame = bot.Frame;
            }
        }
    }
}
