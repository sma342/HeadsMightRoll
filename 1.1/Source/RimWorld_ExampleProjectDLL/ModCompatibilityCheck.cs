using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace HeadsMightRoll
{
    [StaticConstructorOnStartup]
    public class ModCompatibilityCheck
    {
        private const string modSpecificName_ModName = "whatever";

        public static bool modSpecificNameIsActive
        {
            get
            {
                return ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name == modSpecificName_ModName);
            }
        }
    }
}