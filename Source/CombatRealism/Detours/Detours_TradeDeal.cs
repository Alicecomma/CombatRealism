using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CommunityCoreLibrary;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism.Detours
{
    internal static class Detours_TradeDeal
    {
        private static readonly FieldInfo pawnFieldInfo = typeof(Pawn_EquipmentTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);

            [DetourClassMethod(typeof(TradeDeal), "UpdateCurrencyCount", InjectionSequence.DLLLoad, InjectionTiming.Priority_23)]
        internal static void UpdateCurrencyCount(this TradeDeal _this)
        {
            float silverCount = 0f;
            FieldInfo tradeablesFieldInfo = typeof(TradeDeal).GetField("tradeables", BindingFlags.Instance | BindingFlags.NonPublic);
            List<Tradeable> tradeables = (List<Tradeable>)tradeablesFieldInfo.GetValue(_this);
            foreach (Tradeable current in tradeables)
            {
                if (!current.IsCurrency)
                {
                    silverCount += current.CurTotalSilverCost;
                }
            }
            _this.SilverTradeable.countToDrop = -Mathf.CeilToInt(silverCount);
        }
    }
}
