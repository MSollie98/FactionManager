using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionManager
{
#if !v1_3
    public class UnloadedWorldObjectComp : WorldObjectComp, IThingHolder
    {
        private ThingOwner<Pawn> unloadedPawns;

        public UnloadedWorldObjectComp()
        {
            this.unloadedPawns = new ThingOwner<Pawn>(this, oneStackOnly: false, LookMode.Reference);
            Log.Message("UnloadedWorldObjectComp " + this.parent + " Init");

        }

        public override void CompTick()
        {
            base.CompTick();
            
            if(this.unloadedPawns != null && this.unloadedPawns.Count > 0)
            {
                foreach(Pawn p in this.unloadedPawns)
                {
                    if(p == null)
                    {
                        Log.Warning("Tried to tick a null pawn in " + this);
                        continue;
                    }
                    bool suspended;
                    using (new ProfilerBlock("Suspended"))
                    {
                        suspended = p.Suspended;
                    }
                    if (!suspended)
                    {
#if v1_3 || v1_4
                        p.ageTracker.AgeTick();
#else
                        if (!p.IsMutant || p.mutant.Def.shouldTickAge)
                        {
                            p.ageTracker.AgeTick();
                        }
#endif
                    }
                }
            }


        }

        public void LoadAllColonists()
        {
            if (PersistenceUtility.utilityStatus != PersistenceUtilityStatus.PostLoading)
            {
                throw new Exception(this + " :: LoadAllColonists called at the wrong time: " + PersistenceUtility.utilityStatus);
            }
            if (this.unloadedPawns != null)
            {
                if (unloadedPawns.Count > 0)
                {
                    if (this.ParentHasMap)
                    {
                        Log.Message("Drop-podding colonists");
                        Map map = (this.parent as MapParent).Map;
                        UnloadedWorldObjectComp.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, unloadedPawns, 110, instaDrop: false, leaveSlag: false, canRoofPunch: false, forbid: false);
                        unloadedPawns.Clear();
                    }
                   
                }
            }
        }

        private static void DropThingsNear(IntVec3 dropCenter, Map map, IEnumerable<Thing> things, int openDelay = 110, bool instaDrop = false, bool leaveSlag = false, bool canRoofPunch = true, bool forbid = true, bool allowFogged = true, bool canTransfer = false, Faction faction = null)
        {
            List<List<Thing>> thingsGroups = new List<List<Thing>>();
            thingsGroups.Clear();
            foreach (Thing thing in things)
            {
                List<Thing> list = new List<Thing>();
                list.Add(thing);
                thingsGroups.Add(list);
            }
            foreach (List<Thing> thingsGroup in thingsGroups)
            {
                if (!DropCellFinder.TryFindDropSpotNear(dropCenter, map, out var result, allowFogged, canRoofPunch) && (canRoofPunch || !DropCellFinder.TryFindDropSpotNear(dropCenter, map, out result, allowFogged, canRoofPunch: true)))
                {
                    if (!dropCenter.IsValid)
                    {
                        continue;
                    }
                    Log.Warning(string.Concat("DropThingsNear failed to find a place to drop ", thingsGroup.FirstOrDefault(), " near ", dropCenter, ". Dropping on random square instead."));
                    result = CellFinderLoose.RandomCellWith((IntVec3 c) => c.Walkable(map), map);
                }
                if (forbid)
                {
                    for (int i = 0; i < thingsGroup.Count; i++)
                    {
                        thingsGroup[i].SetForbidden(value: true, warnOnFail: false);
                    }
                }
                if (instaDrop)
                {
                    foreach (Thing item in thingsGroup)
                    {
                        GenPlace.TryPlaceThing(item, result, map, ThingPlaceMode.Near);
                    }
                    continue;
                }
                ActiveDropPodInfo activeDropPodInfo = new ActiveDropPodInfo();
                foreach (Thing item2 in thingsGroup)
                {
                    activeDropPodInfo.innerContainer.TryAddOrTransfer(item2);
                }
                activeDropPodInfo.openDelay = openDelay;
                activeDropPodInfo.leaveSlag = leaveSlag;
                DropPodUtility.MakeDropPodAt(result, map, activeDropPodInfo, faction);
            }
            thingsGroups.Clear();
        }


        public void UnloadAllColonists()
        {
            Log.Message("Unloading all colonists in " + this.parent);
            if (this.parent == null)
            {
                throw new Exception("UnloadAllColonists parent == null");
            }
            if(this.unloadedPawns == null)
            {
                Log.Warning("UnloadAllColonists " + this.parent + " unloadedPawns == null");
                this.unloadedPawns = new ThingOwner<Pawn>(this, oneStackOnly: false, LookMode.Deep);
            }
            if (this.parent is MapParent)
            {
                if(!this.ParentHasMap)
                {
                    throw new Exception("Parent does not have a map");
                }
                Map map = (this.parent as MapParent).Map;
                
                if (map == null)
                {
                    throw new Exception("UnloadAllColonists map == null");
                }
                Log.Message("Have everything we need to unload.");

                if(map.mapPawns == null)
                {
                    throw new Exception("UnloadAllColonists map.mapPawns == null");
                }
                if(map.mapPawns.AllPawns == null)
                {
                    throw new Exception("UnloadAllColonists map.mapPawns.AllPawns == null");
                }

                List<Pawn> list3 = map.mapPawns.AllPawns.ToList();

                Log.Message("Unloading " + list3.Count + " colonists");
                foreach(Pawn p in list3)
                {
                    Log.Message("Unloading " + p);
                    if (p == null)
                    {
                        Log.Warning("Tried to add a null pawn to " + this);
                        return;
                    }
                    if (p.Dead)
                    {
                        Log.Warning(string.Concat("Tried to add ", p, " to ", this, ", but this pawn is dead."));
                        return;
                    }
                    if(p.carryTracker.CarriedThing == null)
                    {
                        Log.Message(string.Concat("Pawn ", p, " is not carrying anything."));
                    } else
                    {
                        Log.Message(string.Concat("Pawn ", p, " is carrying ", p.carryTracker.CarriedThing));
                        Pawn pawn = p.carryTracker.CarriedThing as Pawn;
                        if (pawn != null)
                        {
                            p.carryTracker.innerContainer.Remove(pawn);
                        }
                    }
                    p.DeSpawnOrDeselect();
                    if (unloadedPawns.TryAddOrTransfer(p))
                    {
                       Log.Message(string.Concat("Added pawn ", p, " to unloadedWorldObj: " + this));
                    }
                    else
                    {
                        Log.Error(string.Concat("Couldn't add pawn ", p, " to unloadedWorldObj: " + this));
                    }
                }
            } else
            {

            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {   
                if (this.unloadedPawns != null)
                {
                    unloadedPawns.RemoveAll((Pawn x) => x.Destroyed);
                }

            }

            Scribe_Deep.Look(ref unloadedPawns, "unloadedPawns", this);
            if(this.unloadedPawns != null)
            {
                Log.Message(this + " :: Have " + this.unloadedPawns.Count + " unloaded pawns. ");
            }

        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return unloadedPawns;
        }
    }
#endif
}
