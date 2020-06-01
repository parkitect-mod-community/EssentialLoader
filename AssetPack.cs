using System;
using System.Collections.Generic;

namespace PMC.Shop
{
    [Serializable]
    public class AssetPack
    {
        public List<Asset> Assets = new List<Asset>();
        public string Name;
        public string Description;
    }
}
