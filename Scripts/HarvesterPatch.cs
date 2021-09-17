using Assets.Scripts.Objects;
using HarmonyLib;
using System;
using UnityEngine;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Networking;
using System.Reflection;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Util;
using Assets.Scripts.Objects.Chutes;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;

namespace LetHarvieUseFertilizer.Scripts
{
	[HarmonyPatch(typeof(Harvester))]
	public class HarvesterPatch
	{
		public static bool GetIsTray(Harvester harvester)
		{
			return (harvester.HydroponicTray != null) ? harvester.HydroponicTray.GetThing : null;
		}

		public static bool GetIsHarvesting(Harvester harvester)
        {
			bool isHarvesting = false;
			try
			{
				var harvesterType = typeof(Harvester);
				var harvesterField = harvesterType.GetField("_isHarvesting", BindingFlags.NonPublic | BindingFlags.Instance);
				isHarvesting = (bool)harvesterField.GetValue(harvester);
			}
			catch(Exception ex)
            {
				Debug.LogError(LetHarvieUseFertilizer.ModName + ": Could not get _isHarvesting via Reflection of Harvester: " + ex.Message);
            }
			return isHarvesting;
		}

		public static bool GetIsPlanting(Harvester harvester)
		{
			bool isPlanting = false;
			try
			{
				var harvesterType = typeof(Harvester);
				var harvesterField = harvesterType.GetField("_isPlanting", BindingFlags.NonPublic | BindingFlags.Instance);
				isPlanting = (bool)harvesterField.GetValue(harvester);
			}
			catch (Exception ex)
			{
				Debug.LogError(LetHarvieUseFertilizer.ModName + ": Could not get _isPlanting via Reflection of Harvester: " + ex.Message);
			}
			return isPlanting;
		}

		[HarmonyPatch("TryPlantSeed")]
		[HarmonyPrefix]
		public static bool TryPlantSeed(Harvester __instance, ref bool __result)
		{
			bool flag = !GameManager.IsServer;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool flag2 = !__instance.Powered || !__instance.OnOff;
				if (flag2)
				{
					result = false;
				}
				else
				{
					bool isThread = GameManager.IsThread;
					if (isThread)
					{
						UnityMainThreadDispatcher.Instance().Enqueue(delegate ()
						{
							__instance.TryPlantSeed();
						});
						result = true;
					}
					else
					{
						bool flag3 = (ArmControl)__instance.Activate == ArmControl.Idle || (!GetIsHarvesting(__instance) && !GetIsPlanting(__instance));
						if (flag3)
						{
							Plant importPlant = __instance.ImportingThing as Plant;
							bool flag4 = importPlant != null && importPlant.OnUseItem(1f, __instance.ImportingThing as Plant);
							if (flag4)
							{
								Seed seed = __instance.ImportingThing as Seed;
								bool flag5 = seed != null;
								DynamicThing childThing;
								if (flag5)
								{
									childThing = OnServer.Create(seed.PlantType, __instance.ThingTransform.position, __instance.ThingTransform.rotation, __instance.OwnerSteamId, null);
								}
								else
								{
									childThing = OnServer.Create(__instance.ImportingThing.Prefab as Plant, __instance.ThingTransform.position, __instance.ThingTransform.rotation, __instance.OwnerSteamId, null);
								}
								OnServer.MoveToSlot(childThing, __instance.GetRobotHandSlot);
							}
							Fertiliser importFertilizer = __instance.ImportingThing as Fertiliser;
							bool flag6 = importFertilizer != null && importFertilizer.OnUseItem(1f, __instance.ImportingThing as Fertiliser);
							if (flag6)
							{
								Fertiliser fert = __instance.ImportingThing as Fertiliser;
								bool flag7 = fert != null;
								DynamicThing childThing;
								if (flag7)
								{
									childThing = OnServer.Create(fert, __instance.ThingTransform.position, __instance.ThingTransform.rotation, __instance.OwnerSteamId, null);
								}
                                else 
								{
									childThing = OnServer.Create(__instance.ImportingThing.Prefab as Fertiliser, __instance.ThingTransform.position, __instance.ThingTransform.rotation, __instance.OwnerSteamId, null);
								}
								OnServer.MoveToSlot(childThing, __instance.GetRobotHandSlot);
							}
							OnServer.Interact(__instance.InteractActivate, 1, false);
							result = true;
						}
						else
						{
							result = false;
						}
					}
				}
			}
			__result = result;

			return false; // skip original method
		}

		[HarmonyPatch("OnArmPlant")]
		[HarmonyPrefix]
		public static bool OnArmPlant(Harvester __instance)
		{
			Plant plant;
			Fertiliser fert;
			if (GameManager.IsServer)
			{
				plant = (__instance.GetRobotHandSlot.Occupant as Plant);
			}
			else
			{
				plant = null;
			}
			if (plant != null)
			{
				plant.NetworkQuantity = 1;
				bool flag3 = !GetIsTray(__instance) || __instance.HydroponicTray.IsBeingDestroyed;
				if (flag3)
				{
					OnServer.MoveToWorld(plant);
				}
				else
				{
					OnServer.MoveToSlot(plant, __instance.HydroponicTray.InputSlot);
				}
			}
			if (GameManager.IsServer)
			{
				fert = (__instance.GetRobotHandSlot.Occupant as Fertiliser);
			}
			else
			{
				fert = null;
			}
			if (fert != null)
			{
				fert.NetworkQuantity = 1;
				bool flag3 = !GetIsTray(__instance) || __instance.HydroponicTray.IsBeingDestroyed;
				if (flag3)
				{
					OnServer.MoveToWorld(fert);
				}
				else
				{
					HydroponicTray tray = __instance.HydroponicTray as HydroponicTray;
					HydroponicsTrayDevice device = __instance.HydroponicTray as HydroponicsTrayDevice;
					if (tray != null)
                    {
						OnServer.MoveToSlot(fert, (__instance.HydroponicTray as HydroponicTray).InputSlot1);
					}
					else if (device != null)
                    {
						OnServer.MoveToSlot(fert, (__instance.HydroponicTray as HydroponicsTrayDevice).InputSlot1);
					}
					else
                    {
						Debug.LogError(LetHarvieUseFertilizer.ModName + ": Could not get HydroponicTray slot1 of Harvester");
					}	
				}
			}

			return false; // skip original method
		}
	}
}