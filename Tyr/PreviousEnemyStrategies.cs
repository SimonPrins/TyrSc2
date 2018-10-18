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
                Tyr.Bot.Register("CannonRush");
            }
        }

        public void SetLifting()
        {
            if (!Lifting)
            {
                Lifting = true;
                Tyr.Bot.Register("Lifting");
            }
        }

        public void SetMassRoach()
        {
            if (!MassRoach)
            {
                MassRoach = true;
                Tyr.Bot.Register("MassRoach");
            }
        }

        public void SetMassHydra()
        {
            if (!MassHydra)
            {
                MassHydra = true;
                Tyr.Bot.Register("MassHydra");
            }
        }

        public void SetFourRax()
        {
            if (!FourRax)
            {
                FourRax = true;
                Tyr.Bot.Register("FourRax");
            }
        }

        public void SetReaperRush()
        {
            if (!ReaperRush)
            {
                ReaperRush = true;
                Tyr.Bot.Register("ReaperRush");
            }
        }

        public void SetTerranTech()
        {
            if (!TerranTech)
            {
                TerranTech = true;
                Tyr.Bot.Register("TerranTech");
            }
        }

        public void SetMech()
        {
            if (!Mech)
            {
                Mech = true;
                Tyr.Bot.Register("Mech");
            }
        }

        public void SetBio()
        {
            if (!Bio)
            {
                Bio = true;
                Tyr.Bot.Register("Bio");
            }
        }

        public void SetThreeGate()
        {
            if (!ThreeGate)
            {
                ThreeGate = true;
                Tyr.Bot.Register("ThreeGate");
            }
        }

        public void SetSkyToss()
        {
            if (!SkyToss)
            {
                SkyToss = true;
                Tyr.Bot.Register("SkyToss");
            }
        }
    }
}
