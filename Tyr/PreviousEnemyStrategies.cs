using Tyr.Util;

namespace Tyr
{
    public class PreviousEnemyStrategies
    {
        public bool CannonRush;
        public bool Lifting;
        public bool MassRoach;
        public bool MassHydra;
        public bool FourRax;
        public bool ReaperRush;
        public bool TerranTech;
        public bool Mech;
        public bool Bio;
        public bool ThreeGate;
        public bool SkyToss;

        public void Load(string[] lines)
        {
            foreach (string line in lines)
            {
                if (line == "CannonRush")
                    CannonRush = true;
                else if (line == "Lifting")
                    Lifting = true;
                else if (line == "MassRoach")
                    MassRoach = true;
                else if (line == "MassHydra")
                    MassHydra = true;
                else if (line == "FourRax")
                    FourRax = true;
                else if (line == "ReaperRush")
                    ReaperRush = true;
                else if (line == "TerranTech")
                    TerranTech = true;
                else if (line == "Mech")
                    Mech = true;
                else if (line == "Bio")
                    Bio = true;
                else if (line == "ThreeGate")
                    ThreeGate = true;
                else if (line == "SkyToss")
                    SkyToss = true;
            }
        }

        public void SetCannonRush()
        {
            if (!CannonRush)
            {
                CannonRush = true;
                FileUtil.Register("CannonRush");
            }
        }

        public void SetLifting()
        {
            if (!Lifting)
            {
                Lifting = true;
                FileUtil.Register("Lifting");
            }
        }

        public void SetMassRoach()
        {
            if (!MassRoach)
            {
                MassRoach = true;
                FileUtil.Register("MassRoach");
            }
        }

        public void SetMassHydra()
        {
            if (!MassHydra)
            {
                MassHydra = true;
                FileUtil.Register("MassHydra");
            }
        }

        public void SetFourRax()
        {
            if (!FourRax)
            {
                FourRax = true;
                FileUtil.Register("FourRax");
            }
        }

        public void SetReaperRush()
        {
            if (!ReaperRush)
            {
                ReaperRush = true;
                FileUtil.Register("ReaperRush");
            }
        }

        public void SetTerranTech()
        {
            if (!TerranTech)
            {
                TerranTech = true;
                FileUtil.Register("TerranTech");
            }
        }

        public void SetMech()
        {
            if (!Mech)
            {
                Mech = true;
                FileUtil.Register("Mech");
            }
        }

        public void SetBio()
        {
            if (!Bio)
            {
                Bio = true;
                FileUtil.Register("Bio");
            }
        }

        public void SetThreeGate()
        {
            if (!ThreeGate)
            {
                ThreeGate = true;
                FileUtil.Register("ThreeGate");
            }
        }

        public void SetSkyToss()
        {
            if (!SkyToss)
            {
                SkyToss = true;
                FileUtil.Register("SkyToss");
            }
        }
    }
}
