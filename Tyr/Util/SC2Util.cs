using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC2APIProtocol;

namespace Tyr.Util
{
    /*
     * Util class for performing common operations for the SC2API.
     */
    public abstract class SC2Util
    {
        public static int GetDataValue(ImageData data, int x, int y)
        {
            int pixelID = x + (data.Size.Y - 1 - y) * data.Size.X;
            return data.Data[pixelID];
        }

        public static bool GetTilePlacable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Tyr.Bot.GameInfo.StartRaw.PlacementGrid.Size.X || y >= Tyr.Bot.GameInfo.StartRaw.PlacementGrid.Size.Y)
                return false;
            return SC2Util.GetDataValue(Tyr.Bot.GameInfo.StartRaw.PlacementGrid, x, y) == 255;
        }

        public static Point2D Point(float x, float y)
        {
            Point2D result = new Point2D
            {
                X = x,
                Y = y
            };
            return result;
        }

        public static Point Point(float x, float y, float z)
        {
            Point result = new Point
            {
                X = x,
                Y = y,
                Z = z
            };
            return result;
        }

        public static float DistanceSq(Point pos1, Point2D pos2)
        {
            return DistanceSq(To2D(pos1), pos2);
        }

        public static float DistanceSq(Point pos1, Point pos2)
        {
            return DistanceSq(To2D(pos1), To2D(pos2));
        }

        public static float DistanceSq(Point2D pos1, Point pos2)
        {
            return DistanceSq(pos1, To2D(pos2));
        }

        public static float DistanceSq(Point2D pos1, Point2D pos2)
        {
            return (pos1.X - pos2.X) * (pos1.X - pos2.X) + (pos1.Y - pos2.Y) * (pos1.Y - pos2.Y);
        }

        public static float DistanceGrid(Point pos1, Point pos2)
        {
            return DistanceGrid(To2D(pos1), To2D(pos2));
        }

        public static float DistanceGrid(Point pos1, Point2D pos2)
        {
            return DistanceGrid(To2D(pos1), pos2);
        }

        public static float DistanceGrid(Point2D pos1, Point pos2)
        {
            return DistanceGrid(pos1, To2D(pos2));
        }

        public static float DistanceGrid(Point2D pos1, Point2D pos2)
        {
            return Math.Abs(pos1.X - pos2.X) + Math.Abs(pos1.Y - pos2.Y);
        }

        public static Point2D To2D(Point pos)
        {
            return Point(pos.X, pos.Y);
        }

        public static Point2D Normalize(Point2D point)
        {
            float length = (float)Math.Sqrt(point.X * point.X + point.Y * point.Y);
            return Point(point.X / length, point.Y / length);
        }
    }
}
