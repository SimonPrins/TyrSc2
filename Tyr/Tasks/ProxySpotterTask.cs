using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class ProxySpotterTask : Task
    {
        public static ProxySpotterTask Task = new ProxySpotterTask();
        Point2D LastPos = null;

        public ProxySpotterTask() : base(8)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && units.Count == 0 && LastPos != null;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Bot.Bot.TargetManager.AttackTarget, Count = 1, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            float distance = LastPos == null ? 40 * 40 : SC2Util.DistanceSq(Bot.Bot.MapAnalyzer.StartLocation, LastPos);
            foreach (Unit enemy in Bot.Bot.Enemies())
                if (enemy.UnitType == UnitTypes.PROBE)
                {
                    float newDist = SC2Util.DistanceSq(Bot.Bot.MapAnalyzer.StartLocation, enemy.Pos);
                    if (newDist < distance)
                    {
                        distance = newDist;
                        LastPos = SC2Util.To2D(enemy.Pos);
                    }
                }
            return LastPos != null;
        }

        public override void OnFrame(Bot tyr)
        {
            if (LastPos == null)
            {
                Clear();
                return;
            }
            foreach (Agent agent in Units)
            {
                if (agent.DistanceSq(LastPos) <= 2 * 2)
                    LastPos = null;
                else
                    agent.Order(Abilities.ATTACK, LastPos);
            }
        }
    }
}
