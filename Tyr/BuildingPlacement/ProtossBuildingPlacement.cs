using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.BuildingPlacement
{
    /*
     * This class is used to find build locations for Protoss structures.
     */
    public class ProtossBuildingPlacement
    {
        private static List<Agent> Pylons = new List<Agent>();
        private static int UpdateFrame = 0;

        public static Point2D FindPlacement(Point2D target, Point2D size, uint type)
        {
            Point2D reference = SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation);

            if (type == UnitTypes.PYLON)
                return FindPlacementPylon(reference, target, size, type);
            else
                return FindPlacementProduction(reference, target, size, type);
        }

        public static Point2D FindPlacementPylon(Point2D reference, Point2D target, Point2D size, uint type)
        {
            UpdatePylonList();
            Point2D result = null;
            float score = 1000000;
            for (float x = reference.X - 34.5f; x <= reference.X + 28; x += 10)
                for (float y = reference.Y - 30.5f; y <= reference.Y + 30; y += 2)
                {
                    if (!RectBuildable(x - 1f, y - 1f, x + 1f, y + 1f))
                        continue;

                    bool adjacentPylon = false;
                    foreach (Agent pylon in Pylons)
                    {
                        // Pylons should not be placed corner to corner as this prevents units from walking between them.
                        if (Math.Abs(pylon.Unit.Pos.X - x) == 2
                            && Math.Abs(pylon.Unit.Pos.Y - y) == 2)
                        {
                            adjacentPylon = true;
                            break;
                        }
                    }
                    if (adjacentPylon)
                        continue;

                    float newScore = 0;
                    if (Pylons.Count == 0)
                    {
                        if (!Tyr.Bot.MapAnalyzer.StartArea[(int)x, (int)y])
                            continue;

                        if (Tyr.Bot.MapAnalyzer.WallDistances[(int)x, (int)y] < 8
                            || SC2Util.DistanceSq(new Point2D() { X = x, Y = y }, Tyr.Bot.MapAnalyzer.StartLocation) <= 7 * 7)
                            newScore += 100000;
                        if (newScore > score)
                            continue;
                    }
                    if (Tyr.Bot.MapAnalyzer.StartArea[(int)x, (int)y]
                        && Tyr.Bot.MapAnalyzer.WallDistances[(int)x, (int)y] < 4)
                        newScore += 1000;

                    float closestPylonDist = 8 * 8;
                    foreach (Agent pylon in Pylons)
                        closestPylonDist = Math.Min(closestPylonDist, SC2Util.DistanceSq(pylon.Unit.Pos, SC2Util.Point(x, y)));

                    newScore += SC2Util.DistanceSq(target, SC2Util.Point(x, y)) - closestPylonDist * 4;

                    foreach (Unit gas in Tyr.Bot.Observation.Observation.RawData.Units)
                    {
                        if (!UnitTypes.GasGeysers.Contains(gas.UnitType))
                            continue;

                        if (SC2Util.DistanceSq(gas.Pos, SC2Util.Point(x, y)) <= 5 * 5)
                            newScore += 10000;
                    }
                    
                    if (newScore > score)
                        continue;

                    bool blocked = false;
                    foreach (Unit unit in Tyr.Bot.Observation.Observation.RawData.Units)
                        if (!BuildingPlacer.CheckDistClose(x - 1f, y - 1f, x + 1f, y + 1f, SC2Util.To2D(unit.Pos), unit.UnitType))
                        {
                            blocked = true;
                            break;
                        }

                    foreach (BuildRequest request in ConstructionTask.Task.UnassignedRequests)
                        if (!BuildingPlacer.CheckDistClose(x - 1f, y - 1f, x + 1f, y + 1f, request.Pos, request.Type))
                        {
                            blocked = true;
                            break;
                        }

                    foreach (BuildRequest request in ConstructionTask.Task.BuildRequests)
                        if (!BuildingPlacer.CheckDistClose(x - 1f, y - 1f, x + 1f, y + 1f, request.Pos, request.Type))
                        {
                            blocked = true;
                            break;
                        }

                    foreach (Base b in Tyr.Bot.BaseManager.Bases)
                        if (!BuildingPlacer.CheckDistClose(x - 1f, y - 1f, x + 1f, y + 1f, b.BaseLocation.Pos, UnitTypes.NEXUS))
                        {
                            blocked = true;
                            break;
                        }

                    if (blocked)
                        continue;

                    score = newScore;
                    result = SC2Util.Point(x, y);
                }
            return result;
        }
        
        public static Point2D FindPlacementProduction(Point2D reference, Point2D target, Point2D size, uint type)
        {
            UpdatePylonList();
            Point2D result = null;
            float distance = 1000000;
            bool secondBuilding = false;
            for (float x = reference.X - 38f; x <= reference.X + 28; x += secondBuilding ? 7f : 3f)
            {
                for (float y = reference.Y - 30f; y <= reference.Y + 30f; y += 3f)
                {
                    float newDist = SC2Util.DistanceSq(target, SC2Util.Point(x, y));

                    if (newDist > distance)
                        continue;

                    if (!RectBuildable(x - 2.5f, y - 2.5f, x + 2.5f, y + 2.5f))
                        continue;

                    bool blocked = false;
                    foreach (Unit unit in Tyr.Bot.Observation.Observation.RawData.Units)
                        if (!BuildingPlacer.CheckDistClose(x - 1.5f, y - 1.5f, x + 1.5f, y + 1.5f, SC2Util.To2D(unit.Pos), unit.UnitType))
                        {
                            blocked = true;
                            break;
                        }
                    foreach (ReservedBuilding building in Tyr.Bot.buildingPlacer.ReservedLocation)
                        if (!BuildingPlacer.CheckDistClose(x - 1.5f, y - 1.5f, x + 1.5f, y + 1.5f, building.Pos, building.Type))
                        {
                            blocked = true;
                            break;
                        }

                    foreach (Base b in Tyr.Bot.BaseManager.Bases)
                        if (!BuildingPlacer.CheckDistClose(x - 1.5f, y - 1.5f, x + 1.5f, y + 1.5f, b.BaseLocation.Pos, UnitTypes.NEXUS))
                        {
                            blocked = true;
                            break;
                        }

                    if (blocked)
                        continue;

                    if (SC2Util.DistanceSq(new Point2D() { X = 41.5f, Y = 131.5f }, new Point2D() { X = x, Y = y}) <= 3 * 3)
                    {
                        BuildingPlacer.CheckDistDebug(x - 1.5f, y - 1.5f, x + 1.5f, y + 1.5f, new Point2D() { X = 39.5f, Y = 129.5f }, UnitTypes.GATEWAY);
                    }

                    bool hasPower = false;
                    foreach (Agent pylon in Pylons)
                    {
                        if (pylon.Unit.BuildProgress < (Tyr.Bot.Build.Completed(UnitTypes.GATEWAY) == 0 ? 0.3 : 0.70))
                            continue;

                        if (Tyr.Bot.MapAnalyzer.MapHeight((int)pylon.Unit.Pos.X, (int)pylon.Unit.Pos.Y) < Tyr.Bot.MapAnalyzer.MapHeight((int)x, (int)y))
                            continue;

                        if (IsBuildingInPowerField(new Point2D() { X = x, Y = y }, size, SC2Util.To2D(pylon.Unit.Pos)))
                        {
                            hasPower = true;
                            break;
                        }
                    }
                    if (!hasPower)
                        continue;

                    distance = newDist;
                    result = SC2Util.Point(x, y);
                }
                secondBuilding = !secondBuilding;
            }

            return result;
        }

        public static void UpdatePylonList()
        {
            if (Tyr.Bot.Frame == UpdateFrame)
                return;
            UpdateFrame = Tyr.Bot.Frame;

            Pylons = new List<Agent>();
            foreach (Agent agent in Tyr.Bot.Units())
                if (agent.Unit.UnitType == UnitTypes.PYLON)
                    Pylons.Add(agent);
        }

        public static bool IsBuildingInPowerField(Point2D pos, Point2D size, Point2D pylonPos)
        {
            return pos.X - size.X / 2f >= pylonPos.X - 7 && pos.X + size.X / 2f <= pylonPos.X + 7
                && pos.Y - size.Y / 2f >= pylonPos.Y - 7 && pos.Y + size.Y / 2f <= pylonPos.Y + 7
                && SC2Util.DistanceGrid(pylonPos, pos) <= 10 - size.X / 2f - size.Y / 2f;
        }

        public static bool RectBuildable(float x1, float y1, float x2, float y2)
        {
            BoolGrid creep = new ImageBoolGrid(Tyr.Bot.Observation.Observation.RawData.MapState.Creep, 1);
            for (float x = x1; x <= x2; x++)
                for (float y = y1; y <= y2; y++)
                {
                    if (!SC2Util.GetTilePlacable((int)x, (int)y) || creep[(int)(x), (int)(y)])
                    {
                        return false;
                    }
                    if (Tyr.Bot.buildingPlacer.LimitBuildArea != null
                        && !Tyr.Bot.buildingPlacer.LimitBuildArea[(int)x, (int)y])
                        return false;
                }
            return true;
        }
    }
}
