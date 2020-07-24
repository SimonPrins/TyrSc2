using SC2APIProtocol;
using System;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class OneBaseStalkerImmortal : Build
    {
        public int RequiredSize = 8;
        private bool PhoenixScoutSent = false;
        public bool UsePhoenixScout = true;
        public bool StartZealots = false;
        public bool DoubleRobo = false;
        public bool EarlySentry = true;
        public bool UseSentry = false;
        public bool UseCombatSim = true;
        public bool AggressiveMicro = false;
        public bool DenyScouting = false;
        public bool ShieldBatteries = false;
        public bool Scouting = true;
        public bool Observer = false;
        public Func<bool> ExpandCondition;
        private bool StartExpanding = false;
        public bool ObserverScout = false;

        private bool ImmortalNearEnemy = false;

        private KillTargetController KillImmortals = new KillTargetController(UnitTypes.IMMORTAL);
        private KillTargetController KillStalkers = new KillTargetController(UnitTypes.STALKER);
        private KillTargetController KillRobos = new KillTargetController(UnitTypes.ROBOTICS_FACILITY, true);
        private KillTargetController KillStargates = new KillTargetController(UnitTypes.STARGATE, true);

        private StutterForwardController StutterForwardController = new StutterForwardController() { TowardEnemies = true };
        private StutterController StutterController = new StutterController();
        private FearCannonsController FearCannonsWithoutShieldsController = new FearCannonsController() { OnlyWhenLowShields = true, AttackCannonsInMain = false };
        private FearCannonsController FearCannonsController = new FearCannonsController();



        public override string Name()
        {
            return "OneBaseStalkerImmortal";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1 || Scouting)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ScoutTask.Enable();
            ArmyObserverTask.Enable();
            if (ObserverScout)
                ObserverScoutTask.Enable();
            if (Bot.Main.EnemyRace == SC2APIProtocol.Race.Zerg || Bot.Main.EnemyRace == SC2APIProtocol.Race.Protoss)
                ForceFieldRampTask.Enable();
            if (DenyScouting)
                DenyScoutTask.Enable();
            WorkerRushDefenseTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            if (ExpandCondition == null)
                ExpandCondition = WillExpand;

            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(FearCannonsWithoutShieldsController);
            MicroControllers.Add(FearCannonsController);
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
            result.If(() => !StartExpanding || Count(UnitTypes.IMMORTAL) < 2 || Count(UnitTypes.STALKER) + Count(UnitTypes.ZEALOT) + Count(UnitTypes.IMMORTAL) < 8 || Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.ZEALOT, 4, () => TotalEnemyCount(UnitTypes.ROACH) == 0 && StartZealots);
            result.Train(UnitTypes.OBSERVER, 1, () => Bot.Main.EnemyRace == SC2APIProtocol.Race.Terran || Count(UnitTypes.IMMORTAL) >= (DoubleRobo ? 4 : 3) || Observer);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) < 5 || !StartExpanding || Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.OBSERVER, 2, () => TotalEnemyCount(UnitTypes.BANSHEE) > 0);
            result.Train(UnitTypes.IMMORTAL, () => TotalEnemyCount(UnitTypes.BANSHEE) == 0 && TotalEnemyCount(UnitTypes.BATTLECRUISER) == 0);
            result.Train(UnitTypes.STALKER, 1, () => !StartZealots);
            if (Bot.Main.EnemyRace != SC2APIProtocol.Race.Zerg && UseSentry)
                result.Train(UnitTypes.SENTRY, 1, () => !PhoenixScoutSent && EarlySentry && !StrategyAnalysis.CannonRush.Get().Detected && TotalEnemyCount(UnitTypes.FORGE) + TotalEnemyCount(UnitTypes.PHOTON_CANNON) == 0);
            result.Upgrade(UpgradeType.WarpGate, () => !DoubleRobo || Count(UnitTypes.IMMORTAL) >= 4);
            result.Train(UnitTypes.STALKER, () => (!StartZealots || TotalEnemyCount(UnitTypes.ROACH) + TotalEnemyCount(UnitTypes.MUTALISK) > 0) && (!DoubleRobo || Count(UnitTypes.STALKER) < 3 || Gas() >= 125));
            result.Train(UnitTypes.ZEALOT, () => TotalEnemyCount(UnitTypes.ROACH) + TotalEnemyCount(UnitTypes.MUTALISK) == 0 && StartZealots);
            result.Train(UnitTypes.IMMORTAL, () => TotalEnemyCount(UnitTypes.BATTLECRUISER) > 0 && Count(UnitTypes.STALKER) >= 10);

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
            if (ShieldBatteries)
            {
                result.Building(UnitTypes.PYLON, Main, MainDefensePos);
                result.Building(UnitTypes.SHIELD_BATTERY, Main, MainDefensePos, 2);
            }
            result.If(() => StartExpanding);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY, () => TotalEnemyCount(UnitTypes.BATTLECRUISER) > 0);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !DoubleRobo && TotalEnemyCount(UnitTypes.BATTLECRUISER) == 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            GasWorkerTask.WorkersPerGas = 3;

            if (!StartExpanding && ExpandCondition())
            {
                StartExpanding = true;
                if (Completed(UnitTypes.IMMORTAL) + Completed(UnitTypes.STALKER) < 8)
                    TimingAttackTask.Task.Clear();
            }

            if (CompletedCannonProxy.Get().Detected)
                foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                    task.StopAndClear(true);

            if (Completed(UnitTypes.IMMORTAL) >= 1)
                FearCannonsController.Range = 5;
            else if (Completed(UnitTypes.IMMORTAL) == 0)
                FearCannonsController.Range = 10;

            tyr.buildingPlacer.BuildCompact = true;

            TimingAttackTask.Task.DefendOtherAgents = false;
            if (StartExpanding)
                TimingAttackTask.Task.RequiredSize = 20;
            else if (TotalEnemyCount(UnitTypes.BANSHEE) == 0 && Completed(UnitTypes.IMMORTAL) >= (DoubleRobo ? 3 : 2)) 
                TimingAttackTask.Task.RequiredSize = 5;
            else
                TimingAttackTask.Task.RequiredSize = RequiredSize;

            if (!ImmortalNearEnemy)
                foreach (Agent agent in tyr.Units())
                    if (agent.Unit.UnitType == UnitTypes.IMMORTAL && agent.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]) <= 20 * 20)
                        ImmortalNearEnemy = true;

            tyr.DrawText("Immortal near enemey: " + ImmortalNearEnemy);

            if (AggressiveMicro 
                && (ImmortalNearEnemy || tyr.Frame >= 22.4 * 60 * 5)
                && EnemyCount(UnitTypes.ZEALOT) == 0
                && EnemyCount(UnitTypes.PHOTON_CANNON) == 0)
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

            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (tyr.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY
                    && agent.Unit.UnitType != UnitTypes.ROBOTICS_FACILITY)
                    continue;

                agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
            }

            ScoutTask.Task.ScoutType = UnitTypes.PHOENIX;
            ScoutTask.Task.Target = tyr.TargetManager.PotentialEnemyStartLocations[0];

            if (Count(UnitTypes.PHOENIX) > 0)
                PhoenixScoutSent = true;

            if (!PhoenixScoutSent && tyr.EnemyRace != SC2APIProtocol.Race.Zerg && UsePhoenixScout)
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

        public bool WillExpand()
        {
            if (Main.BaseLocation.MineralFields.Count < 8)
                return true;
            if (!TimingAttackTask.Task.AttackSent)
                return false;
            int attackingImmortals = 0;
            int attackingUnits = 0;
            foreach (Agent agent in TimingAttackTask.Task.Units)
            {
                if (agent.Unit.UnitType == UnitTypes.IMMORTAL)
                    attackingImmortals++;
                attackingUnits++;
            }
            return attackingImmortals >= 1 && attackingUnits <= 5;
        }
    }
}
