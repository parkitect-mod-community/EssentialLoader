/**
* Copyright 2019 PMC
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Parkitect.Mods.AssetPacks;
using UnityEngine;

namespace PMC.ExtendedLoader
{
    public class Main : AssetMod
    {
        private static Dictionary<string, ExtendedLoader> ExtenedLoader { get; } =
            new Dictionary<string, ExtendedLoader>();

        public static HashSet<string> GuidTracker = new HashSet<string>();

        private readonly Harmony _harmony = new Harmony("PMC.ExtendedLoader");
        // private readonly bool _isPatched = false;
        // public Main()
        // {
        //     _harmony.PatchAll();
        // }

        public override void onEnabled()
        {
            _harmony.PatchAll();
        }

        public override void onDisabled()
        {
            foreach (var loaders in ExtenedLoader.Values)
            {
                loaders.onDisabled();
            }
            ExtenedLoader.Clear();
        }

        [HarmonyPatch(typeof(ModManager.ModEntry))]
        class ModEntryPatch01
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(ModManager.ModEntry.disableMod))]
            public static void onDisablePostfix(ModManager.ModEntry __instance, ref bool __result)
            {


                Debug.Log("Extended loader/ Disabling:" + __instance.mod.Identifier);

                if (ExtenedLoader.ContainsKey(__instance.mod.Identifier) && ExtenedLoader[__instance.mod.Identifier].IsLoaded)
                    ExtenedLoader[__instance.mod.Identifier].onDisabled();
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(ModManager.ModEntry.enableMod))]
            public static void onEnablePostfix(ModManager.ModEntry __instance, ref bool __result)
            {
                if (!(GameController.Instance != null && GameController.Instance.isCampaignScenario)  && __result)
                {
                    if (!ExtenedLoader.ContainsKey(__instance.mod.Identifier))
                    {
                        string[] files = Directory.GetFiles(__instance.path, "*.assetProject",
                            SearchOption.TopDirectoryOnly);
                        if (files.Length != 0)
                        {
                            ExtendedLoader loader = new ExtendedLoader(files[0]);
                            ExtenedLoader.Add(__instance.mod.Identifier, loader);
                        }
                    }

                    Debug.Log("Extended loader/ Loading:" + __instance.mod.Identifier);
                    if (ExtenedLoader.ContainsKey(__instance.mod.Identifier))
                    {
                        try
                        {
                            if (!ExtenedLoader[__instance.mod.Identifier].IsLoaded)
                                ExtenedLoader[__instance.mod.Identifier].OnEnabled();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }
            }
        }


    }
}

