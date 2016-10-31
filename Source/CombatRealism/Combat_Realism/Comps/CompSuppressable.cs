using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class CompSuppressable : ThingComp
    {
        public CompProperties_Suppressable Props
        {
            get
            {
                return (CompProperties_Suppressable)props;
            }
        }

        // --------------- Global constants ---------------

        public const float minSuppressionDist = 15f;        //Minimum distance to be suppressed from, so melee won't be suppressed if it closes within this distance
        private const float maxSuppression = 200f;          //Cap to prevent suppression from building indefinitely
        private const float suppressionDecayRate = 15f;    //How much suppression decays per second
        private const int ticksPerMote = 150;               //How many ticks between throwing a mote

        // --------------- Location calculations ---------------

        /*
         * We track the initial location from which a pawn was suppressed and the total amount of suppression coming from that location separately.
         * That way if suppression stops coming from location A but keeps coming from location B the location will get updated without bouncing 
         * pawns or having to track fire coming from multiple locations
         */
        private IntVec3 suppressorLocInt;
        public IntVec3 suppressorLoc
        {
            get
            {
                return suppressorLocInt;
            }
        }
        private float locSuppressionAmount = 0f;

        // --------------- Suppression calculations ---------------
        private float currentSuppressionInt = 0f;
        public float currentSuppression
        {
            get
            {
                return currentSuppressionInt;
            }
        }
        public float parentArmor
        {
            get
            {
                float armorValue = 0f;
                Pawn pawn = parent as Pawn;
                if (pawn != null)
                {
                    //Get most protective piece of armor
                    if (pawn.apparel.WornApparel != null && pawn.apparel.WornApparel.Count > 0)
                    {
                        List<Apparel> wornApparel = new List<Apparel>(pawn.apparel.WornApparel);
                        foreach (Apparel apparel in wornApparel)
                        {
                            float apparelArmor = apparel.GetStatValue(StatDefOf.ArmorRating_Sharp, true);
                            if (apparelArmor > armorValue)
                            {
                                armorValue = apparelArmor;
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("Tried to get parent armor of non-pawn");
                }
                return armorValue;
            }
        }
        private float suppressionThreshold
        {
            get
            {
                float threshold = 0f;
                Pawn pawn = parent as Pawn;
                if (pawn != null)
                {
                    //Get morale
                    float hardBreakThreshold = pawn.GetStatValue(StatDefOf.MentalBreakThreshold) + 0.15f;
                    float currentMood = pawn.needs != null && pawn.needs.mood != null ? pawn.needs.mood.CurLevel : 0.5f;
                    threshold = Mathf.Max(0, (currentMood - hardBreakThreshold));
                }
                else
                {
                    Log.Error("Tried to get suppression threshold of non-pawn");
                }
                return threshold * maxSuppression * 0.5f;
            }
        }

        public bool isSuppressed = false;
        public bool isHunkering
        {
            get
            {
                if (currentSuppressionInt > suppressionThreshold * 2)
                {
                    if (isSuppressed)
                    {
                        return true;
                    }
                    // Removing suppression log
                            else
                            {
                                Log.Warning("Hunkering without suppression, this should never happen");
                            } 
                }
                return false;
            }
        }

        // --------------- Public functions ---------------
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.LookValue<float>(ref currentSuppressionInt, "currentSuppression", 0f);
            Scribe_Values.LookValue<IntVec3>(ref suppressorLocInt, "suppressorLoc");
            Scribe_Values.LookValue<float>(ref locSuppressionAmount, "locSuppression", 0f);
        }

        public void AddSuppression(float amount, IntVec3 origin)
        {
            //Add suppression to global suppression counter
            currentSuppressionInt += amount;
            if (currentSuppressionInt > maxSuppression)
            {
                currentSuppressionInt = maxSuppression;
            }

            //Add suppression to current suppressor location if appropriate
            if (suppressorLocInt == origin)
            {
                locSuppressionAmount += amount;
            }
            else if (locSuppressionAmount < suppressionThreshold)
            {
                suppressorLocInt = origin;
                locSuppressionAmount = currentSuppressionInt;
            }

            //Assign suppressed status and interrupt activity if necessary
            /*
             * Disabled because suppression works counter-intuitively
            */

            if (!isSuppressed && currentSuppressionInt > suppressionThreshold)
            {
                isSuppressed = true;
                Pawn pawn = parent as Pawn;
                if (pawn != null)
                {
                    if (pawn.jobs != null)
                    {
                        pawn.jobs.StopAll(false);
                    }
                }
                else
                {
                    Log.Error("Trying to suppress non-pawn " + parent.ToString() + ", this should never happen");
                }
            }
            /*

            */
        }

        public override void CompTick()
        {
            base.CompTick();

            //Apply decay once per second
            if (Gen.IsHashIntervalTick(parent, 60))
            {
                //Decay global suppression
                if (currentSuppressionInt > suppressionDecayRate)
                {
                    currentSuppressionInt -= suppressionDecayRate;

                    //Check if pawn is still suppressed
                    if (isSuppressed && currentSuppressionInt <= suppressionThreshold)
                    {
                        isSuppressed = false;
                    }
                }
                else if (currentSuppressionInt > 0)
                {
                    currentSuppressionInt = 0;
                    isSuppressed = false;
                }

                //Decay location suppression
                if (locSuppressionAmount > suppressionDecayRate)
                {
                    locSuppressionAmount -= suppressionDecayRate;
                }
                else if (locSuppressionAmount > 0)
                {
                    locSuppressionAmount = 0;
                }
            }
            //Throw mote at set interval
            if (Gen.IsHashIntervalTick(parent, ticksPerMote))
            {
                if (isSuppressed)
                {
                    MoteMaker.ThrowText(parent.Position.ToVector3Shifted(), "CR_SuppressedMote".Translate());
                }
            }
        }
    }
}