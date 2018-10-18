using static Tyr.Builds.BuildLists.ConditionalStep;

namespace Tyr.Builds.BuildLists
{
    public class MorphStep : BuildStep
    {
        public uint UnitType;
        public int Number = 1;
        public Test Condition = () => { return true; };

        public MorphStep(uint unitType)
        {
            UnitType = unitType;
        }

        public MorphStep(uint unitType, int number)
        {
            UnitType = unitType;
            Number = number;
        }

        public MorphStep(uint unitType, Test condition)
        {
            UnitType = unitType;
            Condition = condition;
        }

        public MorphStep(uint unitType, int number, Test condition)
        {
            UnitType = unitType;
            Number = number;
            Condition = condition;
        }

        public override string ToString()
        {
            return "Morphing " + UnitType + " " + Number;
        }
    }
}
