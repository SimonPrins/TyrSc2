using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Tasks;
using Tyr.Util;
using static Tyr.Builds.BuildLists.ConditionalStep;

namespace Tyr.Builds.BuildLists
{
    public class BuildList
    {
        private List<BuildStep> Steps = new List<BuildStep>();
        bool BuiltThisFrame;

        public static BuildList operator +(BuildList list, BuildStep step)
        {
            list.Steps.Add(step);
            return list;
        }

        public bool Construct()
        {
            BuiltThisFrame = false;
            Dictionary<uint, int> desired = new Dictionary<uint, int>();
            Dictionary<BuildingAtBase, int> desiredPerBase = new Dictionary<BuildingAtBase, int>();
            
            for (int pos = 0; pos < Steps.Count; pos++)
            {
                BuildStep step = null;
                try
                {
                    step = Steps[pos];
                    if (step.GetType() == typeof(BuildingStep))
                    {
                        BuildingStep building = (BuildingStep)step;
                        if (!building.Condition.Invoke())
                            continue;

                        if (UnitTypes.LookUp[building.UnitType].TechRequirement != 0
                            && Tyr.Bot.UnitManager.Completed(UnitTypes.LookUp[building.UnitType].TechRequirement) == 0
                            && UnitTypes.LookUp[building.UnitType].TechRequirement != UnitTypes.HATCHERY)
                        {
                            Tyr.Bot.DrawText("Skipping list. Build tech for " + UnitTypes.LookUp[building.UnitType].Name + " not available: " + UnitTypes.LookUp[building.UnitType].TechRequirement);
                            return true;
                        }

                        Add(desired, building.UnitType, building.Number);
                        if (building.Exact)
                        {
                            bool built = false;
                            foreach (BuildRequest request in ConstructionTask.Task.BuildRequests)
                                if (request.Type == building.UnitType && SC2Util.DistanceSq(request.Pos, building.DesiredPos) <= 2)
                                {
                                    built = true;
                                    break;
                                }
                            foreach (BuildRequest request in ConstructionTask.Task.UnassignedRequests)
                                if (request.Type == building.UnitType && SC2Util.DistanceSq(request.Pos, building.DesiredPos) <= 2)
                                {
                                    built = true;
                                    break;
                                }
                            if (!built)
                                foreach (Agent agent in Tyr.Bot.UnitManager.Agents.Values)
                                    if (agent.Unit.UnitType == building.UnitType && SC2Util.DistanceSq(agent.Unit.Pos, building.DesiredPos) <= 2)
                                    {
                                        built = true;
                                        break;
                                    }
                            if (!built)
                            {
                                if (!Construct(building, 1))
                                    return false;
                            }
                        }

                        if (building.DesiredBase == null && desired[building.UnitType] > Tyr.Bot.UnitManager.Count(building.UnitType)
                            && !building.Exact)
                            if (!Construct(building, desired[building.UnitType] - Tyr.Bot.UnitManager.Count(building.UnitType)))
                                return false;
                        if (building.DesiredBase != null)
                        {
                            BuildingAtBase key = new BuildingAtBase(building.UnitType, building.DesiredBase);
                            Add(desiredPerBase, key, building.Number);

                            if (desiredPerBase[key] > Build.Count(building.DesiredBase, building.UnitType)
                                && !building.Exact)
                            {
                                Tyr.Bot.DrawText("Building " + UnitTypes.LookUp[building.UnitType].Name + " at base.");
                                if (!Construct(building, desiredPerBase[key] - Build.Count(building.DesiredBase, building.UnitType)))
                                    return false;
                            }
                        }
                    }
                    else if (step.GetType() == typeof(ConditionalStep))
                    {
                        if (!((ConditionalStep)step).Check())
                        {
                            Tyr.Bot.DrawText("Skipping list. Condition not met.");
                            return true;
                        }
                    }
                    else if (step.GetType() == typeof(GotoStep))
                    {
                        if (BuiltThisFrame)
                            return true;
                        if (((GotoStep)step).Pos < 0)
                            pos += ((GotoStep)step).Pos;
                        else
                            pos = ((GotoStep)step).Pos - 1;
                    }
                    if (step.GetType() == typeof(MorphStep))
                    {
                        MorphStep morph = (MorphStep)step;
                        if (!morph.Condition.Invoke())
                            continue;

                        if (UnitTypes.LookUp[morph.UnitType].TechRequirement != 0
                            && Tyr.Bot.UnitManager.Completed(UnitTypes.LookUp[morph.UnitType].TechRequirement) == 0
                            && UnitTypes.LookUp[morph.UnitType].TechRequirement != UnitTypes.HATCHERY)
                        {
                            Tyr.Bot.DrawText("Skipping list. Morph tech for " + UnitTypes.LookUp[morph.UnitType].Name + " not available: " + UnitTypes.LookUp[morph.UnitType].TechRequirement);
                            return true;
                        }

                        if (morph.UnitType == UnitTypes.CORRUPTOR)
                            Console.WriteLine("Morphing corruptor.");
                        Add(desired, morph.UnitType, morph.Number);

                        if (desired[morph.UnitType] > Tyr.Bot.UnitManager.Count(morph.UnitType))
                            if (!Morph(morph, desired[morph.UnitType] - Tyr.Bot.UnitManager.Count(morph.UnitType)))
                                return false;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Exception in buildlist at step " + pos + ": " + step, e);
                }
            } 
            return true;
        }

        public void Building(uint unitType)
        {
            Steps.Add(new BuildingStep(unitType));
        }

        public void Building(uint unitType, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, condition));
        }

