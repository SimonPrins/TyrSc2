using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class Switchup : Build
    {
        private Point2D EnemyMain = null;
        private bool Proxy = false;
        private bool InBaseBarracks = false;
        private PvTStalkerImmortal PvTStalkerImmortal;
        private OneBaseStalkerImmortal OneBaseStalkerImmortal;
        public override string Name()
        {
            return "Switchup";
        }

        public override Build OverrideBuild()
        {
            if (EnemyMain == null)
                EnemyMain = Bot.Bot.TargetManager.PotentialEnemyStartLocations[0];
            if (!InBaseBarracks
                && !Proxy)
            {
                foreach (Unit unit in Bot.Bot.Enemies())
                {
                    if (unit.UnitType != UnitTypes.BARRACKS
                        && unit.UnitType != UnitTypes.FACTORY)
                        continue;
                    if (SC2Util.DistanceSq(unit.Pos, EnemyMain) <= 40 * 40)
                        InBaseBarracks = true;
                    else
                        Proxy = true;
                }
            }
            if (InBaseBarracks)
            {
                if (PvTStalkerImmortal == null)
                {
                    PvTStalkerImmortal = new PvTStalkerImmortal();
                    PvTStalkerImmortal.OnStart(Bot.Bot);
                    PvTStalkerImmortal.InitializeTasks();
                }
                return PvTStalkerImmortal;
            }
            else
            {
                if (OneBaseStalkerImmortal == null)
                {
                    OneBaseStalkerImmortal = new OneBaseStalkerImmortal() { Scouting = true };
                    OneBaseStalkerImmortal.OnStart(Bot.Bot);
                    OneBaseStalkerImmortal.InitializeTasks();
                }
                return OneBaseStalkerImmortal;
            }
        }

        public override void OnFrame(Bot tyr)
        {
        }

        public override void OnStart(Bot tyr)
        {
        }
    }
}
