﻿using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class WorkerSafetyTask : Task
    {
        public static WorkerSafetyTask Task = new WorkerSafetyTask();
        private Dictionary<ulong, ulong> SafetyTarget = new Dictionary<ulong, ulong>();

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
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

            if (agent.DistanceSq(Bot.Main.BaseManager.Main.BaseLocation.Pos) >= 40 * 40)
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

            foreach (Agent possibleCannon in Bot.Main.UnitManager.Agents.Values)
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
            foreach (Unit enemy in Bot.Main.Enemies())
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
            return Bot.Main.Build.Completed(UnitTypes.PHOTON_CANNON) > 0;
        }

        public override void OnFrame(Bot bot)
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
                if (!SafetyTarget.ContainsKey(agent.Unit.Tag) || !bot.UnitManager.Agents.ContainsKey(SafetyTarget[agent.Unit.Tag]))
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
                safetyCannon = bot.UnitManager.Agents[SafetyTarget[agent.Unit.Tag]];

                agent.Order(Abilities.MOVE, SC2Util.To2D(safetyCannon.Unit.Pos));
            }
        }
    }
}
