using System;
using System.IO;
using Parkitilities;
using Parkitilities.AssetPack;
using Parkitilities.PathStylesBuilder;
using Parkitilities.ShopBuilder;
using UnityEngine;


namespace PMC.ExtendedLoader
{
    public class ExtendedLoader
    {
        public String Path { get; private set; }
        public bool IsLoaded { get; private set; }
        private readonly AssetManagerLoader _assetManagerLoader = new AssetManagerLoader();

        private AssetBundle _bundle;

        public ExtendedLoader(String path)
        {
            Path = path;
        }


        private void _bindIngredients<TTarget>(IngredientBuilder<TTarget> builder, ShopProduct product,
            ShopIngredient ingredient)
            where TTarget : class
        {
            var ingredientBuilder = builder
                .Cost(ingredient.Price)
                .Tweakable(ingredient.Tweakable)
                .DisplayName(ingredient.Name)
                .DefaultAmount(ingredient.Amount)
                .Id(product.Guid + "_" + ingredient.Name);

            foreach (var effect in ingredient.Effects)
            {
                ingredientBuilder.Effect(ProductShopUtility.ConvertEffectType(effect.Type), effect.Amount);
            }
        }

        private void _loadShop(Asset asset)
        {
            GameObject go = AssetPackUtilities.LoadAsset<GameObject>(_bundle, asset.Guid);
            if (go == null)
                throw new Exception("Can't find Object:" + asset.Guid);

            var builder = Parkitility.CreateProductShop<ProductShop>(go)
                .DisplayName(asset.Name)
                .Id(asset.Guid)
                .Price(asset.Price)
                .WalkableFlag(Asset.ConvertWalkable(asset.Walkable));

            foreach (var box in AssetPackUtilities.ConvertBoundingBox(asset.BoundingBoxes.ToArray()))
            {
                builder.AddBoundingBox(box);
            }

            foreach (var product in asset.Products)
            {
                GameObject productGo = AssetPackUtilities.LoadAsset<GameObject>(_bundle, product.Guid);
                if (productGo == null)
                {
                    Debug.Log("Can't find product game object for:" + product.Name);
                    continue;
                }

                switch (product.ProductType)
                {
                    case ProductType.ON_GOING:
                        var ongoingProductBuilder = Parkitility
                            .CreateOnGoingProduct<OngoingEffectProduct>(productGo)
                            .Id(product.Guid)
                            .DisplayName(product.Name)
                            .Duration(product.Duration)
                            .DestroyWhenDepleted(product.DestroyWhenDepleted)
                            .RemoveFromInventoryWhenDepleted(product.RemoveWhenDepleted)
                            .TwoHanded(product.IsTwoHanded)
                            .InterestingToLookAt(product.IsInterestingToLookAt)
                            .DefaultPrice(product.Price)
                            .HandSide(ProductShopUtility.ConvertToSide(product.HandSide));

                        foreach (var shopIngredient in product.Ingredients)
                        {
                            _bindIngredients(ongoingProductBuilder.AddIngredient(_assetManagerLoader),
                                product, shopIngredient);
                        }

                        builder.AddProduct(_assetManagerLoader, ongoingProductBuilder);
                        break;
                    case ProductType.BALLOON:
                        Debug.Log(product.Name);
                        var balloonBuilder = Parkitility
                            .CreateBalloonProduct<Balloon>(productGo)
                            .Id(product.Guid)
                            .DisplayName(product.Name)
                            .DestroyWhenDepleted(false)
                            .Duration(180)
                            .DefaultPrice(product.Price)
                            .DefaultMass(product.DefaultMass)
                            .DefaultDrag(product.DefaultDrag)
                            .DefaultAngularDrag(product.DefaultAngularDrag)
                            .RemoveFromInventoryWhenDepleted(true);

                        if (product.HasCustomColors)
                        {
                            balloonBuilder.CustomColor(
                                AssetPackUtilities.ConvertColors(product.CustomColors, product.ColorCount));
                        }

                        foreach (var shopIngredient in product.Ingredients)
                        {
                            _bindIngredients(balloonBuilder.AddIngredient(_assetManagerLoader),
                                product, shopIngredient);
                        }

                        builder.AddProduct(_assetManagerLoader, balloonBuilder);
                        break;
                    case ProductType.WEARABLE:
                        var wearableProductBuilder = Parkitility
                            .CreateWearableProduct<WearableProduct>(productGo)
                            .Id(product.Guid)
                            .DisplayName(product.Name)
                            .TwoHanded(product.IsTwoHanded)
                            .InterestingToLookAt(product.IsInterestingToLookAt)
                            .DefaultPrice(product.Price)
                            .HandSide(ProductShopUtility.ConvertToSide(product.HandSide))
                            .TemperaturePreference(
                                ProductShopUtility.ConvertTemperaturePreference(
                                    product.TemperaturePreference))
                            .SeasonalPreference(
                                ProductShopUtility.ConvertSeasonalPreference(
                                    product.SeasonalPreference))
                            .BodyLocation(ProductShopUtility.ConvertBodyLocation(product.BodyLocation))
                            .HideHair(product.HideHair)
                            .HideOnRide(product.HideOnRide);

                        if (product.HasCustomColors)
                        {
                            wearableProductBuilder.CustomColor(
                                AssetPackUtilities.ConvertColors(product.CustomColors, product.ColorCount));
                        }


                        foreach (var shopIngredient in product.Ingredients)
                        {
                            _bindIngredients(wearableProductBuilder.AddIngredient(_assetManagerLoader),
                                product, shopIngredient);
                        }

                        builder.AddProduct(_assetManagerLoader, wearableProductBuilder);
                        break;
                    case ProductType.CONSUMABLE:
                        var consumableBuilder = Parkitility
                            .CreateConsumableProduct<ConsumableProduct>(productGo)
                            .Id(product.Guid)
                            .DisplayName(product.Name)
                            .TwoHanded(product.IsTwoHanded)
                            .InterestingToLookAt(product.IsInterestingToLookAt)
                            .TemperaturePreference(
                                ProductShopUtility.ConvertTemperaturePreference(
                                    product.TemperaturePreference))
                            .ConsumeAnimation(
                                ProductShopUtility.ConvertConsumeAnimation(product.ConsumeAnimation))
                            .DefaultPrice(product.Price)
                            .HandSide(ProductShopUtility.ConvertToSide(product.HandSide));

                        GameObject trashGo = AssetPackUtilities.LoadAsset<GameObject>(_bundle, product.TrashGuid);
                        if (trashGo != null)
                        {
                            consumableBuilder.Trash<Trash>(trashGo, _assetManagerLoader)
                                .Id(product.TrashGuid)
                                .Disgust(product.DisgustFactor)
                                .Volume(product.Volume)
                                .CanWiggle(product.CanWiggle);
                        }

                        foreach (var shopIngredient in product.Ingredients)
                        {
                            _bindIngredients(consumableBuilder.AddIngredient(_assetManagerLoader),
                                product,
                                shopIngredient);
                        }

                        builder.AddProduct(_assetManagerLoader, consumableBuilder);
                        break;
                }
            }

            builder.Build(_assetManagerLoader);
        }


