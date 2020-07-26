using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class StalkerInvasion : Build
    {
        private Point2D EnemyNatural = null;
        private Point2D EnemyThird = null;

        private KillTargetController KillCycloneController = new KillTargetController(UnitTypes.CYCLONE);
        private KillTargetController KillBansheeController = new KillTargetController(UnitTypes.BANSHEE);

        private bool MarineRushSuspected = false;

        public override string Name()
        {
            return "StalkerInvasion";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            StalkerBlinkInMainTask.Enable();
            WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ForceFieldRampTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new BlinkForwardController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new TargetUnguardedBuildingsController());
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new KillTargetController(UnitTypes.SCV) { MaxDist = 4 });
            MicroControllers.Add(KillCycloneController);
            MicroControllers.Add(KillBansheeController);
            MicroControllers.Add(new KillTargetController(UnitTypes.SCV, true));

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            bot.TargetManager.PrefferDistant = false;


            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 32, () => Completed(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.STALKER, 3);
            result.Upgrade(UpgradeType.Blink);
            result.If(() => UpgradeType.LookUp[UpgradeType.Blink].Started() || Completed(UnitTypes.TWILIGHT_COUNSEL) == 0);
            result.If(() => Bot.Main.BaseManager.Main.BaseLocation.MineralFields.Count >= 8 || Count(UnitTypes.NEXUS) >= 2);
            result.Upgrade(UpgradeType.WarpGate);
            result.Train(UnitTypes.STALKER, 6);
            result.Train(UnitTypes.SENTRY, 1);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.GATEWAY, () => Count(UnitTypes.STALKER) >= 2);
            result.Building(UnitTypes.GATEWAY, () => Minerals() >= 350 && Count(UnitTypes.STALKER) >= 8 && UpgradeType.LookUp[UpgradeType.Blink].Started());
            result.If(() => Bot.Main.BaseManager.Main.BaseLocation.MineralFields.Count < 8);
            result.Building(UnitTypes.GATEWAY, 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Minerals() >= 400);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (FourRax.Get().Detected)
                MarineRushSuspected = true;
            ForceFieldRampTask.Task.StopAndClear(!MarineRushSuspected);

            if (MarineRushSuspected)
            {
                StalkerBlinkInMainTask.Task.Stopped = true;
                TimingAttackTask.Task.RequiredSize = 20;
                TimingAttackTask.Task.RetreatSize = 6;
            }
            else
            {
                TimingAttackTask.Task.Stopped = true;
                //TimingAttackTask.Task.RequiredSize = 10;
                //TimingAttackTask.Task.RetreatSize = 6;
            }

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;


            Point2D enemyRamp = bot.MapAnalyzer.GetEnemyRamp();
            int rampDepots = 0;
            foreach (Unit enemy in bot.Enemies())
            {
                if (enemy.UnitType != UnitTypes.SUPPLY_DEPOT)
                    continue;
                if (enemy.DisplayType != DisplayType.Visible)
                    continue;
                if (SC2Util.DistanceSq(enemyRamp, enemy.Pos) >= 4 * 4)
                    continue;
                rampDepots++;
            }
            if (rampDepots >= 3)
            {
                KillBansheeController.Stopped = true;
                KillCycloneController.Stopped = true;
            }

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 2 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
            }

            bot.NexusAbilityManager.Stopped = Count(UnitTypes.STALKER) == 0;
            if (!UpgradeType.LookUp[UpgradeType.Blink].Done())
                bot.NexusAbilityManager.PriotitizedAbilities.Add(UpgradeType.LookUp[UpgradeType.Blink].Ability);
            else
                bot.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.STALKER].Ability);

            if (EnemyNatural == null)
                EnemyNatural = bot.MapAnalyzer.GetEnemyNatural().Pos;
            if (EnemyThird == null)
                EnemyThird = bot.MapAnalyzer.GetEnemyThird().Pos;
        }
    }
}
