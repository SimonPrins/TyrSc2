using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class WorkerSafetyTask : Task
    {
        public static WorkerSafetyTask Task = new WorkerSafetyTask();
        private Dictionary<ulong, ulong> SafetyTarget = new Dictionary<ulong, ulong>();

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public WorkerSafetyTask() : base(10)
        { }

        public override List<UnitDescriptor> GetDescriptors()
        {
            UnitDescriptor descriptor = new UnitDescriptor();
            descriptor.AddType(UnitTypes.PROBE);
            return new List<UnitDescriptor>() { descriptor };
        }

        public override bool DoWant(Agent agent)
        {
            if (!UnderThreat(agent, 6))
                return false;

            Agent cannon = GetSafetyCannon(agent);
            if (cannon == null)
                return false;

            if (agent.DistanceSq(Tyr.Bot.BaseManager.Main.BaseLocation.Pos) >= 40 * 40)
                return false;

            Safe(agent, cannon);
            return true;
        }

        private void Safe(Agent probe, Agent cannon)
        {
            if (!SafetyTarget.ContainsKey(probe.Unit.Tag))
                SafetyTarget.Add(probe.Unit.Tag, cannon.Unit.Tag);
            else
                SafetyTarget[probe.Unit.Tag] = cannon.Unit.Tag;
        }

        private Agent GetSafetyCannon(Agent agent)
        {
            float distance = 1000 * 1000;
            Agent cannon = null;

            foreach (Agent possibleCannon in Tyr.Bot.UnitManager.Agents.Values)
            {
                if (possibleCannon.Unit.UnitType != UnitTypes.PHOTON_CANNON
                    || possibleCannon.Unit.BuildProgress < 0.95)
                    continue;

                float newDist = agent.DistanceSq(possibleCannon);
                if (newDist < distance)
                {
                    distance = newDist;
                    cannon = possibleCannon;
                }
            }

            return cannon;
        }

        private bool UnderThreat(Agent agent, float radius)
        {
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.REAPER
                    && enemy.UnitType != UnitTypes.BANSHEE)
                    continue;
                if (agent.DistanceSq(enemy) < radius * radius)
                    return true;
            }
            return false;
        }

        public override bool IsNeeded()
        {
            return Tyr.Bot.Build.Completed(UnitTypes.PHOTON_CANNON) > 0;
        }

        public override void OnFrame(Tyr tyr)
        {
            for (int i = units.Count - 1; i >= 0; i--)
            {
                Agent agent = units[i];
                if (!UnderThreat(agent, 8))
                {
                    IdleTask.Task.Add(agent);
                    RemoveAt(i);
                    continue;
                }

                Agent safetyCannon = null;
                if (!SafetyTarget.ContainsKey(agent.Unit.Tag) || !tyr.UnitManager.Agents.ContainsKey(SafetyTarget[agent.Unit.Tag]))
                {
                    safetyCannon = GetSafetyCannon(agent);
                    if (safetyCannon == null)
                    {
                        IdleTask.Task.Add(agent);
                        RemoveAt(i);
                        continue;
                    }
                    Safe(agent, safetyCannon);
                }
                safetyCannon = tyr.UnitManager.Agents[SafetyTarget[agent.Unit.Tag]];

                agent.Order(Abilities.MOVE, SC2Util.To2D(safetyCannon.Unit.Pos));
            }
        }
    }
}
