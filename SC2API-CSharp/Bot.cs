using System;
using System.Collections.Generic;
using System.Text;
using SC2APIProtocol;

namespace SC2API_CSharp
{
    public interface Bot
    {
        IEnumerable<SC2APIProtocol.Action> onFrame(ResponseObservation observation);
        void OnEnd(ResponseObservation observation, Result result);
        void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponseObservation observation, uint playerId, String opponentId);
    }
}
