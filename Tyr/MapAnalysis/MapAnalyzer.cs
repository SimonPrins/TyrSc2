using System;
using System.Collections.Generic;
using System.Diagnostics;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.MapAnalysis
{
    public class MapAnalyzer
    {
        public List<BaseLocation> BaseLocations { get; private set; } = new List<BaseLocation>();
        public Point StartLocation { get; private set; }
        public BoolGrid Placement;
        public BoolGrid StartArea;
        public BoolGrid MainAndPocketArea;
        private int[,] enemyDistances;
        public int[,] WallDistances;

        // Positions for wallin, needs better place.
        public Point2D building1 = null;
        public Point2D building2 = null;
        public Point2D building3 = null;

        public void Analyze(Tyr tyr)
        {
            // Determine the start location.
            foreach (Unit unit in tyr.Observation.Observation.RawData.Units)
                if (unit.Owner == tyr.PlayerId && UnitTypes.ResourceCenters.Contains(unit.UnitType))
                    StartLocation = unit.Pos;

            List<MineralField> mineralFields = new List<MineralField>();

            foreach (Unit mineralField in tyr.Observation.Observation.RawData.Units)
                if (UnitTypes.MineralFields.Contains(mineralField.UnitType))
                    mineralFields.Add(new MineralField() { Pos = mineralField.Pos, Tag = mineralField.Tag });

            Dictionary<ulong, int> mineralSetIds = new Dictionary<ulong, int>();
            List<List<MineralField>> mineralSets = new List<List<MineralField>>();
            int currentSet = 0;
            foreach (MineralField mineralField in mineralFields)
            {
                if (mineralSetIds.ContainsKey(mineralField.Tag))
                    continue;
                BaseLocation baseLocation = new BaseLocation();
                BaseLocations.Add(baseLocation);
                mineralSetIds.Add(mineralField.Tag, currentSet);
                baseLocation.MineralFields.Add(mineralField);

                for (int i = 0; i < baseLocation.MineralFields.Count; i++)
                {
                    MineralField mineralFieldA = baseLocation.MineralFields[i];
                    foreach (MineralField closeMineralField in mineralFields)
                    {
                        if (mineralSetIds.ContainsKey(closeMineralField.Tag))
                            continue;

                        if (SC2Util.DistanceGrid(mineralFieldA.Pos, closeMineralField.Pos) <= 5)
                        {
                            mineralSetIds.Add(closeMineralField.Tag, currentSet);
                            baseLocation.MineralFields.Add(closeMineralField);
                        }
                    }
                }
                currentSet++;
            }

            List<Gas> gasses = new List<Gas>();
            foreach (Unit unit in tyr.Observation.Observation.RawData.Units)
                if (UnitTypes.GasGeysers.Contains(unit.UnitType))
                    gasses.Add(new Gas() { Pos = unit.Pos, Tag = unit.Tag });
            
            foreach (BaseLocation loc in BaseLocations)
            {
                float x = 0;
                float y = 0;
                foreach (MineralField field in loc.MineralFields)
                {
                    x += (int)field.Pos.X;
                    y += (int)field.Pos.Y;
                }
                x /= loc.MineralFields.Count;
                y /= loc.MineralFields.Count;

                // Round to nearest half position. Nexii are 5x5 and therefore always centered in the middle of a tile.
                x = (int)(x) + 0.5f;
                y = (int)(y) + 0.5f;

                // Temporary position, we still need a proper position.
                loc.Pos = SC2Util.Point(x, y);

                MineralField closest = null;
                float distance = 10000;
                foreach (MineralField field in loc.MineralFields)
                    if (SC2Util.DistanceGrid(field.Pos, loc.Pos) < distance)
                    {
                        distance = SC2Util.DistanceGrid(field.Pos, loc.Pos);
                        closest = field;
                    }

                // Move the estimated base position slightly away from the closest mineral.
                // This ensures that the base location will not end up on the far side of the minerals.
                if (closest.Pos.X < loc.Pos.X)
                    loc.Pos.X += 2;
                else if (closest.Pos.X > loc.Pos.X)
                    loc.Pos.X -= 2;
                if (closest.Pos.Y < loc.Pos.Y)
                    loc.Pos.Y += 2;
                else if (closest.Pos.Y > loc.Pos.Y)
                    loc.Pos.Y -= 2;

                for (int i = 0; i < gasses.Count; i++)
                {
                    if (SC2Util.DistanceGrid(loc.Pos, gasses[i].Pos) <= 24)
                    {
                        loc.Gasses.Add(gasses[i]);
                        gasses[i] = gasses[gasses.Count - 1];
                        gasses.RemoveAt(gasses.Count - 1);
                        i--;
                    }
                }
                
                float closestDist = 1000000;
                for (int i = 0; i < 20; i++)
                {
                    for (int j = 0; j == 0 || j < i; j++)
                    {
                        float maxDist;
                        Point2D newPos;
                        newPos = SC2Util.Point(loc.Pos.X + i - j, loc.Pos.Y + j);
                        maxDist = checkPosition(newPos, loc);
                        if (maxDist < closestDist)
                        {
                            loc.Pos = newPos;
                            closestDist = maxDist;
                        }

                        newPos = SC2Util.Point(loc.Pos.X + i - j, loc.Pos.Y - j);
                        maxDist = checkPosition(newPos, loc);
                        if (maxDist < closestDist)
                        {
                            loc.Pos = newPos;
                            closestDist = maxDist;
                        }

                        newPos = SC2Util.Point(loc.Pos.X - i + j, loc.Pos.Y + j);
                        maxDist = checkPosition(newPos, loc);
                        if (maxDist < closestDist)
                        {
                            loc.Pos = newPos;
                            closestDist = maxDist;
                        }

                        newPos = SC2Util.Point(loc.Pos.X - i + j, loc.Pos.Y - j);
                        maxDist = checkPosition(newPos, loc);
                        if (maxDist < closestDist)
                        {
                            loc.Pos = newPos;
                            closestDist = maxDist;
                        }
                    }
                }

                if (closestDist >= 999999)
                    System.Console.WriteLine("Unable to find proper base placement: " + loc.Pos);
            }
            
            if (tyr.GameInfo.MapName.Contains("Blueshift"))
            {
                foreach (BaseLocation loc in BaseLocations)
                {
                    if (SC2Util.DistanceSq(loc.Pos, SC2Util.Point(141.5f, 112.5f)) <= 5 * 5 && (loc.Pos.X != 141.5 || loc.Pos.Y != 112.5))
                    {
                        System.Console.WriteLine("Incorrect base location, fixing: " + loc.Pos);
                        loc.Pos = SC2Util.Point(141.5f, 112.5f);
                    } else if (SC2Util.DistanceSq(loc.Pos, SC2Util.Point(34.5f, 63.5f)) <= 5 * 5 && (loc.Pos.X != 34.5 || loc.Pos.Y != 63.5))
                    {
                        System.Console.WriteLine("Incorrect base location, fixing: " + loc.Pos);
                        loc.Pos = SC2Util.Point(34.5f, 63.5f);
                    }
                }

            }

            Stopwatch stopWatch = Stopwatch.StartNew();

            int width = Tyr.Bot.GameInfo.StartRaw.MapSize.X;
            int height = Tyr.Bot.GameInfo.StartRaw.MapSize.Y;

            Placement = new ImageBoolGrid(tyr.GameInfo.StartRaw.PlacementGrid);
            StartArea = Placement.GetConnected(SC2Util.To2D(StartLocation));

            ArrayBoolGrid startLocations = new ArrayBoolGrid(Placement.Width(), Placement.Height());
            foreach (Point2D startLoc in Tyr.Bot.GameInfo.StartRaw.StartLocations)
                for (int x = -2; x <= 2; x++)
                    for (int y = -2; y <= 2; y++)
                        startLocations[(int)startLoc.X + x, (int)startLoc.Y + y] = true;
            for (int x = -2; x <= 2; x++)
                for (int y = -2; y <= 2; y++)
                    startLocations[(int)StartLocation.X + x, (int)StartLocation.Y + y] = true;

            BoolGrid unPathable = new ImageBoolGrid(Tyr.Bot.GameInfo.StartRaw.PathingGrid).GetAnd(startLocations.Invert());
            BoolGrid pathable = unPathable.Invert();

            BoolGrid chokes = Placement.Invert().GetAnd(pathable);
            BoolGrid mainExits = chokes.GetAdjacent(StartArea);

            enemyDistances = EnemyDistances;
            int dist = 1000;
            Point2D mainRamp = null;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (mainExits[x, y])
                    {
                        int newDist = enemyDistances[x, y];
                        if (newDist < dist)
                        {
                            dist = newDist;
                            mainRamp = SC2Util.Point(x, y);
                        }
                    }
                }

            BoolGrid ramp = chokes.GetConnected(mainRamp);

            BoolGrid pathingWithoutRamp = pathable.GetAnd(ramp.Invert());
            MainAndPocketArea = pathingWithoutRamp.GetConnected(SC2Util.To2D(StartLocation));

            if (Tyr.Bot.MyRace == Race.Protoss)
                DetermineWall(ramp, unPathable);

            WallDistances = Distances(unPathable);

            stopWatch.Stop();
            System.Console.WriteLine("Total time to find wall: " + stopWatch.ElapsedMilliseconds);


            /*
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (WallDistances[x, y] == 0)
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Red);
                    else if (WallDistances[x, y] >= 25)
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Green);
                    else
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.FromArgb(WallDistances[x, y] * 10, WallDistances[x, y] * 10, WallDistances[x, y] * 10));
                }
            foreach (BaseLocation loc in BaseLocations)
                for (int dx = -2; dx <= 2; dx++)
                    for (int dy = -2; dy <= 2; dy++)
                        bmp.SetPixel((int)loc.Pos.X + dx, height - 1 - (int)loc.Pos.Y - dy, System.Drawing.Color.Blue);

            foreach (Unit unit in tyr.Observation.Observation.RawData.Units)
            {
                if (UnitTypes.GasGeysers.Contains(unit.UnitType))
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            bmp.SetPixel((int)unit.Pos.X + dx, height - 1 - (int)unit.Pos.Y - dy, System.Drawing.Color.Cyan);
                if (UnitTypes.MineralFields.Contains(unit.UnitType))
                    for (int dx = 0; dx <= 1; dx++)
                        bmp.SetPixel((int)(unit.Pos.X - 0.5f) + dx, height - 1 - (int)(unit.Pos.Y - 0.5f), System.Drawing.Color.Cyan);
            }
            bmp.Save(@"C:\Users\Simon\Desktop\WallDistances.png");
            */

        }

        public int MapHeight(int x, int y)
        {
            return SC2Util.GetDataValue(Tyr.Bot.GameInfo.StartRaw.TerrainHeight, x, y);
        }

        private void DetermineWall(BoolGrid ramp, BoolGrid unPathable)
        {
            BoolGrid rampAdjacent = unPathable.GetAdjacent(ramp);
            BoolGrid rampSides = unPathable.GetConnected(rampAdjacent, 5);
            List<BoolGrid> sides = rampSides.GetGroups();

            BoolGrid shrunkenStart = StartArea.Shrink();

            List<Point2D> building1Positions = Placable(sides[0], shrunkenStart).ToList();
            List<Point2D> building2Positions = Placable(sides[1], shrunkenStart).ToList();

            float wallScore = 1000;


            foreach (Point2D p1 in building1Positions)
                foreach (Point2D p2 in building2Positions)
                {
                    if (System.Math.Abs(p1.X - p2.X) < 3 && System.Math.Abs(p1.Y - p2.Y) < 3)
                        continue;

                    float newScore = SC2Util.DistanceGrid(p1, p2);
                    if (newScore >= wallScore)
                        continue;

                    for (float i = -2.5f; i < 3; i++)
                    {
                        if (CheckPylon(SC2Util.Point(p1.X + 2.5f, p1.Y + i), p1, p2))
                        {
                            wallScore = newScore;
                            building1 = p1;
                            building2 = p2;
                            building3 = SC2Util.Point(p1.X + 2.5f, p1.Y + i);
                        }
                        if (CheckPylon(SC2Util.Point(p1.X - 2.5f, p1.Y + i), p1, p2))
                        {
                            wallScore = newScore;
                            building1 = p1;
                            building2 = p2;
                            building3 = SC2Util.Point(p1.X - 2.5f, p1.Y + i);
                        }
                        if (CheckPylon(SC2Util.Point(p1.X + i, p1.Y + 2.5f), p1, p2))
                        {
                            wallScore = newScore;
                            building1 = p1;
                            building2 = p2;
                            building3 = SC2Util.Point(p1.X + i, p1.Y + 2.5f);
                        }
                        if (CheckPylon(SC2Util.Point(p1.X + i, p1.Y - 2.5f), p1, p2))
                        {
                            wallScore = newScore;
                            building1 = p1;
                            building2 = p2;
                            building3 = SC2Util.Point(p1.X + i, p1.Y - 2.5f);
                        }
                    }
                }

        }

        private bool CheckPylon(Point2D pylon, Point2D p1, Point2D p2)
        {
            if (!StartArea[SC2Util.Point(pylon.X, pylon.Y)])
                return false;
            if (!StartArea[SC2Util.Point(pylon.X + 0.6f, pylon.Y)])
                return false;
            if (!StartArea[SC2Util.Point(pylon.X, pylon.Y + 0.6f)])
                return false;
            if (!StartArea[SC2Util.Point(pylon.X + 0.6f, pylon.Y + 0.6f)])
                return false;

            float dist = System.Math.Max(System.Math.Abs(pylon.X - p2.X), Math.Abs(pylon.Y - p2.Y));
            return dist > 2.4 && dist < 2.6;
        }

        private BoolGrid Placable(BoolGrid around, BoolGrid shrunkenStart)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(around.Width(), around.Height());
            for (int x = 0; x < around.Width(); x++)
                for (int y = 0; y < around.Height(); y++)
                {
                    if (around[x, y])
                    {
                        for (int i = -2; i <= 2; i++)
                        {
                            if (shrunkenStart[x + i, y - 2])
                                result[x + i, y - 2] = true;
                            if (shrunkenStart[x + i, y + 2])
                                result[x + i, y + 2] = true;
                            if (shrunkenStart[x + 2, y + i])
                                result[x + 2, y + i] = true;
                            if (shrunkenStart[x - 2, y + i])
                                result[x - 2, y + i] = true;
                        }
                    }
                }
            return result;
        }
        
        private float checkPosition(Point2D pos, BaseLocation loc)
        {
            //System.Console.WriteLine("Checking: " + pos);
            foreach (MineralField mineralField in loc.MineralFields)
                if (SC2Util.DistanceGrid(mineralField.Pos, pos) <= 10
                    && System.Math.Abs(mineralField.Pos.X - pos.X) <= 5.5
                    && System.Math.Abs(mineralField.Pos.Y - pos.Y) <= 5.5)
                {
                    return 100000000;
                }
            foreach (Gas gas in loc.Gasses)
                if (SC2Util.DistanceGrid(gas.Pos, pos) <= 11
                    && System.Math.Abs(gas.Pos.X - pos.X) <= 6.1
                    && System.Math.Abs(gas.Pos.Y - pos.Y) <= 6.1)
                {
                    return 100000000;
                }

            // Check if a resource center can actually be built here.
            for (float x = -2.5f; x < 2.5f + 0.1f; x++)
                for (float y = -2.5f; y < 2.5f + 0.1f; y++)
                    if (!SC2Util.GetTilePlacable((int)System.Math.Round(pos.X + x), (int)System.Math.Round(pos.Y + y)))
                        return 100000000;
            
            float maxDist = 0;
            foreach (MineralField mineralField in loc.MineralFields)
                maxDist += SC2Util.DistanceSq(mineralField.Pos, pos);

            foreach (Gas gas in loc.Gasses)
                maxDist += SC2Util.DistanceSq(gas.Pos, pos);
            return maxDist;
        }

        public Point2D CrossSpawn()
        {
            int dist = 0;
            Point2D crossSpawn = null;
            foreach (Point2D enemy in Tyr.Bot.GameInfo.StartRaw.StartLocations)
            {
                int enemyDist = (int)SC2Util.DistanceSq(enemy, StartLocation);
                if (enemyDist > dist)
                {
                    crossSpawn = enemy;
                    dist = enemyDist;
                }
            }
            return crossSpawn;
        }

        public int[,] EnemyDistances { get
            {
                if (enemyDistances == null)
                    enemyDistances = Distances(CrossSpawn());
                return enemyDistances;
            }
        }

        public int[,] Distances(Point2D pos)
        {
            int width = Tyr.Bot.GameInfo.StartRaw.MapSize.X;
            int height = Tyr.Bot.GameInfo.StartRaw.MapSize.Y;
            ImageData pathingData = Tyr.Bot.GameInfo.StartRaw.PathingGrid;
            int[,] distances = new int[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    distances[x, y] = 1000000000;
            distances[(int)pos.X, (int)pos.Y] = 0;

            Queue<Point2D> q = new Queue<Point2D>();
            q.Enqueue(pos);

            while (q.Count > 0)
            {
                Point2D cur = q.Dequeue();
                check(pathingData, distances, q, SC2Util.Point(cur.X + 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(pathingData, distances, q, SC2Util.Point(cur.X - 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(pathingData, distances, q, SC2Util.Point(cur.X, cur.Y + 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(pathingData, distances, q, SC2Util.Point(cur.X, cur.Y - 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
            }

            return distances;
        }

        public int[,] Distances(BoolGrid start)
        {
            int width = Tyr.Bot.GameInfo.StartRaw.MapSize.X;
            int height = Tyr.Bot.GameInfo.StartRaw.MapSize.Y;
            ImageData pathingData = Tyr.Bot.GameInfo.StartRaw.PathingGrid;
            int[,] distances = new int[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    distances[x, y] = 1000000000;


            Queue<Point2D> q = new Queue<Point2D>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (start[x, y])
                    {
                        distances[x, y] = 0;
                        q.Enqueue(SC2Util.Point(x, y));
                    }
                }

            while (q.Count > 0)
            {
                Point2D cur = q.Dequeue();
                check(pathingData, distances, q, SC2Util.Point(cur.X + 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(pathingData, distances, q, SC2Util.Point(cur.X - 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(pathingData, distances, q, SC2Util.Point(cur.X, cur.Y + 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
                check(pathingData, distances, q, SC2Util.Point(cur.X, cur.Y - 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
            }

            return distances;
        }

        private void check(ImageData pathingData, int[,] distances, Queue<Point2D> q, Point2D pos, int width, int height, int newVal)
        {
            if (check(pathingData, pos, width, height) && distances[(int)pos.X, (int)pos.Y] == 1000000000)
            {
                q.Enqueue(pos);
                distances[(int)pos.X, (int)pos.Y] = newVal;
            }
        }

        private bool check(ImageData pathingData, Point2D pos, int width, int height)
        {
            if (pos.X < 0 || pos.X >= width || pos.Y < 0 || pos.Y >= height)
                return false;
            if (SC2Util.GetDataValue(pathingData, (int)pos.X, (int)pos.Y) == 0)
                return true;

            foreach (Point2D p in Tyr.Bot.GameInfo.StartRaw.StartLocations)
                if (SC2Util.DistanceGrid(pos, p) <= 3)
                    return true;
            if (SC2Util.DistanceGrid(pos, StartLocation) <= 3)
                return true;
            return false;
        }

        public Point2D Walk(Point2D start, int[,] distances, int steps)
        {
            Point2D cur = start;
            int dx = 0;
            int dy = 0;
            for (int i = 0; i <= steps; i++)
            {
                List<Point2D> newDirections = new List<Point2D>();
                newDirections.Add(SC2Util.Point(cur.X + 1, cur.Y));
                newDirections.Add(SC2Util.Point(cur.X - 1, cur.Y));
                newDirections.Add(SC2Util.Point(cur.X, cur.Y + 1));
                newDirections.Add(SC2Util.Point(cur.X, cur.Y - 1));

                for (int j = newDirections.Count - 1; j >= 0; j--)
                {
                    Point2D next = newDirections[j];
                    if (distances[(int)cur.X, (int)cur.Y] <= distances[(int)next.X, (int)next.Y])
                        newDirections.RemoveAt(j);
                }

                if (newDirections.Count == 0)
                    break;

                Point2D goTo;
                if (newDirections.Count == 1 || newDirections[0].X - cur.X != dx || newDirections[0].Y - cur.Y != dy)
                    goTo = newDirections[0];
                else
                    goTo = newDirections[1];

                dx = (int)(goTo.X - cur.X);
                dy = (int)(goTo.Y - cur.Y);
                cur = goTo;

                if (distances[(int)cur.X, (int)cur.Y] == 0)
                    break;
            }
            return cur;
        }
    }
}
