using System;
using System.Collections.Generic;
using Parkitect.Mods.AssetPacks;

namespace PMC.Shop
{
    [Serializable]
    public class Asset
    {
        public int FootprintX = 1;
        public int FootprintZ = 1;
        public int DefaultTrainLength = 1;
        public int MinTrainLength = 1;
        public int MaxTrainLength = 1;
        public string Guid;
        public string Name;
        public AssetType Type;
        public float Price;
        public string Category;
        public string SubCategory;
        public bool BuildOnGrid;
        public bool SnapCenter;
        public float GridSubdivision;
        public float HeightDelta;
        public bool HasCustomColors;
        public List<CustomColor> CustomColors;
        public int ColorCount;
        public bool IsResizable;
        public bool SeeThrough;
        public bool BlocksRain;
        public float MinSize;
        public float MaxSize;
        public bool HasBackRest;
        public bool HasMidPost;
        public int WallSettings;
        public float Height;
        public bool LightsTurnOnAtNight;
        public bool LightsUseCustomColors;
        public int LightsCustomColorSlot;
        public string Description;
        public float RainProtection;
        public float Excitement;
        public float Intensity;
        public float Nausea;
        public string TrackedRideName;
        public CoasterCar LeadCar;
        public CoasterCar Car;
        public CoasterCar RearCar;
        public string FlatRideCategory;
        public List<BoundingBox> BoundingBoxes;
        public List<Waypoint> Waypoints;
        public AspectRatio AspectRatio;
        public List<ShopProduct> Products;
    }
}
