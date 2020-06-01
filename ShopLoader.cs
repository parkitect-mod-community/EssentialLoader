using System;
using System.Collections.Generic;
using System.IO;
using MiniJSON;
using UnityEngine;
using Object = System.Object;

namespace PMC.Shop
{
    public enum Temperature { NONE, COLD, HOT }
    public enum HandSide { LEFT, RIGHT }
    public enum ConsumeAnimation { GENERIC, DRINK_STRAW, LICK, WITH_HANDS }
    public enum ProductType { ON_GOING, CONSUMABLE, WEARABLE }
    public enum Seasonal { WINTER, SPRING, SUMMER, AUTUMN, NONE }
    public enum Body { HEAD, FACE, BACK }
    public enum EffectTypes { HUNGER, THIRST, HAPPINESS, TIREDNESS, SUGARBOOST }


    public class ShopLoader
    {
        public String Path { get; private set; }
        public bool IsLoaded { get; private set; }

        // private AssetPack _assetPack;
        private AssetBundle _bundle;
        private List<UnityEngine.Object> _assetObjects = new List<UnityEngine.Object>();
        private GameObject _hider;

        public ShopLoader(String path)
        {
            Path = path;
            // _assetPack = JsonUtility.FromJson<AssetPack>(File.ReadAllText(Path));
        }

        private T _tryGet<T>(Object obj, T defaultV)
        {
            if(obj.GetType() == typeof(T))
                return defaultV;
            return (T) obj;
        }

        private T _tryGet<T>(Dictionary<string,object> obj, string key, T defaultV)
        {
            if (obj.ContainsKey(key))
                return defaultV;
            return _tryGet<T>(obj[key], defaultV);
        }

