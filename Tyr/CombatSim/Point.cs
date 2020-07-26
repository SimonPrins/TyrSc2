namespace SC2Sharp.CombatSim
{
    public class Point
    {
        public float X, Y;

        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ")";
        }
    }
}
