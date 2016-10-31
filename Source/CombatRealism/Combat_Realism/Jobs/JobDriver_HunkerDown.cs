using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Combat_Realism
{
    class JobDriver_HunkerDown : JobDriver
    {
        private const int getUpCheckInterval = 10;
        private bool startedIncapacitated;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<bool>(ref this.startedIncapacitated, "startedIncapacitated", false, false);
        }

        public override PawnPosture Posture
        {
            get
            {
                return PawnPosture.LayingAny;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            //Define Toil
            Toil toilWait = new Toil();
            toilWait.initAction = () =>
            {
                toilWait.actor.pather.StopDead();
            };

            Toil toilNothing = new Toil();
            //toilNothing.initAction = () => {};
            toilNothing.defaultCompleteMode = ToilCompleteMode.Delay;
            toilNothing.defaultDuration = getUpCheckInterval;



            Toil shootInPanic = new Toil()
            {
                initAction = delegate
                {
                    Pawn pawnAgressor = this.TargetThingA as Pawn;
                    if (pawnAgressor != null)
                    {
                        this.startedIncapacitated = pawnAgressor.Downed;
                    }
                    this.pawn.pather.StopDead();
                },
                tickAction = delegate
                {
                    if (this.TargetA.HasThing)
                    {
                        Pawn pawn = this.TargetA.Thing as Pawn;
                        if (this.TargetA.Thing.Destroyed || (pawn != null && !this.startedIncapacitated && pawn.Downed))
                        {
                            this.EndJobWith(JobCondition.Succeeded);
                            return;
                        }
                    }
                    this.pawn.equipment.TryStartAttack(this.TargetA);
                },
                defaultDuration = 5,
                defaultCompleteMode = ToilCompleteMode.Delay
            };

            // Start Toil
  //          yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
    //        yield return shootInPanic;
            yield return toilWait;
            yield return toilNothing;
            yield return Toils_Jump.JumpIf(toilNothing, () =>
            {
                CompSuppressable comp = pawn.TryGetComp<CompSuppressable>();
                if (comp == null)
                {
                    return false;
                }
                float distToSuppressor = (pawn.Position - comp.suppressorLoc).LengthHorizontal;
                if (distToSuppressor < CompSuppressable.minSuppressionDist)
                {
                    return false;
                }
                return comp.isHunkering;
            });
        }
    }
}
