using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class Lifting : Strategy
    {
        private static Strategy Singleton = new Lifting();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            foreach (Unit unit in Bot.Main.Enemies())
                if (unit.IsFlying && UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    return true;
            return false;
        }

        public override string Name()
        {
            return "Lifting";
        }
    }
}