        public void EnableShop()
        {
            var dict = Json.Deserialize(File.ReadAllText(Path)) as Dictionary<string,Object>;
            if (GameController.Instance != null && GameController.Instance.isCampaignScenario)
                return;
            if(dict == null)
                return;

            Debug.Log("Loading asset pack for shop " + dict["Name"] + " with " + ((List<object>) dict["Assets"]).Count + " assets");
            _bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path),
                "assetPack"));
            if (_bundle == null)
                throw new Exception("Failed to load AssetBundle!");
            _hider = new GameObject("Hider");
            UnityEngine.Object.DontDestroyOnLoad(_hider);

            IsLoaded = true;
            foreach (var asset in dict["Assets"] as List<object>)
            {
                try
                {
                    var aa = asset as Dictionary<string, Object>;

                    if ((AssetType)(int)(Int64)aa["Type"] ==  AssetType.Shop)
                    {
                        Debug.Log("loading shop:" + (string)aa["Name"]);
                        GameObject go = UnityEngine.Object.Instantiate<GameObject>(
                            _bundle.LoadAsset<GameObject>(string.Format("Assets/Resources/AssetPack/{0}.prefab",
                                (string) aa["Guid"])));

                        ProductShop productShop = go.AddComponent<ProductShop>();
                        var products = new List<Product>();
                        foreach (var decoratorProduct in aa["Products"] as List<object>)
                        {
                            var bb = (Dictionary<string, Object>)decoratorProduct;
                            GameObject productGo = UnityEngine.Object.Instantiate<GameObject>(
                                _bundle.LoadAsset<GameObject>(string.Format("Assets/Resources/AssetPack/{0}.prefab",
                                    (string) bb["Guid"])));
                            productGo.name = (string) bb["Guid"];
                            Product product = null;
                            switch ((ProductType)(int)(Int64)bb["ProductType"])
                            {
                                case ProductType.ON_GOING:
                                    product = _toOngoingProduct(productGo, bb);
                                    break;
                                case ProductType.WEARABLE:
                                    product = _toWearableProduct(productGo, bb);
                                    break;
                                case ProductType.CONSUMABLE:
                                    product = _bindConsumableProduct(productGo, bb);
                                    break;
                                default:
                                    Debug.Log("Failed to Load Product: " + bb["Name"]);
                                    break;
                            }

                            if (product != null)
                            {


                                // Debug.Log("b5" + product + bb["Name"]);
                                // BindingFlags flags = BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic;
                                // typeof(Product).GetField("displayName", flags).SetValue(product, (string) bb["Name"]);

                                switch ((HandSide)(int)(Int64)bb["HandSide"])
                                {
                                    case HandSide.LEFT:
                                        product.handSide = Hand.Side.LEFT;
                                        break;
                                    case HandSide.RIGHT:
                                        product.handSide = Hand.Side.RIGHT;
                                        break;
                                }

                                product.isTwoHanded = (bool) bb["IsTwoHanded"];
                                product.interestingToLookAt = (bool) bb["IsInterestingToLookAt"];
                                product.defaultPrice = (float) (double) bb["Price"];

                                if (bb["Ingredients"] != null)
                                {
                                    List<Ingredient> ingredients = new List<Ingredient>();

                                    foreach (var decoratorIngredient in bb["Ingredients"] as List<object>)
                                    {
                                        ingredients.Add(
                                            _toIngredient((Dictionary<string, Object>)decoratorIngredient));
                                    }

                                    product.ingredients = ingredients.ToArray();
                                    product.boughtFrom = productShop;
                                }
                                _registerGameObject(bb, productGo);

                                products.Add(product);
                            }
                        }

                        productShop.products = products.ToArray();
                        go.name = (string) aa["Guid"];
                        new MaterialDecorator().replaceMaterials(go);
                        _setupBounds(go, aa, _bundle);
                        _registerGameObject(aa, go);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError((object) ex);
                }
            }


            _hider.SetActive(false);
            _bundle.Unload(false);
        }

        private void _setupBounds(GameObject assetGO, Dictionary<string,Object> asset, AssetBundle assetBundle)
        {
            if (asset["BoundingBoxes"] == null || ((List<object>) asset["BoundingBoxes"]).Count <= 0)
                return;
            foreach (var boundingBox1 in asset["BoundingBoxes"] as List<object>)
            {
                var temp = boundingBox1 as Dictionary<string, Object>;

                global::BoundingBox boundingBox2 = assetGO.AddComponent<global::BoundingBox>();
                Bounds bounds = new Bounds();
                List<object> bmin = temp["BoundsMin"] as List<object>;
                List<object> bmax = temp["BoundsMax"] as List<object>;

                Vector3 min = new Vector3((float) (double)bmin[0], (float) (double)bmin[1], (float) (double)bmin[2]);
                Vector3 max = new Vector3((float) (double)bmax[0], (float) (double)bmax[1], (float) (double)bmax[2]);
                bounds.SetMinMax(min, max);
                boundingBox2.setBounds(bounds);
                boundingBox2.layers = BoundingVolume.Layers.Buildvolume;
            }
        }

        private void _registerGameObject(Dictionary<string,Object> asset, UnityEngine.Object assetObject)
        {
            UnityEngine.Object.DontDestroyOnLoad(assetObject);
            if (assetObject is GameObject)
            {
                GameObject gameObject = assetObject as GameObject;
                SerializedMonoBehaviour component = gameObject.GetComponent<SerializedMonoBehaviour>();
                component.dontSerialize = true;
                component.isPreview = true;
                ScriptableSingleton<AssetManager>.Instance.registerObject((UnityEngine.Object) component);
                this._assetObjects.Add((UnityEngine.Object) component);

                BuildableObject buildableObject = component as BuildableObject;
                if (buildableObject != null)
                {
                    buildableObject.setDisplayName((string) asset["Name"]);
                    buildableObject.price = (float) (double)asset["Price"];
                    buildableObject.canBeRefunded = false;
                    buildableObject.isStatic = true;
                }

                UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object) component.gameObject);
                gameObject.transform.SetParent(this._hider.transform);
            }
            else
            {
                ScriptableSingleton<AssetManager>.Instance.registerObject(assetObject);
                this._assetObjects.Add(assetObject);
            }
        }

        private Ingredient _toIngredient(Dictionary<string,Object> ingredient)
        {
            Ingredient result = new Ingredient();
            var resource = ScriptableObject.CreateInstance<Resource>();

            List<ConsumableEffect> consumableEffects = new List<ConsumableEffect>();
            foreach (var decIngredient in (List<object>) ingredient["Effects"])
            {
                var temp = (Dictionary<string, Object>)decIngredient;
                var ef = new ConsumableEffect();
                switch ((EffectTypes)(int)(Int64)temp["Type"])
                {
                    case EffectTypes.HUNGER:
                        ef.affectedStat = ConsumableEffect.AffectedStat.HUNGER;
                        break;
                    case EffectTypes.THIRST:
                        ef.affectedStat = ConsumableEffect.AffectedStat.THIRST;
                        break;
                    case EffectTypes.HAPPINESS:
                        ef.affectedStat = ConsumableEffect.AffectedStat.HAPPINESS;
                        break;
                    case EffectTypes.TIREDNESS:
                        ef.affectedStat = ConsumableEffect.AffectedStat.TIREDNESS;
                        break;
                    case EffectTypes.SUGARBOOST:
                        ef.affectedStat = ConsumableEffect.AffectedStat.SUGARBOOST;
                        break;
                }

                ef.amount = (float)(double)temp["Amount"];
                consumableEffects.Add(ef);
            }

            resource.effects = consumableEffects.ToArray();
            resource.setDisplayName((string) ingredient["Name"]);
            resource.setCosts((float)(double)ingredient["Price"]);
            //TODO: resource texture
            // resource.resourceTexture

            result.resource = resource;
            result.tweakable = (bool) ingredient["Tweakable"];
            result.defaultAmount = (float)(double) ingredient["Amount"];
            return result;
        }

        private Product _toOngoingProduct(GameObject go, Dictionary<string,Object> pr)
        {
            OngoingEffectProduct result = go.AddComponent<OngoingEffectProduct>();
            result.duration = (int)(Int64) pr["Duration"];
            result.destroyWhenDepleted = (bool) pr["DestroyWhenDepleted"];
            result.removeFromInventoryWhenDepleted = (bool) pr["RemoveWhenDepleted"];
            return result;
        }

        private Product _toWearableProduct(GameObject go, Dictionary<string,Object> pr)
        {
            WearableProduct result = go.AddComponent<WearableProduct>();
            switch ((Body)(int)(Int64)pr["BodyLocation"])
            {
                case Body.HEAD:
                    result.bodyLocation = WearableProduct.BodyLocation.HEAD;
                    break;
                case Body.FACE:
                    result.bodyLocation = WearableProduct.BodyLocation.FACE;
                    break;
                case Body.BACK:
                    result.bodyLocation = WearableProduct.BodyLocation.BACK;
                    break;
            }

            switch ((Seasonal)(int)(Int64)pr["SeasonalPreference"])
            {
                case Seasonal.NONE:
                    result.seasonalPreference = WearableProduct.SeasonalPreference.NONE;
                    break;
                case Seasonal.AUTUMN:
                    result.seasonalPreference = WearableProduct.SeasonalPreference.AUTUMN;
                    break;
                case Seasonal.SPRING:
                    result.seasonalPreference = WearableProduct.SeasonalPreference.SPRING;
                    break;
                case Seasonal.SUMMER:
                    result.seasonalPreference = WearableProduct.SeasonalPreference.SUMMER;
                    break;
                case Seasonal.WINTER:
                    result.seasonalPreference = WearableProduct.SeasonalPreference.WINTER;
                    break;
            }

            switch ((Temperature)(int)(Int64)pr["TemperaturePreference"])
            {
                case Temperature.HOT:
                    result.temperaturePreference = TemperaturePreference.HOT;
                    break;
                case Temperature.COLD:
                    result.temperaturePreference = TemperaturePreference.COLD;
                    break;
                case Temperature.NONE:
                    result.temperaturePreference = TemperaturePreference.NONE;
                    break;
            }

            result.dontHideHair = !(bool)pr["HideHair"];
            result.hideOnRides = (bool) pr["HideOnRide"];

            return result;
        }


        private Product _bindConsumableProduct(GameObject go, Dictionary<string,Object> pr)
        {
            ConsumableProduct result = go.AddComponent<ConsumableProduct>();
            switch ((ConsumeAnimation)(int)(Int64)pr["ConsumeAnimation"])
            {
                case ConsumeAnimation.LICK:
                    result.consumeAnimation = ConsumableProduct.ConsumeAnimation.LICK;
                    break;
                case ConsumeAnimation.GENERIC:
                    result.consumeAnimation = ConsumableProduct.ConsumeAnimation.GENERIC;
                    break;
                case ConsumeAnimation.WITH_HANDS:
                    result.consumeAnimation = ConsumableProduct.ConsumeAnimation.WITH_HANDS;
                    break;
                case ConsumeAnimation.DRINK_STRAW:
                    result.consumeAnimation = ConsumableProduct.ConsumeAnimation.DRINK_STRAW;
                    break;
            }

            switch ((Temperature)(int)(Int64)pr["Temprature"])
            {
                case Temperature.HOT:
                    result.temperaturePreference = TemperaturePreference.HOT;
                    break;
                case Temperature.COLD:
                    result.temperaturePreference = TemperaturePreference.COLD;
                    break;
                case Temperature.NONE:
                    result.temperaturePreference = TemperaturePreference.NONE;
                    break;
            }

            result.portions = (int)(Int64) pr["Portions"];

            return result;
        }

        public void DisableShop()
        {
            IsLoaded = false;
            if (_bundle != null)
                _bundle.Unload(false);
            foreach (UnityEngine.Object assetObject in _assetObjects)
                ScriptableSingleton<AssetManager>.Instance.unregisterObject(assetObject);
            if (!(_hider != null))
                return;
            _assetObjects.Clear();
            UnityEngine.Object.Destroy(_hider);
        }
    }
}
