using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class AccessDenied : Build
    {
        private bool ProxyDetected;
        private bool ScoutDetected = false;
        private SoftLeashController SoftLeash = new SoftLeashController(new HashSet<uint> { UnitTypes.STALKER, UnitTypes.IMMORTAL }, UnitTypes.IMMORTAL, 6);

        public override string Name()
        {
            return "AccessDenied";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ScoutTask.Enable();
            ArmyObserverTask.Enable();
            ForceFieldRampTask.Enable();
            DenyScoutTask.Enable();
            HuntScoutTask.Enable();
            HuntProxyTask.Enable();
            AttackLocationTask.Enable();
            WorkersAttackLocationTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(SoftLeash);
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new FearMinesController());
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(new StutterController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && (Count(UnitTypes.CYBERNETICS_CORE) > 0 || ProxyDetected));
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.OBSERVER, 1, () => Count(UnitTypes.IMMORTAL) >= 4);
            result.Train(UnitTypes.ZEALOT, 2, () => ProxyDetected);
            result.Train(UnitTypes.IMMORTAL);
            result.Train(UnitTypes.STALKER, 1);
            //result.Train(UnitTypes.SENTRY, 1);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.IMMORTAL) >= 4);
            result.Train(UnitTypes.STALKER, () => Count(UnitTypes.STALKER) < 3 || Gas() >= 125);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY);
            result.If(() => !ProxyDetected || Count(UnitTypes.ZEALOT) > 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.GATEWAY, () => ProxyDetected && Count(UnitTypes.ZEALOT) > 0);
            result.If(() => !ProxyDetected || Count(UnitTypes.ZEALOT) >= 2);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !ProxyDetected || Count(UnitTypes.ZEALOT) + Count(UnitTypes.STALKER) >= 4);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => !ProxyDetected);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            bool enemyHasArmy = TotalEnemyCount(UnitTypes.ZEALOT) + TotalEnemyCount(UnitTypes.STALKER) + TotalEnemyCount(UnitTypes.SENTRY) + TotalEnemyCount(UnitTypes.IMMORTAL) + TotalEnemyCount(UnitTypes.ADEPT) > 0;
            
            if (enemyHasArmy || Completed(UnitTypes.ZEALOT) + Completed(UnitTypes.STALKER) >= 2)
            {
                DenyScoutTask.Task.Stopped = true;
                DenyScoutTask.Task.Clear();
                HuntScoutTask.Task.Stopped = true;
                HuntScoutTask.Task.Clear();
                HuntProxyTask.Task.Stopped = true;
                HuntScoutTask.Task.Clear();
                WorkersAttackLocationTask.Task.Stopped = true;
                WorkersAttackLocationTask.Task.Clear();
                foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                {
                    task.Stopped = true;
                    task.Clear();
                }
                
            }

            SoftLeash.Stopped = Completed(UnitTypes.IMMORTAL) == 0;
            tyr.buildingPlacer.BuildCompact = true;
            TimingAttackTask.Task.DefendOtherAgents = false;
            if (ProxyDetected)
                TimingAttackTask.Task.RequiredSize = 5;
            else if (Completed(UnitTypes.IMMORTAL) >= 3)
                TimingAttackTask.Task.RequiredSize = 6;
            else
                TimingAttackTask.Task.RequiredSize = 8;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;

            float proxyDist = 100 * 100;
            foreach (Unit enemy in tyr.Enemies())
            {
                if (enemy.UnitType == UnitTypes.PYLON)
                {
                    float dist = SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation);
                    if (dist < proxyDist)
                    {
                        ProxyDetected = true;
                        AttackLocationTask.Task.AttackTarget = SC2Util.To2D(enemy.Pos);
                        WorkersAttackLocationTask.Task.AttackTarget = SC2Util.To2D(enemy.Pos);
                        DenyScoutTask.Task.StartFrame = 0;
                    }
                }
                if (enemy.UnitType == UnitTypes.PROBE && SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) <= 50 * 50)
                    ScoutDetected = true;
            }


            tyr.DrawText("ScoutDetected: " + ScoutDetected);

            DenyScoutTask.Task.Stopped = enemyHasArmy || ProxyDetected || (!ProxyDetected && !ScoutDetected);
            if (DenyScoutTask.Task.Stopped)
                DenyScoutTask.Task.Clear();
        }
    }
}
