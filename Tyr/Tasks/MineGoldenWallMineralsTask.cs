using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class MineGoldenWallMineralsTask : Task
    {
        public static MineGoldenWallMineralsTask Task = new MineGoldenWallMineralsTask();

        public Point2D FirstMineralPos;
        public Point2D SecondMineralPos;
        private bool Done = false;

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public MineGoldenWallMineralsTask() : base(10)
        {}

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> descriptors = new List<UnitDescriptor>();
            if (Units.Count == 0)
                descriptors.Add(new UnitDescriptor() { UnitTypes = UnitTypes.WorkerTypes, Count = 1});
            return descriptors;

        }

        public override bool IsNeeded()
        {
            return Bot.Main.Map == MapAnalysis.MapEnum.GoldenWall && !Done;
        }

        public override void OnFrame(Bot bot)
        {
            if (Bot.Main.Map != MapAnalysis.MapEnum.GoldenWall)
                return;
            Agent resourceCenter = bot.Build.Main.ResourceCenter;
            if (FirstMineralPos == null)
            {
                List<Point2D> minerals = new List<Point2D>();
                float avgX = 0;
                float avgY = 0;

                foreach (Unit mineral in bot.Observation.Observation.RawData.Units)
                {
                    if (mineral.Alliance != Alliance.Neutral)
                        continue;
                    float mainDist = SC2Util.DistanceSq(bot.MapAnalyzer.StartLocation, mineral.Pos);
                    if (mainDist < 15 * 15)
                        continue;
                    if (mainDist > 19 * 19)
                        continue;
                    minerals.Add(SC2Util.To2D(mineral.Pos));
                    avgX += mineral.Pos.X;
                    avgY += mineral.Pos.Y;
                }
                avgX /= minerals.Count;
                avgY /= minerals.Count;
                Point2D avgPos = new Point2D() { X = avgX, Y = avgY};

                System.Console.WriteLine("Minerals before: " + minerals.Count);
                minerals.RemoveAll(mineral => SC2Util.DistanceSq(mineral, avgPos) < 2);
                System.Console.WriteLine("MineralsRemaining: " + minerals.Count);

                Point2D furthest = null;
                float dist = -1;
                foreach (Point2D mineral in minerals)
                {
                    float newDist = SC2Util.DistanceSq(avgPos, mineral);
                    if (newDist < dist)
                        continue;
                    dist = newDist;
                    furthest = mineral;
                }

                minerals.Sort((a, b) => System.Math.Sign(SC2Util.DistanceSq(b, furthest) - SC2Util.DistanceSq(a, furthest)));
                FirstMineralPos = minerals[0];
                SecondMineralPos = minerals[1];
                System.Console.WriteLine("Distance between minerals: " + SC2Util.DistanceSq(furthest, FirstMineralPos));
                System.Console.WriteLine("First: " + FirstMineralPos);
                System.Console.WriteLine("Second: " + SecondMineralPos);

            }


            foreach (Agent agent in units)
            {
                foreach (Unit mineral in bot.Observation.Observation.RawData.Units)
                {
                    if (mineral.Alliance != Alliance.Neutral)
                        continue;
                    if (SC2Util.DistanceSq(mineral.Pos, FirstMineralPos) > 0.25)
                        continue;
                    bot.DrawLine(agent, mineral.Pos);
                }
                foreach (Unit mineral in bot.Observation.Observation.RawData.Units)
                {
                    if (mineral.Alliance != Alliance.Neutral)
                        continue;
                    if (SC2Util.DistanceSq(mineral.Pos, SecondMineralPos) > 0.25)
                        continue;

                    bot.DrawLine(agent, mineral.Pos);
                }
                if (agent.IsCarryingResources())
                {
                    if (resourceCenter != null)
                        agent.Order(Abilities.MOVE,  resourceCenter.Unit.Tag);
                    continue;
                }
                Unit targetMineral = null;
                float dist = 20 * 20;
                foreach (Unit mineral in bot.Observation.Observation.RawData.Units)
                {
                    if (mineral.Alliance != Alliance.Neutral)
                        continue;
                    if (SC2Util.DistanceSq(mineral.Pos, FirstMineralPos) > 0.25
                        && SC2Util.DistanceSq(mineral.Pos, SecondMineralPos) > 0.25)
                        continue;
                    float newDist = SC2Util.DistanceSq(mineral.Pos, bot.MapAnalyzer.StartLocation);
                    if (newDist > dist)
                        continue;
                    dist = newDist;
                    targetMineral = mineral;
                }
                if (targetMineral != null)
                    agent.Order(Abilities.MOVE, targetMineral.Tag);
                else
                {
                    System.Console.WriteLine("Done.");
                    Done = true;
                    Clear();
                }
            }
        }
    }
}
