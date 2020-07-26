using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.BuildingPlacement;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class Sanity : Build
    {
        private bool DefendColossus = false;
        private WallInCreator WallIn;
        public override string Name()
        {
            return "Sanity";
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
            ArmyObserverTask.Enable();
            if (Bot.Main.EnemyRace == SC2APIProtocol.Race.Zerg || Bot.Main.EnemyRace == SC2APIProtocol.Race.Protoss)
                ForceFieldRampTask.Enable();
            WorkerRushDefenseTask.Enable();
            KillOwnUnitTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(new StutterController());
            TimingAttackTask.Task.BeforeControllers.Add(new SoftLeashController(UnitTypes.STALKER, UnitTypes.IMMORTAL, 5) { MinEnemyRange = 25 });

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY });
                WallIn.ReserveSpace();
            }

            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2);
            result.If(() => Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.OBSERVER) == 0);
            result.Train(UnitTypes.OBSERVER, 1, () => Count(UnitTypes.IMMORTAL) >= 1);
            result.Train(UnitTypes.IMMORTAL);
            result.Train(UnitTypes.VOID_RAY);
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
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.FORGE, Main, WallIn.Wall[2].Pos, true, () => Bot.Main.Frame < 22.4 * 60 * 4);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            result.Building(UnitTypes.PHOTON_CANNON, Main, WallIn.Wall[1].Pos, 5, () => Completed(UnitTypes.FORGE) > 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY);
            return result;
        }

        private BuildList MainBuildListOld()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.If(() => Bot.Main.Frame >= 22.4 * 131);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !DefendColossus);
            result.Building(UnitTypes.STARGATE, () => DefendColossus);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY);
            result.If(() => Minerals() >= 250);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            bot.buildingPlacer.BuildInsideMainOnly = true;
            bot.buildingPlacer.BuildCompact = true;
            TimingAttackTask.Task.RequiredSize = 30;


            KillOwnUnitTask.Task.Priority = 6;
            if (Count(Main, UnitTypes.FORGE) > 0
                && Completed(UnitTypes.STALKER) >= 28)
            {
                foreach (Agent agent in bot.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.FORGE)
                        continue;
                    if (agent.DistanceSq(WallIn.Wall[2].Pos) <= 4)
                    {
                        KillOwnUnitTask.Task.TargetTag = agent.Unit.Tag;
                        break;
                    }
                }
            }

            if (bot.Observation.Chat != null)
            {
                foreach (ChatReceived chat in bot.Observation.Chat)
                {
                    if (chat.PlayerId == bot.PlayerId)
                        continue;
                    if (!chat.Message.Contains("chosen"))
                        continue;
                    if (chat.Message.Contains("2-Base Colossus"))
                        DefendColossus = true;
                }
            }
        }
    }
}
