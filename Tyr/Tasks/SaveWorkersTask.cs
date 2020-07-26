using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class SaveWorkersTask : Task
    {
        public static SaveWorkersTask Task = new SaveWorkersTask();
        
        public SaveWorkersTask() : base(9)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            if (!agent.IsWorker || agent.DistanceSq(Bot.Main.MapAnalyzer.StartLocation) < 15 * 15)
                return false;

            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.CYCLONE)
                    continue;
                if (agent.DistanceSq(enemy) <= 8 * 8)
                    return true;
            }
            return false;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            for (int i = Units.Count - 1; i >= 0; i--)
            {
                Agent agent = Units[i];
                if (agent.DistanceSq(bot.MapAnalyzer.StartLocation) > 15 * 15)
                    continue;
                ClearAt(i);
            }

            foreach (Agent agent in units)
                agent.Order(Abilities.MOVE, SC2Util.To2D(bot.MapAnalyzer.StartLocation));
        }
    }
}
