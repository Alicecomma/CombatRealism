using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    class ArtilleryMarker : AttachableThing
    {
        public const string MarkerDef = "ArtilleryMarker";

        public float aimingAccuracy = 1f;
        public float aimEfficiency = 1f;
        public float lightingShift = 0f;
        public float weatherShift = 0f;

        private int lifetimeTicks = 1800;

        public override string InspectStringAddon
        {
            get { return "CR_MarkedForArtillery".Translate() + " " + ((int)(lifetimeTicks / 60)).ToString() + " s"; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<float>(ref aimingAccuracy, "aimingAccuracy");
            Scribe_Values.LookValue<float>(ref aimEfficiency, "aimEfficiency");
            Scribe_Values.LookValue<float>(ref lightingShift, "lightingShift");
            Scribe_Values.LookValue<float>(ref weatherShift, "weatherShift");
            Scribe_Values.LookValue<int>(ref lifetimeTicks, "lifetimeTicks");
        }

        public override void Tick()
        {
            lifetimeTicks--;
            if (lifetimeTicks <= 0)
            {
                Destroy();
            }
        }

        public override void AttachTo(Thing parent)
        {
            if (parent != null)
            {
                CompAttachBase comp = parent.TryGetComp<CompAttachBase>();
                if (comp != null)
                {
                    if (parent.HasAttachment(ThingDef.Named(MarkerDef)))
                    {
                        ArtilleryMarker oldMarker = (ArtilleryMarker)parent.GetAttachment(ThingDef.Named(MarkerDef));
                        oldMarker.Destroy();
                    }
                }
            }
            base.AttachTo(parent);
        }

    }
}
