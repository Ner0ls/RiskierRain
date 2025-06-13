using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwanSongExtended.Storms
{
    public class StormObjectiveTracker : ObjectivePanelController.ObjectiveTracker
    {
        public StormObjectiveTracker()
        {
            this.baseToken = StormsCore.stormShelterObjectiveToken;
        }
    }
}
