using System;
using System.Collections.Generic;
using System.Text;

namespace Tyr.Plugins
{
    public interface Plugin
    {
        void OnInitialize();
        void OnStart();
        void OnFrame();
    }
}
