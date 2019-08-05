using SC2APIProtocol;
using System;
using System.Collections.Generic;
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

            
            BoolGrid pathable = Tyr.Bot.MapAnalyzer.Pathable;
            BoolGrid unPathable = pathable.Invert();
            BoolGrid ramp = Tyr.Bot.MapAnalyzer.Ramp;

            BoolGrid rampAdjacent = unPathable.GetAdjacent(ramp);
            BoolGrid rampSides = unPathable.GetConnected(rampAdjacent, 5);
            List<BoolGrid> sides = rampSides.GetGroups();

            List<Point2D> building1Positions = Placable(sides[0], Tyr.Bot.MapAnalyzer.StartArea, BuildingType.LookUp[types[0]].Size, true);
            List<Point2D> building2Positions = Placable(sides[1], Tyr.Bot.MapAnalyzer.StartArea, BuildingType.LookUp[types[2]].Size, true);

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
            GetPlacableAround(Tyr.Bot.MapAnalyzer.StartArea, Wall[0].Pos, Wall[0].Size, Wall[1].Size, around1, true);
            HashSet<Point2D> around2 = new HashSet<Point2D>();
            GetPlacableAround(Tyr.Bot.MapAnalyzer.StartArea, Wall[2].Pos, Wall[2].Size, Wall[1].Size, around2, true);
            around1.IntersectWith(around2);

            foreach (Point2D pos in around1)
            {
                Wall[1].Pos = new Point2D() { X = pos.X + 0.5f, Y = pos.Y + 0.5f };
                break;
            }
            //DrawResult(unPathable, null, building1Positions, building2Positions);
        }

        public void CreateNatural(List<uint> types)
        {
            BoolGrid pathable = Tyr.Bot.MapAnalyzer.Pathable;
            BoolGrid unPathable = pathable.Invert();
            Point2D naturalExit = Tyr.Bot.BaseManager.NaturalDefensePos;
            BoolGrid naturalWalls = unPathable.GetConnected(new Point2D() { X = 0, Y = 0 }).Crop((int)naturalExit.X - 12, (int)naturalExit.Y - 12, (int)naturalExit.X + 12, (int)naturalExit.Y + 12);
            List<BoolGrid> sides = naturalWalls.GetGroups();
            Dictionary<BoolGrid, int> counts = new Dictionary<BoolGrid, int>();
            foreach (BoolGrid side in sides)
                counts.Add(side, side.Count());
            sides.Sort((BoolGrid a, BoolGrid b) => counts[b] - counts[a]);

            List<Point2D> building1Positions = Placable(sides[0], Tyr.Bot.MapAnalyzer.Placement, BuildingType.LookUp[types[0]].Size, false);
            List<Point2D> building2Positions = Placable(sides[1], Tyr.Bot.MapAnalyzer.Placement, BuildingType.LookUp[types[3]].Size, false);
            int naturalHeight = Tyr.Bot.MapAnalyzer.MapHeight((int)Tyr.Bot.BaseManager.Natural.BaseLocation.Pos.X, (int)Tyr.Bot.BaseManager.Natural.BaseLocation.Pos.Y);
            building1Positions = building1Positions.FindAll((p) => Tyr.Bot.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);
            building2Positions = building2Positions.FindAll((p) => Tyr.Bot.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);


            FindWall(types, building1Positions, building2Positions, Tyr.Bot.MapAnalyzer.Placement, false);
            //DrawResult(unPathable, naturalWalls, building1Positions, building2Positions);
        }

        public void CreateFullNatural(List<uint> types)
        {
            BoolGrid pathable = Tyr.Bot.MapAnalyzer.Pathable;
            BoolGrid unPathable = pathable.Invert();
            Point2D naturalExit = Tyr.Bot.BaseManager.NaturalDefensePos;
            BoolGrid naturalWalls = unPathable.GetConnected(new Point2D() { X = 0, Y = 0 }).Crop((int)naturalExit.X - 12, (int)naturalExit.Y - 12, (int)naturalExit.X + 12, (int)naturalExit.Y + 12);
            List<BoolGrid> sides = naturalWalls.GetGroups();
            Dictionary<BoolGrid, int> counts = new Dictionary<BoolGrid, int>();
            foreach (BoolGrid side in sides)
                counts.Add(side, side.Count());
            sides.Sort((BoolGrid a, BoolGrid b) => counts[b] - counts[a]);

            List<Point2D> building1Positions = Placable(sides[0], Tyr.Bot.MapAnalyzer.Placement, BuildingType.LookUp[types[0]].Size, false);
            List<Point2D> building2Positions = Placable(sides[1], Tyr.Bot.MapAnalyzer.Placement, BuildingType.LookUp[types[3]].Size, false);
            int naturalHeight = Tyr.Bot.MapAnalyzer.MapHeight((int)Tyr.Bot.BaseManager.Natural.BaseLocation.Pos.X, (int)Tyr.Bot.BaseManager.Natural.BaseLocation.Pos.Y);
            building1Positions = building1Positions.FindAll((p) => Tyr.Bot.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);
            building2Positions = building2Positions.FindAll((p) => Tyr.Bot.MapAnalyzer.MapHeight((int)p.X, (int)p.Y) == naturalHeight);


            FindWall(types, building1Positions, building2Positions, Tyr.Bot.MapAnalyzer.Placement, true);
            //DrawResult(unPathable, naturalWalls, building1Positions, building2Positions);
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
            else return false;

            Point2D zealotPos = SC2Util.Point(end.X, end.Y);
            zealotPos = SC2Util.TowardCardinal(zealotPos, middle, 2);
            zealotPos = SC2Util.TowardCardinal(zealotPos, Tyr.Bot.BaseManager.Natural.BaseLocation.Pos, 0.5f);
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
            Point2D natural = Tyr.Bot.BaseManager.Natural.BaseLocation.Pos;
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
                /*
                if (natural.Y > middle.Y)
                {
                    pylonPositions[2] = SC2Util.Point(middle.X + 0.5f, middle.Y + 2.5f);
                    pylonPositions[3] = SC2Util.Point(middle.X - 0.5f, middle.Y + 2.5f);
                }
                else
                {
                    pylonPositions[2] = SC2Util.Point(middle.X + 0.5f, middle.Y - 2.5f);
                    pylonPositions[3] = SC2Util.Point(middle.X - 0.5f, middle.Y - 2.5f);
                }
                */
            } else
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
                /*
                if (natural.X > middle.X)
                {
                    pylonPositions[2] = SC2Util.Point(middle.X + 2.5f, middle.Y + 0.5f);
                    pylonPositions[3] = SC2Util.Point(middle.X + 2.5f, middle.Y - 0.5f);
                }
                else
                {
                    pylonPositions[2] = SC2Util.Point(middle.X - 2.5f, middle.Y + 0.5f);
                    pylonPositions[3] = SC2Util.Point(middle.X - 2.5f, middle.Y - 0.5f);
                }
                */
            }

            Point2D pylonPos = null;
            foreach (Point2D pos in pylonPositions)
            {
                if (ProtossBuildingPlacement.IsBuildingInPowerField(start, SC2Util.Point(3, 3), pos)
                    && ProtossBuildingPlacement.IsBuildingInPowerField(end, SC2Util.Point(3, 3), pos)
                    && Tyr.Bot.buildingPlacer.CheckDistanceClose(pos, UnitTypes.PYLON, start, types[0])
                    && Tyr.Bot.buildingPlacer.CheckDistanceClose(pos, UnitTypes.PYLON, end, types[3]))
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
                Tyr.Bot.buildingPlacer.ReservedLocation.Add(new ReservedBuilding() { Type = building.Type, Pos = building.Pos });
        }

        /*
    private void DrawResult(BoolGrid unPathable, BoolGrid naturalWalls, List<Point2D> building1Positions, List<Point2D> building2Positions)
    {
        if (!Tyr.Debug)
            return;

        int width = Tyr.Bot.GameInfo.StartRaw.MapSize.X;
        int height = Tyr.Bot.GameInfo.StartRaw.MapSize.Y;
        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (naturalWalls != null && naturalWalls[x, y])
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Red);
                else if (unPathable[x, y])
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
                else if (!Tyr.Bot.MapAnalyzer.Placement[x, y])
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Gray);
                else
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.White);
            }


        //DrawBuildings(bmp);
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

        foreach (Point2D pos in building1Positions)
            bmp.SetPixel((int)pos.X, height - 1 - (int)pos.Y, System.Drawing.Color.Blue);
        foreach (Point2D pos in building2Positions)
            bmp.SetPixel((int)pos.X, height - 1 - (int)pos.Y, System.Drawing.Color.Green);

        int width = Tyr.Bot.GameInfo.StartRaw.MapSize.X;
        int height = Tyr.Bot.GameInfo.StartRaw.MapSize.Y;
        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (Tyr.Bot.MapAnalyzer.StartArea[x, y])
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Green);
                else if (Tyr.Bot.MapAnalyzer.Ramp[x, y])
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Blue);
                else
                    bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
            }

        for (float x = Wall[0].Pos.X - BuildingType.LookUp[Wall[0].Type].Size.X / 2f + 0.5f; x < Wall[0].Pos.X + BuildingType.LookUp[Wall[0].Type].Size.X / 2f - 0.5f + 0.1f; x++)
            for (float y = Wall[0].Pos.Y - BuildingType.LookUp[Wall[0].Type].Size.Y / 2f + 0.5f; y < Wall[0].Pos.Y + BuildingType.LookUp[Wall[0].Type].Size.Y / 2f - 0.5f + 0.1f; y++)
                bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Red);
        for (float x = Wall[2].Pos.X - BuildingType.LookUp[Wall[2].Type].Size.X / 2f + 0.5f; x < Wall[2].Pos.X + BuildingType.LookUp[Wall[2].Type].Size.X / 2f + 0.1f - 0.5f; x++)
            for (float y = Wall[2].Pos.Y - BuildingType.LookUp[Wall[2].Type].Size.Y / 2f + 0.5f; y < Wall[2].Pos.Y + BuildingType.LookUp[Wall[2].Type].Size.Y / 2f + 0.1f - 0.5f; y++)
                bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Yellow);
        for (float x = Wall[1].Pos.X - BuildingType.LookUp[Wall[1].Type].Size.X / 2f + 0.5f; x < Wall[1].Pos.X + BuildingType.LookUp[Wall[1].Type].Size.X / 2f + 0.1f - 0.5f; x++)
            for (float y = Wall[1].Pos.Y - BuildingType.LookUp[Wall[1].Type].Size.Y / 2f + 0.5f; y < Wall[1].Pos.Y + BuildingType.LookUp[Wall[1].Type].Size.Y / 2f + 0.1f - 0.5f; y++)
                bmp.SetPixel((int)x, height - 1 - (int)y, System.Drawing.Color.Purple);

        bmp.Save(@"C:\Users\Simon\Desktop\WallPlacement.png");
    }
*/

        /*
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
        */

        private List<Point2D> Placable(BoolGrid around, BoolGrid startArea, Point2D size, bool allowCorners)
        {
            Point2D size1x1 = new Point2D() { X = 1, Y = 1 };
            List<Point2D> result = new List<Point2D>();

            for (int x = 0; x < around.Width(); x++)
                for (int y = 0; y < around.Height(); y++)
                    if (around[x, y])
                        GetPlacableAround(startArea, new Point2D() { X = x, Y = y}, size1x1, size, result, allowCorners);

            return result;
        }

        private void GetPlacableAround(BoolGrid startArea, Point2D pos, Point2D size1, Point2D size2, ICollection<Point2D> result, bool allowCorners)
        {
            float xOffset = (size1.X + size2.X) / 2f;
            float yOffset = (size1.Y + size2.Y) / 2f;
            for (float i = -xOffset + (allowCorners ? 0 : 1); i < 0.1f + xOffset - (allowCorners ? 0 : 1); i++)
            {
                Point2D checkPos = new Point2D() { X = pos.X + i, Y = pos.Y - yOffset };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
                checkPos = new Point2D() { X = pos.X + i, Y = pos.Y + yOffset };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
            }
            for (float i = -yOffset + (allowCorners ? 0 : 1); i < 0.1f + yOffset - (allowCorners ? 0 : 1); i++)
            {
                Point2D checkPos = new Point2D() { X = pos.X + xOffset, Y = pos.Y + i };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
                checkPos = new Point2D() { X = pos.X - xOffset, Y = pos.Y + i };
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
