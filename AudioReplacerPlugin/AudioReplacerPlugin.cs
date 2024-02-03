using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace AudioReplacerPlugin
{
    [BepInPlugin("TitaniumTurbine.AudioReplacer", "AudioReplacer.Plugin", "0.1.0")]
    public class AudioReplacerPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(AudioReplacerPlugin));
        }

        [HarmonyPatch(typeof(ItemDropship), "Start")]
        [HarmonyPostfix]
        public static void DropshipAudioPatch(ref AudioClip[] ___chitterSFX)
        {
            
        }
    }
}
