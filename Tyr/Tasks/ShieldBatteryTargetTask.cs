﻿using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class ShieldBatteryTargetTask : Task
    {
        public static ShieldBatteryTargetTask Task = new ShieldBatteryTargetTask();
        public ShieldBatteryTargetTask() : base(8)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.SHIELD_BATTERY;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            foreach (Agent battery in units)
            {
                float dist = 1000000;
                Agent target = null;
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (agent.Unit.Shield < agent.Unit.ShieldMax && (agent.Unit.UnitType == UnitTypes.FORGE || agent.Unit.UnitType == UnitTypes.GATEWAY || agent.Unit.UnitType == UnitTypes.PYLON))
                    {
                        float newDist = SC2Util.DistanceSq(battery.Unit.Pos, agent.Unit.Pos);
                        if (newDist < dist)
                        {
                            target = agent;
                            dist = newDist;
                        }
                    }

                if (target != null)
                    battery.Order(Abilities.MOVE, target.Unit.Tag);
            }
        }
    }
}
