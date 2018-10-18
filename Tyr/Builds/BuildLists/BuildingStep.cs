using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
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
    }
}
