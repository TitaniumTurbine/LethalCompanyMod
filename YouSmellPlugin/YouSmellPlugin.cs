using BepInEx;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace LethalCompanyMod.YouSmellPlugin
{
    [BepInPlugin("LethalCompanyMod.YouSmell", "YouSmell.Plugin", "0.1.0")]
    public class YouSmellPlugin : BaseUnityPlugin
    {
        
        private void Awake()
        {
            // Plugin startup logic
            

            Harmony.CreateAndPatchAll(typeof(YouSmellPlugin));
        }

        [HarmonyPatch(typeof(MenuManager), "Start")]
        [HarmonyPrefix]
        static bool PatchMenuManagerStart()
        {
            var gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach(var gameObject in gameObjects)
            {
                if(gameObject.name == "Canvas")
                {
                    var mm = gameObject.GetComponentInChildren<MenuManager>();
                    mm.DisplayMenuNotification("You smell", "[ Ok ]");
                }
            }

            return true;
        }
    }
}
