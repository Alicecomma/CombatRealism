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
    internal static class Detours_Tradeable
    {
        // Copy-pasted from Tradeable so we don't have to fetch it through reflection
        private static readonly SimpleCurve LaunchPricePostFactorCurve = new SimpleCurve
        {
            new CurvePoint(2000f, 1f),
            new CurvePoint(12000f, 0.5f),
            new CurvePoint(200000f, 0.2f)
        };

            [DetourClassMethod(typeof(Tradeable), "PriceFor", InjectionSequence.DLLLoad, InjectionTiming.Priority_23)]
        internal static float PriceFor(this Tradeable _this, TradeAction action)
        {
            float num = TradeSession.trader.TraderKind.PriceTypeFor(_this.ThingDef, action).PriceMultiplier();
            float num2 = TradeUtility.RandomPriceFactorFor(TradeSession.trader, _this);
            float num3;
            if (action == TradeAction.PlayerBuys)
            {
                num3 = _this.BaseMarketValue * (1f - TradeSession.playerNegotiator.GetStatValue(StatDefOf.TradePriceImprovement, true)) * num * num2;
                num3 = Mathf.Max(num3, 0.5f);
            }
            else
            {
                num3 = _this.BaseMarketValue * Find.Storyteller.difficulty.baseSellPriceFactor * _this.AnyThing.GetStatValue(StatDefOf.SellPriceFactor, true) * (1f + TradeSession.playerNegotiator.GetStatValue(StatDefOf.TradePriceImprovement, true)) * num * num2;
                num3 *= LaunchPricePostFactorCurve.Evaluate(num3);
                num3 = Mathf.Max(num3, 0.01f);
                if (num3 >= _this.PriceFor(TradeAction.PlayerBuys))
                {
                    Log.ErrorOnce("Skill of negotitator trying to put sell price above buy price.", 65387);
                    num3 = _this.PriceFor(TradeAction.PlayerBuys);
                }
            }
            if (num3 > 99.5f)
            {
                num3 = Mathf.Round(num3);
            }
            return num3;
        }
    }
}
