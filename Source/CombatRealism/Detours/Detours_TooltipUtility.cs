using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityCoreLibrary;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism.Detours
{
    internal static class Detours_TooltipUtility
    {
            [DetourClassMethod(typeof(TooltipUtility), "ShotCalculationTipString", InjectionSequence.DLLLoad, InjectionTiming.Priority_23)]
        internal static string ShotCalculationTipString(Thing target)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (Find.Selector.SingleSelectedThing != null)
            {
                Verb verb = null;
                Verb verbCR = null;
                Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
                if (pawn != null && pawn != target && pawn.equipment != null && pawn.equipment.Primary != null && pawn.equipment.PrimaryEq.PrimaryVerb is Verb_LaunchProjectile)
                {
                    verb = pawn.equipment.PrimaryEq.PrimaryVerb;
                }
                Building_TurretGun building_TurretGun = Find.Selector.SingleSelectedThing as Building_TurretGun;
                if (building_TurretGun != null && building_TurretGun != target)
                {
                    verb = building_TurretGun.AttackVerb;
                }
                if (verb != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("ShotBy".Translate(new object[]
                    {
                        Find.Selector.SingleSelectedThing.LabelShort
                    }) + ": ");
                    if (verb.CanHitTarget(target))
                    {
                        stringBuilder.Append(ShotReport.HitReportFor(verb.caster, verb, target).GetTextReadout());
                    }
                    else
                    {
                        stringBuilder.Append("CannotHit".Translate());
                    }
                }
                // Append CR tooltip
                else
                {

                    if (pawn != null && pawn != target && pawn.equipment != null &&
                        pawn.equipment.Primary != null && pawn.equipment.PrimaryEq.PrimaryVerb is Verb_LaunchProjectileCR)
                    {
                        verbCR = pawn.equipment.PrimaryEq.PrimaryVerb;
                    }
                    Building_TurretGun building_TurretGun2 = Find.Selector.SingleSelectedThing as Building_TurretGun;
                    if (building_TurretGun != null && building_TurretGun != target)
                    {
                        verbCR = building_TurretGun.AttackVerb;
                    }
                    if (verbCR != null)
                    {
                        stringBuilder.AppendLine();
                   //   stringBuilder.Append("ShotBy".Translate(new object[] { pawn.LabelShort }) + ":\n");
                        stringBuilder.Append("Shot stat. temporarily unavailable!");
                        if (verbCR.CanHitTarget(target))
                        {
                   //       stringBuilder.Append(ShiftVecReportFor(target).GetTextReadout());
                        }
                        else
                        {
                            stringBuilder.Append("CannotHit".Translate());
                        }
                    }
                }
            }
            return stringBuilder.ToString();
        }
    }
}
