
using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class MassSentries : Build
    {
        public bool HallucinationScout = false;

        private WallInCreator WallIn = new WallInCreator();
        private WallInCreator NaturalWall = new WallInCreator();

        private bool TyckleFightChatSent = false;
        private bool MessageSent = false;
        private bool DefenseMode = false;

        private Point2D RampDefensePos;

        private bool ProxySuspected = false;
        private BoolGrid MainAndNatural;
        private Point2D NaturalCannonDefensePos;

        private bool WarpPrismDrops = true;
        private bool StopWarpPrisms = false;

        public bool AntiBC = false;
        public bool SkipNatural = false;

        public override string Name()
        {
            string name = "MassSentries";
            if (AntiBC)
                return name += "AntiBC";
            if (SkipNatural)
                return name += "SkipNatural";
            return name;
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            HallucinationAttackTask.Enable();
            DefenseTask.Enable();
            MassSentriesTask.Enable();
            WorkerScoutTask.Enable();
            ForceFieldRampTask.Enable();
            KillOwnUnitTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            WorkerRushDefenseTask.Enable();
            ArmyObserverTask.Enable();
            SentryWarpInTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new StayByCannonsController());
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController() { FleeEnemies = false, UseHallucaination = true }) ;
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new TargetUnguardedBuildingsController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new KillTargetController(UnitTypes.SCV) { MaxDist = 4 });
            MicroControllers.Add(new KillTargetController(UnitTypes.SCV, true));

            if (SkipNatural)
                foreach (Base b in bot.BaseManager.Bases)
                {
                    if (b == Main)
                        continue;
                    if (SC2Util.DistanceSq(b.BaseLocation.Pos, Main.BaseLocation.Pos) >= 50 * 50)
                        continue;
                    BuildingPlacement.ReservedBuilding reservedExpand = new BuildingPlacement.ReservedBuilding();
                    reservedExpand.Pos = b.BaseLocation.Pos;
                    reservedExpand.Type = UnitTypes.NEXUS;
                    bot.buildingPlacer.ReservedLocation.Add(reservedExpand);
                }

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            bot.TargetManager.PrefferDistant = false;

            RampDefensePos = new PotentialHelper(MainDefensePos, 4).To(bot.MapAnalyzer.GetMainRamp()).Get();

            if (Bot.Main.EnemyRace == Race.Terran)
            {
                WallIn.CreateReaperWall(new List<uint> { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.CYBERNETICS_CORE });
                WallIn.ReserveSpace();
            }

            NaturalCannonDefensePos = NaturalDefensePos;

            if (Bot.Main.EnemyRace == Race.Zerg && !SkipNatural)
            {
                NaturalWall.CreateFullNatural(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY });
                NaturalWall.ReserveSpace();
                
                if (NaturalWall.Wall.Count >= 4)
                {
                    if (bot.Map == MapEnum.Acropolis
                        || bot.Map == MapEnum.Thunderbird)
                    {
                        WallBuilding temp = NaturalWall.Wall[3];
                        NaturalWall.Wall[3] = NaturalWall.Wall[0];
                        NaturalWall.Wall[0] = temp;
                        NaturalCannonDefensePos = new PotentialHelper(NaturalDefensePos, 4).To(Natural.BaseLocation.Pos).Get();
                    }
                    MainAndNatural = Bot.Main.MapAnalyzer.FindMainAndNaturalArea(NaturalWall);
                }
            }

            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0);
            Set += ExpandBuildings();
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Completed(UnitTypes.NEXUS) >= 2);
            //result.Train(UnitTypes.SENTRY, () => Gas() >= 150 && (Tyr.Bot.Frame >= 22.4 * 60 * 10 || Completed(UnitTypes.WARP_PRISM) == 0 || Completed(UnitTypes.WARP_PRISM_PHASING) == 1 || Count(UnitTypes.SENTRY) < 20 || !WarpPrismDrops));
            result.Train(UnitTypes.SENTRY, () => Gas() >= 150);
            result.Train(UnitTypes.WARP_PRISM, 1, () => !StopWarpPrisms);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.PROBE, 60, () => Completed(UnitTypes.NEXUS) >= 3);
            result.Train(UnitTypes.PROBE, 80, () => Completed(UnitTypes.NEXUS) >= 4);
            result.Upgrade(UpgradeType.WarpGate);
            //result.Train(UnitTypes.SENTRY, () => Completed(UnitTypes.CYBERNETICS_CORE) > 0 && Gas() >= 100 && (Tyr.Bot.Frame >= 22.4 * 60 * 10 || Completed(UnitTypes.WARP_PRISM) == 0 || Completed(UnitTypes.WARP_PRISM_PHASING) == 1 || Count(UnitTypes.SENTRY) < 20 || !WarpPrismDrops));
            result.Train(UnitTypes.SENTRY, () => Completed(UnitTypes.CYBERNETICS_CORE) > 0 && Gas() >= 100);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.If(() => { Bot.Main.DrawText("Executing main buildlist."); return true; });
            if (NaturalWall.Wall.Count >= 4)
            {
                result.Building(UnitTypes.PYLON, Natural, NaturalWall.Wall[2].Pos, true);
                result.Building(UnitTypes.GATEWAY, Natural, NaturalWall.Wall[1].Pos, true);
            }
            else if (WallIn.Wall.Count >= 3)
            {
                result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
                result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            }
            else
            {
                result.Building(UnitTypes.PYLON, Main);
                result.If(() => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.GATEWAY, Main);
            }
            result.Building(UnitTypes.ASSIMILATOR);
            if (NaturalWall.Wall.Count >= 4)
                result.Building(UnitTypes.CYBERNETICS_CORE, Natural, NaturalWall.Wall[3].Pos, true);
            else if (WallIn.Wall.Count >= 3)
                result.Building(UnitTypes.CYBERNETICS_CORE, Main, WallIn.Wall[2].Pos, true);
            else
                result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.ASSIMILATOR, () => !DefenseMode || (Count(UnitTypes.CYBERNETICS_CORE) > 0 && Count(UnitTypes.FORGE) > 0));
            result.Building(UnitTypes.GATEWAY, () => (!DefenseMode || Count(UnitTypes.PHOTON_CANNON) > 0) && (Bot.Main.EnemyRace != Race.Zerg || Count(UnitTypes.CYBERNETICS_CORE) > 0));
            if (NaturalWall.Wall.Count >= 4)
                result.Building(UnitTypes.FORGE, Natural, NaturalWall.Wall[0].Pos, true, () => DefenseMode && Count(UnitTypes.FORGE) == 0);
            else
                result.Building(UnitTypes.FORGE, () => DefenseMode);
            if (NaturalWall.Wall.Count >= 4)
            {
                result.If(() => { Bot.Main.DrawText("Building natural cannons. " + DefenseMode);  return true; });
                result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalCannonDefensePos, 3, () => DefenseMode && !Bot.Main.buildingPlacer.CannonPlacementFailed);
                result.Building(UnitTypes.PYLON, Natural, NaturalCannonDefensePos, () => DefenseMode && !Bot.Main.buildingPlacer.CannonPlacementFailed);
                result.Building(UnitTypes.SHIELD_BATTERY, Natural, NaturalCannonDefensePos, 2, () => DefenseMode && !Bot.Main.buildingPlacer.CannonPlacementFailed);
            }
            else
            {
                result.Building(UnitTypes.PYLON, Main, RampDefensePos, () => DefenseMode);
                result.Building(UnitTypes.PHOTON_CANNON, Main, RampDefensePos, 3, () => DefenseMode);
                result.Building(UnitTypes.SHIELD_BATTERY, Main, RampDefensePos, 2, () => DefenseMode);
            }
            result.If(() => !DefenseMode || Completed(UnitTypes.SENTRY) >= 15 || (Bot.Main.EnemyRace == Race.Zerg && (Completed(UnitTypes.PHOTON_CANNON) >= 3 || Minerals() >= 600 || Bot.Main.buildingPlacer.CannonPlacementFailed)));
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.FORGE, Main, () => !DefenseMode);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            if (AntiBC)
            {
                result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos, () => !DefenseMode);
                result.Building(UnitTypes.GATEWAY, Main);
                result.Building(UnitTypes.PHOTON_CANNON, 5, () => !DefenseMode);
                result.Building(UnitTypes.SHIELD_BATTERY, 3, () => !DefenseMode);
            }
            else if (!SkipNatural)
            {
                result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos, () => NaturalWall.Wall.Count < 4 || !DefenseMode);
                result.Building(UnitTypes.GATEWAY, Main);
                result.Building(UnitTypes.PHOTON_CANNON, Natural, NaturalDefensePos, 3, () => NaturalWall.Wall.Count < 4 || !DefenseMode);
                result.Building(UnitTypes.SHIELD_BATTERY, Natural, NaturalDefensePos, 2, () => NaturalWall.Wall.Count < 4 || !DefenseMode);
                result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos, () => NaturalWall.Wall.Count < 4 || !DefenseMode);
            } else
            {
                result.Building(UnitTypes.GATEWAY, Main);
            }
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => WarpPrismDrops);
            result.If(() => Bot.Main.EnemyRace != Race.Zerg || Count(Main, UnitTypes.FORGE) > 0);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Count(UnitTypes.PROBE) >= 55 || Minerals() >= 600);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.GATEWAY, Main);
            result.If(() => Count(UnitTypes.PROBE) >= 75 || Minerals() >= 600);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.GATEWAY, Main);
            result.If(() => Bot.Main.BaseManager.Main.BaseLocation.MineralFields.Count < 8);
            result.Building(UnitTypes.NEXUS);
            if (!SkipNatural)
                result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Minerals() >= 400);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            bot.TaskManager.CombatSimulation.SimulationLength = 0;

            if (Completed(UnitTypes.WARP_PRISM) > 0)
                StopWarpPrisms = true;
            if (ProxySuspected)
            {
                bot.TargetManager.TargetAllBuildings = true;
                MassSentriesTask.Task.RequiredSize = 15;
                MassSentriesTask.Task.RetreatSize = 6;
            }
            else if (bot.EnemyRace == Race.Protoss && !Stalker.Get().DetectedPreviously)
            {
                MassSentriesTask.Task.RequiredSize = 50;
                MassSentriesTask.Task.RetreatSize = 10;
            }
            else if (bot.EnemyRace == Race.Terran && SiegeTank.Get().DetectedPreviously)
            {
                MassSentriesTask.Task.RequiredSize = 50;
                MassSentriesTask.Task.RetreatSize = 10;
            }
            else if (!WarpPrismDrops)
            {
                MassSentriesTask.Task.RequiredSize = 25;
                MassSentriesTask.Task.RetreatSize = 10;
            }
            else if (MassSentriesTask.Task.AttackSent || Completed(UnitTypes.WARP_PRISM) > 0)
            {
                MassSentriesTask.Task.RequiredSize = 20;
                MassSentriesTask.Task.RetreatSize = 10;
            }else
            {
                MassSentriesTask.Task.RequiredSize = 30;
                MassSentriesTask.Task.RetreatSize = 10;
            }

            TrainStep.WarpInLocation = null;
            if (Completed(UnitTypes.WARP_PRISM_PHASING) > 0)
            {
                foreach (Agent agent in bot.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.WARP_PRISM_PHASING)
                        continue;
                    TrainStep.WarpInLocation = SC2Util.To2D(agent.Unit.Pos);
                    break;
                }
            }

            if (Bot.Main.Frame <= 2)
                SentryWarpInTask.Task.Stopped = true;
            else if (MassSentriesTask.Task.AttackSent)
                SentryWarpInTask.Task.Stopped = false;

            bot.DrawText("SentryWarpInTask Stopped: " + SentryWarpInTask.Task.Stopped);

            DefenseTask.GroundDefenseTask.UseForceFields = true;

            if (NaturalWall.Wall.Count >= 4 && Count(UnitTypes.NEXUS) < 2)
            {
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 40;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 25;
                IdleTask.Task.OverrideTarget = NaturalDefensePos;
            }
            else
            {
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 25;
                IdleTask.Task.OverrideTarget = null;
            }

            if (WorkerScoutTask.Task.BaseCircled() && (bot.EnemyRace != Race.Zerg || !DefenseMode))
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            if (!DefenseMode && TotalEnemyCount(UnitTypes.ROACH_WARREN) > 0
                && bot.Frame <= 22.4 * 90
                && !Expanded.Get().Detected)
                DefenseMode = true;

            if (!DefenseMode && TotalEnemyCount(UnitTypes.BARRACKS) == 0
                && bot.Frame >= 22.4 * 90
                && bot.Frame <= 22.4 * 120
                && bot.EnemyRace == Race.Terran
                && !Expanded.Get().Detected)
            {
                DefenseMode = true;
                ProxySuspected = true;
            }
            if (!DefenseMode 
                && FourRax.Get().Detected && bot.Frame <= 22.4 * 90
                && !Expanded.Get().Detected)
                DefenseMode = true;
            if (!DefenseMode && TotalEnemyCount(UnitTypes.SPAWNING_POOL) > 0 
                && bot.Frame <= 22.4 * 80
                && !Expanded.Get().Detected)
                DefenseMode = true;
            if (Expanded.Get().Detected)
                DefenseMode = false;

            KillOwnUnitTask.Task.Priority = 6;
            if (Count(Main, UnitTypes.FORGE) > 0
                && Completed(UnitTypes.SENTRY) >= 18
                && NaturalWall.Wall.Count >= 4)
            {
                foreach (Agent agent in bot.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.FORGE)
                        continue;
                    if (agent.DistanceSq(NaturalWall.Wall[0].Pos) <= 4)
                    {
                        KillOwnUnitTask.Task.TargetTag = agent.Unit.Tag;
                        break;
                    }
                }
            }

            if (NaturalWall.Wall.Count >= 4)
            {
                if (Count(Main, UnitTypes.FORGE) > 0
                    && Completed(UnitTypes.SENTRY) >= 18)
                    bot.buildingPlacer.LimitBuildArea = null;
                else
                    bot.buildingPlacer.LimitBuildArea = MainAndNatural;
            }

            if (Completed(UnitTypes.SENTRY) >= 15)
                DefenseMode = false;

            ForceFieldRampTask.Task.Stopped = (!SkipNatural && !DefenseMode) || NaturalWall.Wall.Count >= 4;
            if (ForceFieldRampTask.Task.Stopped)
                ForceFieldRampTask.Task.Clear();

            if (!TyckleFightChatSent && StrategyAnalysis.WorkerRush.Get().Detected)
            {
                TyckleFightChatSent = true;
                bot.Chat("TICKLE FIGHT! :D");
            }

            if (!MessageSent)
                if (MassSentriesTask.Task.AttackSent)
                {
                    MessageSent = true;
                    bot.Chat("Prepare to be TICKLED! :D");
                }

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (MassSentriesTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
            }
        }
    }
}
