﻿using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.BuildingPlacement;
using SC2Sharp.Tasks;
using SC2Sharp.Util;
using static SC2Sharp.Builds.BuildLists.ConditionalStep;

namespace SC2Sharp.Builds.BuildLists
{
    public class TrainStep : BuildStep
    {
        public uint UnitType;
        public int Number = 1000000;
        public Test Condition = () => { return true; };


        public static Point2D WarpInLocation = null;
        public static Point2D LastWarpInLocation = null;
        public static int LastWarpInFrame = 0;

        public TrainStep(uint unitType)
        {
            UnitType = unitType;
        }

        public TrainStep(uint unitType, int number)
        {
            UnitType = unitType;
            Number = number;
        }

        public TrainStep(uint unitType, Test condition)
        {
            UnitType = unitType;
            Condition = condition;
        }

        public TrainStep(uint unitType, int number, Test condition)
        {
            UnitType = unitType;
            Number = number;
            Condition = condition;
        }

        public override string ToString()
        {
            if (!UnitTypes.LookUp.ContainsKey(UnitType))
                throw new System.Exception("Unable to find building with type: " + UnitType  + ". Please add it to the BuildingType class.");
            return "Train " + UnitTypes.LookUp[UnitType].Name + " " + Number;
        }

        public StepResult Perform(BuildListState state)
        {
            if (!Condition.Invoke())
                return new NextItem();

            if (UnitTypes.LookUp[UnitType].TechRequirement != 0
                && Bot.Main.UnitManager.Completed(UnitTypes.LookUp[UnitType].TechRequirement) == 0
                && UnitTypes.LookUp[UnitType].TechRequirement != UnitTypes.HATCHERY
                && UnitTypes.LookUp[UnitType].TechRequirement != UnitTypes.TECH_LAB)
            {
                Bot.Main.DrawText("Tech requirement not met to train " + UnitTypes.LookUp[UnitType].Name + " requires: " + UnitTypes.LookUp[UnitType].TechRequirement);
                return new NextItem();
            }

            int alreadyTrained = Bot.Main.UnitManager.Count(UnitType) + state.GetTraining(UnitType);
            TrainingType trainType = TrainingType.LookUp[UnitType];
            foreach (Agent agent in ProductionTask.Task.Units)
            {
                if (Build.FoodLeft() < trainType.Food)
                {
                    Bot.Main.DrawText("Not enough food to train " + UnitTypes.LookUp[UnitType].Name);
                    break;
                }
                if (alreadyTrained >= Number)
                    break;

                if (agent.Unit.BuildProgress < 0.99)
                    continue;

                if (trainType.IsAddOn && agent.GetAddOn() != null)
                    continue;

                if (!trainType.ProducingUnits.Contains(agent.Unit.UnitType))
                    continue;

                if (agent.Unit.Orders != null && agent.Unit.Orders.Count >= 2)
                    continue;

                if (agent.CurrentAbility() != 0 && (agent.GetAddOn() == null || !IsReactor(agent.GetAddOn().Unit.UnitType)) )
                    continue;

                if (agent.Command != null)
                    continue;

                if (Bot.Main.Frame - agent.LastOrderFrame < 5)
                    continue;

                if (agent.Unit.UnitType == UnitTypes.GATEWAY && UpgradeType.LookUp[UpgradeType.WarpGate].Done())
                    continue;

                Bot.Main.ReservedGas += trainType.Gas;
                Bot.Main.ReservedMinerals += trainType.Minerals;

                if (Bot.Main.Build.Minerals() < 0)
                    return new NextList();
                if (Bot.Main.Build.Gas() < 0)
                    return new NextItem();

                if (agent.Unit.UnitType == UnitTypes.WARP_GATE)
                {
                    bool success = WarpIn(agent, trainType);
                    if (!success)
                        continue;
                }
                else
                {
                    agent.Order((int)trainType.Ability);
                    Bot.Main.UnitManager.UnitTraining(trainType.UnitType);
                }

                state.AddTraining(UnitType, 1);
                alreadyTrained++;
            }

            return new NextItem();
        }

        private bool WarpIn(Agent warpGate, TrainingType trainType)
        {
            int framesSinceLastWarpIn = Bot.Main.Frame - LastWarpInFrame;
            
            if (framesSinceLastWarpIn <= 4)
            {
                foreach (Agent agent in Bot.Main.Units())
                {
                    if (agent.DistanceSq(LastWarpInLocation) <= 0.5f * 0.5f)
                    {
                        LastWarpInFrame = 0;
                        break;
                    }
                }
            }

            Point2D aroundTile;
            Point2D placement;
            if (WarpInLocation != null)
            {
                aroundTile = WarpInLocation;
                placement = WarpInPlacer.FindPlacement(aroundTile, trainType.UnitType);
                if (placement != null)
                {
                    if (framesSinceLastWarpIn >= 10)
                    {
                        LastWarpInFrame = Bot.Main.Frame;
                        LastWarpInLocation = placement;
                    }
                    warpGate.Order((int)trainType.WarpInAbility, placement);
                    return true;
                }
            }

            if (Bot.Main.BaseManager.Natural.Owner == Bot.Main.PlayerId 
                && Bot.Main.BaseManager.Natural.BuildingsCompleted.ContainsKey(UnitTypes.PYLON) 
                && Bot.Main.BaseManager.Natural.BuildingsCompleted[UnitTypes.PYLON] >= 1)
            {
                aroundTile = Bot.Main.BaseManager.Natural.OppositeMineralLinePos;
                placement = WarpInPlacer.FindPlacement(aroundTile, trainType.UnitType);
                if (placement != null)
                {
                    if (framesSinceLastWarpIn >= 10)
                    {
                        LastWarpInFrame = Bot.Main.Frame;
                        LastWarpInLocation = placement;
                    }
                    warpGate.Order((int)trainType.WarpInAbility, placement);
                    return true;
                }
            }

            aroundTile = SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation);
            placement = WarpInPlacer.FindPlacement(aroundTile, trainType.UnitType);
            if (placement != null)
            {
                if (framesSinceLastWarpIn >= 10)
                {
                    LastWarpInFrame = Bot.Main.Frame;
                    LastWarpInLocation = placement;
                }
                warpGate.Order((int)trainType.WarpInAbility, placement);
                Bot.Main.UnitManager.UnitTraining(trainType.UnitType);
                return true;
            }
            return false;
        }

        private bool IsReactor(uint unitType)
        {
            return unitType == UnitTypes.REACTOR
                || unitType == UnitTypes.BARRACKS_REACTOR
                || unitType == UnitTypes.FACTORY_REACTOR
                || unitType == UnitTypes.STARPORT_REACTOR;
        }

        private bool CheckTypeMatches(uint requiredType, uint foundType)
        {
            if (requiredType == foundType)
                return true;
            return UnitTypes.EquivalentTypes.ContainsKey(foundType) && UnitTypes.EquivalentTypes[foundType].Contains(requiredType);
        }
    }
}
