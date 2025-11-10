using System;
using System.Collections.Generic;
using System.Linq;

namespace Game {
    public class CookedCraftingRecipe : IEquatable<CookedCraftingRecipe> {
        public readonly int ResultValue;
        public readonly int ResultCount;
        public readonly int RemainsValue;
        public readonly int RemainsCount;
        public readonly float RequiredPlayerLevel;

        /// <summary>
        ///     键：方块完整值，其中第11位为1时表示判断数据位，为0时表示不判断数据位<br />
        ///     值：数量
        /// </summary>
        public readonly Dictionary<int, int> Ingredients;

        public bool IsFavorite;

        //以下是临时赋值
        public int CraftableTimes = 0;
        public bool AnyIngredientInInventory = false;

        public CookedCraftingRecipe(int resultValue,
            int resultCount,
            int remainsValue = 0,
            int remainsCount = 0,
            float requiredPlayerLevel = 0,
            Dictionary<int, int> ingredients = null,
            bool isFavorite = false) {
            ResultValue = resultValue;
            ResultCount = resultCount;
            RemainsValue = remainsValue;
            RemainsCount = remainsCount;
            RequiredPlayerLevel = requiredPlayerLevel;
            Ingredients = ingredients ?? [];
            IsFavorite = isFavorite;
        }

        /// <param name="input">
        ///     键：方块完整值，其中第11位为0时，数量是数据位为任何值时的总数<br />
        ///     值：数量
        /// </param>
        public int CalculateMaxCraftingTimes(Dictionary<int, IngredientInfo> input) {
            int times = int.MaxValue;
            foreach ((int requiredValue, int requiredCount) in Ingredients) {
                if (!input.TryGetValue(requiredValue, out IngredientInfo info)) {
                    return 0;
                }
                times = Math.Min(times, info.Count / requiredCount);
            }
            return times;
        }

        public override bool Equals(object obj) => obj is CookedCraftingRecipe other && Equals(other);

        public bool Equals(CookedCraftingRecipe other) {
            if (other is null) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return ResultValue == other.ResultValue
                && ResultCount == other.ResultCount
                && RemainsValue == other.RemainsValue
                && RemainsCount == other.RemainsCount
                && RequiredPlayerLevel.Equals(other.RequiredPlayerLevel)
                && AreDictionariesEqual(Ingredients, other.Ingredients);
        }

        public override int GetHashCode() {
            HashCode hash = new();
            hash.Add(ResultValue);
            hash.Add(ResultCount);
            hash.Add(RemainsValue);
            hash.Add(RemainsCount);
            hash.Add(RequiredPlayerLevel);
            hash.Add(GetDictionaryHashCode(Ingredients));
            return hash.ToHashCode();
        }

        public static bool operator ==(CookedCraftingRecipe left, CookedCraftingRecipe right) => Equals(left, right);

        public static bool operator !=(CookedCraftingRecipe left, CookedCraftingRecipe right) => !Equals(left, right);

        public static bool AreDictionariesEqual(Dictionary<int, int> dict1, Dictionary<int, int> dict2) {
            if (dict1 == dict2) {
                return true;
            }
            if (dict1 == null
                || dict2 == null) {
                return false;
            }
            if (dict1.Count != dict2.Count) {
                return false;
            }
            foreach (KeyValuePair<int, int> kvp in dict1) {
                if (!dict2.TryGetValue(kvp.Key, out int value)
                    || value != kvp.Value) {
                    return false;
                }
            }
            return true;
        }

        public static int GetDictionaryHashCode(Dictionary<int, int> dictionary) {
            if (dictionary == null
                || dictionary.Count == 0) {
                return 0;
            }
            int[] sortedKeys = dictionary.Keys.OrderBy(key => key).ToArray();
            HashCode hash = new();
            foreach (int key in sortedKeys) {
                hash.Add(key);
                hash.Add(dictionary[key]);
            }
            return hash.ToHashCode();
        }

        public class IngredientInfo {
            public int Count;
            public HashSet<ComponentInventoryBase> Inventories = [];

            public IngredientInfo(int count, ComponentInventoryBase inventory) {
                Count = count;
                Inventories.Add(inventory);
            }
        }
    }
}