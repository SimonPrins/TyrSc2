using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Util;

namespace Tyr.Tasks
{
    class CannonRushTask : Task
    {
        public static CannonRushTask Task = new CannonRushTask();
        private int LastBuiltFrame = 0;

        private Point2D CannonLocation;
        private Point2D NextCannonLocation;

        public CannonRushTask() : base(8)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (Units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = Tyr.Bot.TargetManager.AttackTarget, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.PROBE } });
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            DetermineCannonLocation();
            if (CannonLocation == null)
                return;

            bool firstPylonDone = false;
            bool secondPylonDone = false;
            Point2D firstPylonPos = new Point2D() { X = CannonLocation.X + 1, Y = CannonLocation.Y - 1 };
            Point2D secondPylonPos = new Point2D() { X = CannonLocation.X + 1, Y = CannonLocation.Y + 1 };
            bool firstCannonDone = false;
            bool secondCannonDone = false;
            Point2D firstCannonPos = new Point2D() { X = CannonLocation.X - 1, Y = CannonLocation.Y - 1 };
            Point2D secondCannonPos = new Point2D() { X = CannonLocation.X - 1, Y = CannonLocation.Y + 1 };
            bool powerAvailable = false;
            bool cannonCover = false;
            int totalCannonCount = 0;
            foreach (Agent agent in tyr.Units())
            {
                if (agent.Unit.UnitType == UnitTypes.PYLON)
                {
                    if (agent.DistanceSq(firstPylonPos) <= 2)
                    {
                        firstPylonDone = true;
                        if (agent.Unit.BuildProgress >= 0.99)
                            powerAvailable = true;
                    }
                    else if (agent.DistanceSq(secondPylonPos) <= 2)
                    {
                        secondPylonDone = true;
                        if (agent.Unit.BuildProgress >= 0.99)
                            powerAvailable = true;
                    }
                }
                if (agent.Unit.UnitType == UnitTypes.PHOTON_CANNON)
                {
                    if (agent.DistanceSq(CannonLocation) <= 30 * 30)
                        totalCannonCount++;
                    if (agent.DistanceSq(firstCannonPos) <= 2)
                    {
                        firstCannonDone = true;
                        if (agent.Unit.BuildProgress >= 0.99)
                            cannonCover = true;
                    }
                    else if (agent.DistanceSq(secondCannonPos) <= 2)
                    {
                        secondCannonDone = true;
                        if (agent.Unit.BuildProgress >= 0.99)
                            cannonCover = true;
                    }
                    else if (NextCannonLocation != null && agent.DistanceSq(NextCannonLocation) <= 2)
                    {
                        NextCannonLocation = null;
                    }
                }
            }

            if (firstCannonDone && secondCannonDone && cannonCover && totalCannonCount < 4 && NextCannonLocation == null)
            {
                NextCannonLocation = tyr.buildingPlacer.FindPlacement(new PotentialHelper(CannonLocation, 5).To(tyr.TargetManager.PotentialEnemyStartLocations[0]).Get(), new Point2D() { X = 2, Y = 2 }, UnitTypes.PHOTON_CANNON);
            }

            foreach (Agent agent in Units)
            {
                if (agent.DistanceSq(CannonLocation) >= 20 * 20)
                {
                    agent.Order(Abilities.MOVE, CannonLocation);
                    continue;
                }
                if (!firstPylonDone)
                {
                    agent.Order(881, firstPylonPos);
                    continue;
                }
                if (!secondPylonDone)
                {
                    agent.Order(881, secondPylonPos);
                    continue;
                }
                if (!firstCannonDone)
                {
                    agent.Order(887, firstCannonPos);
                    continue;
                }
                if (!secondCannonDone)
                {
                    agent.Order(887, secondCannonPos);
                    continue;
                }
                if (NextCannonLocation != null)
                {
                    agent.Order(887, NextCannonLocation);
                    continue;
                }
            }

            
        }

        private void DetermineCannonLocation()
        {
            if (CannonLocation != null)
                return;
            
            if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count != 1)
                return;
            Point2D enemyMain = Tyr.Bot.TargetManager.PotentialEnemyStartLocations[0];
            Point2D enemyRamp = Tyr.Bot.MapAnalyzer.GetEnemyRamp();

            for (float x = -20; x <= 20; x++)
                for (float y = -20; y <= 20; y++)
                {
                    Point2D loc = new Point2D() { X = enemyMain.X + x, Y = enemyMain.Y + y };
                    float enemyMainDist = SC2Util.DistanceSq(loc, enemyMain);
                    if (enemyMainDist <= 16 * 16 || enemyMainDist >= 18 * 18)
                        continue;
                    float enemyRampDist = SC2Util.DistanceSq(loc, enemyRamp);
                    if (enemyRampDist <= 18 * 18 || enemyRampDist >= 20 * 20)
                        continue;

                    if (!(ProtossBuildingPlacement.RectBuildable(loc.X - 2, loc.Y - 2, loc.X + 2, loc.Y + 2)))
                        continue;

                    CannonLocation = loc;
                    System.Console.WriteLine("Picked: " + loc);
                    return;
                }
        }
    }
}
