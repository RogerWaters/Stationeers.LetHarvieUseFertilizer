using BepInEx;
using HarmonyLib;
using System;
using UnityEngine;

namespace LetHarvieUseFertilizer.Scripts
{
    #region BepInEx
    [BepInEx.BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class LetHarvieUseFertilizerMod : BepInEx.BaseUnityPlugin
    {
        public const string pluginGuid = "Grilled_Salmon.LetHarvieUseFertilizer";
        public const string pluginName = "LetHarvieUseFertilizer";
        public const string pluginVersion = "0.2";
        public static void Log(string line)
        {
            Debug.Log("[" + pluginName + "]: " + line);
        }

        void Awake()
        {
            try
            {
                Log("Loaded");
                var harmony = new Harmony(pluginGuid);
                harmony.PatchAll();
                Log("Patch succeeded");
            }
            catch (Exception e)
            {
                Log("Patch Failed");
                Log(e.ToString());
            }
        }
    }
    #endregion
}
