using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionManager
{
#if v1_3
    [HarmonyPatch(typeof(MapDeiniter), "PassPawnsToWorld", new Type[] { typeof(Map) })]
    class MapDeiniter_PassPawnsToWorld_Patch
    {
        [HarmonyPrefix]
        static void PassPawnsToWorld(ref Map map)
        {
            if (PersistenceUtility.utilityStatus != PersistenceUtilityStatus.Saving)
                return;
            
            List<Pawn> list3 = map.mapPawns.AllPawns.ToList();
            for (int i = 0; i < list3.Count; i++)
            {
                try
                {
                    Pawn pawn = list3[i];

                    if (!pawn.Destroyed)
                    {
                        pawn.Destroy();
                    }
                    Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not despawn and pass to world " + list3[i] + ": " + ex);
                }
            }
        }
    }
#elif v1_4
    [HarmonyPatch(typeof(MapDeiniter), "PassPawnsToWorld", new Type[] { typeof(Map) })]
    class MapDeiniter_PassPawnsToWorld_Patch
    {
        [HarmonyPrefix]
        static void PassPawnsToWorld(ref Map map)
        {
            if (PersistenceUtility.utilityStatus != PersistenceUtilityStatus.Saving)
                return;

            List<Pawn> list3 = map.mapPawns.AllPawns.ToList();
            for (int i = 0; i < list3.Count; i++)
            {
                try
                {
                    Pawn pawn = list3[i];

                    if (!pawn.Destroyed)
                    {
                        pawn.Destroy();
                    }
                    Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not despawn and pass to world " + list3[i] + ": " + ex);
                }
            }
        }
    }


    [HarmonyPatch(typeof(MapDeiniter), "PassPawnsToWorld_NewTemp", new Type[] { typeof(Map), typeof(bool) })]
    class MapDeiniter_PassPawnsToWorld_NewTemp_Patch
    {
        [HarmonyPrefix]
        static void PassPawnsToWorld_NewTemp(ref Map map, ref bool notifyPlayer)
        {
            if (PersistenceUtility.utilityStatus != PersistenceUtilityStatus.Saving)
                return;
            
            List<Pawn> list3 = map.mapPawns.AllPawns.ToList();
            for (int i = 0; i < list3.Count; i++)
            {
                try
                {
                    Pawn pawn = list3[i];

                    if (!pawn.Destroyed)
                    {
                        pawn.Destroy();
                    }
                    Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not despawn and pass to world " + list3[i] + ": " + ex);
                }
            }
        }
    }
#else
    [HarmonyPatch(typeof(MapDeiniter), "PassPawnsToWorld", new Type[] { typeof(Map), typeof(bool) })]
    class MapDeiniter_PassPawnsToWorld_Patch
    {
        [HarmonyPrefix]
        static void PassPawnsToWorld(ref Map map, ref bool notifyPlayer)
        {
            if (PersistenceUtility.utilityStatus != PersistenceUtilityStatus.Saving)
                return;

            List<Pawn> list3 = map.mapPawns.AllPawns.ToList();
            for (int i = 0; i < list3.Count; i++)
            {
                try
                {
                    Pawn pawn = list3[i];

                    if (!pawn.Destroyed)
                    {
                        pawn.Destroy();
                    }
                    Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not despawn and pass to world " + list3[i] + ": " + ex);
                }
            }
        }
    }
#endif
}
