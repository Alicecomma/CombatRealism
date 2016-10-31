using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Combat_Realism
{
    public class Verb_ShootCR : Verb_LaunchProjectileCR
    {
        protected override int ShotsPerBurst
        {
            get
            {
                if (compFireModes != null)
                {
                    if (compFireModes.currentFireMode == FireMode.SingleFire)
                    {
                        return 1;
                    }
                    if ((compFireModes.currentFireMode == FireMode.BurstFire || (useDefaultModes && compFireModes.Props.aiUseBurstMode))
                        && compFireModes.Props.aimedBurstShotCount > 0)
                    {
                        return compFireModes.Props.aimedBurstShotCount;
                    }
                }
                return verbPropsCR.burstShotCount;
            }
        }

        private CompFireModes compFireModesInt = null;
        private CompFireModes compFireModes
        {
            get
            {
                if (compFireModesInt == null && ownerEquipment != null)
                {
                    compFireModesInt = ownerEquipment.TryGetComp<CompFireModes>();
                }
                return compFireModesInt;
            }
        }

        private bool shouldAim
        {
            get
            {
                if (compFireModes != null)
                {
                    if (CasterIsPawn)
                    {
                        // Check for hunting job
                        if (CasterPawn.jobs != null && CasterPawn.jobs.curJob != null && CasterPawn.jobs.curJob.def == JobDefOf.Hunt)
                            return true;

                        // Check for suppression
                        CompSuppressable comp = caster.TryGetComp<CompSuppressable>();
                        if (comp != null)
                        {
                            if (comp.isSuppressed)
                            {
                                return false;
                            }
                        }
                    }
                    return compFireModes.currentAimMode == AimMode.AimedShot || (useDefaultModes && compFireModes.Props.aiUseAimMode);
                }
                return false;
            }
        }
        private bool isAiming = false;
        private int xpTicks = 0;                        // Tracker to see how much xp should be awarded for time spent aiming + bursting

        // How much time to spend on aiming
        private const int aimTicksMin = 30;
        private const int aimTicksMax = 240;

        // XP amounts
        private const float objectXP = 0.1f;
        private const float pawnXP = 0.75f;
        private const float hostileXP = 3.6f;

        protected override float swayAmplitude
        {
            get
            {
                float sway = base.swayAmplitude;
                if (shouldAim)
                {
                    sway *= Mathf.Max(0, 1 - aimingAccuracy);
                }
                return sway;
            }
        }

        // Whether this gun should use default AI firing modes
        private bool useDefaultModes
        {
            get
            {
                return !(caster.Faction == Faction.OfPlayer);
            }
        }

        /// <summary>
        /// Handles activating aim mode at the start of the burst
        /// </summary>
        public override void WarmupComplete()
        {
            if (xpTicks <= 0)
                xpTicks = Mathf.CeilToInt(verbProps.warmupTicks * 0.5f);

            if (shouldAim && !isAiming)
            {
                float targetDist = (currentTarget.Cell - caster.Position).LengthHorizontal;
                int aimTicks = (int)Mathf.Lerp(aimTicksMin, aimTicksMax, (targetDist / 100));
                if (CasterIsPawn)
                {
                    CasterPawn.stances.SetStance(new Stance_Warmup(aimTicks, currentTarget, this));
                    isAiming = true;
                    return;
                }
                else
                {
                    Building_TurretGunCR turret = caster as Building_TurretGunCR;
                    if (turret != null)
                    {
                        turret.burstWarmupTicksLeft += aimTicks;
                        isAiming = true;
                        return;
                    }
                }
            }

            // Shooty stuff
            base.WarmupComplete();
            isAiming = false;
        }

        public override void VerbTickCR()
        {
            if (isAiming)
            {
                xpTicks++;
                if (!shouldAim)
                {
                    WarmupComplete();
                }
                if (CasterIsPawn && CasterPawn.stances.curStance.GetType() != typeof(Stance_Warmup))
                {
                    isAiming = false;
                }
            }
            // Increase shootTicks while bursting so we can calculate XP afterwards
            else if (state == VerbState.Bursting)
            {
                xpTicks++;
            }
            else if (xpTicks > 0)
            {
                // Reward XP to shooter pawn
                if (ShooterPawn != null && ShooterPawn.skills != null)
                {
                    float xpPerTick = objectXP;
                    Pawn targetPawn = currentTarget.Thing as Pawn;
                    if (targetPawn != null)
                    {
                        if (targetPawn.HostileTo(caster.Faction))
                        {
                            xpPerTick = hostileXP;
                        }
                        else
                        {
                            xpPerTick = pawnXP;
                        }
                    }
                    ShooterPawn.skills.Learn(SkillDefOf.Shooting, xpPerTick * xpTicks);
                }
                xpTicks = 0;
            }
        }

        /// <summary>
        /// Reset selected fire mode back to default when gun is dropped
        /// </summary>
        public override void Notify_Dropped()
        {
            base.Notify_Dropped();
            if (compFireModes != null)
            {
                compFireModes.ResetModes();
            }
            caster = null;
        }

        /// <summary>
        /// Checks to see if fire mode is set to hold fire before doing the base check
        /// </summary>
        public override bool CanHitTargetFrom(IntVec3 root, TargetInfo targ)
        {
            if (CasterIsPawn && !CasterPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight)) return false;
            if (compFireModes != null && compFireModes.currentAimMode == AimMode.HoldFire
                && (!CasterIsPawn || CasterPawn.CurJob == null || CasterPawn.CurJob.def != JobDefOf.Hunt))
                return false;
            return base.CanHitTargetFrom(root, targ);
        }

        protected override bool TryCastShot()
        {
            //Reduce ammunition
            if (compAmmo != null)
            {
                if (!compAmmo.TryReduceAmmoCount())
                {
                    if (compAmmo.hasMagazine)
                        compAmmo.TryStartReload();
                    return false;
                }
            }
            if (base.TryCastShot())
            {
                //Drop casings
                if (verbPropsCR.ejectsCasings && projectilePropsCR.dropsCasings)
                {
                    CR_Utility.ThrowEmptyCasing(caster.DrawPos, ThingDef.Named(projectilePropsCR.casingMoteDefname));
                }
                return true;
            }
            return false;
        }
    }
}