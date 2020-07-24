using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class PvPAdeptAllIn : Build
    {
        private AdeptPhaseEnemyMainController AdeptPhaseEnemyMainController = new AdeptPhaseEnemyMainController();
        private FearEnemyController FearImmortalsController = new FearEnemyController(UnitTypes.ADEPT, UnitTypes.IMMORTAL, 12) { EnemyBaseRange = 20 };
        private bool ChatMessageSent = false;

        public override string Name()
        {
            return "PvPAdeptAllIn";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            ForwardProbeTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(AdeptPhaseEnemyMainController);
            MicroControllers.Add(new AdeptKillWorkersController());
            MicroControllers.Add(FearImmortalsController);
            MicroControllers.Add(new StutterController());

            Set += ProtossBuildUtil.Pylons();
            Set += MainBuildList();
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Train(UnitTypes.PROBE, 19);
            result.Building(UnitTypes.PYLON);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.GATEWAY, 2);
            result.Upgrade(UpgradeType.WarpGate);
            result.Building(UnitTypes.TWILIGHT_COUNSEL, () => Count(UnitTypes.ADEPT) >= 18 || TotalEnemyCount(UnitTypes.IMMORTAL) > 0);
            result.Upgrade(UpgradeType.Charge);
            result.Train(UnitTypes.ZEALOT, () => UpgradeType.LookUp[UpgradeType.Charge].Done() || TotalEnemyCount(UnitTypes.IMMORTAL) > 0);
            result.Train(UnitTypes.ADEPT, () => Completed(UnitTypes.TWILIGHT_COUNSEL) == 0 || UpgradeType.LookUp[UpgradeType.Charge].Started());
            result.Building(UnitTypes.GATEWAY, () => Count(UnitTypes.ADEPT) >= 3);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (!ChatMessageSent)
            {
                if (Completed(UnitTypes.ADEPT_PHASE_SHIFT) > 0)
                {
                    tyr.Chat("As requested by AndyMan, we're doing some Adept harass!");
                    ChatMessageSent = true;
                }
            }
            AdeptPhaseEnemyMainController.Stopped = tyr.Frame >= 22.4 * 60 * 6 && TotalEnemyCount(UnitTypes.IMMORTAL) == 0;
            ForwardProbeTask.Task.EnemyBaseRange = 80;

            ForwardProbeTask.Task.Stopped = tyr.Frame < 22.4 * 165 || SkippedNatural.Get().Detected;
            if (ForwardProbeTask.Task.Stopped)
                ForwardProbeTask.Task.Clear();
        }
    }
}
