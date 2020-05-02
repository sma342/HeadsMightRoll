using RimWorld;
using Verse;

namespace HeadsMightRoll
{
    public class Tools
    {
        // Debug
        public static void Warn(string warning, bool debug = false)
        {
            if (debug)
                Log.Warning(warning);
        }


        public static string PawnResumeString(Pawn pawn)
        {
            return (
                pawn?.LabelShort.CapitalizeFirst() +
                ", " +
                (int)pawn?.ageTracker?.AgeBiologicalYears + " y/o" +
                " " +
                pawn.gender.GetLabel() +
            //    " " + pawn?.gender.ToString()?.Translate()?.ToLower() +
                ", " + pawn?.def?.label + "(" + pawn.kindDef + ")"
            );
        }

    }
}