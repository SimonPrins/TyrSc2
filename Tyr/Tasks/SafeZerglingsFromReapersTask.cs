using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class SafeZerglingsFromReapersTask : Task
    {
        public static SafeZerglingsFromReapersTask Task = new SafeZerglingsFromReapersTask();

        public SafeZerglingsFromReapersTask() : base(10)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            bool speedDone = UpgradeType.LookUp[UpgradeType.MetabolicBoost].Done();
            bool multipleReapers = Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 2;
            
            if (!CloseReaper(agent))
                return false;

            if (multipleReapers && agent.Unit.Health < agent.Unit.HealthMax)
                return true;
            if (agent.Unit.Health < agent.Unit.HealthMax - 12)
                return true;

            return multipleReapers && !speedDone;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { UnitTypes = new HashSet<uint>() { UnitTypes.ZERGLING } });
            return result;
        }

        public override bool IsNeeded()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) > 0;
        }

        public override void OnFrame(Tyr tyr)
        {
            Point2D target = tyr.BaseManager.Main.MineralLinePos;
            foreach (Agent agent in units)
                if (SC2Util.DistanceSq(agent.Unit.Pos, target) >= 2 * 2)
                    agent.Order(Abilities.MOVE, target);

            for (int i = units.Count - 1; i >= 0; i--)
            {
                Agent agent = units[i];
                if (agent.Unit.Health < agent.Unit.HealthMax)
                    continue;

                if (!CloseReaper(agent))
                    ClearAt(i);
            }
        }

        public bool CloseReaper(Agent agent)
        {
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.REAPER)
                    continue;
                if (agent.DistanceSq(enemy) < 15 * 15)
                    return true;
            }
            return false;
        }
    }
}
