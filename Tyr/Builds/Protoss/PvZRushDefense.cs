using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class PvZRushDefense : Build
    {
        public int RequiredSize = 15;

        public override string Name()
        {
            return "PvZRushDefense";
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
            HallucinationAttackTask.Enable();
            WorkerRushDefenseTask.Enable();
            ForceFieldRampTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController() { UseHallucaination = true, FleeEnemies = false });
            MicroControllers.Add(new DodgeBallController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            tyr.TargetManager.PrefferDistant = false;


            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.ZEALOT, 1, () => Completed(UnitTypes.CYBERNETICS_CORE) == 0);
            result.Train(UnitTypes.SENTRY, 1);
            result.Train(UnitTypes.ZEALOT, 8, () => TotalEnemyCount(UnitTypes.ZERGLING) >= 15);
            result.Train(UnitTypes.STALKER, 2);
            result.Upgrade(UpgradeType.WarpGate);
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
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.SHIELD_BATTERY, Main, MainDefensePos);
            result.Building(UnitTypes.GATEWAY, Main, 2);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            BalanceGas();
            tyr.TaskManager.CombatSimulation.SimulationLength = 0;
            TimingAttackTask.Task.RequiredSize = RequiredSize;
            TimingAttackTask.Task.RetreatSize = 6;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;

            if (TimingAttackTask.Task.AttackSent)
            {
                ForceFieldRampTask.Task.Stopped = true;
                ForceFieldRampTask.Task.Clear();
            }

            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (tyr.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 2 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
            }
        }
    }
}
