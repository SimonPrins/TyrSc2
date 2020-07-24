using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class GreedySentries : Build
    {
        public int RequiredSize = 10;
        private bool TyckleFightChatSent = false;
        private bool MessageSent = false;

        public override string Name()
        {
            return "GreedySentries";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            MassSentriesTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            HallucinationAttackTask.Enable();
            WorkerRushDefenseTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new StayByCannonsController());
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
            result.Train(UnitTypes.PROBE, 26, () => Completed(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.PROBE, 32, () => Completed(UnitTypes.NEXUS) >= 3);
            result.Train(UnitTypes.PROBE, 40, () => Completed(UnitTypes.NEXUS) >= 4);
            result.Train(UnitTypes.PROBE, 48, () => Completed(UnitTypes.NEXUS) >= 5);
            result.Train(UnitTypes.SENTRY, 3);
            result.If(() => Bot.Main.BaseManager.Main.BaseLocation.MineralFields.Count >= 8 || Count(UnitTypes.NEXUS) >= 2);
            result.Upgrade(UpgradeType.WarpGate);
            result.Train(UnitTypes.SENTRY);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.NEXUS);
            result.If(() => Count(UnitTypes.NEXUS) >= 2);
            result.Building(UnitTypes.ASSIMILATOR, 4);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.GATEWAY, () => Minerals() >= 200);
            result.Building(UnitTypes.GATEWAY, () => Minerals() >= 250);
            result.Building(UnitTypes.GATEWAY, () => Minerals() >= 250);
            result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 4, () => Completed(UnitTypes.FORGE) > 0 && Minerals() >= 250);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos, () => Count(UnitTypes.PHOTON_CANNON) >= 4 && Minerals() >= 200);
            result.Building(UnitTypes.SHIELD_BATTERY, Natural, NaturalDefensePos, 2, () => Count(UnitTypes.PHOTON_CANNON) >= 4 && Minerals() >= 200);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.If(() => Bot.Main.BaseManager.Main.BaseLocation.MineralFields.Count < 8 || (Minerals() >= 450 && Count(UnitTypes.WARP_GATE) >= 5));
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.FORGE);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Minerals() >= 400);
            result.Building(UnitTypes.GATEWAY, () => Minerals() >= 250);
            result.If(() => Minerals() >= 450 && Count(UnitTypes.WARP_GATE) >= 6);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Minerals() >= 400);
            result.Building(UnitTypes.GATEWAY, () => Minerals() >= 250);
            result.If(() => Minerals() >= 450 && Count(UnitTypes.WARP_GATE) >= 7);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Minerals() >= 400);
            result.Building(UnitTypes.GATEWAY, () => Minerals() >= 250);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            tyr.TaskManager.CombatSimulation.SimulationLength = 0;
            MassSentriesTask.Task.RequiredSize = RequiredSize;
            MassSentriesTask.Task.RetreatSize = 6;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;

            DefenseTask.GroundDefenseTask.UseForceFields = true;

            if (!TyckleFightChatSent && StrategyAnalysis.WorkerRush.Get().Detected)
            {
                TyckleFightChatSent = true;
                tyr.Chat("TICKLE FIGHT! :D");
            }

            if (!MessageSent)
                if (MassSentriesTask.Task.AttackSent)
                {
                    MessageSent = true;
                    tyr.Chat("Prepare to be TICKLED! :D");
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
