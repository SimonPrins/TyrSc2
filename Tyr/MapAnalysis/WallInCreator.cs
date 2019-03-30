using SC2APIProtocol;
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


            BoolGrid unPathable = Tyr.Bot.MapAnalyzer.UnPathable;
            BoolGrid ramp = Tyr.Bot.MapAnalyzer.Ramp;

            BoolGrid rampAdjacent = unPathable.GetAdjacent(ramp);
            BoolGrid rampSides = unPathable.GetConnected(rampAdjacent, 5);
            List<BoolGrid> sides = rampSides.GetGroups();

            List<Point2D> building1Positions = Placable(sides[0], Tyr.Bot.MapAnalyzer.StartArea, BuildingType.LookUp[types[0]].Size);
            List<Point2D> building2Positions = Placable(sides[1], Tyr.Bot.MapAnalyzer.StartArea, BuildingType.LookUp[types[2]].Size);

            float wallScore = 1000;
            foreach (Point2D p1 in building1Positions)
                foreach (Point2D p2 in building2Positions)
                {
                    if (System.Math.Abs(p1.X - p2.X) < 0.1f + (BuildingType.LookUp[types[0]].Size.X + BuildingType.LookUp[types[2]].Size.X) / 2f
                        && System.Math.Abs(p1.Y - p2.Y) < 0.1f + (BuildingType.LookUp[types[0]].Size.Y + BuildingType.LookUp[types[2]].Size.Y) / 2f)
                        continue;

                    float newScore = SC2Util.DistanceGrid(p1, p2);
                    if (newScore >= wallScore)
                        continue;
                    
                    Wall[0].Pos = p1;
                    Wall[2].Pos = p2;
                    wallScore = newScore;
                }

            HashSet<Point2D> around1 = new HashSet<Point2D>();
            GetPlacableAround(Tyr.Bot.MapAnalyzer.StartArea, Wall[0].Pos, Wall[0].Size, Wall[1].Size, around1);
            HashSet<Point2D> around2 = new HashSet<Point2D>();
            GetPlacableAround(Tyr.Bot.MapAnalyzer.StartArea, Wall[2].Pos, Wall[2].Size, Wall[1].Size, around2);
            around1.IntersectWith(around2);

            foreach (Point2D pos in around1)
            {
                Wall[1].Pos = new Point2D() { X = pos.X + 0.5f, Y = pos.Y + 0.5f };
                break;
            }
        }

        public void ReserveSpace()
        {
            foreach (WallBuilding building in Wall)
                Tyr.Bot.buildingPlacer.ReservedLocation.Add(new ReservedBuilding() { Type = building.Type, Pos = building.Pos });
        }

        private void DrawResult()
        {
            if (!Tyr.Debug)
                return;

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

        private List<Point2D> Placable(BoolGrid around, BoolGrid startArea, Point2D size)
        {
            Point2D size1x1 = new Point2D() { X = 1, Y = 1 };
            List<Point2D> result = new List<Point2D>();

            for (int x = 0; x < around.Width(); x++)
                for (int y = 0; y < around.Height(); y++)
                    if (around[x, y])
                        GetPlacableAround(startArea, new Point2D() { X = x, Y = y}, size1x1, size, result);

            return result;
        }

        private void GetPlacableAround(BoolGrid startArea, Point2D pos, Point2D size1, Point2D size2, ICollection<Point2D> result)
        {
            float xOffset = (size1.X + size2.X) / 2f;
            float yOffset = (size1.Y + size2.Y) / 2f;
            for (float i = -xOffset; i < 0.1f + xOffset; i++)
            {
                Point2D checkPos = new Point2D() { X = pos.X + i, Y = pos.Y - yOffset };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
                checkPos = new Point2D() { X = pos.X + i, Y = pos.Y + yOffset };
                if (CheckRect(startArea, checkPos, size2))
                    result.Add(checkPos);
            }
            for (float i = -yOffset; i < 0.1f + yOffset; i++)
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
