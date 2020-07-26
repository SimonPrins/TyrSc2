using System;
using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.BuildingPlacement
{
    /*
     * This class is responsible for managing the placement of buildings.
     */
    public class ProxyBuildingPlacer
    {   
        public static Point2D FindPlacement(Point2D target, Point2D size, uint type)
        {
            Point2D result = findPlacementLocal(target, size, type, 20);
            return result;
        }

        private static Point2D findPlacementLocal(Point2D target, Point2D size, uint type, int maxDist)
        {
            target = SC2Util.Point((int)target.X + 0.5f * (size.X % 2f), (int)target.Y + 0.5f * (size.Y % 2f));

            for (int range = 0; range < maxDist; range++)
            {
                for (int x = -range; x <= range; x++)
                {
                    if (CheckPlacement(SC2Util.Point(target.X + x, target.Y - range), size, type, null, false))
                        return SC2Util.Point(target.X + x, target.Y - range);
                    if (CheckPlacement(SC2Util.Point(target.X + x, target.Y + range), size, type, null, false))
                        return SC2Util.Point(target.X + x, target.Y + range);
                }
                for (int y = -range + 1; y <= range - 1; y++)
                {
                    if (CheckPlacement(SC2Util.Point(target.X + range, target.Y + y), size, type, null, false))
                        return SC2Util.Point(target.X + range, target.Y + y);
                    if (CheckPlacement(SC2Util.Point(target.X - range, target.Y + y), size, type, null, false))
                        return SC2Util.Point(target.X - range, target.Y + y);
                }
            }
            // No placement found.
            return null;
        }

        public static bool CheckPlacement(Point2D location, Point2D size, uint type, BuildRequest skipRequest, bool buildingsOnly)
        {
            // Check if the building can be placed on this position of the map.
            for (float x = -size.X / 2f; x < size.X / 2f + 0.1f; x++)
                for (float y = -size.Y / 2f; y < size.Y / 2f + 0.1f; y++)
                    if (!SC2Util.GetTilePlacable((int)Math.Round(location.X + x), (int)Math.Round(location.Y + y)))
                        return false;

            if (CanHaveAddOn(type))
            {
                if (!SC2Util.GetTilePlacable((int)Math.Round(location.X + 2f), (int)Math.Round(location.Y - 1)))
                    return false;
                if (!SC2Util.GetTilePlacable((int)Math.Round(location.X + 2f), (int)Math.Round(location.Y)))
                    return false;
                if (!SC2Util.GetTilePlacable((int)Math.Round(location.X + 3f), (int)Math.Round(location.Y - 1)))
                    return false;
                if (!SC2Util.GetTilePlacable((int)Math.Round(location.X + 3f), (int)Math.Round(location.Y)))
                    return false;
            }
            
            foreach (Unit unit in Bot.Main.Observation.Observation.RawData.Units)
                if (!unit.IsFlying && (unit.Owner != Bot.Main.PlayerId || unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED || UnitTypes.BuildingTypes.Contains(unit.UnitType)) && !CheckDistance(location, type, SC2Util.To2D(unit.Pos), unit.UnitType, buildingsOnly))
                    return false;

            foreach (BuildRequest request in ConstructionTask.Task.UnassignedRequests)
                if (request != skipRequest && !CheckDistance(location, type, request.Pos, request.Type, buildingsOnly))
                    return false;

            foreach (BuildRequest request in ConstructionTask.Task.BuildRequests)
                if (request != skipRequest && !CheckDistance(location, type, request.Pos, request.Type, buildingsOnly))
                    return false;

            if (Bot.Main.MyRace == Race.Zerg && type != UnitTypes.HATCHERY && type != UnitTypes.EXTRACTOR)
            {
                BoolGrid creep = new ImageBoolGrid(Bot.Main.Observation.Observation.RawData.MapState.Creep, 1);
                for (float dx = -size.X / 2f; dx <= size.X / 2f + 0.01f; dx++)
                    for (float dy = -size.Y / 2f; dy <= size.Y / 2f + 0.01f; dy++)
                        if (!creep[(int)(location.X + dx), (int)(location.Y + dy)])
                            return false;
            }
            if (Bot.Main.MyRace != Race.Zerg)
            {
                BoolGrid creep = new ImageBoolGrid(Bot.Main.Observation.Observation.RawData.MapState.Creep, 1);
                for (float dx = -size.X / 2f; dx <= size.X / 2f + 0.01f; dx++)
                    for (float dy = -size.Y / 2f; dy <= size.Y / 2f + 0.01f; dy++)
                        if (creep[(int)(location.X + dx), (int)(location.Y + dy)])
                            return false;
            }

            if (type != UnitTypes.PYLON && Bot.Main.MyRace == Race.Protoss)
            {
                foreach (Unit unit in Bot.Main.Observation.Observation.RawData.Units)
                {
                    if (unit.UnitType != UnitTypes.PYLON || unit.BuildProgress < 1)
                        continue;

                    if (Bot.Main.MapAnalyzer.MapHeight((int)unit.Pos.X, (int)unit.Pos.Y) < Bot.Main.MapAnalyzer.MapHeight((int)location.X, (int)location.Y))
                        continue;

                    if (location.X - size.X / 2f >= unit.Pos.X - 6 && location.X + size.X / 2f <= unit.Pos.X + 6
                        && location.Y - size.Y / 2f >= unit.Pos.Y - 7 && location.Y + size.Y / 2f <= unit.Pos.Y + 7)
                    {
                        if (SC2Util.DistanceGrid(unit.Pos, location) <= 10 - size.X / 2f - size.Y / 2f)
                            return true;
                    }
                }
                return false;
            }

            return true;
        }

        public static bool CheckDistance(Point2D location, uint buildingType, Point2D unitPos, uint unitType, bool buildingsOnly)
        {
            if (buildingsOnly && !BuildingType.LookUp.ContainsKey(unitType))
                return true;

            if (unitType == UnitTypes.ADEPT_PHASE_SHIFT
                || unitType == UnitTypes.KD8_CHARGE)
                return true;
            
            if (UnitTypes.CombatUnitTypes.Contains(unitType))
                return SC2Util.DistanceGrid(unitPos, location) > 1;
            if (UnitTypes.WorkerTypes.Contains(unitType))
                return SC2Util.DistanceGrid(unitPos, location) > 3;
            
            return CheckDistanceClose(location, buildingType, unitPos, unitType);
        }

        public static bool CheckDistanceClose(Point2D location, uint buildingType, Point2D unitPos, uint unitType)
        {
            float dx = BuildingType.LookUp[buildingType].Size.X / 2f + (BuildingType.LookUp.ContainsKey(unitType) ? BuildingType.LookUp[unitType].Size.X / 2f: 1f) - 0.1f;
            float dy = BuildingType.LookUp[buildingType].Size.Y / 2f + (BuildingType.LookUp.ContainsKey(unitType) ? BuildingType.LookUp[unitType].Size.Y / 2f : 1f) - 0.1f;
            
            return Math.Abs(location.X - unitPos.X) >= dx || Math.Abs(location.Y - unitPos.Y) >= dy;
        }

        public static bool CanHaveAddOn(uint type)
        {
            return type == UnitTypes.BARRACKS || type == UnitTypes.FACTORY || type == UnitTypes.STARPORT;
        }
    }
}
