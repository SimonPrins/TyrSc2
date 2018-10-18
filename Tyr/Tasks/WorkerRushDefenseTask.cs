using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.MapAnalysis;
using Tyr.Util;

namespace Tyr.Tasks
{
    class WorkerRushDefenseTask : Task
    {
        public static WorkerRushDefenseTask Task = new WorkerRushDefenseTask();
        private MineralField mineral = null;

        public WorkerRushDefenseTask() : base(10)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
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

        public override void OnFrame(Tyr tyr)
        {
            if (mineral == null && tyr.BaseManager.Main.BaseLocation.MineralFields.Count > 0)
                mineral = tyr.BaseManager.Main.BaseLocation.MineralFields[0];

            Unit closestEnemy = null;
            float distance = 10 * 10;
            float hp = 1000;
            foreach (Unit enemy in tyr.Enemies())
            {
                float newDist = SC2Util.DistanceSq(mineral.Pos, enemy.Pos);
                if (newDist < 2 * 2)
                {
                    if (enemy.Health < hp)
                    {
                        closestEnemy = enemy;
                        distance = newDist;
                        hp = enemy.Health;
                    }
                }
                else if (newDist < distance)
                {
                    closestEnemy = enemy;
                    distance = newDist;
                }
            }
            foreach (Agent agent in Units)
            {
                if (closestEnemy != null && (agent.DistanceSq(closestEnemy) > 2 * 2 || agent.DistanceSq(mineral.Pos) <= 2 * 2))
                {
                    agent.Order(Abilities.ATTACK, closestEnemy.Tag);
                    tyr.DrawLine(agent, closestEnemy.Pos);
                }
                else
                    agent.Order(Abilities.MOVE, mineral.Tag);
            }
        }

        private Unit GetBroodling(Agent agent)
        {
            Unit broodling = null;
            float dist = 6 * 6;
            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BROODLING)
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    dist = newDist;
                    broodling = enemy;
                }
            }
            return broodling;
        }
    }
}
