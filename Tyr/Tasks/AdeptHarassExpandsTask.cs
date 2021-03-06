﻿using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class AdeptHarassExpandsTask : Task
    {
        public static AdeptHarassExpandsTask Task = new AdeptHarassExpandsTask();
        private List<Point2D> Bases = new List<Point2D>();

        public int RequiredSize = 6;
        
        public static void Enable()
        {
            Enable(Task);
        }

        public AdeptHarassExpandsTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.ADEPT && Units.Count < RequiredSize;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.Build.Completed(UnitTypes.ADEPT) >= RequiredSize;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            int required = RequiredSize - Units.Count;
            if (required > 0)
                result.Add(new UnitDescriptor() { Count = required, UnitTypes = new HashSet<uint>() { UnitTypes.ADEPT } });
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            for (int i = Bases.Count - 1; i >= 0; i--)
            {
                if (bot.TargetManager.PotentialEnemyStartLocations.Count == 1
                    && SC2Util.DistanceSq(Bases[i], bot.TargetManager.PotentialEnemyStartLocations[0]) <= 25 * 25)
                {
                    Bases.RemoveAt(i);
                    continue;
                }
                if (bot.TargetManager.PotentialEnemyStartLocations.Count == 1
                    && SC2Util.DistanceSq(Bases[i], bot.TargetManager.PotentialEnemyStartLocations[0]) >= 70 * 70)
                {
                    Bases.RemoveAt(i);
                    continue;
                }

                bool closeEnemy = false;
                foreach (Unit enemy in bot.Enemies())
                {
                    if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, Bases[i]) <= 8 * 8)
                    {
                        closeEnemy = true;
                        break;
                    }
                }
                if (closeEnemy)
                    continue;

                bool closeAlly = false;
                foreach (Agent agent in Units)
                    if (agent.DistanceSq(Bases[i]) <= 4 * 4)
                    {
                        closeAlly = true;
                        break;
                    }
                if (closeAlly)
                    Bases.RemoveAt(i);
            }

            if (Bases.Count == 0)
                foreach (BaseLocation b in bot.MapAnalyzer.BaseLocations)
                    Bases.Add(b.Pos);
            

            float distance = 1000000;
            Point2D target = null;
            
            foreach (Point2D b in Bases)
            {
                foreach (Agent agent in units)
                {
                    float dist = agent.DistanceSq(b);
                    if (dist < distance)
                    {
                        distance = dist;
                        target = b;
                    }
                }
            }

            foreach (Agent agent in units)
            {
                if (Bot.Main.Frame % 48 == 0)
                {
                    bool closeEnemy = false;
                    foreach (Unit enemy in Bot.Main.Enemies())
                    {
                        if (agent.DistanceSq(enemy) <= 8 * 8)
                        {
                            closeEnemy = true;
                            break;
                        }
                    }
                    if (closeEnemy)
                    {
                        agent.Order(2544, bot.TargetManager.PotentialEnemyStartLocations[0]);
                        continue;
                    }
                }
                bot.MicroController.Attack(agent, target);
            }
        }
    }
}
