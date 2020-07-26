using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class TwoBaseStalker : Build
    {

        public override string Name()
        {
            return "TwoBaseStalker";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ScoutTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new BlinkForwardController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new FallBackController() { ReturnFire = true, MainDist = 40 });
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(new StutterController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            bot.TargetManager.PrefferDistant = false;


            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0 && (Count(UnitTypes.STALKER) >= 3 || Count(Main, UnitTypes.PYLON) == 1));
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
            result.If(() => Bot.Main.BaseManager.Main.BaseLocation.MineralFields.Count >= 8 || Count(UnitTypes.NEXUS) >= 2);
            result.Upgrade(UpgradeType.WarpGate);
            result.Train(UnitTypes.STALKER, 8);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.GATEWAY, () => Count(UnitTypes.STALKER) >= 4);
            result.Building(UnitTypes.GATEWAY, 2, () => Count(UnitTypes.STALKER) >= 7);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.STALKER) >= 9 || Minerals() >= 300);
            result.Building(UnitTypes.ASSIMILATOR, () => Minerals() >= 300);
            result.Building(UnitTypes.TWILIGHT_COUNSEL, () => Count(UnitTypes.STALKER) >= 12);
            result.Building(UnitTypes.PYLON, Natural);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            TimingAttackTask.Task.DefendOtherAgents = false;
            TimingAttackTask.Task.RequiredSize = 10;
            TimingAttackTask.Task.RetreatSize = 6;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;

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
            bot.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.STALKER].Ability);

        }
    }
}
