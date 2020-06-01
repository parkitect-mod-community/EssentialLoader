using System;
using System.Collections.Generic;

namespace PMC.Shop
{

    [Serializable]
    public enum Temperature { NONE, COLD, HOT }
    [Serializable]
    public enum HandSide { LEFT, RIGHT }
    [Serializable]
    public enum ConsumeAnimation { GENERIC, DRINK_STRAW, LICK, WITH_HANDS }
    [Serializable]
    public enum ProductType { ON_GOING, CONSUMABLE, WEARABLE }
    [Serializable]
    public enum Seasonal { WINTER, SPRING, SUMMER, AUTUMN, NONE }
    [Serializable]
    public enum Body { HEAD, FACE, BACK }
    [Serializable]
    public enum EffectTypes { HUNGER, THIRST, HAPPINESS, TIREDNESS, SUGARBOOST }

    [Serializable]
    public class ShopIngredient
    {
        public string Name;
        public float Price;
        public float Amount;
        public bool Tweakable;
        public List<Effect> Effects;
    }

    [Serializable]
    public class Effect
    {
        public EffectTypes Type;
        public float Amount;
    }

    [Serializable]
    public class ShopProduct
    {
        public List<ShopIngredient> Ingredients;

        public ProductType ProductType;

        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        /// <value>
        /// The unique identifier.
        /// </value>
        public string Guid;

        //base
        public string Name;
        public float Price;

        public bool IsTwoHanded;
        public bool IsInterestingToLookAt;
        public HandSide HandSide;

        //ongoing
        public int Duration;
        public bool RemoveWhenDepleted;
        public bool DestroyWhenDepleted;

        //wearable
        public Body BodyLocation;
        public Seasonal SeasonalPreference;
        public Temperature TemperaturePreference;
        public bool HideOnRide;
        public bool HideHair;

        //consumable
        public ConsumeAnimation ConsumeAnimation;
        public Temperature Temprature;
        public int Portions;

    }
}
