using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class Dishwasher : Build
    {
        public int RequiredSize = 8;
        public bool UseCombatSim = true;
        public bool AggressiveMicro = false;
        public bool DenyScouting = false;

        private bool ImmortalNearEnemy = false;

        private KillTargetController KillImmortals = new KillTargetController(UnitTypes.IMMORTAL);
        private KillTargetController KillStalkers = new KillTargetController(UnitTypes.STALKER);
        private KillTargetController KillRobos = new KillTargetController(UnitTypes.ROBOTICS_FACILITY, true);
        private KillTargetController KillStargates = new KillTargetController(UnitTypes.STARGATE, true);

        private StutterForwardController StutterForwardController = new StutterForwardController() { TowardEnemies = true };
        private StutterController StutterController = new StutterController();



        public override string Name()
        {
            return "Dishwasher";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Bot.BaseManager.Pocket.BaseLocation.Pos);
            ArmyObserverTask.Enable();
            if (Bot.Bot.EnemyRace == SC2APIProtocol.Race.Zerg || Bot.Bot.EnemyRace == SC2APIProtocol.Race.Protoss)
                ForceFieldRampTask.Enable();
            if (DenyScouting)
                DenyScoutTask.Enable();
            WorkerRushDefenseTask.Enable();
        }

        public override void OnStart(Bot tyr)
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
            if (UseCombatSim)
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
            result.Train(UnitTypes.OBSERVER, 1, () => Count(UnitTypes.IMMORTAL) >= 4);
            result.Train(UnitTypes.IMMORTAL);
            result.Train(UnitTypes.STALKER, 1);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.IMMORTAL) >= 4);
            result.Train(UnitTypes.STALKER, () => (Count(UnitTypes.STALKER) < 3 || Gas() >= 125));

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
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.PYLON, Main, MainDefensePos);
            result.Building(UnitTypes.SHIELD_BATTERY, Main, MainDefensePos, 2);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            TimingAttackTask.Task.DefendOtherAgents = false;
            if (TotalEnemyCount(UnitTypes.PHOENIX) > 0 && Completed(UnitTypes.IMMORTAL) >= 3) 
                TimingAttackTask.Task.RequiredSize = 4;
            else
                TimingAttackTask.Task.RequiredSize = 20;

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
        }
    }
}
