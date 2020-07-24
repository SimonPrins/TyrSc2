using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Bio : Strategy
    {
        private static Strategy Singleton = new Bio();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Count(UnitTypes.MEDIVAC) > 0
                    || Count(UnitTypes.MARAUDER) + Count(UnitTypes.MARINE) >= 20
                    || Count(UnitTypes.MARAUDER) >= 4;
        }

        public override string Name()
        {
            return "Bio";
        }
    }
}
