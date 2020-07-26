using System.Collections.Generic;
using SC2Sharp.Agents;

namespace SC2Sharp.Tasks
{
    public class TransformTask : Task
    {
        public static TransformTask Task = new TransformTask();
        Dictionary<uint, int> TransformationMap = new Dictionary<uint, int>();

        public TransformTask() : base(2)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public void HellionsToHellbats()
        {
            if (Bot.Main.Build.Completed(UnitTypes.ARMORY) == 0)
                TransformationMap.Remove(UnitTypes.HELLION);
            else if (!TransformationMap.ContainsKey(UnitTypes.HELLION))
                TransformationMap.Add(UnitTypes.HELLION, 1998);

            if (TransformationMap.ContainsKey(UnitTypes.HELLBAT))
                TransformationMap.Remove(UnitTypes.HELLBAT);
        }

        public void HellbatsToHellions()
        {
            if (!TransformationMap.ContainsKey(UnitTypes.HELLBAT))
                TransformationMap.Add(UnitTypes.HELLBAT, 1978);

            if (TransformationMap.ContainsKey(UnitTypes.HELLION))
                TransformationMap.Remove(UnitTypes.HELLION);
        }

        public void ThorsToSingleTarget()
        {
            if (!TransformationMap.ContainsKey(UnitTypes.THOR))
                TransformationMap.Add(UnitTypes.THOR, 2362);

            if (TransformationMap.ContainsKey(UnitTypes.THOR))
                TransformationMap.Remove(UnitTypes.HELLBAT);
        }

        public override bool DoWant(Agent agent)
        {
            return TransformationMap.ContainsKey(agent.Unit.UnitType) 
                && (agent.DistanceSq(Bot.Main.MapAnalyzer.StartLocation) <= 20 * 20 || agent.DistanceSq(Bot.Main.BaseManager.NaturalDefensePos) <= 15 * 15);
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            List<Agent> done = new List<Agent>();
            foreach (Agent agent in units)
            {
                if (TransformationMap.ContainsKey(agent.Unit.UnitType))
                    agent.Order(TransformationMap[agent.Unit.UnitType]);
                else
                    done.Add(agent);
            }
            foreach (Agent agent in done)
            {
                IdleTask.Task.Add(agent);
                units.Remove(agent);
            }
        }
    }
}
