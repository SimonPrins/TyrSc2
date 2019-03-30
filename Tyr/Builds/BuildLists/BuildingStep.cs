using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Tasks;
using Tyr.Util;
using static Tyr.Builds.BuildLists.ConditionalStep;

namespace Tyr.Builds.BuildLists
{
    public class BuildingStep : BuildStep
    {
        public uint UnitType;
        public Base DesiredBase;
        public Point2D DesiredPos;
        public bool Exact;
        public int Number = 1;
        public Test Condition = () => { return true; };

        public BuildingStep(uint unitType)
        {
            UnitType = unitType;
        }

        public BuildingStep(uint unitType, Base desiredBase)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, bool exact)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Exact = exact;
        }

        public BuildingStep(uint unitType, int number)
        {
            UnitType = unitType;
            Number = number;
        }

        public BuildingStep(uint unitType, Base desiredBase, int number)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            Number = number;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, int number)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Number = number;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, bool exact, int number)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Exact = exact;
            Number = number;
        }

        public BuildingStep(uint unitType, Test condition)
        {
            UnitType = unitType;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, bool exact, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Exact = exact;
            Condition = condition;
        }

        public BuildingStep(uint unitType, int number, Test condition)
        {
            UnitType = unitType;
            Number = number;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, int number, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            Number = number;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, int number, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Number = number;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, bool exact, int number, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Exact = exact;
            Number = number;
            Condition = condition;
        }

        public override string ToString()
        {
            if (!BuildingType.LookUp.ContainsKey(UnitType))
                throw new System.Exception("Unable to find building with type: " + UnitType  + ". Please add it to the BuildingType class.");
            return "Building " + BuildingType.LookUp[UnitType].Name + " " + Number + (Exact ? " exact" : "");
        }

        public StepResult Perform(BuildListState state)
        {
            if (!Condition.Invoke())
                return new NextItem();

            if (UnitTypes.LookUp[UnitType].TechRequirement != 0
                && Tyr.Bot.UnitManager.Completed(UnitTypes.LookUp[UnitType].TechRequirement) == 0
                && UnitTypes.LookUp[UnitType].TechRequirement != UnitTypes.HATCHERY)
            {
                Tyr.Bot.DrawText("Skipping list. Build tech for " + UnitTypes.LookUp[UnitType].Name + " not available: " + UnitTypes.LookUp[UnitType].TechRequirement);
                return new NextList();
            }

            state.AddDesired(UnitType, Number);
            if (DesiredPos != null)
            {
                bool built = false;
                foreach (BuildRequest request in ConstructionTask.Task.BuildRequests)
                    if (request.Type == UnitType && request.Exact == Exact && request.AroundLocation == DesiredPos)
                    {
                        built = true;
                        break;
                    }
                foreach (BuildRequest request in ConstructionTask.Task.UnassignedRequests)
                    if (request.Type == UnitType && request.Exact == Exact && request.AroundLocation == DesiredPos)
                    {
                        built = true;
                        break;
                    }
                if (!built)
                    foreach (Agent agent in Tyr.Bot.UnitManager.Agents.Values)
                        if (CheckTypeMatches(UnitType, agent.Unit.UnitType) && agent.Exact == Exact && agent.AroundLocation == DesiredPos)
                        {
                            built = true;
                            break;
                        }
                if (!built)
                {
                    if (!Construct(state, 1))
                        return new WaitForResources();
                }
            }

            if (DesiredBase == null && state.Desired[UnitType] > Tyr.Bot.UnitManager.Count(UnitType)
                && !Exact)
                if (!Construct(state, state.Desired[UnitType] - Tyr.Bot.UnitManager.Count(UnitType)))
                    return new WaitForResources();
            if (DesiredBase != null)
            {
                BuildingAtBase key = new BuildingAtBase(UnitType, DesiredBase);
                state.AddDesiredPerBase(key, Number);

                if (state.DesiredPerBase[key] > Build.Count(DesiredBase, UnitType)
                    && !Exact)
                {
                    if (!Construct(state, state.DesiredPerBase[key] - Build.Count(DesiredBase, UnitType)))
                        return new WaitForResources();
                }
            }

            return new NextItem();
        }

        private bool CheckTypeMatches(uint requiredType, uint foundType)
        {
            if (requiredType == foundType)
                return true;
            return UnitTypes.EquivalentTypes.ContainsKey(foundType) && UnitTypes.EquivalentTypes[foundType].Contains(requiredType);
        }

        private bool Construct(BuildListState state, int number)
        {
            for (int i = 0; i < number; i++)
            {
                bool isCenter = UnitTypes.ResourceCenters.Contains(UnitType);

                if (Tyr.Bot.Minerals() < BuildingType.LookUp[UnitType].Minerals - (UnitTypes.ResourceCenters.Contains(UnitType) ? 75 : 25)
                    || (Tyr.Bot.Gas() < BuildingType.LookUp[UnitType].Gas - 16 && BuildingType.LookUp[UnitType].Gas > 0))
                {
                    Tyr.Bot.DrawText("Not enough resources for " + UnitTypes.LookUp[UnitType].Name + ".");
                    return false;
                }

                if (UnitTypes.GasGeysers.Contains(UnitType))
                {
                    if (Tyr.Bot.BaseManager.AvailableGasses == 0)
                        return true;
                    if (DesiredBase != null)
                    {
                        bool available = false;
                        foreach (Gas gas in DesiredBase.BaseLocation.Gasses)
                            if (gas.Available)
                            {
                                available = true;
                                break;
                            }
                        if (!available)
                            return true;
                    }
                }

                if (DesiredPos == null && DesiredBase == null)
                {
                    state.BuiltThisFrame = true;
                    if (!Build.Construct(UnitType))
                    {
                        if (isCenter
                            && Tyr.Bot.UnitManager.Count(UnitTypes.COMMAND_CENTER) + Tyr.Bot.UnitManager.Count(UnitTypes.NEXUS) + Tyr.Bot.UnitManager.Count(UnitTypes.HATCHERY) > Tyr.Bot.UnitManager.Completed(UnitTypes.COMMAND_CENTER) + Tyr.Bot.UnitManager.Completed(UnitTypes.NEXUS) + Tyr.Bot.UnitManager.Completed(UnitTypes.HATCHERY))
                            return true;
                        return !UnitTypes.DefensiveBuildingsTypes.Contains(UnitType);
                    }
                }
                else if (DesiredPos == null)
                {
                    state.BuiltThisFrame = true;
                    if (!Build.Construct(UnitType, DesiredBase))
                        return !UnitTypes.DefensiveBuildingsTypes.Contains(UnitType);
                }
                else
                {
                    state.BuiltThisFrame = true;
                    if (!Build.Construct(UnitType, DesiredBase, DesiredPos, Exact))
                        return !UnitTypes.DefensiveBuildingsTypes.Contains(UnitType);
                }
            }
            return true;
        }
    }
}
