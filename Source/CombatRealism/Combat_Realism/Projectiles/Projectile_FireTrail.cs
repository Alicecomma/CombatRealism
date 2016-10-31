using System;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class Projectile_FireTrail : ProjectileCR
    {
        private int TicksforAppearence = 5;
        private int ticksToDetonation;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref ticksToDetonation, "ticksToDetonation", 0, false);
        }
        public override void Tick()
        {
            base.Tick();
            if (ticksToDetonation > 0)
            {
                ticksToDetonation--;
                if (ticksToDetonation <= 0)
                {
                    Explode();
                }
            }
            if (--TicksforAppearence == 0)
            {
                ThrowFireTrail(Position.ToVector3Shifted(), 0.5f);
                TicksforAppearence = 5;
            }
        }
        protected override void Impact(Thing hitThing)
        {
            if (def.projectile.explosionDelay == 0)
            {
                Explode();
                return;
            }
            landed = true;
            ticksToDetonation = def.projectile.explosionDelay;
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, def.projectile.damageDef, launcher.Faction);
        }
        protected virtual void Explode()
        {
            Destroy(DestroyMode.Vanish);
            ProjectilePropertiesCR propsCR = def.projectile as ProjectilePropertiesCR;
            ThingDef preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
            float explosionSpawnChance = def.projectile.explosionSpawnChance;
            GenExplosion.DoExplosion(Position,
                def.projectile.explosionRadius,
                def.projectile.damageDef,
                launcher,
                def.projectile.soundExplode,
                def,
                equipmentDef,
                def.projectile.postExplosionSpawnThingDef,
                def.projectile.explosionSpawnChance,
                1,
                propsCR == null ? false : propsCR.damageAdjacentTiles,
                preExplosionSpawnThingDef,
                def.projectile.explosionSpawnChance,
                1);
            CompExplosiveCR comp = this.TryGetComp<CompExplosiveCR>();
            if (comp != null)
            {
                comp.Explode(launcher, Position);
            }
        }
        public static void ThrowFireTrail(Vector3 loc, float size)
        {
            if (!loc.ShouldSpawnMotesAt() || MoteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_Firetrail"), null);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.exactRotation = Rand.Range(-0.5f, 0.5f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.008f, 0.012f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3());
        }
    }
}