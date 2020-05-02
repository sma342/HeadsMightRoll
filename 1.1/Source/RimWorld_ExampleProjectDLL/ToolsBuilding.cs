using RimWorld;
using Verse;


namespace HeadsMightRoll
{

    public class ToolsBuilding
    {

        public static bool CheckBuilding(Building building)
        {
            if (building == null || building.Map == null || building.Position == null)
                return false;
            return true;
        }

    }
}