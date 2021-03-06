﻿using System.Collections.Generic;

namespace SC2Sharp.Managers
{
    public class EffectManager : Manager
    {
        public List<Effect> Effects = new List<Effect>();
        
        public void OnFrame(Bot bot)
        {
            Update(bot);

            for (int i = Effects.Count - 1; i >= 0; i--)
            {
                Effect effect = Effects[i];
                if (bot.Frame - effect.LastSeenFrame >= 23)
                {
                    Effects[i] = Effects[Effects.Count - 1];
                    Effects.RemoveAt(Effects.Count - 1);
                }
            }
        }

        public void Update(Bot bot)
        {
            if (Bot.Main.Observation.Observation.RawData.Effects == null)
                return;

            foreach (SC2APIProtocol.Effect effect in Bot.Main.Observation.Observation.RawData.Effects)
                if (effect.EffectId == 11)
                {
                    bool found = false;
                    foreach (Effect previous in Effects)
                    {
                        if (effect.EffectId == previous.EffectId
                            && effect.Pos[0].X == previous.Pos.X
                            && effect.Pos[0].Y == previous.Pos.Y)
                        {
                            previous.LastSeenFrame = bot.Frame;
                            found = true;
                            break;
                        }
                            
                    }

                    if (!found)
                        Effects.Add(new Effect() { Pos = effect.Pos[0], EffectId = effect.EffectId, LastSeenFrame = bot.Frame, FirstSeenFrame = bot.Frame });
                }
        }
    }
}