        public void Building(uint unitType, Base desiredBase)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase));
        }

        public void Building(uint unitType, Base desiredBase, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, condition));
        }

        public void Building(uint unitType, int number)
        {
            Steps.Add(new BuildingStep(unitType, number));
        }

        public void Building(uint unitType, int number, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, number, condition));
        }

        public void Building(uint unitType, Base desiredBase, int number)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, number));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos, condition));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos, int number)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos, number));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos, int number, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos, number, condition));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos, bool exact)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos, exact));
        }

        public void Goto(int pos)
        {
            Steps.Add(new GotoStep(pos));
        }

        public void If(Test condition)
        {
            Steps.Add(new ConditionalStep(condition));
        }

        public void Morph(uint unitType)
        {
            Steps.Add(new MorphStep(unitType));
        }

        public void Morph(uint unitType, int number)
        {
            Steps.Add(new MorphStep(unitType, number));
        }

        public void Morph(uint unitType, Test condition)
        {
            Steps.Add(new MorphStep(unitType, condition));
        }

        public void Morph(uint unitType, int number, Test condition)
        {
            Steps.Add(new MorphStep(unitType, number, condition));
        }

        private bool Construct(BuildingStep building, int number)
        {
            for (int i = 0; i < number; i++)
            {
                bool isCenter = UnitTypes.ResourceCenters.Contains(building.UnitType);

                if (Tyr.Bot.Minerals() < BuildingType.LookUp[building.UnitType].Minerals - (UnitTypes.ResourceCenters.Contains(building.UnitType) ? 75 : 25)
                    || Tyr.Bot.Gas() < BuildingType.LookUp[building.UnitType].Gas - 16)
                {
                    Tyr.Bot.DrawText("Not enough resources for " + UnitTypes.LookUp[building.UnitType].Name + ".");
                    return false;
                }

                if (building.DesiredPos == null && building.DesiredBase == null)
                {
                    BuiltThisFrame = true;
                    if (!Build.Construct(building.UnitType))
                        return false;
                }
                else if (building.DesiredPos == null)
                {
                    BuiltThisFrame = true;
                    if (!Build.Construct(building.UnitType, building.DesiredBase))
                        return false;
                }
                else
                {
                    BuiltThisFrame = true;
                    if (!Build.Construct(building.UnitType, building.DesiredBase, building.DesiredPos, building.Exact))
                        return false;
                }
            }
            return true;
        }

        private bool Morph(MorphStep morph, int number)
        {
            if (number <= 0)
                return true;


            if (Tyr.Bot.Minerals() < MorphingType.LookUpToType[morph.UnitType].Minerals
                || Tyr.Bot.Gas() < MorphingType.LookUpToType[morph.UnitType].Gas)
                return false;

            BuiltThisFrame = true;
            Tyr.Bot.DrawText("Morphing: " + UnitTypes.LookUp[morph.UnitType].Name);
            MorphingTask.Task.Morph(morph.UnitType);

            return number == 1;
        }

        private void Add<T>(Dictionary<T, int> dict, T key, int val)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, 0);
            dict[key] += val;
        }
    }
}
