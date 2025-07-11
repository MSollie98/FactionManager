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
#if v1_6
        private ThingOwner<Building> unloadedBuildings;
#endif
        public UnloadedWorldObjectComp()
        {
            this.unloadedPawns = new ThingOwner<Pawn>(this, oneStackOnly: false, LookMode.Deep);
#if v1_6
            this.unloadedBuildings = new ThingOwner<Building>(this, oneStackOnly: false, LookMode.Deep);
#endif
            Log.Message("UnloadedWorldObjectComp " + this.parent + " Init");

        }

#if v1_3 || v1_4 || v1_5
//CompTickInterval is not applicable. Also, interval is very smart for prevention of lag ngl.
#else
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);

            if (this.unloadedPawns != null && this.unloadedPawns.Count > 0)
            {
                foreach (Pawn p in this.unloadedPawns)
                {
                    if (p == null)
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

                        if (!p.IsMutant || p.mutant.Def.disableAging)
                        {
                            p.ageTracker.AgeTickInterval(delta);
                        }
                    }
                }
            }

        }
#endif


        public override void CompTick()
        {
            base.CompTick();
#if v1_3 || v1_4 || v1_5
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
#elif (v1_5)
                        if (!p.IsMutant || p.mutant.Def.shouldTickAge)
                        {
                            p.ageTracker.AgeTick();
                        }
#else
                        if (!p.IsMutant || p.mutant.Def.disableAging)
                        {
                            p.ageTracker.AgeTickInterval(delta);
                        }
#endif
                    }
                }
            }

#endif
        }

        public void LoadAllColonists()
        {
            if (PersistenceUtility.utilityStatus != PersistenceUtilityStatus.PostLoading)
            {
                throw new Exception(this + " :: LoadAllColonists called at the wrong time: " + PersistenceUtility.utilityStatus);
            }
#if v1_6
            if (this.unloadedBuildings != null)
            {
                if (unloadedBuildings.Count > 0)
                {
                    if (this.ParentHasMap)
                    {
                        Log.Message("Respawning caskets");
                        Map map = (this.parent as MapParent).Map;
                        UnloadedWorldObjectComp.DropBuildings(map, unloadedBuildings);
                        unloadedBuildings.Clear();
                    }

                }
            }
#endif
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

        private static void DropBuildings(Map map, IEnumerable<Building> things)
        {
            List<List<Building>> thingsGroups = new List<List<Building>>();
            thingsGroups.Clear();
            foreach (Building thing in things)
            {
                List<Building> list = new List<Building>();
                list.Add(thing);
                thingsGroups.Add(list);
            }
            foreach (List<Building> thingsGroup in thingsGroups)
            {
                foreach (Building building in thingsGroup)
                {
                    Log.Message("Respawning Casket " + building.ToString());
                    GenSpawn.SpawnBuildingAsPossible(building, map, true);
                    Log.Message("Respawned Casket !");
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
#if !v1_6
                ActiveDropPodInfo activeDropPodInfo = new ActiveDropPodInfo();
#elif v1_6
                ActiveTransporterInfo activeDropPodInfo = new ActiveTransporterInfo();
#endif
                bool needToPod = false;
                foreach (Thing item2 in thingsGroup)
                {
                    activeDropPodInfo.innerContainer.TryAddOrTransfer(item2);
                    
                    bool canSpawnAt = GenSpawn.CanSpawnAt(item2.def, item2.Position, map, item2.Rotation);

                    if(canSpawnAt)
                    {
                        Log.Message("Thing [" + item2.ToString() + "] can spawn at " + item2.Position.ToString());
                        GenSpawn.Spawn(item2, item2.Position, map, item2.Rotation, WipeMode.VanishOrMoveAside);
                    } else
                    {
                        
                        Log.Warning("Thing [" + item2.ToString() + "] cannot spawn at " + item2.Position.ToString());
                        activeDropPodInfo.innerContainer.TryAddOrTransfer(item2);
                        needToPod = true;
                    }
                }
                activeDropPodInfo.openDelay = openDelay;
                activeDropPodInfo.leaveSlag = leaveSlag;
                
                if(needToPod)
                {
                DropPodUtility.MakeDropPodAt(result, map, activeDropPodInfo, faction);
                }
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
#if v1_6
            if(this.unloadedBuildings == null)
            {
                this.unloadedBuildings = new ThingOwner<Building>(this, oneStackOnly: false, LookMode.Deep);
            }      
#endif
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
#if v1_6
                    if(p.InContainerEnclosed)
                    {
                        if(p.ParentHolder is Building_Casket || p.ParentHolder is Building_HoldingPlatform)
                        {
                            Building casket = (Building)p.ParentHolder;
                            casket.DeSpawnOrDeselect();

                            if(unloadedBuildings.TryAddOrTransfer(casket))
                            {
                                Log.Message(string.Concat("Added building ", casket, " for pawn ",p,"  to unloadedWorldObj: " + this));
                                continue;
                            }
                            else
                            {
                                Log.Error(string.Concat("Couldnt add building ", casket, " for pawn ", p, "  to unloadedWorldObj: " + this));

                            }
                        }
                    }
#endif
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
#if v1_6
                if(this.unloadedBuildings != null)
                {
                    unloadedBuildings.RemoveAll((Building x) => x.Destroyed);
                }
#endif

            }

            Scribe_Deep.Look(ref unloadedPawns, "unloadedPawns", this);
#if v1_6
            Scribe_Deep.Look(ref unloadedBuildings, "unloadedBuildings", this);
            if (this.unloadedBuildings != null)
            {
                Log.Message(this + " :: Have " + this.unloadedBuildings.Count + " unloaded buildings. ");
            }
#endif
            if (this.unloadedPawns != null)
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
