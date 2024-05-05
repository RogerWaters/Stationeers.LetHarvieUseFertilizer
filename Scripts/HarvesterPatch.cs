using HarmonyLib;
using Assets.Scripts;
using Assets.Scripts.Util;
using Assets.Scripts.Objects.Chutes;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Genetics;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;
using Plant = Assets.Scripts.Objects.Items.Plant;

namespace LetHarvieUseFertilizer.Scripts
{
	[HarmonyPatch(typeof(Harvester))]
	public class HarvesterPatch
    {
        private static readonly Traverse TryPlantSeeds = 
            Traverse.Create<Harvester>().Method("TryPlantSeed");

		[HarmonyPatch("TryPlantSeed")]
		[HarmonyPrefix]
		public static bool TryPlantSeed(Harvester __instance, ref bool __result, bool ____isHarvesting, bool ____isPlanting)
		{
            if (!GameManager.RunSimulation || !__instance.Powered || !__instance.OnOff)
            {
                __result = false;
                return false; // skip original method
            }
            if (GameManager.IsThread)
            {
                var tryPlantSeed = Traverse.Create(__instance).Method("TryPlantSeed");
                UnityMainThreadDispatcher.Instance()
                    .Enqueue(() => tryPlantSeed.GetValue());
                __result = true;
                return false; // skip original method
            }

            var currentState = (ArmControl)__instance.Activate;
            if (currentState != ArmControl.Idle && (____isHarvesting || ____isPlanting))
            {
                __result = false;
                return false; // skip original method
            }
            var robotHandSlot = __instance.Slots[2];
            if (__instance.ImportingThing is Plant importPlant)
            {
                GeneCollection genes =
                    GameManager.RunSimulation ?
                        GeneCollection.Copy(importPlant.Genes) :
                        null;
                if (importPlant.OnUseItem(1f, importPlant))
                {
                    Plant plant;
                    if (!(importPlant is Seed seed))
                    {
                        plant = OnServer.Create<Plant>(importPlant.SourcePrefab, robotHandSlot);
                    }
                    else
                    {
                        plant = OnServer.Create<Plant>(seed.PlantType, robotHandSlot);
                    }
                    if (plant)
                    {
                        plant.ApplySeedTraits(genes);
                    }
                }
            }
            if (__instance.ImportingThing is Fertiliser fertilizer)
            {
                if (fertilizer.OnUseItem(1f, fertilizer))
                {
                    OnServer.Create<Fertiliser>(fertilizer.SourcePrefab, robotHandSlot);
                }
            }

            OnServer.Interact(__instance.InteractActivate, 1);
            __result = true;

            return false; // skip original method
        }

		[HarmonyPatch("OnArmPlant")]
		[HarmonyPrefix]
		public static bool OnArmPlant(Harvester __instance, IHarvestable ____hydroponicTray)
        {
            if (!GameManager.RunSimulation)
            {
                return false;
            }
            var robotHandSlot = __instance.Slots[2];
            var thing = robotHandSlot.Get();
            var plantOccupant = thing as Plant;
            var fertilizerOccupant = thing as Fertiliser;

            if (!(plantOccupant) && !(fertilizerOccupant))
            {
                return false;
            }

            thing.SetQuantity(1);
            if (____hydroponicTray.IsBeingDestroyed)
            {
                OnServer.MoveToWorld(thing);
                return false;
            }
            if (fertilizerOccupant)
            {
                if (____hydroponicTray is HydroponicTray tray)
                {
                    if (tray.InputSlot1.Get())
                    {
                        OnServer.MoveToWorld(thing);
                        return false;
                    }
                    OnServer.MoveToSlot(thing, tray.InputSlot1);
                } 
                else if (____hydroponicTray is HydroponicsTrayDevice trayDevice)
                {
                    if (trayDevice.InputSlot1.Get())
                    {
                        OnServer.MoveToWorld(thing);
                        return false;
                    }
                    OnServer.MoveToSlot(thing, trayDevice.InputSlot1);
                }
                else
                {
                    OnServer.MoveToWorld(thing);
                }
            }
            else if (plantOccupant)
            {
                if (____hydroponicTray.InputSlot.Get())
                {
                    OnServer.MoveToWorld(thing); 
                    return false;
                }
                plantOccupant.PlanterName = ____hydroponicTray is IGrower hydroponicTray ? hydroponicTray.CustomName : (string)null;
                OnServer.MoveToSlot(thing, ____hydroponicTray.InputSlot);
            }

            return false;
		}
	}
}