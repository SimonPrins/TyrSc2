using Tyr.Agents;

namespace Tyr.StrategyAnalysis
{
    public class PhotonCannon : Strategy
    {
        private static PhotonCannon Singleton = new PhotonCannon();

        public static Strategy Get()
        {
            return Singleton;
        }

        public override bool Detect()
        {
            return Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) > 0;
        }

        public override string Name()
        {
            return "PhotonCannon";
        }
    }
}
