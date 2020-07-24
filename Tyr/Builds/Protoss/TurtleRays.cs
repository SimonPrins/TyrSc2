﻿using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class TurtleRays : Build
    {
        private WallInCreator WallIn;
        private Point2D CannonPos;

        public override string Name()
        {
            return "TurtleRays";
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

        public override void OnStart(Bot tyr)
        {
            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.Create(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY });
                WallIn.ReserveSpace();
            }

            Base third = null;
            float dist = 1000000;
            foreach (Base b in tyr.BaseManager.Bases)
            {
                if (b == Main
                    || b == Natural)
                    continue;
                float newDist = SC2Util.DistanceSq(b.BaseLocation.Pos, Main.BaseLocation.Pos);
                if (newDist > dist)
                    continue;
                dist = newDist;
                third = b;
            }
            CannonPos = new PotentialHelper(tyr.MapAnalyzer.StartLocation, 18).To(third.BaseLocation.Pos).Get();
            
            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) >= 2);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 21);
            result.Train(UnitTypes.VOID_RAY);

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
            result.Building(UnitTypes.PHOTON_CANNON, Main, new PotentialHelper(WallIn.Wall[1].Pos, 2).To(MainDefensePos).Get());
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.PYLON, Main, CannonPos);
            //result.Building(UnitTypes.SHIELD_BATTERY, Main, ShieldBatteryPos, true, () => Minerals() >= 400);
            result.Building(UnitTypes.STARGATE, () => Count(UnitTypes.VOID_RAY) > 0);
            result.Building(UnitTypes.PHOTON_CANNON, Main, CannonPos, 2, () => Count(UnitTypes.VOID_RAY) > 0);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(1568);
            ProxyTask.Task.EvadeEnemies = true;
            
            tyr.buildingPlacer.BuildCompact = true;
            tyr.TargetManager.PrefferDistant = false;
            tyr.TargetManager.TargetAllBuildings = true;

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.Stopped = true;
            
            TimingAttackTask.Task.RequiredSize = 5;
            TimingAttackTask.Task.RetreatSize = 0;
            TimingAttackTask.Task.UnitType = UnitTypes.VOID_RAY;
        }
    }
}
