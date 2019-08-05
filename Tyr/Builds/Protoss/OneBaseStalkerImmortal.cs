using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class OneBaseStalkerImmortal : Build
    {
        public int RequiredSize = 8;
        private bool PhoenixScoutSent = false;
        public bool StartZealots = false;
        public bool DoubleRobo = false;
        public bool EarlySentry = true;
        public bool UseCombatSim = true;
        public bool AggressiveMicro = false;

        private bool ImmortalNearEnemy = false;

        private KillTargetController KillImmortals = new KillTargetController(UnitTypes.IMMORTAL);
        private KillTargetController KillStalkers = new KillTargetController(UnitTypes.STALKER);
        private KillTargetController KillRobos = new KillTargetController(UnitTypes.ROBOTICS_FACILITY, true);
        private KillTargetController KillStargates = new KillTargetController(UnitTypes.STARGATE, true);

        private StutterForwardController StutterForwardController = new StutterForwardController() { TowardEnemies = true };
        private StutterController StutterController = new StutterController();



        public override string Name()
        {
            return "OneBaseStalkerImmortal";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Tyr.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Tyr.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ScoutTask.Enable();
            ArmyObserverTask.Enable();
            if (Tyr.Bot.EnemyRace == SC2APIProtocol.Race.Zerg)
                ForceFieldRampTask.Enable();
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DodgeBallController());
            if (AggressiveMicro)
            {
                MicroControllers.Add(KillImmortals);
                MicroControllers.Add(KillStalkers);
                MicroControllers.Add(KillRobos);
                MicroControllers.Add(KillStargates);
                MicroControllers.Add(new SoftLeashController(UnitTypes.STALKER, UnitTypes.IMMORTAL, 5) { MinEnemyRange = 25 });
            }
            if (!StartZealots && UseCombatSim)
                MicroControllers.Add(new FallBackController() { ReturnFire = true, MainDist = 40 });
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(StutterForwardController);
            MicroControllers.Add(StutterController);

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.ZEALOT, 4, () => TotalEnemyCount(UnitTypes.ROACH) == 0 && StartZealots);
            result.Train(UnitTypes.OBSERVER, 1, () => Tyr.Bot.EnemyRace == SC2APIProtocol.Race.Terran || Count(UnitTypes.IMMORTAL) >= (DoubleRobo ? 4 : 3));
            result.Train(UnitTypes.OBSERVER, 2, () => TotalEnemyCount(UnitTypes.BANSHEE) > 0);
            result.Train(UnitTypes.IMMORTAL, () => TotalEnemyCount(UnitTypes.BANSHEE) == 0);
            result.Train(UnitTypes.STALKER, 1, () => !StartZealots);
            if (Tyr.Bot.EnemyRace != SC2APIProtocol.Race.Zerg)
                result.Train(UnitTypes.SENTRY, 1, () => !PhoenixScoutSent && EarlySentry);
            result.Upgrade(UpgradeType.WarpGate, () => !DoubleRobo || Count(UnitTypes.IMMORTAL) >= 4);
            result.Train(UnitTypes.STALKER, () => (!StartZealots || TotalEnemyCount(UnitTypes.ROACH) > 0) && (!DoubleRobo || Count(UnitTypes.STALKER) < 3 || Gas() >= 125));
            result.Train(UnitTypes.ZEALOT, () => TotalEnemyCount(UnitTypes.ROACH) == 0 && StartZealots);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            if (!DoubleRobo)
                result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR, () => !StartZealots || TotalEnemyCount(UnitTypes.ROACH) > 0);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.GATEWAY, () => TotalEnemyCount(UnitTypes.BANSHEE) > 0);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => 
                (TotalEnemyCount(UnitTypes.BANSHEE) == 0 && Completed(UnitTypes.STALKER) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.SENTRY) >= 8)
                || DoubleRobo);
            /*
            result.If(() => Completed(UnitTypes.STALKER) + Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.SENTRY) >= 8 || Count(UnitTypes.IMMORTAL) >= 3 || Minerals() >= 500);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            */
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            TimingAttackTask.Task.DefendOtherAgents = false;
            if (TotalEnemyCount(UnitTypes.BANSHEE) == 0 && Completed(UnitTypes.IMMORTAL) >= (DoubleRobo ? 4 : 2))
                TimingAttackTask.Task.RequiredSize = 4;
            else
                TimingAttackTask.Task.RequiredSize = RequiredSize;

            if (!ImmortalNearEnemy)
                foreach (Agent agent in tyr.Units())
                    if (agent.Unit.UnitType == UnitTypes.IMMORTAL && agent.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]) <= 20 * 20)
                        ImmortalNearEnemy = true;

            tyr.DrawText("Immortal near enemey: " + ImmortalNearEnemy);

            if (AggressiveMicro && (ImmortalNearEnemy || tyr.Frame >= 22.4 * 60 * 5) && EnemyCount(UnitTypes.ZEALOT) == 0)
            {
                StutterForwardController.Stopped = false;
                StutterController.Stopped = true;
            }
            else
            {
                StutterForwardController.Stopped = true;
                StutterController.Stopped = false;
            }

            KillImmortals.Stopped = EnemyCount(UnitTypes.ZEALOT) >= 0;
            KillStalkers.Stopped = EnemyCount(UnitTypes.ZEALOT) >= 0;

            ScoutTask.Task.ScoutType = UnitTypes.PHOENIX;
            ScoutTask.Task.Target = tyr.TargetManager.PotentialEnemyStartLocations[0];

            if (Count(UnitTypes.PHOENIX) > 0)
                PhoenixScoutSent = true;

            if (!PhoenixScoutSent && tyr.EnemyRace != SC2APIProtocol.Race.Zerg)
            {
                foreach (Agent agent in tyr.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.SENTRY)
                        continue;
                    if (agent.Unit.Energy < 75)
                        continue;
                    // Hallucinate scouting phoenix.
                    agent.Order(154);
                }
            }
        }
    }
}