        private void _loadDoor(Asset asset)
        {
            GameObject go = AssetPackUtilities.LoadAsset<GameObject>(_bundle, asset.Guid);
            if (go == null)
                throw new Exception("Can't find Object:" + asset.Guid);

            WallBuilder<Door> doorBuilder = Parkitility.CreateWall<Door>(go)
                .Id(asset.Guid)
                .BuildLayerMask(LayerMasks.TERRAIN)
                .Price(asset.Price, false)
                .DisplayName(asset.Name)
                .CustomColor(AssetPackUtilities.ConvertColors(asset.CustomColors, asset.ColorCount))
                .BlockRain(asset.BlocksRain)
                .HeightChangeDelta(asset.HeightDelta)
                .SnapGridToCenter(true)
                .OnGrid(true)
                .GridSubdivisions(1f)
                .Category(asset.Category, asset.SubCategory)
                .SeeThrough(asset.SeeThrough);

            if (asset.IsResizable)
            {
                doorBuilder.Resizable(asset.MinSize, asset.MaxSize);
            }

            doorBuilder.Build(_assetManagerLoader);
        }

        private void _loadPath(Asset asset)
        {
            PathStyleBuilder builder = new PathStyleBuilder();

            switch (asset.PathMaterialType)
            {
                case Asset.PathMaterial.Sheet:
                {
                    Texture2D sheet = AssetPackUtilities.LoadAsset<Texture2D>(_bundle, asset.Guid + ".path_sheet");
                    Texture2D mask = AssetPackUtilities.LoadAsset<Texture2D>(_bundle, asset.Guid + ".path_mask");
                    Texture2D normal = AssetPackUtilities.LoadAsset<Texture2D>(_bundle, asset.Guid + ".path_normal");

                    CustomColorsMaskedNormalBuilder materialBuilder = ShaderUtility.PathMaterial();
                    if (sheet != null) materialBuilder.MainTex(sheet);
                    materialBuilder.MainTex(sheet == null ? ShaderUtility.EmptyTexture : sheet);
                    materialBuilder.MaskTex(mask == null ? ShaderUtility.EmptyTexture : mask);
                    materialBuilder.NormalTex(normal == null ? ShaderUtility.EmptyTexture : normal);

                    builder.Material(materialBuilder.build())
                        .Id(asset.Guid)
                        .Name(asset.Name)
                        .CustomColor(AssetPackUtilities.ConvertColors(asset.CustomColors, asset.ColorCount))
                        .Register(asset.PathType, _assetManagerLoader,
                            PathStyleBuilder.GetPathStyle(PathStyleBuilder.NormalPathIds.Gravel,
                                PathStyleBuilder.PathType.Normal));
                }
                    break;
                case Asset.PathMaterial.Tiled:
                {
                    Texture2D sheet = AssetPackUtilities.LoadAsset<Texture2D>(_bundle, asset.Guid + ".path_sheet");
                    Texture2D mask = AssetPackUtilities.LoadAsset<Texture2D>(_bundle, asset.Guid + ".path_mask");
                    Texture2D normal = AssetPackUtilities.LoadAsset<Texture2D>(_bundle, asset.Guid + ".path_normal");

                    CustomColorMaskedCutoutBuilder materialBuilder = ShaderUtility.PathMaterialTiled();
                    if (sheet != null) materialBuilder.MainTex(sheet);
                    materialBuilder.MaskTex(mask == null ? ShaderUtility.EmptyTexture : mask);
                    materialBuilder.MainTex(sheet == null ? ShaderUtility.EmptyTexture : sheet);
                    materialBuilder.NormalTex(normal == null ? ShaderUtility.EmptyTexture : normal);
                    builder.Material(materialBuilder.build())
                        .Id(asset.Guid)
                        .CustomColor(AssetPackUtilities.ConvertColors(asset.CustomColors, asset.ColorCount))
                        .Name(asset.Name)
                        .Register(asset.PathType, _assetManagerLoader,
                            PathStyleBuilder.GetPathStyle(PathStyleBuilder.NormalPathIds.Concrete,
                                PathStyleBuilder.PathType.Normal));
                }
                    break;
            }
        }

        public void OnEnabled()
        {
            _bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path),
                "assetPack"));
            if (_bundle == null)
                throw new Exception("Failed to load AssetBundle!");
            var pack = AssetPackUtilities.LoadAsset(File.ReadAllText(Path));

            IsLoaded = true;
            foreach (var asset in pack.Assets)
            {
                if (!asset.LoadAsset)
                {
                    continue;
                }

                try
                {
                    Debug.Log("Loading Item " + asset.Name + " by type " + asset.TargetType);
                    switch (asset.TargetType)
                    {
                        case AssetType.Shop:
                            _loadShop(asset);
                            break;
                        case AssetType.Door:
                            _loadDoor(asset);
                            break;
                        case AssetType.Path:
                            _loadPath(asset);
                            break;
                    }

                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            _bundle.Unload(false);
        }

        public void onDisabled()
        {
            IsLoaded = false;
            if (_bundle != null)
                _bundle.Unload(false);
            if (_assetManagerLoader != null)
                _assetManagerLoader.Unload();
        }
    }
}

