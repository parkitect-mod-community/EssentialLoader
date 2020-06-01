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
using UnityEngine;

namespace PMC.Shop
{
    public class ShopLoaderMain : IMod
    {
        public static Dictionary<String,ShopLoader> ShopLoaders { get; } = new Dictionary<string, ShopLoader>();

        public ShopLoaderMain()
        {
            // Harmony harmony = new Harmony("PMC.Shop");
            // harmony.PatchAll();
        }

        public void onEnabled()
        {

            foreach (var modEntry in ModManager.Instance.getModEntries())
            {
                IMod mod = modEntry.mod;
                if (modEntry.isEnabled)
                {
                    if (ShopLoaders.ContainsKey(mod.Identifier))
                    {
                        ShopLoaders[mod.Identifier].EnableShop();
                    }
                    else
                    {
                        string[] files = Directory.GetFiles(modEntry.path, "*.assetProject", SearchOption.TopDirectoryOnly);
                        if (files.Length != 0)
                        {
                            ShopLoader loader = new ShopLoader(files[0]);
                            ShopLoaders.Add(mod.Identifier,loader);
                            loader.EnableShop();
                        }
                    }
                }
            }

        }
        //
        // [HarmonyPatch(typeof(IMod))]
        // class ModManagerPatch01
        // {
        //     [HarmonyPostfix]
        //     [HarmonyPatch(nameof(IMod.onDisabled))]
        //     public static void onDisablePostfix(IMod __instance)
        //     {
        //         if (ShopLoaders.ContainsKey(__instance.Identifier))
        //         {
        //             if(ShopLoaders[__instance.Identifier].IsLoaded)
        //                 ShopLoaders[__instance.Identifier].DisableShop();
        //
        //         }
        //     }
        //
        //     [HarmonyPostfix]
        //     [HarmonyPatch(nameof(IMod.onEnabled))]
        //     public static void onEnablePostfix(IMod __instance)
        //     {
        //         if (ShopLoaders.ContainsKey(__instance.Identifier))
        //         {
        //             if(!ShopLoaders[__instance.Identifier].IsLoaded)
        //                 ShopLoaders[__instance.Identifier].DisableShop();
        //         }
        //     }
        // }

        public void onDisabled()
        {
            foreach (var shop in ShopLoaders.Values)
            {
                shop.DisableShop();
            }
        }

        public string Name => "Shop Loader";

        public string Description => "Loads shops into parkitect";

        string IMod.Identifier => "PMC-ShopLoader";
    }
}
