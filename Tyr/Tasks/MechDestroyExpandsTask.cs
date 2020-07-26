using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class MechDestroyExpandsTask : Task
    {
        public static MechDestroyExpandsTask Task = new MechDestroyExpandsTask();
        private List<Point2D> Bases = new List<Point2D>();

        public int MaxSize = 12;
        public int RequiredSize = 12;
        public int RetreatSize = 4;
        public uint UnitType = UnitTypes.HELLBAT;
        
        public static void Enable()
        {
            Enable(Task);
        }

        public MechDestroyExpandsTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitType && Units.Count < MaxSize;
        }

        public override bool IsNeeded()
        {
            Bot.Main.DrawText("Mines needed: " + (Bot.Main.Build.Completed(UnitType) >= RequiredSize));
            return Bot.Main.Build.Completed(UnitType) >= RequiredSize;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Count = MaxSize - Units.Count, UnitTypes = new HashSet<uint>() { UnitType } });
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            Bot.Main.DrawText("Mines attacking: " + Units.Count);
            for (int i = Bases.Count - 1; i >= 0; i--)
            {
                if (bot.TargetManager.PotentialEnemyStartLocations.Count == 1
                    && SC2Util.DistanceSq(Bases[i], bot.TargetManager.PotentialEnemyStartLocations[0]) <= 25 * 25)
                {
                    Bases.RemoveAt(i);
                    continue;
                }

                bool closeEnemy = false;
                foreach (Unit enemy in bot.Enemies())
                {
                    if (!UnitTypes.BuildingTypes.Contains(enemy.UnitType))
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
                foreach (Agent agent in bot.UnitManager.Agents.Values)
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
            
            if (units.Count <= RetreatSize)
            {
                Clear();
                return;
            }

            float distance = 1000000;
            Point2D target = null;
            foreach (BuildingLocation building in bot.EnemyManager.EnemyBuildings.Values)
            {
                if (bot.TargetManager.PotentialEnemyStartLocations.Count == 1
                       && SC2Util.DistanceSq(building.Pos, bot.TargetManager.PotentialEnemyStartLocations[0]) <= 30 * 30)
                    continue;

                foreach (Agent agent in units)
                {
                    float dist = agent.DistanceSq(building.Pos);
                    if (dist < distance)
                    {
                        distance = dist;
                        target = SC2Util.To2D(building.Pos);
                    }
                }
            }
            
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
                bot.MicroController.Attack(agent, target);
        }
    }
}
