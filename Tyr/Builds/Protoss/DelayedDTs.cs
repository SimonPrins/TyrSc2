using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class DelayedDTs : Build
    {
        private WallInCreator WallIn = new WallInCreator();
        public override string Name()
        {
            return "DelayedDTs";
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
            ArmyObserverTask.Enable();
            DTAttackTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new GravitonBeamController());
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.MISSILE_TURRET, 11));
            MicroControllers.Add(new AttackEnemyController(UnitTypes.PHOENIX, UnitTypes.BANSHEE, 15, true));
            MicroControllers.Add(new FallBackController() { ReturnFire = true, MainDist = 40 });
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(new StutterForwardController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;


            WallIn.CreateReaperWall(new List<uint> { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.CYBERNETICS_CORE });
            WallIn.ReserveSpace();

            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.OBSERVER, 2, () => Completed(UnitTypes.STALKER) >= 5);
            result.Train(UnitTypes.DARK_TEMPLAR, 2);
            result.Train(UnitTypes.STALKER, 1);
            result.Upgrade(UpgradeType.WarpGate);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.DARK_SHRINE);
            result.Building(UnitTypes.GATEWAY, 2);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            TimingAttackTask.Task.DefendOtherAgents = false;
            if (Completed(UnitTypes.DARK_TEMPLAR) >= 2)
                TimingAttackTask.Task.RequiredSize = 12;
            else
                TimingAttackTask.Task.RequiredSize = 20;

            bot.buildingPlacer.BuildCompact = true;

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 3 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
            }

            DefenseTask.GroundDefenseTask.IncludePhoenixes = EnemyCount(UnitTypes.CYCLONE) > 0;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
        }
    }
}
