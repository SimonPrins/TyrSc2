using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Tasks
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
            Bot.Bot.TaskManager.Add(Task);
        }

        public void HellionsToHellbats()
        {
            if (Bot.Bot.Build.Completed(UnitTypes.ARMORY) == 0)
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
                && (agent.DistanceSq(Bot.Bot.MapAnalyzer.StartLocation) <= 20 * 20 || agent.DistanceSq(Bot.Bot.BaseManager.NaturalDefensePos) <= 15 * 15);
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
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
