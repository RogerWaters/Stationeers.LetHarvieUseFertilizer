using Stationeers.Addons;
using UnityEngine;

namespace LetHarvieUseFertilizer.Scripts
{
    public class LetHarvieUseFertilizer : IPlugin
    {
        public void OnLoad()
        {
            Debug.Log(LetHarvieUseFertilizer.ModName + ": Loaded");
        }

        public void OnUnload()
        {
            Debug.Log(LetHarvieUseFertilizer.ModName + ": Unloaded");
        }

        public static string WorkshopId = "";

        public static string ModName = "LetHarvieUseFertilizer";
    }
}
