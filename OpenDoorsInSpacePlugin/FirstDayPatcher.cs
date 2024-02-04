using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace OpenDoorsInSpacePlugin
{
    internal class FirstDayPatcher
    {
        public static Harmony harmony = new Harmony("FistDayPatcher");



        [HarmonyPatch(typeof(StartOfRound), "PlayFirstDayShipAnimation")]
        [HarmonyPrefix]
        static bool FirstDay()
        {
            harmony.UnpatchSelf();

            return false;
        }
    }
}
