using System.Collections.Generic;

namespace Tyr.Agents
{
    public class MorphingType
    {
        public uint FromType { get; private set; }
        public uint ToType { get; private set; }
        public int Ability { get; private set; }
        public int Minerals { get; private set; }
        public int Gas { get; private set; }

        public static List<MorphingType> MorphingTypes = CreateList();
        public static Dictionary<uint, MorphingType> LookUpToType = CreateLookUpToType();
        //public static Dictionary<uint, MorphingType> LookUpFromType = CreateLookUpFromType();

        public static List<MorphingType> CreateList()
        {
            List<MorphingType> list = new List<MorphingType>();

            list.Add(new MorphingType() { FromType = UnitTypes.LARVA, ToType = UnitTypes.DRONE, Ability = Abilities.MORPH_DRONE, Minerals = 50 });
            list.Add(new MorphingType() { FromType = UnitTypes.LARVA, ToType = UnitTypes.ZERGLING, Ability = Abilities.MORPH_ZERGLING, Minerals = 50 });
            list.Add(new MorphingType() { FromType = UnitTypes.LARVA, ToType = UnitTypes.OVERLORD, Ability = Abilities.MORPH_OVERLORD, Minerals = 100 });
            list.Add(new MorphingType() { FromType = UnitTypes.LARVA, ToType = UnitTypes.HYDRALISK, Ability = Abilities.MORPH_HYDRA, Minerals = 100, Gas = 50 });
            list.Add(new MorphingType() { FromType = UnitTypes.LARVA, ToType = UnitTypes.MUTALISK, Ability = Abilities.MORPH_MUTALISK, Minerals = 100, Gas = 100 });
            list.Add(new MorphingType() { FromType = UnitTypes.LARVA, ToType = UnitTypes.ROACH, Ability = Abilities.MORPH_ROACH, Minerals = 75, Gas = 25 });
            list.Add(new MorphingType() { FromType = UnitTypes.LARVA, ToType = UnitTypes.CORRUPTOR, Ability = Abilities.MORPH_CORRUPTOR, Minerals = 150, Gas = 100 });
            list.Add(new MorphingType() { FromType = UnitTypes.HYDRALISK, ToType = UnitTypes.LURKER, Ability = Abilities.MORPH_LURKER, Minerals = 50, Gas = 100 });
            list.Add(new MorphingType() { FromType = UnitTypes.OVERLORD, ToType = UnitTypes.OVERSEER, Ability = Abilities.MORPH_OVERSEER, Minerals = 50, Gas = 50 });
            list.Add(new MorphingType() { FromType = UnitTypes.CORRUPTOR, ToType = UnitTypes.BROOD_LORD, Ability = Abilities.MORPH_BROODLORD, Minerals = 150, Gas = 150 });
            list.Add(new MorphingType() { FromType = UnitTypes.ROACH, ToType = UnitTypes.RAVAGER, Ability = Abilities.MORPH_RAVAGER, Minerals = 25, Gas = 75 });

            return list;
        }

        public static Dictionary<uint, MorphingType> CreateLookUpToType()
        {
            Dictionary<uint, MorphingType> LookUp = new Dictionary<uint, MorphingType>();

            foreach (MorphingType morph in MorphingTypes)
                LookUp.Add(morph.ToType, morph);

            return LookUp;
        }

        /*
        public static Dictionary<uint, MorphingType> CreateLookUpFromType()
        {
            Dictionary<uint, MorphingType> LookUp = new Dictionary<uint, MorphingType>();

            foreach (MorphingType morph in MorphingTypes)
                LookUp.Add(morph.FromType, morph);

            return LookUp;
        }
        */
    }
}
