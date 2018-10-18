using Tyr.Managers;

namespace Tyr.Builds.BuildLists
{
    public class BuildingAtBase
    {
        public uint Type;
        public Base B;
        public BuildingAtBase(uint type, Base b)
        {
            Type = type;
            B = b;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(BuildingAtBase))
                return false;
            BuildingAtBase other = (BuildingAtBase)obj;
            return Type == other.Type && B == other.B;
        }

        public override int GetHashCode()
        {
            return B.BaseLocation.Pos.GetHashCode() + (int)Type;
        }
    }
}
