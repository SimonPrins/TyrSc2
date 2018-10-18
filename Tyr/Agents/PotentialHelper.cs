using SC2APIProtocol;
using Tyr.Util;

namespace Tyr.Agents
{
    public class PotentialHelper
    {
        private Point2D Direction = SC2Util.Point(0, 0);
        public float Magnitude;
        public Point2D Origin;

        public PotentialHelper(Point2D origin)
        {
            Origin = origin;
            Magnitude = 1;
        }

        public PotentialHelper(Point origin)
        {
            Origin = SC2Util.To2D(origin);
            Magnitude = 1;
        }

        public PotentialHelper(Point2D origin, float magnitude)
        {
            Origin = origin;
            Magnitude = magnitude;
        }

        public PotentialHelper(Point origin, float magnitude)
        {
            Origin = SC2Util.To2D(origin);
            Magnitude = magnitude;
        }

        public PotentialHelper To(Point2D to)
        {
            To(to, 1);
            return this;
        }

        public PotentialHelper To(Point2D to, float magnitude)
        {
            Point2D delta = SC2Util.Point(to.X - Origin.X, to.Y - Origin.Y);
            float size = (float)System.Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (size > 0)
            {
                delta = SC2Util.Point(delta.X * magnitude / size, delta.Y * magnitude / size);
                Direction = SC2Util.Point(Direction.X + delta.X, Direction.Y + delta.Y);
            }
            return this;
        }

        public PotentialHelper From(Point2D from)
        {
            From(from, 1);
            return this;
        }

        public PotentialHelper From(Point from)
        {
            return From(SC2Util.To2D(from));
        }

        public PotentialHelper From(Point2D from, float magnitude)
        {
            Point2D delta = SC2Util.Point(Origin.X - from.X, Origin.Y - from.Y);
            float size = (float)System.Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (size > 0)
            {
                delta = SC2Util.Point(delta.X * magnitude / size, delta.Y * magnitude / size);
                Direction = SC2Util.Point(Direction.X + delta.X, Direction.Y + delta.Y);
            }
            return this;
        }

        public PotentialHelper From(Point from, float magnitude)
        {
            return From(SC2Util.To2D(from), magnitude);
        }

        public Point2D Get()
        {
            float size = (float)System.Math.Sqrt(Direction.X * Direction.X + Direction.Y * Direction.Y);

            if (size > 0)
                Direction = SC2Util.Point(Direction.X * Magnitude / size, Direction.Y * Magnitude / size);
            return SC2Util.Point(Origin.X + Direction.X, Origin.Y + Direction.Y);
        }
    }
}
