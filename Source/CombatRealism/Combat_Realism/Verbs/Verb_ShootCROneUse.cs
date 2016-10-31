using System;
using Verse;
using RimWorld;

namespace Combat_Realism
{
	public class Verb_ShootCROneUse : Verb_ShootCR
	{
		protected override bool TryCastShot()
		{
			if (base.TryCastShot())
			{
				if (burstShotsLeft <= 1)
				{
					SelfConsume();
				}
				return true;
			}
            if (compAmmo != null && compAmmo.hasMagazine && compAmmo.curMagCount <= 0)
            {
                SelfConsume();
            }
			else if (burstShotsLeft < verbProps.burstShotCount)
			{
				SelfConsume();
			}
			return false;
		}
		public override void Notify_Dropped()
		{
			if (state == VerbState.Bursting && burstShotsLeft < verbProps.burstShotCount)
			{
				SelfConsume();
			}
		}
		private void SelfConsume()
		{
			if (ownerEquipment != null && !ownerEquipment.Destroyed)
            {
                ownerEquipment.Destroy(DestroyMode.Vanish);
			}
		}
	}
}
