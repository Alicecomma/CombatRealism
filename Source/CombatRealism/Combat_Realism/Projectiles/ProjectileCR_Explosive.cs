using System;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    //Cloned from vanilla, completely unmodified
    public class ProjectileCR_Explosive : ProjectileCR
    {
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
            ThrowBigExplode(Position.ToVector3Shifted() + Gen.RandomHorizontalVector(def.projectile.explosionRadius * 0.7f), def.projectile.explosionRadius * 0.6f);
            CompExplosiveCR comp = this.TryGetComp<CompExplosiveCR>();
            if (comp != null)
            {
                comp.Explode(launcher, Position);
            }
        }

        public static void ThrowBigExplode(Vector3 loc, float size)
        {
            if (!loc.ShouldSpawnMotesAt())
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_BigExplode"), null);
            moteThrown.Scale = Rand.Range(5f, 6f) * size;
            moteThrown.exactRotation = Rand.Range(0f, 0f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(6, 8), Rand.Range(0.002f, 0.003f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3());
        }
    }
}