using System;
using System.Collections.Generic;
using System.Text;

namespace SC2Sharp.Plugins
{
    public interface Plugin
    {
        void OnInitialize();
        void OnStart();
        void OnFrame();
    }
}
