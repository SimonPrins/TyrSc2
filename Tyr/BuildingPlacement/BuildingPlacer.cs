using System;
using System.Collections.Generic;
using System.IO;
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
    public class BuildingPlacer
    {
        private Bot bot;
        private bool PylonsFilled = false;
        public bool BuildInsideMainOnly = false;
        public bool SpreadCannons;
        public bool BuildCompact = false;

        public List<ReservedBuilding> ReservedLocation = new List<ReservedBuilding>();
        public BoolGrid LimitBuildArea = null;

        private bool DetailedLogs = false;
        public bool CannonPlacementFailed = false;

        public BuildingPlacer(Bot bot)
        {
            this.bot = bot;
        }

        public Point2D FindPlacement(Point2D target, Point2D size, uint type)
        {
            if (Bot.Main.MyRace == Race.Terran)
            {
                if (type != UnitTypes.REFINERY
                    && type != UnitTypes.COMMAND_CENTER
                    && type != UnitTypes.MISSILE_TURRET)
                    return TerranBuildingPlacement.FindPlacement(target, size, type);
            }
            else if (Bot.Main.MyRace == Race.Protoss)
            {
                if (type != UnitTypes.ASSIMILATOR
                    && type != UnitTypes.NEXUS
                    && type != UnitTypes.PHOTON_CANNON
                    && type != UnitTypes.SHIELD_BATTERY
                    && Bot.Main.MapAnalyzer.StartArea[target]
                    && !BuildCompact
                    && SC2Util.DistanceSq(target, Bot.Main.MapAnalyzer.StartLocation) <= 25 * 25)
                    return ProtossBuildingPlacement.FindPlacement(target, size, type);
            }
            Point2D result = findPlacementLocal(target, size, type, 20);
            if (type == UnitTypes.PYLON)
                PylonsFilled = result == null;
            return result;
        }

        public Point2D FindPlacement(Point2D target, Point2D size, uint type, int maxDist)
        {
            DetailedLogs = false;
            Point2D result = findPlacementLocal(target, size, type, maxDist);
            if (type == UnitTypes.PYLON)
                PylonsFilled = result == null;
            if (result == null && type == 66)
            {
                DebugUtil.WriteLine("No placement found for " + type + " around " + target);
                if (type == 66)
                    CannonPlacementFailed = true;

            }
            return result;
        }

        private Point2D findPlacementLocal(Point2D target, Point2D size, uint type, int maxDist)
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

        public Point2D FindPlacementLocal(Point2D target, Point2D size, uint type, int maxDist, Point2D closeTo, float withinDist)
        {
            target = SC2Util.Point((int)target.X + 0.5f * (size.X % 2f), (int)target.Y + 0.5f * (size.Y % 2f));

            for (int range = 0; range < maxDist; range++)
            {
                for (int x = -range; x <= range; x++)
                {
                    if (checkPlacement(SC2Util.Point(target.X + x, target.Y - range), size, type, closeTo, withinDist))
                        return SC2Util.Point(target.X + x, target.Y - range);
                    if (checkPlacement(SC2Util.Point(target.X + x, target.Y + range), size, type, closeTo, withinDist))
                        return SC2Util.Point(target.X + x, target.Y + range);
                }
                for (int y = -range + 1; y <= range - 1; y++)
                {
                    if (checkPlacement(SC2Util.Point(target.X + range, target.Y + y), size, type, closeTo, withinDist))
                        return SC2Util.Point(target.X + range, target.Y + y);
                    if (checkPlacement(SC2Util.Point(target.X - range, target.Y + y), size, type, closeTo, withinDist))
                        return SC2Util.Point(target.X - range, target.Y + y);
                }
            }
            // No placement found.
            return null;
        }

        private bool checkPlacement(Point2D location, Point2D size, uint type, Point2D closeTo, float withinDist)
        {
            if (SC2Util.DistanceSq(location, closeTo) >= withinDist * withinDist)
                return false;
            return CheckPlacement(location, size, type, null, false);
        }

        public bool CheckPlacement(Point2D location, Point2D size, uint type, BuildRequest skipRequest, bool buildingsOnly)
        {
            // Check if the building can be placed on this position of the map.
            for (float x = -size.X / 2f; x < size.X / 2f + 0.1f; x++)
                for (float y = -size.Y / 2f; y < size.Y / 2f + 0.1f; y++)
                {
                    if (!SC2Util.GetTilePlacable((int)Math.Round(location.X + x), (int)Math.Round(location.Y + y)))
                    {
                        if (DetailedLogs)
                            FileUtil.Debug("Tile not placable " + (location.X + x) + ", " + (location.Y + y) + " for " + location);
                        return false;
                    }

                    if (LimitBuildArea != null
                        && !LimitBuildArea[(int)Math.Round(location.X + x), (int)Math.Round(location.Y + y)])
                    {
                        if (DetailedLogs)
                            FileUtil.Debug("Not in build area " + (location.X + x) + ", " + (location.Y + y) + " for " + location);
                        return false;
                    }
                }

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

            if (BuildInsideMainOnly)
                for (float x = -size.X / 2f; x < size.X / 2f + 0.1f; x++)
                    for (float y = -size.Y / 2f; y < size.Y / 2f + 0.1f; y++)
                        if (!Bot.Main.MapAnalyzer.StartArea[(int)Math.Round(location.X + x), (int)Math.Round(location.Y + y)])
                            return false;

            if (!UnitTypes.ResourceCenters.Contains(type))
            {
                float baseDistance = (type == UnitTypes.MISSILE_TURRET || type == UnitTypes.SPORE_CRAWLER || type == UnitTypes.PYLON) ? 3f : 5f;
                foreach (Base b in Bot.Main.BaseManager.Bases)
                {
                    if (Math.Abs(b.BaseLocation.Pos.X - location.X) < baseDistance
                        && Math.Abs(b.BaseLocation.Pos.Y - location.Y) < baseDistance)
                        return false;
                    
                    foreach (MineralField mineral in b.BaseLocation.MineralFields)
                    {
                        Point2D halfWay = SC2Util.Point((mineral.Pos.X + b.BaseLocation.Pos.X) / 2f, (mineral.Pos.Y + b.BaseLocation.Pos.Y) / 2f);
                        if (SC2Util.DistanceSq(halfWay, location) <= 4 * 4)
                            return false;
                    }
                    foreach (Gas gas in b.BaseLocation.Gasses)
                    {
                        Point2D halfWay = SC2Util.Point((gas.Pos.X + b.BaseLocation.Pos.X) / 2f, (gas.Pos.Y + b.BaseLocation.Pos.Y) / 2f);
                        if (SC2Util.DistanceSq(halfWay, location) <= 4 * 4)
                            return false;
                    }
                }
            }

            foreach (Unit unit in bot.Observation.Observation.RawData.Units)
                if (!unit.IsFlying
                    && (UnitTypes.BuildingTypes.Contains(unit.UnitType) || unit.Alliance != Alliance.Self)
                    && !CheckDistance(location, type, SC2Util.To2D(unit.Pos), unit.UnitType, buildingsOnly))
                {
                    if (DetailedLogs)
                        FileUtil.Debug("Unit close for " + location);
                    return false;
                }

            foreach (BuildRequest request in ConstructionTask.Task.UnassignedRequests)
                if (request != skipRequest && !CheckDistance(location, type, request.Pos, request.Type, buildingsOnly))
                    return false;

            foreach (BuildRequest request in ConstructionTask.Task.BuildRequests)
                if (request != skipRequest && !CheckDistance(location, type, request.Pos, request.Type, buildingsOnly))
                    return false;

            foreach (ReservedBuilding building in Bot.Main.buildingPlacer.ReservedLocation)
                if (!CheckDistClose(location.X - 1.5f, location.Y - 1.5f, location.X + 1.5f, location.Y + 1.5f, building.Pos, building.Type))
                    return false;

            if (Bot.Main.MyRace == Race.Zerg && type != UnitTypes.HATCHERY && type != UnitTypes.EXTRACTOR)
            {
                BoolGrid creep = new ImageBoolGrid(bot.Observation.Observation.RawData.MapState.Creep, 1);
                for (float dx = -size.X / 2f; dx <= size.X / 2f + 0.01f; dx++)
                    for (float dy = -size.Y / 2f; dy <= size.Y / 2f + 0.01f; dy++)
                        if (!creep[(int)(location.X + dx), (int)(location.Y + dy)])
                            return false;
            }
            if (Bot.Main.MyRace != Race.Zerg)
            {
                BoolGrid creep = new ImageBoolGrid(bot.Observation.Observation.RawData.MapState.Creep, 1);
                for (float dx = -size.X / 2f; dx <= size.X / 2f + 0.01f; dx++)
                    for (float dy = -size.Y / 2f; dy <= size.Y / 2f + 0.01f; dy++)
                        if (creep[(int)(location.X + dx), (int)(location.Y + dy)])
                            return false;
            }

            if (type == UnitTypes.PYLON)
            {
                foreach (Agent agent in Bot.Main.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.PYLON)
                        continue;

                    // Pylons should not be placed corner to corner as this prevents units from walking between them.
                    if (Math.Abs(agent.Unit.Pos.X - location.X) == 2
                        && Math.Abs(agent.Unit.Pos.Y - location.Y) == 2)
                        return false;
                }
            }

            if (type != UnitTypes.PYLON && bot.MyRace == Race.Protoss)
            {
                foreach (Unit unit in bot.Observation.Observation.RawData.Units)
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
                if (DetailedLogs)
                    FileUtil.Debug("No Pylon power " + location);
                return false;
            }

            return true;
        }

        public bool CheckDistance(Point2D location, uint buildingType, Point2D unitPos, uint unitType, bool buildingsOnly)
        {
            if (buildingsOnly && !BuildingType.LookUp.ContainsKey(unitType))
                return true;

            if (unitType == UnitTypes.ADEPT_PHASE_SHIFT
                || unitType == UnitTypes.KD8_CHARGE)
                return true;

            if (buildingType == UnitTypes.MISSILE_TURRET
                && unitType == UnitTypes.MISSILE_TURRET)
                return SC2Util.DistanceSq(location, unitPos) >= 6 * 6;

            if (UnitTypes.WorkerTypes.Contains(unitType))
            {
                if (buildingType == UnitTypes.MISSILE_TURRET)
                    return SC2Util.DistanceSq(location, unitPos) >= 2 * 2;
                return true;
            }

            if (UnitTypes.CombatUnitTypes.Contains(unitType))
                return SC2Util.DistanceGrid(unitPos, location) > 1;
            if (UnitTypes.WorkerTypes.Contains(unitType))
                return SC2Util.DistanceGrid(unitPos, location) > 3;

            if (BuildCompact
                    || SC2Util.DistanceSq(location, Bot.Main.MapAnalyzer.StartLocation) > 25 * 25)
                return CheckDistanceClose(location, buildingType, unitPos, unitType);
            if ((buildingType == UnitTypes.PHOTON_CANNON && !SpreadCannons)
                || buildingType == UnitTypes.SHIELD_BATTERY
                || buildingType == UnitTypes.DARK_SHRINE
                || buildingType == UnitTypes.SPINE_CRAWLER
                || (buildingType == UnitTypes.SPORE_CRAWLER && !SpreadCannons)
                || buildingType == UnitTypes.SUPPLY_DEPOT
                || buildingType == UnitTypes.MISSILE_TURRET
                || unitType == UnitTypes.SUPPLY_DEPOT
                || unitType == UnitTypes.SUPPLY_DEPOT_LOWERED
                || buildingType == UnitTypes.CREEP_TUMOR
                || (buildingType == UnitTypes.PYLON && PylonsFilled))
            {
                if (CanHaveAddOn(buildingType))
                {
                    if (!CheckDistanceClose(SC2Util.Point(location.X + 2.5f, location.Y - 0.5f), UnitTypes.REACTOR, unitPos, unitType))
                        return false;
                }
                if (CanHaveAddOn(unitType))
                {
                    if (!CheckDistanceClose(location, buildingType, SC2Util.Point(unitPos.X + 2.5f, unitPos.Y - 0.5f), UnitTypes.REACTOR))
                        return false;
                }
                return CheckDistanceClose(location, buildingType, unitPos, unitType);
            }
            int minDist = 5;
            if (buildingType == UnitTypes.PYLON && !PylonsFilled)
                minDist = 7;
            if (buildingType == UnitTypes.PHOTON_CANNON && unitType == UnitTypes.PHOTON_CANNON)
                minDist = 10;
            else if (buildingType == UnitTypes.PHOTON_CANNON)
                minDist = 3;
            return SC2Util.DistanceGrid(unitPos, location) > minDist;
        }

        public bool CheckDistanceClose(Point2D location, uint buildingType, Point2D unitPos, uint unitType)
        {
            float dx = BuildingType.LookUp[buildingType].Size.X / 2f + (BuildingType.LookUp.ContainsKey(unitType) ? BuildingType.LookUp[unitType].Size.X / 2f : 1f) - 0.1f;
            float dy = BuildingType.LookUp[buildingType].Size.Y / 2f + (BuildingType.LookUp.ContainsKey(unitType) ? BuildingType.LookUp[unitType].Size.Y / 2f : 1f) - 0.1f;
            
            return Math.Abs(location.X - unitPos.X) >= dx || Math.Abs(location.Y - unitPos.Y) >= dy;
        }

        public static bool CheckDistClose(float x1, float y1, float x2, float y2, Point2D unitPos, uint unitType)
        {
            float midX = (x1 + x2) * 0.5f;
            float radX = (x2 - x1) * 0.5f;
            float midY = (y1 + y2) * 0.5f;
            float radY = (y2 - y1) * 0.5f;

            float dx = radX + (BuildingType.LookUp.ContainsKey(unitType) ? BuildingType.LookUp[unitType].Size.X / 2f : 1f) - 0.1f;
            float dy = radY + (BuildingType.LookUp.ContainsKey(unitType) ? BuildingType.LookUp[unitType].Size.Y / 2f : 1f) - 0.1f;

            return Math.Abs(midX - unitPos.X) >= dx || Math.Abs(midY - unitPos.Y) >= dy;

        }

        public static bool CheckDistDebug(float x1, float y1, float x2, float y2, Point2D unitPos, uint unitType)
        {
            float midX = (x1 + x2) * 0.5f;
            float radX = (x2 - x1) * 0.5f;
            float midY = (y1 + y2) * 0.5f;
            float radY = (y2 - y1) * 0.5f;

            float dx = radX + (BuildingType.LookUp.ContainsKey(unitType) ? BuildingType.LookUp[unitType].Size.X / 2f : 1f) - 0.1f;
            float dy = radY + (BuildingType.LookUp.ContainsKey(unitType) ? BuildingType.LookUp[unitType].Size.Y / 2f : 1f) - 0.1f;


            return Math.Abs(midX - unitPos.X) >= dx || Math.Abs(midY - unitPos.Y) >= dy;

        }

        public static bool CanHaveAddOn(uint type)
        {
            return type == UnitTypes.BARRACKS || type == UnitTypes.FACTORY || type == UnitTypes.STARPORT;
        }

        public void BuildInsideWall(WallInCreator wallIn)
        {
            BoolGrid pathable = Bot.Main.MapAnalyzer.Pathable;
            ArrayBoolGrid walledPathable = new ArrayBoolGrid(pathable.Width(), pathable.Height());
            for (int x = 0; x < pathable.Width(); x++)
                for (int y = 0; y < pathable.Height(); y++)
                {
                    if (!pathable[x, y])
                    {
                        walledPathable[x, y] = false;
                        continue;
                    }
                    bool blocked = false;
                    foreach (WallBuilding building in wallIn.Wall)
                    {
                        if (Math.Abs(building.Pos.X - x) <= building.Size.X / 2f
                            && Math.Abs(building.Pos.Y - y) <= building.Size.Y / 2f)
                        {
                            blocked = true;
                            break;
                        }
                    }
                    walledPathable[x, y] = !blocked;
                }
            LimitBuildArea = walledPathable.GetConnected(SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation));

            /*
            DrawGrid(pathable, "Pathable");
            DrawGrid(walledPathable, "WalledPathable");
            DrawGrid(LimitBuildArea, "BuildArea");
            */
        }

        /*
        private void DrawGrid(BoolGrid grid, string name)
        {
            if (!Tyr.Debug)
                return;

            int width = Tyr.Bot.GameInfo.StartRaw.PathingGrid.Size.X;
            int height = Tyr.Bot.GameInfo.StartRaw.PathingGrid.Size.Y;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y])
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.White);
                    else
                        bmp.SetPixel(x, height - 1 - y, System.Drawing.Color.Black);
                }

            bmp.Save(Directory.GetCurrentDirectory() + "/data/" + name + ".png");
        }
        */
    }
}
