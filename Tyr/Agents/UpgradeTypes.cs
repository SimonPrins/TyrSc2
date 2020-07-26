using System;
using System.Collections.Generic;

namespace SC2Sharp.Agents
{
    public class UpgradeType
    {
        public uint UpgradeID;
        public HashSet<uint> ProducingUnits;
        public uint Ability;
        public int Minerals;
        public int Gas;
        public uint Previous;

        public static uint ChitinousPlating = 4;
        public static uint TerranInfantryWeapons1 = 7;
        public static uint TerranInfantryWeapons2 = 8;
        public static uint TerranInfantryWeapons3 = 9;
        public static uint TerranInfantryWeapons = 9;
        public static uint TerranInfantryArmor1 = 11;
        public static uint TerranInfantryArmor2 = 12;
        public static uint TerranInfantryArmor3 = 13;
        public static uint TerranInfantryArmor = 13;
        public static uint Stim = 15;
        public static uint CombatShield = 16;
        public static uint ConcussiveShells = 17;
        public static uint InfernalPreigniter = 19;
        public static uint BansheeCloak = 20;
        public static uint ProtossGroundWeapons1 = 39;
        public static uint ProtossGroundWeapons2 = 40;
        public static uint ProtossGroundWeapons3 = 41;
        public static uint ProtossGroundWeapons = 41;
        public static uint ProtossGroundArmor1 = 42;
        public static uint ProtossGroundArmor2 = 43;
        public static uint ProtossGroundArmor3 = 44;
        public static uint ProtossGroundArmor = 44;
        public static uint ProtossShields1 = 45;
        public static uint ProtossShields2 = 46;
        public static uint ProtossShields3 = 47;
        public static uint ProtossShields = 47;
        public static uint ExtendedThermalLance = 50;
        public static uint PsiStorm = 52;
        public static uint ZergMeleeWeapons1 = 53;
        public static uint ZergMeleeWeapons2 = 54;
        public static uint ZergMeleeWeapons3 = 55;
        public static uint ZergMeleeWeapons = 55;
        public static uint ZergGroundArmor1 = 56;
        public static uint ZergGroundArmor2 = 57;
        public static uint ZergGroundArmor3 = 58;
        public static uint ZergGroundArmor = 58;
        public static uint ZergMissileWeapons1 = 59;
        public static uint ZergMissileWeapons2 = 60;
        public static uint ZergMissileWeapons3 = 61;
        public static uint ZergMissileWeapons = 61;
        public static uint AdrenalGlands = 65;
        public static uint MetabolicBoost = 66;
        public static uint PathogenGlands = 74;
        public static uint YamatoCannon = 76;
        public static uint ProtossAirWeapons1 = 78;
        public static uint ProtossAirWeapons2 = 79;
        public static uint ProtossAirWeapons3 = 80;
        public static uint ProtossAirWeapons = 80;
        public static uint ProtossAirArmor1 = 81;
        public static uint ProtossAirArmor2 = 82;
        public static uint ProtossAirArmor3 = 83;
        public static uint ProtossAirArmor = 83;
        public static uint Charge = 86;
        public static uint Blink = 87;
        public static uint ResonatingGlaives = 130;
        public static uint WarpGate = 84;
        public static uint AnabolicSynthesis = 88;
        public static uint NeuralParasite = 101;
        public static uint GroovedSpines = 134;
        public static uint MuscularAugments = 135;

        public static Dictionary<uint, UpgradeType> LookUp = GetLookUp();

