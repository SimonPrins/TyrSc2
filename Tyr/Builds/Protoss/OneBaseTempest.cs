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
    public class OneBaseTempest : Build
    {
        public bool DefendingStalker = false;
        public int RequiredSize = 1;
        private WallInCreator WallIn;
        private Point2D ShieldBatteryPos;
        private TempestController TempestController = new TempestController();

        public override string Name()
        {
            return "OneBaseTempest";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            ArmyObserverTask.Enable();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(TempestController);
            MicroControllers.Add(new StutterController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY });
                WallIn.ReserveSpace();
                ShieldBatteryPos = SC2Util.TowardCardinal(WallIn.Wall[1].Pos, Main.BaseLocation.Pos, 2);
                Bot.Main.buildingPlacer.ReservedLocation.Add(new ReservedBuilding() { Type = UnitTypes.SHIELD_BATTERY, Pos = ShieldBatteryPos });
            }
            
            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) >= 2);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 21);
            if (DefendingStalker)
                result.Train(UnitTypes.STALKER, 1, () => Count(UnitTypes.STARGATE) > 0);
            result.Train(UnitTypes.TEMPEST);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON, Main, WallIn.Wall[1].Pos, true);
            result.Building(UnitTypes.GATEWAY, Main, WallIn.Wall[0].Pos, true);
            result.If(() => Count(UnitTypes.GATEWAY) > 0);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main, WallIn.Wall[2].Pos, true);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.FORGE);
            result.Building(UnitTypes.PHOTON_CANNON, Main, MainDefensePos, () => !DefendingStalker || Count(UnitTypes.FLEET_BEACON) > 0);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.PYLON, Main);
            //result.Building(UnitTypes.SHIELD_BATTERY, Main, ShieldBatteryPos, true, () => Minerals() >= 400);
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.STARGATE, () => Count(UnitTypes.TEMPEST) > 0);
            result.Building(UnitTypes.PYLON, Main, ShieldBatteryPos, true, () => Minerals() >= 400 && Bot.Main.Frame >= 22.4 * 60 * 5);
            result.Building(UnitTypes.PHOTON_CANNON, Main, MainDefensePos, 2, () => Minerals() >= 400 && Bot.Main.Frame >= 22.4 * 60 * 5);
            result.Building(UnitTypes.SHIELD_BATTERY, Main, MainDefensePos, 2, () => Minerals() >= 400 && Bot.Main.Frame >= 22.4 * 60 * 5);

            return result;
        }

        public override void OnFrame(Bot bot)
        {

            if (!UnitTypes.CanAttackAir(UnitTypes.QUEEN)
                && bot.Frame == 10)
                bot.Chat("Omg Queens can't shoot!");
            bot.NexusAbilityManager.PriotitizedAbilities.Add(1568);
            ProxyTask.Task.EvadeEnemies = true;
            
            bot.buildingPlacer.BuildCompact = true;
            bot.TargetManager.PrefferDistant = false;
            bot.TargetManager.TargetAllBuildings = true;

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = true;
            
            TimingAttackTask.Task.RequiredSize = RequiredSize;
            TimingAttackTask.Task.RetreatSize = 0;
            TimingAttackTask.Task.UnitType = UnitTypes.TEMPEST;

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;
                
                agent.Order(Abilities.MOVE, agent.From(bot.MapAnalyzer.GetMainRamp(), 4));
            }
        }
    }
}
