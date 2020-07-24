using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Util;

namespace Tyr.MapAnalysis
{
    public class WallInCreator
    {
        public List<WallBuilding> Wall = new List<WallBuilding>();
        public void Create(List<uint> types)
        {
            foreach (uint type in types)
                Wall.Add(new WallBuilding() { Type = type });

            
            BoolGrid pathable = Bot.Main.MapAnalyzer.Pathable;
            BoolGrid unPathable = pathable.Invert();
            BoolGrid ramp = Bot.Main.MapAnalyzer.Ramp;

            BoolGrid rampAdjacent = unPathable.GetAdjacent(ramp);
            BoolGrid rampSides = unPathable.GetConnected(rampAdjacent, 5);
            List<BoolGrid> sides = rampSides.GetGroups();

            List<Point2D> building1Positions = Placable(sides[0], Bot.Main.MapAnalyzer.StartArea, BuildingType.LookUp[types[0]].Size, true);
            List<Point2D> building2Positions = Placable(sides[1], Bot.Main.MapAnalyzer.StartArea, BuildingType.LookUp[types[2]].Size, true);

            float wallScore = 1000;
            foreach (Point2D p1 in building1Positions)
                foreach (Point2D p2 in building2Positions)
                {
                    if (System.Math.Abs(p1.X - p2.X) + 0.1f < (BuildingType.LookUp[types[0]].Size.X + BuildingType.LookUp[types[2]].Size.X) / 2f
                        && System.Math.Abs(p1.Y - p2.Y) + 0.1f < (BuildingType.LookUp[types[0]].Size.Y + BuildingType.LookUp[types[2]].Size.Y) / 2f)
                        continue;

                    float newScore = SC2Util.DistanceGrid(p1, p2);
                    if (newScore >= wallScore)
                        continue;
                    
                    Wall[0].Pos = p1;
                    Wall[2].Pos = p2;
                    wallScore = newScore;
                }

            HashSet<Point2D> around1 = new HashSet<Point2D>();
            GetPlacableAround(Bot.Main.MapAnalyzer.StartArea, Wall[0].Pos, Wall[0].Size, Wall[1].Size, around1, true, false);
            HashSet<Point2D> around2 = new HashSet<Point2D>();
            GetPlacableAround(Bot.Main.MapAnalyzer.StartArea, Wall[2].Pos, Wall[2].Size, Wall[1].Size, around2, true, false);
            around1.IntersectWith(around2);

            foreach (Point2D pos in around1)
            {
                Wall[1].Pos = new Point2D() { X = pos.X, Y = pos.Y };
                System.Console.WriteLine("Pylon pos: " + Wall[1].Pos);
                break;
            }
            //DrawResult(unPathable, null, building1Positions, building2Positions);
        }