        public static Dictionary<uint, UpgradeType> GetLookUp()
        {
            Dictionary<uint, UpgradeType> result = new Dictionary<uint, UpgradeType>();

            Add(result, new UpgradeType() { UpgradeID = ChitinousPlating, ProducingUnits = Set(UnitTypes.ULTRALISK_CAVERN), Minerals = 150, Gas = 150, Ability = 265 });
            Add(result, new UpgradeType() { UpgradeID = TerranInfantryWeapons1, ProducingUnits = Set(UnitTypes.ENGINEERING_BAY), Minerals = 100, Gas = 100, Ability = 652 });
            Add(result, new UpgradeType() { UpgradeID = TerranInfantryWeapons2, ProducingUnits = Set(UnitTypes.ENGINEERING_BAY), Minerals = 175, Gas = 175, Ability = 653, Previous = TerranInfantryWeapons1 });
            Add(result, new UpgradeType() { UpgradeID = TerranInfantryWeapons3, ProducingUnits = Set(UnitTypes.ENGINEERING_BAY), Minerals = 250, Gas = 250, Ability = 654, Previous = TerranInfantryWeapons2 });
            Add(result, new UpgradeType() { UpgradeID = TerranInfantryArmor1, ProducingUnits = Set(UnitTypes.ENGINEERING_BAY), Minerals = 100, Gas = 100, Ability = 652 });
            Add(result, new UpgradeType() { UpgradeID = TerranInfantryArmor2, ProducingUnits = Set(UnitTypes.ENGINEERING_BAY), Minerals = 175, Gas = 175, Ability = 653, Previous = TerranInfantryArmor1 });
            Add(result, new UpgradeType() { UpgradeID = TerranInfantryArmor3, ProducingUnits = Set(UnitTypes.ENGINEERING_BAY), Minerals = 250, Gas = 250, Ability = 654, Previous = TerranInfantryArmor2 });
            Add(result, new UpgradeType() { UpgradeID = Stim, ProducingUnits = Set(UnitTypes.BARRACKS_TECH_LAB), Minerals = 50, Gas = 50, Ability = 730 });
            Add(result, new UpgradeType() { UpgradeID = CombatShield, ProducingUnits = Set(UnitTypes.BARRACKS_TECH_LAB), Minerals = 100, Gas = 100, Ability = 731 });
            Add(result, new UpgradeType() { UpgradeID = ConcussiveShells, ProducingUnits = Set(UnitTypes.BARRACKS_TECH_LAB), Minerals = 100, Gas = 100, Ability = 732 });
            Add(result, new UpgradeType() { UpgradeID = BansheeCloak, ProducingUnits = Set(UnitTypes.STARPORT_TECH_LAB), Minerals = 100, Gas = 100, Ability = 790 });
            Add(result, new UpgradeType() { UpgradeID = ProtossGroundWeapons1, ProducingUnits = Set(UnitTypes.FORGE), Minerals = 100, Gas = 100, Ability = 1062 });
            Add(result, new UpgradeType() { UpgradeID = ProtossGroundWeapons2, ProducingUnits = Set(UnitTypes.FORGE), Minerals = 150, Gas = 150, Ability = 1063, Previous = ProtossGroundWeapons1 });
            Add(result, new UpgradeType() { UpgradeID = ProtossGroundWeapons3, ProducingUnits = Set(UnitTypes.FORGE), Minerals = 200, Gas = 200, Ability = 1064, Previous = ProtossGroundWeapons2 });
            Add(result, new UpgradeType() { UpgradeID = ProtossGroundArmor1, ProducingUnits = Set(UnitTypes.FORGE), Minerals = 100, Gas = 100, Ability = 1065 });
            Add(result, new UpgradeType() { UpgradeID = ProtossGroundArmor2, ProducingUnits = Set(UnitTypes.FORGE), Minerals = 150, Gas = 150, Ability = 1066, Previous = ProtossGroundArmor1 });
            Add(result, new UpgradeType() { UpgradeID = ProtossGroundArmor3, ProducingUnits = Set(UnitTypes.FORGE), Minerals = 200, Gas = 200, Ability = 1067, Previous = ProtossGroundArmor2 });
            Add(result, new UpgradeType() { UpgradeID = ProtossShields1, ProducingUnits = Set(UnitTypes.FORGE), Minerals = 150, Gas = 150, Ability = 1068 });
            Add(result, new UpgradeType() { UpgradeID = ProtossShields2, ProducingUnits = Set(UnitTypes.FORGE), Minerals = 225, Gas = 225, Ability = 1069, Previous = ProtossShields1 });
            Add(result, new UpgradeType() { UpgradeID = ProtossShields3, ProducingUnits = Set(UnitTypes.FORGE), Minerals = 300, Gas = 300, Ability = 1070, Previous = ProtossShields2 });
            Add(result, new UpgradeType() { UpgradeID = ExtendedThermalLance, ProducingUnits = Set(UnitTypes.ROBOTICS_BAY), Minerals = 150, Gas = 150, Ability = 1097 });
            Add(result, new UpgradeType() { UpgradeID = PsiStorm, ProducingUnits = Set(UnitTypes.TEMPLAR_ARCHIVE), Minerals = 200, Gas = 200, Ability = 1126 });
            Add(result, new UpgradeType() { UpgradeID = ZergMeleeWeapons1, ProducingUnits = Set(UnitTypes.EVOLUTION_CHAMBER), Minerals = 100, Gas = 100, Ability = 1186 });
            Add(result, new UpgradeType() { UpgradeID = ZergMeleeWeapons2, ProducingUnits = Set(UnitTypes.EVOLUTION_CHAMBER), Minerals = 150, Gas = 150, Ability = 1187, Previous = ZergMeleeWeapons1 });
            Add(result, new UpgradeType() { UpgradeID = ZergMeleeWeapons3, ProducingUnits = Set(UnitTypes.EVOLUTION_CHAMBER), Minerals = 200, Gas = 200, Ability = 1188, Previous = ZergMeleeWeapons2 });
            Add(result, new UpgradeType() { UpgradeID = ZergGroundArmor1, ProducingUnits = Set(UnitTypes.EVOLUTION_CHAMBER), Minerals = 150, Gas = 150, Ability = 1189 });
            Add(result, new UpgradeType() { UpgradeID = ZergGroundArmor2, ProducingUnits = Set(UnitTypes.EVOLUTION_CHAMBER), Minerals = 225, Gas = 225, Ability = 1190, Previous = ZergGroundArmor1 });
            Add(result, new UpgradeType() { UpgradeID = ZergGroundArmor3, ProducingUnits = Set(UnitTypes.EVOLUTION_CHAMBER), Minerals = 300, Gas = 300, Ability = 1191, Previous = ZergGroundArmor2 });
            Add(result, new UpgradeType() { UpgradeID = ZergMissileWeapons1, ProducingUnits = Set(UnitTypes.EVOLUTION_CHAMBER), Minerals = 100, Gas = 100, Ability = 1192 });
            Add(result, new UpgradeType() { UpgradeID = ZergMissileWeapons2, ProducingUnits = Set(UnitTypes.EVOLUTION_CHAMBER), Minerals = 150, Gas = 150, Ability = 1193, Previous = ZergMissileWeapons1 });
            Add(result, new UpgradeType() { UpgradeID = ZergMissileWeapons3, ProducingUnits = Set(UnitTypes.EVOLUTION_CHAMBER), Minerals = 200, Gas = 200, Ability = 1194, Previous = ZergMissileWeapons2 });
            Add(result, new UpgradeType() { UpgradeID = InfernalPreigniter, ProducingUnits = Set(UnitTypes.FACTORY_TECH_LAB), Minerals = 150, Gas = 150, Ability = 761 });
            Add(result, new UpgradeType() { UpgradeID = AdrenalGlands, ProducingUnits = Set(UnitTypes.SPAWNING_POOL), Minerals = 200, Gas = 200, Ability = 1252 });
            Add(result, new UpgradeType() { UpgradeID = MetabolicBoost, ProducingUnits = Set(UnitTypes.SPAWNING_POOL), Minerals = 100, Gas = 100, Ability = 1253 });
            Add(result, new UpgradeType() { UpgradeID = GroovedSpines, ProducingUnits = Set(UnitTypes.HYDRALISK_DEN), Minerals = 100, Gas = 100, Ability = 1282 });
            Add(result, new UpgradeType() { UpgradeID = MuscularAugments, ProducingUnits = Set(UnitTypes.HYDRALISK_DEN), Minerals = 100, Gas = 100, Ability = 1283 });
            Add(result, new UpgradeType() { UpgradeID = YamatoCannon, ProducingUnits = Set(UnitTypes.FUSION_CORE), Minerals = 150, Gas = 150, Ability = 1532 });
            Add(result, new UpgradeType() { UpgradeID = ProtossAirWeapons1, ProducingUnits = Set(UnitTypes.CYBERNETICS_CORE), Minerals = 100, Gas = 100, Ability = 1562 });
            Add(result, new UpgradeType() { UpgradeID = ProtossAirWeapons2, ProducingUnits = Set(UnitTypes.CYBERNETICS_CORE), Minerals = 175, Gas = 175, Ability = 1563, Previous = ProtossAirWeapons1 });
            Add(result, new UpgradeType() { UpgradeID = ProtossAirWeapons3, ProducingUnits = Set(UnitTypes.CYBERNETICS_CORE), Minerals = 250, Gas = 250, Ability = 1564, Previous = ProtossAirWeapons2 });
            Add(result, new UpgradeType() { UpgradeID = ProtossAirArmor1, ProducingUnits = Set(UnitTypes.CYBERNETICS_CORE), Minerals = 150, Gas = 150, Ability = 1565 });
            Add(result, new UpgradeType() { UpgradeID = ProtossAirArmor2, ProducingUnits = Set(UnitTypes.CYBERNETICS_CORE), Minerals = 225, Gas = 225, Ability = 1566, Previous = ProtossAirArmor1 });
            Add(result, new UpgradeType() { UpgradeID = ProtossAirArmor3, ProducingUnits = Set(UnitTypes.CYBERNETICS_CORE), Minerals = 300, Gas = 300, Ability = 1567, Previous = ProtossAirArmor2 });
            Add(result, new UpgradeType() { UpgradeID = Charge, ProducingUnits = Set(UnitTypes.TWILIGHT_COUNSEL), Minerals = 100, Gas = 100, Ability = 1592 });
            Add(result, new UpgradeType() { UpgradeID = Blink, ProducingUnits = Set(UnitTypes.TWILIGHT_COUNSEL), Minerals = 150, Gas = 150, Ability = 1593 });
            Add(result, new UpgradeType() { UpgradeID = ResonatingGlaives, ProducingUnits = Set(UnitTypes.TWILIGHT_COUNSEL), Minerals = 100, Gas = 100, Ability = 1594 });
            Add(result, new UpgradeType() { UpgradeID = AnabolicSynthesis, ProducingUnits = Set(UnitTypes.ULTRALISK_CAVERN), Minerals = 150, Gas = 150, Ability = 263 });
            Add(result, new UpgradeType() { UpgradeID = PathogenGlands, ProducingUnits = Set(UnitTypes.INFESTATION_PIT), Minerals = 150, Gas = 150, Ability = 1454 });
            Add(result, new UpgradeType() { UpgradeID = NeuralParasite, ProducingUnits = Set(UnitTypes.INFESTATION_PIT), Minerals = 150, Gas = 150, Ability = 1455 });
            Add(result, new UpgradeType() { UpgradeID = WarpGate, ProducingUnits = Set(UnitTypes.CYBERNETICS_CORE), Minerals = 50, Gas = 50, Ability = 1568 });

            return result;
        }

        private static HashSet<T> Set<T>(T element)
        {
            return new HashSet<T>() { element };
        }

        private static void Add(Dictionary<uint, UpgradeType> result, UpgradeType upgradeType)
        {
            result.Add(upgradeType.UpgradeID, upgradeType);
        }

        public float Progress()
        {
            if (Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(UpgradeID))
                return 1;
            if (!Bot.Main.UnitManager.ActiveOrders.Contains(Ability))
                return 0;
            foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                if (agent.CurrentAbility() == Ability)
                    return agent.Unit.Orders[0].Progress;
            return 0;
        }

        public bool Started()
        {
            return Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(UpgradeID) || Bot.Main.UnitManager.ActiveOrders.Contains(Ability);
        }

        public bool Done()
        {
            return Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(UpgradeID);
        }
    }
}
