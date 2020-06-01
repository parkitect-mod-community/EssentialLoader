using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Parkitect.Mods.AssetPacks;
using UnityEngine;

namespace PMC.Shop
{
    public class ShopLoader
    {
        public String Path { get; private set; }
        public bool IsLoaded { get; private set; }

        private AssetPack _assetPack;
        private AssetBundle _bundle;
        private List<UnityEngine.Object> _assetObjects = new List<UnityEngine.Object>();
        private GameObject _hider;

        public ShopLoader(String path)
        {
            Path = path;
            _assetPack = JsonUtility.FromJson<AssetPack>(File.ReadAllText(path));
        }

        public void EnableShop()
        {
            if (GameController.Instance != null && GameController.Instance.isCampaignScenario)
                return;
            Debug.Log("Loading asset pack " + _assetPack.Name + " with " + _assetPack.Assets.Count + " assets");
            _bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path),
                "assetPack"));
            if (_bundle == null)
                throw new Exception("Failed to load AssetBundle!");
            _hider = new GameObject("Hider");
            UnityEngine.Object.DontDestroyOnLoad(_hider);

            IsLoaded = true;
            foreach (var asset in _assetPack.Assets)
            {
                if (asset.Type == AssetType.Shop)
                {
                    GameObject go = UnityEngine.Object.Instantiate<GameObject>(
                        _bundle.LoadAsset<GameObject>(string.Format("Assets/Resources/AssetPack/{0}.prefab",
                            (object) asset.Guid)));

                    ProductShop productShop = go.AddComponent<ProductShop>();
                    var products = new List<Product>();
                    foreach (var decoratorProduct in asset.Products)
                    {
                        GameObject productGo = UnityEngine.Object.Instantiate<GameObject>(
                            _bundle.LoadAsset<GameObject>(string.Format("Assets/Resources/AssetPack/{0}.prefab",
                                (object) decoratorProduct.Guid)));
                        productGo.name = decoratorProduct.Guid;
                        Product product = null;
                        switch (decoratorProduct.ProductType)
                        {
                            case ProductType.ON_GOING:
                                product = _toOngoingProduct(productGo, decoratorProduct);
                                break;
                            case ProductType.WEARABLE:
                                product = _toWearableProduct(productGo, decoratorProduct);
                                break;
                            case ProductType.CONSUMABLE:
                                product = _bindConsumableProduct(productGo, decoratorProduct);
                                break;
                            default:
                                Debug.Log("Failed to Load Product: " + decoratorProduct.Name);
                                break;
                        }

                        if (product != null)
                        {

                            BindingFlags flags = BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic;
                            typeof(Product).GetField("displayName", flags).SetValue(product, decoratorProduct.Name);

                            switch (decoratorProduct.HandSide)
                            {
                                case HandSide.LEFT:
                                    product.handSide = Hand.Side.LEFT;
                                    break;
                                case HandSide.RIGHT:
                                    product.handSide = Hand.Side.RIGHT;
                                    break;
                            }

                            product.isTwoHanded = decoratorProduct.IsTwoHanded;
                            product.interestingToLookAt = decoratorProduct.IsInterestingToLookAt;
                            product.defaultPrice = decoratorProduct.Price;

                            if (decoratorProduct.Ingredients != null)
                            {
                                List<Ingredient> ingredients = new List<Ingredient>();

                                foreach (var decoratorIngredient in decoratorProduct.Ingredients)
                                {
                                    ingredients.Add(_toIngredient(decoratorIngredient));
                                }

                                product.ingredients = ingredients.ToArray();
                                product.boughtFrom = productShop;
                            }

                            products.Add(product);
                        }
                    }

                    productShop.products = products.ToArray();
                    go.name = asset.Guid;
                    new MaterialDecorator().Decorate(go, asset, _bundle);
                    _setupBounds(go, asset, _bundle);
                    _registerGameObject(asset, go);
                }
            }

            _hider.SetActive(false);
            _bundle.Unload(false);
        }

        private void _setupBounds(GameObject assetGO, Asset asset, AssetBundle assetBundle)
        {
            if (asset.BoundingBoxes == null || asset.BoundingBoxes.Count <= 0)
                return;
            foreach (BoundingBox boundingBox1 in asset.BoundingBoxes)
            {
                global::BoundingBox boundingBox2 = assetGO.AddComponent<global::BoundingBox>();
                Bounds bounds = new Bounds();
                Vector3 min = new Vector3(boundingBox1.BoundsMin[0], boundingBox1.BoundsMin[1], boundingBox1.BoundsMin[2]);
                Vector3 max = new Vector3(boundingBox1.BoundsMax[0], boundingBox1.BoundsMax[1], boundingBox1.BoundsMax[2]);
                bounds.SetMinMax(min, max);
                boundingBox2.setBounds(bounds);
                boundingBox2.layers = BoundingVolume.Layers.Buildvolume;
            }
        }

        private void _registerGameObject(Asset asset, UnityEngine.Object assetObject)
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
                if ((UnityEngine.Object) buildableObject != (UnityEngine.Object) null)
                {
                    buildableObject.setDisplayName(asset.Name);
                    buildableObject.price = asset.Price;
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

        private Ingredient _toIngredient(ShopIngredient ingredient)
        {
            Ingredient result = new Ingredient();
            var resource = ScriptableObject.CreateInstance<Resource>();

            List<ConsumableEffect> consumableEffects = new List<ConsumableEffect>();
            foreach (var decIngredient in ingredient.Effects)
            {
                var ef = new ConsumableEffect();
                switch (decIngredient.Type)
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

                ef.amount = decIngredient.Amount;
                consumableEffects.Add(ef);
            }

            resource.effects = consumableEffects.ToArray();
            resource.setDisplayName(ingredient.Name);
            resource.setCosts(ingredient.Price);
            //TODO: resource texture
            // resource.resourceTexture

            result.resource = resource;
            result.tweakable = ingredient.Tweakable;
            result.defaultAmount = ingredient.Amount;
            return result;
        }

        private Product _toOngoingProduct(GameObject go, ShopProduct pr)
        {
            OngoingEffectProduct result = go.AddComponent<OngoingEffectProduct>();
            result.duration = pr.Duration;
            result.destroyWhenDepleted = pr.DestroyWhenDepleted;
            result.removeFromInventoryWhenDepleted = pr.RemoveWhenDepleted;
            return result;
        }

        private Product _toWearableProduct(GameObject go, ShopProduct pr)
        {
            WearableProduct result = go.AddComponent<WearableProduct>();
            switch (pr.BodyLocation)
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

            switch (pr.SeasonalPreference)
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

            switch (pr.TemperaturePreference)
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

            result.dontHideHair = !pr.HideHair;
            result.hideOnRides = pr.HideOnRide;

            return result;
        }


        private Product _bindConsumableProduct(GameObject go, ShopProduct pr)
        {
            ConsumableProduct result = go.AddComponent<ConsumableProduct>();
            switch (pr.ConsumeAnimation)
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

            switch (pr.Temprature)
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

            result.portions = pr.Portions;

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