        public void CreateNatural(List<uint> types)
        {
            if (Bot.Main.Map == MapEnum.Zen)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall = new List<WallBuilding>();
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 55.5f, Y = 62.5f }, Type = types[0] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 58.5f, Y = 62.5f }, Type = types[1] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 69.5f, Y = 56.5f }, Type = types[2] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 52.5f, Y = 61.5f }, Type = types[3] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 58, Y = 60 }, Type = UnitTypes.PYLON });
                    return;
                }
                else
                {
                    Wall = new List<WallBuilding>();
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 139.5f, Y = 110.5f }, Type = types[0] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 133.5f, Y = 109.5f }, Type = types[1] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 121.5f, Y = 115.5f }, Type = types[2] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 136.5f, Y = 108.5f }, Type = types[3] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 134, Y = 113 }, Type = UnitTypes.PYLON });
                    return;
                }
            }
            if (Bot.Main.Map == MapEnum.Acropolis)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall = new List<WallBuilding>();
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 39.5f, Y = 102.5f }, Type = types[0] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 32.5f, Y = 104.5f }, Type = types[1] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 37.5f, Y = 102.5f }, Type = types[2] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 35.5f, Y = 102.5f }, Type = types[3] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 35, Y = 105 }, Type = UnitTypes.PYLON });
                    return;
                }
                else
                {
                    Wall = new List<WallBuilding>();
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 136.5f, Y = 68.5f }, Type = types[0] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 143.5f, Y = 67.5f }, Type = types[1] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 138.5f, Y = 68.5f }, Type = types[2] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 140.5f, Y = 68.5f }, Type = types[3] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 140, Y = 66 }, Type = UnitTypes.PYLON });
                    return;
                }
            }
            if (Bot.Main.Map == MapEnum.EternalEmpire)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall = new List<WallBuilding>();
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 43.5f, Y = 61.5f }, Type = types[0] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 42.5f, Y = 58.5f }, Type = types[1] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 41.5f, Y = 56.5f }, Type = types[2] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 41.5f, Y = 54.5f }, Type = types[3] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 40, Y = 59 }, Type = UnitTypes.PYLON });
                    return;
                }
                else
                {
                    Wall = new List<WallBuilding>();
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 132.5f, Y = 110.5f }, Type = types[0] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 133.5f, Y = 113.5f }, Type = types[1] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 133.5f, Y = 115.5f }, Type = types[2] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 134.5f, Y = 117.5f }, Type = types[3] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 136, Y = 113 }, Type = UnitTypes.PYLON });
                    return;
                }
            }
            if (Bot.Main.Map == MapEnum.DeathAura)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall = new List<WallBuilding>();
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 57.5f, Y = 138.5f }, Type = types[0] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 60.5f, Y = 138.5f }, Type = types[1] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 62.5f, Y = 138.5f }, Type = types[2] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 64.5f, Y = 139.5f }, Type = types[3] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 59, Y = 142 }, Type = UnitTypes.PYLON });
                    return;
                }
                else
                {
                    Wall = new List<WallBuilding>();
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 133.5f, Y = 49.5f }, Type = types[0] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 130.5f, Y = 49.5f }, Type = types[1] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 128.5f, Y = 49.5f }, Type = types[2] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 126.5f, Y = 49.5f }, Type = types[3] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 132, Y = 47 }, Type = UnitTypes.PYLON });
                    return;
                }
            }
            if (Bot.Main.Map == MapEnum.IceAndChrome)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall = new List<WallBuilding>();
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 80.5f, Y = 94.5f }, Type = types[0] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 79.5f, Y = 97.5f }, Type = types[1] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 77.5f, Y = 98.5f }, Type = types[2] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 75.5f, Y = 99.5f }, Type = types[3] });
                    Wall.Add(new WallBuilding() { Pos = new Point2D() { X = 77, Y = 96 }, Type = UnitTypes.PYLON });
                    return;
                }
            }

            BoolGrid pathable = Bot.Main.MapAnalyzer.Pathable;
            BoolGrid unPathable = pathable.Invert();
            Point2D naturalExit = Bot.Main.BaseManager.NaturalDefensePos;
            ArrayBoolGrid unPathableIncludingMinerals = new ArrayBoolGrid(unPathable.Width(), unPathable.Height());
            for (int x = 0; x < unPathable.Width(); x++)
                for (int y = 0; y < unPathable.Height(); y++)
                    unPathableIncludingMinerals[x, y] = unPathable[x, y];
            foreach (Unit mineral in Bot.Main.Observation.Observation.RawData.Units)
            {
                if (mineral.Alliance != Alliance.Neutral)
                    continue;
                unPathableIncludingMinerals[(int)mineral.Pos.X - 1, (int)mineral.Pos.Y] = true;
                unPathableIncludingMinerals[(int)mineral.Pos.X    , (int)mineral.Pos.Y] = true;
                unPathableIncludingMinerals[(int)mineral.Pos.X + 1, (int)mineral.Pos.Y] = true;
                unPathableIncludingMinerals[(int)mineral.Pos.X, (int)mineral.Pos.Y + 1] = true;
                unPathableIncludingMinerals[(int)mineral.Pos.X, (int)mineral.Pos.Y - 1] = true;
            }
            BoolGrid naturalWalls = unPathableIncludingMinerals.GetConnected(new Point2D() { X = 0, Y = 0 }).Crop((int)naturalExit.X - 12, (int)naturalExit.Y - 12, (int)naturalExit.X + 12, (int)naturalExit.Y + 12);
            List<BoolGrid> sides = naturalWalls.GetGroups();
            Dictionary<BoolGrid, int> counts = new Dictionary<BoolGrid, int>();
            foreach (BoolGrid side in sides)
                counts.Add(side, side.Count());
            sides.Sort((BoolGrid a, BoolGrid b) => counts[b] - counts[a]);

            if (sides.Count < 2)
            {
                DrawResult(unPathable, naturalWalls, new List<Point2D>(), new List<Point2D>());
                DebugUtil.WriteLine("Could not find natural wall sides.");
                return;
            }

            List<Point2D> building1Positions = Placable(sides[0], Bot.Main.MapAnalyzer.Placement, BuildingType.LookUp[types[0]].Size, false);
            List<Point2D> building2Positions = Placable(sides[1], Bot.Main.MapAnalyzer.Placement, BuildingType.LookUp[types[3]].Size, false);
            int naturalHeight = Bot.Main.MapAnalyzer.MapHeight((int)Bot.Main.BaseManager.Natural.BaseLocation.Pos.X, (int)Bot.Main.BaseManager.Natural.BaseLocation.Pos.Y);
            building1Positions = building1Positions.FindAll((p) => Bot.Main.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);
            building2Positions = building2Positions.FindAll((p) => Bot.Main.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);

            Point2D buildingPosition1 = null;
            Point2D buildingPosition2 = null;
            float dist = 1000000;
            foreach(Point2D point1 in building1Positions)
            {
                foreach (Point2D point2 in building2Positions)
                {
                    float newDist = SC2Util.DistanceSq(point1, point2);
                    if (newDist > dist)
                        continue;
                    dist = newDist;
                    buildingPosition1 = point1;
                    buildingPosition2 = point2;
                }
            }
            building1Positions = new List<Point2D>() { buildingPosition1 };
            building2Positions = new List<Point2D>() { buildingPosition2 };
            FileUtil.Debug("Start building positions: " + buildingPosition1 + " " + buildingPosition2);

            FindWall(types, building1Positions, building2Positions, Bot.Main.MapAnalyzer.Placement, false);

            if (Wall.Count > 0)
            {

                if (Bot.Main.Map == MapEnum.WorldOfSleepers
                    && Bot.Main.MapAnalyzer.StartLocation.X <= 80
                    && Math.Abs(Wall[4].Pos.Y - Wall[3].Pos.Y) <= 2.5
                    && Math.Abs(Wall[4].Pos.X - Wall[3].Pos.X) <= 2.5)
                {
                    Wall[4].Pos = new Point2D() { X = Wall[4].Pos.X + 1, Y = Wall[4].Pos.Y - 2 };
                }
                return;
            }
            // No wall was found. Try three straight buildings with zealot at the end.
            building2Positions = Placable(sides[1], Bot.Main.MapAnalyzer.Placement, BuildingType.LookUp[types[3]].Size, false, true);
            building2Positions = building2Positions.FindAll((p) => Bot.Main.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);

            buildingPosition1 = null;
            buildingPosition2 = null;
            dist = 1000000;
            foreach (Point2D point1 in building1Positions)
            {
                foreach (Point2D point2 in building2Positions)
                {
                    float newDist = SC2Util.DistanceSq(point1, point2);
                    if (newDist > dist)
                        continue;
                    dist = newDist;
                    buildingPosition1 = point1;
                    buildingPosition2 = point2;
                }
            }
            building1Positions = new List<Point2D>() { buildingPosition1 };
            building2Positions = new List<Point2D>() { buildingPosition2 };
            FileUtil.Debug("Start building positions: " + buildingPosition1 + " " + buildingPosition2);

            FindWallZealotAtEnd(types, building1Positions, building2Positions, Bot.Main.MapAnalyzer.Placement);
            DrawResult(unPathableIncludingMinerals, naturalWalls, building1Positions, building2Positions);
        }

        public void CreateFullNatural(List<uint> types)
        {
            BoolGrid pathable = Bot.Main.MapAnalyzer.Pathable;
            BoolGrid unPathable = pathable.Invert();
            Point2D naturalExit = Bot.Main.BaseManager.NaturalDefensePos;
            BoolGrid naturalWalls = unPathable.GetConnected(new Point2D() { X = 0, Y = 0 }).Crop((int)naturalExit.X - 12, (int)naturalExit.Y - 12, (int)naturalExit.X + 12, (int)naturalExit.Y + 12);
            List<BoolGrid> sides = naturalWalls.GetGroups();
            if (sides.Count < 2)
                return;
            Dictionary<BoolGrid, int> counts = new Dictionary<BoolGrid, int>();
            foreach (BoolGrid side in sides)
                counts.Add(side, side.Count());
            sides.Sort((BoolGrid a, BoolGrid b) => counts[b] - counts[a]);

            List<Point2D> building1Positions = Placable(sides[0], Bot.Main.MapAnalyzer.Placement, BuildingType.LookUp[types[0]].Size, false);
            List<Point2D> building2Positions = Placable(sides[1], Bot.Main.MapAnalyzer.Placement, BuildingType.LookUp[types[3]].Size, false);
            int naturalHeight = Bot.Main.MapAnalyzer.MapHeight((int)Bot.Main.BaseManager.Natural.BaseLocation.Pos.X, (int)Bot.Main.BaseManager.Natural.BaseLocation.Pos.Y);
            building1Positions = building1Positions.FindAll((p) => Bot.Main.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);
            building2Positions = building2Positions.FindAll((p) => Bot.Main.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);


            FindWall(types, building1Positions, building2Positions, Bot.Main.MapAnalyzer.Placement, true);
            //DrawResult(unPathable, naturalWalls, building1Positions, building2Positions);
        }

        public void CreateReaperWall(List<uint> types)
        {
            foreach (uint type in types)
                Wall.Add(new WallBuilding() { Type = type });


            BoolGrid startArea = Bot.Main.MapAnalyzer.StartArea;
            BoolGrid placement = Bot.Main.MapAnalyzer.Placement;

            BoolGrid mainAndNatural = Bot.Main.MapAnalyzer.Pathable.GetConnected(Bot.Main.MapAnalyzer.StartArea, 25).GetOr(Bot.Main.MapAnalyzer.StartArea);
            ArrayBoolGrid outside = (ArrayBoolGrid)placement.GetAnd(mainAndNatural.Invert());

            if (Bot.Main.Map == MapEnum.Thunderbird)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall[0].Pos = new Point2D() { X = 51.5f, Y = 167 - 45.5f };
                    Wall[1].Pos = new Point2D() { X = 53f, Y = 167 - 43f };
                    Wall[2].Pos = new Point2D() { X = 55.5f, Y = 167 - 41.5f };
                }
                else
                {
                    Wall[0].Pos = new Point2D() { X = 136.5f, Y = 167 - 136.5f };
                    Wall[1].Pos = new Point2D() { X = 139f, Y = 167 - 135f };
                    Wall[2].Pos = new Point2D() { X = 140.5f, Y = 167 - 132.5f };
                }
                //DrawReaperWall(outside, mainAndNatural, new ArrayBoolGrid(placement.Width(), placement.Height()), new ArrayBoolGrid(placement.Width(), placement.Height()), null, null);
                return;
            }
            if (Bot.Main.Map == MapEnum.Zen)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall[0].Pos = new Point2D() { X = 65.5f, Y = 25.5f };
                    Wall[1].Pos = new Point2D() { X = 63f, Y = 26f };
                    Wall[2].Pos = new Point2D() { X = 65.5f, Y = 28.5f };
                }
                else
                {
                    Wall[0].Pos = new Point2D() { X = 126.5f, Y = 146.5f };
                    Wall[1].Pos = new Point2D() { X = 129f, Y = 145f };
                    Wall[2].Pos = new Point2D() { X = 126.5f, Y = 143.5f };
                }
                return;
            }
            if (Bot.Main.Map == MapEnum.IceAndChrome)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall[0].Pos = new Point2D() { X = 87.5f, Y = 77.5f };
                    Wall[1].Pos = new Point2D() { X = 88, Y = 75f };
                    Wall[2].Pos = new Point2D() { X = 89.5f, Y = 72.5f };
                }
                else
                {
                    Wall[0].Pos = new Point2D() { X = 164.5f, Y = 165.5f };
                    Wall[1].Pos = new Point2D() { X = 167f, Y = 164f };
                    Wall[2].Pos = new Point2D() { X = 168.5f, Y = 161.5f };
                }
                return;
            }
            if (Bot.Main.Map == MapEnum.DeathAura)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall[0].Pos = new Point2D() { X = 46.5f, Y = 128.5f };
                    Wall[1].Pos = new Point2D() { X = 49f, Y = 130f };
                    Wall[2].Pos = new Point2D() { X = 51.5f, Y = 131.5f };
                }
                else
                {
                    Wall[0].Pos = new Point2D() { X = 140.5f, Y = 56.5f };
                    Wall[1].Pos = new Point2D() { X = 143, Y = 58 };
                    Wall[2].Pos = new Point2D() { X = 145.5f, Y = 59.5f };
                }
                return;
            }
            if (Bot.Main.Map == MapEnum.Simulacrum)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall[0].Pos = new Point2D() { X = 76.5f, Y = 132.5f };
                    Wall[1].Pos = new Point2D() { X = 77f, Y = 135f };
                    Wall[2].Pos = new Point2D() { X = 79.5f, Y = 135.5f };
                }
                else
                {
                    Wall[0].Pos = new Point2D() { X = 136.5f, Y = 48.5f };
                    Wall[1].Pos = new Point2D() { X = 139, Y = 49 };
                    Wall[2].Pos = new Point2D() { X = 139.5f, Y = 51.5f };
                }
                return;
            }
            if (Bot.Main.Map == MapEnum.Nightshade)
            {
                if (Bot.Main.MapAnalyzer.StartLocation.X <= 100)
                {
                    Wall[0].Pos = new Point2D() { X = 50.5f, Y = 124.5f };
                    Wall[1].Pos = new Point2D() { X = 52f, Y = 127f };
                    Wall[2].Pos = new Point2D() { X = 54.5f, Y = 128.5f };
                }
                else
                {
                    Wall[0].Pos = new Point2D() { X = 137.5f, Y = 43.5f };
                    Wall[1].Pos = new Point2D() { X = 140, Y = 45 };
                    Wall[2].Pos = new Point2D() { X = 141.5f, Y = 47.5f };
                }
                return;
            }
            //DrawReaperWall(outside, mainAndNatural, new ArrayBoolGrid(placement.Width(), placement.Height()), new ArrayBoolGrid(placement.Width(), placement.Height()), null, null);
            if (CreateReaperWall(types, outside, 2, false))
                return;
            int mainHeight = Bot.Main.MapAnalyzer.MapHeight((int)Bot.Main.MapAnalyzer.StartLocation.X, (int)Bot.Main.MapAnalyzer.StartLocation.Y);

            outside = (ArrayBoolGrid)mainAndNatural.Invert();

            for (int x = 0; x < outside.Width(); x++)
                for (int y = 0; y < outside.Height(); y++)
                {
                    if (outside[x, y]
                        && (Bot.Main.MapAnalyzer.MapHeight(x, y) < mainHeight - 16
                        || Bot.Main.MapAnalyzer.MapHeight(x, y) >= mainHeight))
                        outside[x, y] = false;
                }
            if (CreateReaperWall(types, outside, 0, true))
                return;
            Wall = new List<WallBuilding>();
        }

        private bool CreateReaperWall(List<uint> types, ArrayBoolGrid outside, int minSize, bool cliffCutoffHeight)
        {
            BoolGrid startArea = Bot.Main.MapAnalyzer.StartArea;
            BoolGrid placement = Bot.Main.MapAnalyzer.Placement;

            int mainHeight = Bot.Main.MapAnalyzer.MapHeight((int)Bot.Main.MapAnalyzer.StartLocation.X, (int)Bot.Main.MapAnalyzer.StartLocation.Y);

            BoolGrid grownStartArea = startArea.Grow();
            BoolGrid grownOutside = outside.Grow();
            ArrayBoolGrid reaperCliffs = (ArrayBoolGrid)grownOutside.GetAnd(grownStartArea);
            if (cliffCutoffHeight)
                for (int x = 0; x < reaperCliffs.Width(); x++)
                    for (int y = 0; y < reaperCliffs.Height(); y++)
                        if (reaperCliffs[x, y] && SC2Util.GetDataValue(Bot.Main.GameInfo.StartRaw.TerrainHeight, x, y) >= mainHeight - 24)
                            reaperCliffs[x, y] = false;
            //DrawReaperWall(outside, startArea, reaperCliffs, new ArrayBoolGrid(placement.Width(), placement.Height()), null, null);
            List<BoolGrid> reaperCliffGroups = reaperCliffs.GetGroups();
            BoolGrid reaperCliff = null;
            int count = minSize;
            foreach (BoolGrid group in reaperCliffGroups)
            {
                int newCount = group.Count();
                if (newCount > count)
                {
                    count = newCount;
                    reaperCliff = group;
                }
            }

            if (reaperCliff == null)
                return false;

            if (reaperCliff.Count() <= 3)
                reaperCliff = placement.Invert().GetConnected(reaperCliff, 3);

            BoolGrid nonReaperCliffs = placement.Invert().GetAnd(reaperCliff.Invert());
            BoolGrid cliffAdjacent = nonReaperCliffs.GetAdjacent(reaperCliff);
            BoolGrid cliffSides = nonReaperCliffs.GetConnected(cliffAdjacent, 4);

            //DrawReaperWall(outside, startArea, reaperCliff, cliffSides, null, null);


            List<BoolGrid> sides = cliffSides.GetGroups();
            if (sides.Count < 2)
                return false;
            BoolGrid side1 = sides[0];
            int side1Count = side1.Count();
            BoolGrid side2 = sides[1];
            int side2Count = side2.Count();
            if (side1Count < side2Count)
            {
                int tempCount = side1Count;
                side1Count = side2Count;
                side2Count = tempCount;
                BoolGrid tempSide = side1;
                side1 = side2;
                side2 = tempSide;
            }
            for (int i = 2; i < sides.Count; i++)
            {
                BoolGrid next = sides[i];
                int nextCount = next.Count();
                if (nextCount < side2Count)
                    continue;

                side2 = next;
                side2Count = nextCount;

                if (side1Count < side2Count)
                {
                    int tempCount = side1Count;
                    side1Count = side2Count;
                    side2Count = tempCount;
                    BoolGrid tempSide = side1;
                    side1 = side2;
                    side2 = tempSide;
                }
            }

            List<Point2D> building1Positions = Placable(side1, Bot.Main.MapAnalyzer.StartArea, BuildingType.LookUp[types[0]].Size, false);
            List<Point2D> building2Positions = Placable(side2, Bot.Main.MapAnalyzer.StartArea, BuildingType.LookUp[types[2]].Size, false);

            DrawReaperWall(outside, startArea, reaperCliff, side1.GetOr(side2), building1Positions, building2Positions);

            float wallScore = 1000;
            foreach (Point2D p1 in building1Positions)
                foreach (Point2D p2 in building2Positions)
                {
                    if (System.Math.Abs(p1.X - p2.X) + 0.1f < (BuildingType.LookUp[types[0]].Size.X + BuildingType.LookUp[types[2]].Size.X) / 2f
                        && System.Math.Abs(p1.Y - p2.Y) + 0.1f < (BuildingType.LookUp[types[0]].Size.Y + BuildingType.LookUp[types[2]].Size.Y) / 2f)
                        continue;

                    float newScore = SC2Util.DistanceGrid(p1, p2);
                    if (newScore >= wallScore)
                        continue;

                    Wall[0].Pos = p1;
                    Wall[2].Pos = p2;
                    wallScore = newScore;
                }

            HashSet<Point2D> around1 = new HashSet<Point2D>();
            GetPlacableAround(Bot.Main.MapAnalyzer.StartArea, Wall[0].Pos, Wall[0].Size, Wall[1].Size, around1, true, false);
            HashSet<Point2D> around2 = new HashSet<Point2D>();
            GetPlacableAround(Bot.Main.MapAnalyzer.StartArea, Wall[2].Pos, Wall[2].Size, Wall[1].Size, around2, true, false);
            around1.IntersectWith(around2);

            foreach (Point2D pos in around1)
            {
                Wall[1].Pos = new Point2D() { X = pos.X, Y = pos.Y };
                break;
            }

            //DrawReaperWall(outside, startArea, reaperCliffs, cliffSides, null, null);
            return true;
        }

        private void FindWall(List<uint> types, List<Point2D> startPositions, List<Point2D> endPositions, BoolGrid placable, bool full)
        {
            foreach (Point2D start in startPositions)
                foreach (Point2D end in endPositions)
                {
                    if (Math.Abs(start.X - end.X) > 7
                        || Math.Abs(start.Y - end.Y) > 7)
                        continue;
                    for (int i = -1; i <= 1; i++)
                        if (CheckPlacement(types, start, end, placable, i, full))
                            return;
                }
            foreach (Point2D start in startPositions)
                foreach (Point2D end in endPositions)
                {
                    if (Math.Abs(start.X - end.X) > 7
                        || Math.Abs(start.Y - end.Y) > 7)
                        continue;
                    if (CheckPlacement(types, start, end, placable, -2, full))
                        return;
                    if (CheckPlacement(types, start, end, placable, 2, full))
                        return;
                }
        }

        private void FindWallZealotAtEnd(List<uint> types, List<Point2D> startPositions, List<Point2D> endPositions, BoolGrid placable)
        {
            foreach (Point2D start in startPositions)
                foreach (Point2D end in endPositions)
                {
                    if (Math.Abs(start.X - end.X) > 7
                        || Math.Abs(start.Y - end.Y) > 7)
                        continue;
                    for (int i = -1; i <= 1; i++)
                        if (CheckPlacementZealotAtEnd(types, start, end, placable, i))
                            return;
                }
            foreach (Point2D start in startPositions)
                foreach (Point2D end in endPositions)
                {
                    if (Math.Abs(start.X - end.X) > 7
                        || Math.Abs(start.Y - end.Y) > 7)
                        continue;
                    if (CheckPlacementZealotAtEnd(types, start, end, placable, -2))
                        return;
                    if (CheckPlacementZealotAtEnd(types, start, end, placable, 2))
                        return;
                }
        }

        private bool CheckPlacement(List<uint> types, Point2D start, Point2D end, BoolGrid placable, int i, bool full)
        {
            int spaceBetween = full ? 5 : 4;
            if (CheckPlacement(types, start, end, SC2Util.Point(end.X + i, end.Y + spaceBetween), placable, full))
                return true;
            if (CheckPlacement(types, start, end, SC2Util.Point(end.X + i, end.Y - spaceBetween), placable, full))
                return true;
            if (CheckPlacement(types, start, end, SC2Util.Point(end.X + spaceBetween, end.Y + i), placable, full))
                return true;
            if (CheckPlacement(types, start, end, SC2Util.Point(end.X - spaceBetween, end.Y + i), placable, full))
                return true;
            return false;
        }

        private bool CheckPlacementZealotAtEnd(List<uint> types, Point2D start, Point2D end, BoolGrid placable, int i)
        {
            int spaceBetween = 3;
            if (CheckPlacementZealotAtEnd(types, start, end, SC2Util.Point(end.X + i, end.Y + spaceBetween), placable))
                return true;
            if (CheckPlacementZealotAtEnd(types, start, end, SC2Util.Point(end.X + i, end.Y - spaceBetween), placable))
                return true;
            if (CheckPlacementZealotAtEnd(types, start, end, SC2Util.Point(end.X + spaceBetween, end.Y + i), placable))
                return true;
            if (CheckPlacementZealotAtEnd(types, start, end, SC2Util.Point(end.X - spaceBetween, end.Y + i), placable))
                return true;
            return false;
        }

        private bool CheckPlacement(List<uint> types, Point2D start, Point2D end, Point2D middle, BoolGrid placable, bool full)
        {
            if (full)
                return CheckPlacementFull(types, start, end, middle, placable);
            else
                return CheckPlacement(types, start, end, middle, placable);
        }

        private bool CheckPlacement(List<uint> types, Point2D start, Point2D end, Point2D middle, BoolGrid placable)
        {
            if (!CheckRect(placable, middle.X - 1, middle.Y - 1, middle.X + 1, middle.Y + 1))
                return false;

            if (Math.Abs(start.X - middle.X) == 3)
            {
                if (Math.Abs(start.Y - middle.Y) >= 3)
                    return false;
            }
            else if (Math.Abs(start.Y - middle.Y) == 3)
            {
                if (Math.Abs(start.X - middle.X) >= 3)
                    return false;
            }
            else
                return false;

            DebugUtil.WriteLine("end: " + end + " middle: " + middle);
            Point2D zealotPos = SC2Util.Point(end.X, end.Y);
            zealotPos = SC2Util.TowardCardinal(zealotPos, middle, 2);
            DebugUtil.WriteLine("zealotPos initial: " + zealotPos);

            if (Math.Abs(zealotPos.X - end.X) > Math.Abs(zealotPos.Y - end.Y) )
            {
                if (zealotPos.Y - Bot.Main.BaseManager.Natural.BaseLocation.Pos.Y > 0)
                {
                    zealotPos.Y -= 0.5f;
                    DebugUtil.WriteLine("zealotPos 1: " + zealotPos);
                }
                else
                {
                    zealotPos.Y += 0.5f;
                    DebugUtil.WriteLine("zealotPos 2: " + zealotPos);
                }
            } else
            {
                if (zealotPos.X - Bot.Main.BaseManager.Natural.BaseLocation.Pos.X > 0)
                {
                    zealotPos.X -= 0.5f;
                    DebugUtil.WriteLine("zealotPos 3: " + zealotPos);
                }
                else
                {
                    zealotPos.X += 0.5f;
                    DebugUtil.WriteLine("zealotPos 4: " + zealotPos);
                }
            }
            /*
            if (end.X - middle.X == 4)
                zealotPos.X -= 2;
            else if (end.X - middle.X == -4)
                zealotPos.X += 2;
            else if (end.Y - middle.Y == 4)
                zealotPos.Y -= 2;
            else
                zealotPos.Y += 2;
                */
            Point2D[] pylonPositions = new Point2D[2];
            Point2D natural = Bot.Main.BaseManager.Natural.BaseLocation.Pos;
            if (Math.Abs(natural.X - middle.X) >= Math.Abs(natural.Y - middle.Y))
            {
                if (natural.X > middle.X)
                {
                    pylonPositions[0] = SC2Util.Point(middle.X + 2.5f, middle.Y + 0.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X + 2.5f, middle.Y - 0.5f);
                }
                else
                {
                    pylonPositions[0] = SC2Util.Point(middle.X - 2.5f, middle.Y + 0.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X - 2.5f, middle.Y - 0.5f);
                }
            }
            else
            {
                if (natural.Y > middle.Y)
                {
                    pylonPositions[0] = SC2Util.Point(middle.X + 0.5f, middle.Y + 2.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X - 0.5f, middle.Y + 2.5f);
                }
                else
                {
                    pylonPositions[0] = SC2Util.Point(middle.X + 0.5f, middle.Y - 2.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X - 0.5f, middle.Y - 2.5f);
                }
            }

            Point2D pylonPos = null;
            foreach (Point2D pos in pylonPositions)
            {
                if (ProtossBuildingPlacement.IsBuildingInPowerField(start, SC2Util.Point(3, 3), pos)
                    && ProtossBuildingPlacement.IsBuildingInPowerField(end, SC2Util.Point(3, 3), pos)
                    && Bot.Main.buildingPlacer.CheckDistanceClose(pos, UnitTypes.PYLON, start, types[0])
                    && Bot.Main.buildingPlacer.CheckDistanceClose(pos, UnitTypes.PYLON, end, types[3]))
                    pylonPos = pos;
            }
            if (pylonPos == null)
                return false;

            Wall = new List<WallBuilding>();
            Wall.Add(new WallBuilding() { Pos = start, Type = types[0] });
            Wall.Add(new WallBuilding() { Pos = middle, Type = types[1] });
            Wall.Add(new WallBuilding() { Pos = zealotPos, Type = types[2] });
            Wall.Add(new WallBuilding() { Pos = end, Type = types[3] });
            Wall.Add(new WallBuilding() { Pos = pylonPos, Type = UnitTypes.PYLON });
            if (Bot.Main.Map == MapEnum.WinterGate && Bot.Main.MapAnalyzer.StartLocation.X < 60)
                Wall[4].Pos.X--;

            return true;
        }

        private bool CheckPlacementZealotAtEnd(List<uint> types, Point2D start, Point2D end, Point2D middle, BoolGrid placable)
        {
            FileUtil.Debug("Checking point: " + middle);
            if (!CheckRect(placable, middle.X - 1, middle.Y - 1, middle.X + 1, middle.Y + 1))
            {
                FileUtil.Debug("Position fails 1.");
                return false;
            }

            if (Math.Abs(start.X - middle.X) == 3)
            {
                if (Math.Abs(start.Y - middle.Y) >= 3)
                {
                    FileUtil.Debug("Position fails 2.");
                    return false;
                }
            }
            else if (Math.Abs(start.Y - middle.Y) == 3)
            {
                if (Math.Abs(start.X - middle.X) >= 3)
                {
                    FileUtil.Debug("Position fails 3.");
                    return false;
                }
            }
            else
            {
                FileUtil.Debug("Position fails 4.");
                return false;
            }

            Point2D zealotPos = SC2Util.Point(end.X, end.Y);
            zealotPos = SC2Util.FromCardinal(zealotPos, middle, 2);

            /*
            if (Math.Abs(zealotPos.X - end.X) > Math.Abs(zealotPos.Y - end.Y))
            {
                if (zealotPos.Y - Tyr.Bot.BaseManager.Natural.BaseLocation.Pos.Y > 0)
                    zealotPos.Y--;
                else zealotPos.Y++;
            }
            else
            {
                if (zealotPos.X - Tyr.Bot.BaseManager.Natural.BaseLocation.Pos.X > 0)
                    zealotPos.X--;
                else zealotPos.X++;
            }
            */

            Point2D[] pylonPositions = new Point2D[2];
            Point2D natural = Bot.Main.BaseManager.Natural.BaseLocation.Pos;
            if (Math.Abs(natural.X - middle.X) >= Math.Abs(natural.Y - middle.Y))
            {
                if (natural.X > middle.X)
                {
                    pylonPositions[0] = SC2Util.Point(middle.X + 2.5f, middle.Y + 0.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X + 2.5f, middle.Y - 0.5f);
                }
                else
                {
                    pylonPositions[0] = SC2Util.Point(middle.X - 2.5f, middle.Y + 0.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X - 2.5f, middle.Y - 0.5f);
                }
            }
            else
            {
                if (natural.Y > middle.Y)
                {
                    pylonPositions[0] = SC2Util.Point(middle.X + 0.5f, middle.Y + 2.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X - 0.5f, middle.Y + 2.5f);
                }
                else
                {
                    pylonPositions[0] = SC2Util.Point(middle.X + 0.5f, middle.Y - 2.5f);
                    pylonPositions[1] = SC2Util.Point(middle.X - 0.5f, middle.Y - 2.5f);
                }
            }

            Point2D pylonPos = null;
            foreach (Point2D pos in pylonPositions)
            {
                if (ProtossBuildingPlacement.IsBuildingInPowerField(start, SC2Util.Point(3, 3), pos)
                    && ProtossBuildingPlacement.IsBuildingInPowerField(end, SC2Util.Point(3, 3), pos)
                    && Bot.Main.buildingPlacer.CheckDistanceClose(pos, UnitTypes.PYLON, start, types[0])
                    && Bot.Main.buildingPlacer.CheckDistanceClose(pos, UnitTypes.PYLON, end, types[3]))
                    pylonPos = pos;
            }
            if (pylonPos == null)
            {
                FileUtil.Debug("Position fails 5.");
                return false;
            }

            Wall = new List<WallBuilding>();
            Wall.Add(new WallBuilding() { Pos = start, Type = types[0] });
            Wall.Add(new WallBuilding() { Pos = middle, Type = types[1] });
            Wall.Add(new WallBuilding() { Pos = zealotPos, Type = types[2] });
            Wall.Add(new WallBuilding() { Pos = end, Type = types[3] });
            Wall.Add(new WallBuilding() { Pos = pylonPos, Type = UnitTypes.PYLON });
            if (Bot.Main.Map == MapEnum.WinterGate && Bot.Main.MapAnalyzer.StartLocation.X < 60)
                Wall[4].Pos.X--;

            return true;
        }

        private bool CheckPlacementFull(List<uint> types, Point2D start, Point2D end, Point2D middle, BoolGrid placable)
        {
            if (!CheckRect(placable, middle.X - 1, middle.Y - 1, middle.X + 1, middle.Y + 1))
                return false;

            if (Math.Abs(start.X - middle.X) == 3)
            {
                if (Math.Abs(start.Y - middle.Y) >= 3)
                    return false;
            }
            else if (Math.Abs(start.Y - middle.Y) == 3)
            {
                if (Math.Abs(start.X - middle.X) >= 3)
                    return false;
            }
            else return false;

            Point2D pylonPos = SC2Util.Point(end.X, end.Y);
            pylonPos = SC2Util.TowardCardinal(pylonPos, middle, 2.5f);

            Wall = new List<WallBuilding>();
            Wall.Add(new WallBuilding() { Pos = start, Type = types[0] });
            Wall.Add(new WallBuilding() { Pos = middle, Type = types[1] });
            Wall.Add(new WallBuilding() { Pos = pylonPos, Type = types[2] });
            Wall.Add(new WallBuilding() { Pos = end, Type = types[3] });

            return true;
        }

        public void ReserveSpace()
        {
            foreach (WallBuilding building in Wall)
                Bot.Main.buildingPlacer.ReservedLocation.Add(new ReservedBuilding() { Type = building.Type, Pos = building.Pos });
        }

        private void DrawResult(BoolGrid unPathable, BoolGrid naturalWalls, List<Point2D> building1Positions, List<Point2D> building2Positions)
        {
            if (!Bot.Debug)
                return;

            int width = Bot.Main.GameInfo.StartRaw.MapSize.X;
            int height = Bot.Main.GameInfo.StartRaw.MapSize.Y;
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (naturalWalls != null && naturalWalls[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Red);
                    else if (unPathable[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
                    else if (!Bot.Main.MapAnalyzer.Placement[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Gray);
                    else
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.White);
                }


            //DrawBuildings(bmp);
            if (Wall.Count >= 5)
            {
                for (float x = Wall[0].Pos.X - BuildingType.LookUp[Wall[0].Type].Size.X / 2f + 0.5f; x < Wall[0].Pos.X + BuildingType.LookUp[Wall[0].Type].Size.X / 2f - 0.5f + 0.1f; x++)
                    for (float y = Wall[0].Pos.Y - BuildingType.LookUp[Wall[0].Type].Size.Y / 2f + 0.5f; y < Wall[0].Pos.Y + BuildingType.LookUp[Wall[0].Type].Size.Y / 2f - 0.5f + 0.1f; y++)
                        bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Green);
                for (float x = Wall[3].Pos.X - BuildingType.LookUp[Wall[3].Type].Size.X / 2f + 0.5f; x < Wall[3].Pos.X + BuildingType.LookUp[Wall[3].Type].Size.X / 2f + 0.1f - 0.5f; x++)
                    for (float y = Wall[3].Pos.Y - BuildingType.LookUp[Wall[3].Type].Size.Y / 2f + 0.5f; y < Wall[3].Pos.Y + BuildingType.LookUp[Wall[3].Type].Size.Y / 2f + 0.1f - 0.5f; y++)
                        bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Yellow);
                for (float x = Wall[1].Pos.X - BuildingType.LookUp[Wall[1].Type].Size.X / 2f + 0.5f; x < Wall[1].Pos.X + BuildingType.LookUp[Wall[1].Type].Size.X / 2f + 0.1f - 0.5f; x++)
                    for (float y = Wall[1].Pos.Y - BuildingType.LookUp[Wall[1].Type].Size.Y / 2f + 0.5f; y < Wall[1].Pos.Y + BuildingType.LookUp[Wall[1].Type].Size.Y / 2f + 0.1f - 0.5f; y++)
                        bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Purple);

                for (float x = Wall[4].Pos.X - BuildingType.LookUp[Wall[4].Type].Size.X / 2f + 0.5f; x < Wall[4].Pos.X + BuildingType.LookUp[Wall[4].Type].Size.X / 2f + 0.1f - 0.5f; x++)
                    for (float y = Wall[4].Pos.Y - BuildingType.LookUp[Wall[4].Type].Size.Y / 2f + 0.5f; y < Wall[4].Pos.Y + BuildingType.LookUp[Wall[4].Type].Size.Y / 2f + 0.1f - 0.5f; y++)
                        bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Cyan);
                bmp.SetPixel((int)Wall[2].Pos.X, height - 1 - (int)Wall[2].Pos.Y, System.Drawing.Color.Blue);
            }

            foreach (Point2D pos in building1Positions)
                bmp.SetPixel((int)pos.X, height - 1 - (int)pos.Y, System.Drawing.Color.Blue);
            foreach (Point2D pos in building2Positions)
                bmp.SetPixel((int)pos.X, height - 1 - (int)pos.Y, System.Drawing.Color.Green);

            string dataFolder = Directory.GetCurrentDirectory() + "/data/Tyr/";
            bmp.Save(dataFolder + "NaturalWall.png");
        }

        private void DrawBuildings(System.Drawing.Bitmap bmp)
        {
            int i = 0;
            System.Drawing.Color[] colors = new System.Drawing.Color[] { System.Drawing.Color.Blue, System.Drawing.Color.Green, System.Drawing.Color.Yellow, System.Drawing.Color.Cyan, System.Drawing.Color.Magenta };
            foreach (WallBuilding building in Wall)
            {
                DrawBuilding(bmp, building, colors[i % colors.Length]);
                i++;
            }
        }

        private void DrawBuilding(System.Drawing.Bitmap bmp, WallBuilding building, System.Drawing.Color color)
        {
            if (building.Pos == null)
                return;
            if (!BuildingType.LookUp.ContainsKey(building.Type))
            {
                bmp.SetPixel((int)building.Pos.X, bmp.Height - 1 - (int)building.Pos.Y, color);
                return;
            }
            BuildingType type = BuildingType.LookUp[building.Type];
            for (float x = building.Pos.X - type.Size.X / 2f + 0.5f; x < building.Pos.X + type.Size.X / 2f - 0.5f + 0.1f; x++)
                for (float y = building.Pos.Y - type.Size.Y / 2f + 0.5f; y < building.Pos.Y + type.Size.Y / 2f - 0.5f + 0.1f; y++)
                    bmp.SetPixel((int)x, bmp.Height - 1 - (int)y, color);

        }
        private void DrawReaperWall(BoolGrid outside, BoolGrid startArea, BoolGrid reaperCliff, BoolGrid cliffSides, List<Point2D> building1Positions, List<Point2D> building2Positions)
        {
            if (!Bot.Debug)
                return;

            int width = Bot.Main.GameInfo.StartRaw.MapSize.X;
            int height = Bot.Main.GameInfo.StartRaw.MapSize.Y;
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    int mapHeight = Bot.Main.MapAnalyzer.MapHeight(x, y);
                    int val = Math.Min(255, Math.Max(0, (mapHeight - 191) * 8));
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.FromArgb(val, val, val));
                    
                }

            if (building1Positions != null)
                foreach (Point2D point in building1Positions)
                    bmp.SetPixel((int)point.X, height - 1 - (int)point.Y, System.Drawing.Color.Yellow);
            if (building2Positions != null)
                foreach (Point2D point in building2Positions)
                    bmp.SetPixel((int)point.X, height - 1 - (int)point.Y, System.Drawing.Color.Yellow);

            DrawBuildings(bmp);

            string dataFolder = Directory.GetCurrentDirectory() + "/data/Tyr/";
            bmp.Save(dataFolder + "ReaperWall.png");
        }

        private List<Point2D> Placable(BoolGrid around, BoolGrid startArea, Point2D size, bool allowCorners)
        {
            return Placable(around, startArea, size, allowCorners, false);
        }

        private List<Point2D> Placable(BoolGrid around, BoolGrid startArea, Point2D size, bool allowCorners, bool oneSpace)
        {
            Point2D size1x1 = new Point2D() { X = 1, Y = 1 };
            List<Point2D> result = new List<Point2D>();

            for (int x = 0; x < around.Width(); x++)
                for (int y = 0; y < around.Height(); y++)
                    if (around[x, y])
                        GetPlacableAround(startArea, new Point2D() { X = x + 0.5f, Y = y + 0.5f }, size1x1, size, result, allowCorners, oneSpace);

            return result;
        }

        private void GetPlacableAround(BoolGrid startArea, Point2D pos, Point2D size1, Point2D size2, ICollection<Point2D> result, bool allowCorners, bool oneSpace)
        {
            float xOffset = (size1.X + size2.X) / 2f;
            float yOffset = (size1.Y + size2.Y) / 2f;
            for (float i = -xOffset + (allowCorners ? 0 : 1); i < 0.1f + xOffset - (allowCorners ? 0 : 1); i++)
            {
                Point2D checkPos = new Point2D() { X = pos.X + i, Y = pos.Y - (oneSpace ? yOffset + 1 : yOffset) };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
                checkPos = new Point2D() { X = pos.X + i, Y = pos.Y + (oneSpace ? yOffset + 1 : yOffset) };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
            }
            for (float i = -yOffset + (allowCorners ? 0 : 1); i < 0.1f + yOffset - (allowCorners ? 0 : 1); i++)
            {
                Point2D checkPos = new Point2D() { X = pos.X + (oneSpace ? xOffset + 1 : xOffset), Y = pos.Y + i };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
                checkPos = new Point2D() { X = pos.X - (oneSpace ? xOffset + 1 : xOffset), Y = pos.Y + i };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
            }
        }

        private bool CheckRect(BoolGrid grid, Point2D pos, Point2D size)
        {
            return CheckRect(grid, 
                pos.X - size.X / 2f + 0.5f, 
                pos.Y - size.Y / 2f + 0.5f, 
                pos.X + size.X / 2f - 0.5f, 
                pos.Y + size.Y / 2f - 0.5f);
        }

        private bool CheckRect(BoolGrid grid, float minX, float minY, float maxX, float maxY)
        {
            for (float x = minX; x < maxX + 0.1f; x++)
                for (float y = minY; y < maxY + 0.1f; y++)
                    if (!grid[(int)x, (int)y])
                        return false;
            return true;
        }
    }
}
