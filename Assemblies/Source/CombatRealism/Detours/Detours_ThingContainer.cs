﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism.Detours
{
    public static class Detours_ThingContainer
    {
        private static FieldInfo innerListFieldInfo = typeof(ThingContainer).GetField("innerList", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo maxStacksFieldInfo = typeof(ThingContainer).GetField("maxStacks", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool TryAdd(this ThingContainer _this, Thing item)
        {
            if (item.stackCount > _this.AvailableStackSpace)
            {
                Log.Error(string.Concat(new object[]
		{
			"Add item with stackCount=",
			item.stackCount,
			" with only ",
			_this.AvailableStackSpace,
			" in container. Splitting and adding..."
		}));
                return _this.TryAdd(item, _this.AvailableStackSpace);
            }

            List<Thing> innerList = (List<Thing>)innerListFieldInfo.GetValue(_this);    // Fetch innerList through reflection

            SlotGroupUtility.Notify_TakingThing(item);
            if (item.def.stackLimit > 1)
            {
                for (int i = 0; i < innerList.Count; i++)
                {
                    if (innerList[i].def == item.def)
                    {
                        int num = item.stackCount;
                        if (num > _this.AvailableStackSpace)
                        {
                            num = _this.AvailableStackSpace;
                        }
                        Thing other = item.SplitOff(num);
                        if (!innerList[i].TryAbsorbStack(other, false))
                        {
                            Log.Error("ThingContainer did TryAbsorbStack " + item + " but could not absorb stack.");
                        }
                    }
                    if (item.Destroyed)
                    {
                        return true;
                    }
                }
            }

            int maxStacks = (int)maxStacksFieldInfo.GetValue(_this);    // Fetch maxStacks through reflection

            if (innerList.Count >= maxStacks)
            {
                return false;
            }
            if (item.SpawnedInWorld)
            {
                item.DeSpawn();
            }
            if (item.HasAttachment(ThingDefOf.Fire))
            {
                item.GetAttachment(ThingDefOf.Fire).Destroy(DestroyMode.Vanish);
            }
            item.holder = _this;
            innerList.Add(item);
            Pawn pawn = item as Pawn;
            if (pawn != null && !pawn.Downed)
            {
                Find.Reservations.ReleaseAllClaimedBy(pawn);
                pawn.stances.CancelBusyStanceSoft();
                pawn.jobs.StopAll(false);
                pawn.pather.StopDead();
                if (pawn.Drafted)
                {
                    pawn.drafter.Drafted = false;
                }
            }
            Utility.TryUpdateInventory(_this.owner as Pawn_InventoryTracker);   // Item has been added, notify CompInventory
            return true;
        }

        public static bool TryDrop(this ThingContainer _this, Thing thing, IntVec3 dropLoc, ThingPlaceMode mode, out Thing lastResultingThing)
        {
            List<Thing> innerList = (List<Thing>)innerListFieldInfo.GetValue(_this);    // Fetch innerList through reflection

            if (!innerList.Contains(thing))
            {
                Log.Error(string.Concat(new object[]
		{
			_this.owner,
			" container tried to drop  ",
			thing,
			" which it didn't contain."
		}));
                lastResultingThing = null;
                return false;
            }
            if (GenDrop.TryDropSpawn(thing, dropLoc, mode, out lastResultingThing))
            {
                innerList.Remove(thing);
                Utility.TryUpdateInventory(_this.owner as Pawn_InventoryTracker);
                return true;
            }
            return false;
        }

        public static bool TryDrop(this ThingContainer _this, Thing thing, IntVec3 dropLoc, ThingPlaceMode mode, int count, out Thing resultingThing)
        {
            if (thing.stackCount < count)
            {
                Log.Error(string.Concat(new object[]
		{
			"Tried to drop ",
			count,
			" of ",
			thing,
			" while only having ",
			thing.stackCount
		}));
                count = thing.stackCount;
            }
            if (count == thing.stackCount)
            {
                if (GenDrop.TryDropSpawn(thing, dropLoc, mode, out resultingThing))
                {
                    List<Thing> innerList = (List<Thing>)innerListFieldInfo.GetValue(_this);    // Fetch innerList through reflection
                    innerList.Remove(thing);
                    Utility.TryUpdateInventory(_this.owner as Pawn_InventoryTracker);   // Thing dropped, update inventory
                    return true;
                }
                return false;
            }
            else
            {
                Thing thing2 = thing.SplitOff(count);
                if (GenDrop.TryDropSpawn(thing2, dropLoc, mode, out resultingThing))
                {
                    Utility.TryUpdateInventory(_this.owner as Pawn_InventoryTracker);   // Thing dropped, update inventory
                    return true;
                }
                thing.stackCount += thing2.stackCount;
                return false;
            }
        }
    }
}