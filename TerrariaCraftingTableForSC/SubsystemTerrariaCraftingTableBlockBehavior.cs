using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;
using TemplatesDatabase;
using IngredientInfo = Game.CookedCraftingRecipe.IngredientInfo;

namespace Game {
    public class SubsystemTerrariaCraftingTableBlockBehavior : SubsystemBlockBehavior {
        public SubsystemBlockEntities m_subsystemBlockEntities;
        public SubsystemPickables m_subsystemPickables;
        public SubsystemTerrain m_subsystemTerrain;
        public SubsystemAudio m_subsystemAudio;

        public readonly List<CookedCraftingRecipe> CookedRecipes = [];
        public readonly HashSet<CookedCraftingRecipe> FavoriteCookedRecipes = [];
        public volatile bool m_isInitialized;
        public volatile bool m_isDisposed;
        public const string fName = "SubsystemTerrariaCraftingTableBlockBehavior";

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            ValuesDictionary valuesDictionary2 = valuesDictionary.GetValue<ValuesDictionary>("Favorites", []);
            foreach ((string key, object obj) in valuesDictionary2) {
                if (obj is not string str) {
                    continue;
                }
                string[] array = str.Split(';');
                if (array.Length != 6) {
                    continue;
                }
                string[] array1 = array[0].Split(':');
                int resultValue;
                switch (array1.Length) {
                    case 2: {
                        int resultContents = BlocksManager.GetBlockIndex(array1[0]);
                        if (resultContents == -1) {
                            continue;
                        }
                        if (!int.TryParse(array1[1], out int resultData)) {
                            continue;
                        }
                        resultValue = Terrain.MakeBlockValue(resultContents, 0, resultData);
                        break;
                    }
                    case 1: {
                        resultValue = BlocksManager.GetBlockIndex(array1[0]);
                        if (resultValue == -1) {
                            continue;
                        }
                        break;
                    }
                    default: continue;
                }
                if (!int.TryParse(array[1], out int resultCount)) {
                    continue;
                }
                string[] array2 = array[2].Split(':');
                int remainsValue;
                switch (array2.Length) {
                    case 2: {
                        int remainsContents = BlocksManager.GetBlockIndex(array2[0]);
                        if (remainsContents == -1) {
                            continue;
                        }
                        if (!int.TryParse(array2[1], out int remainsData)) {
                            continue;
                        }
                        remainsValue = Terrain.MakeBlockValue(remainsContents, 0, remainsData);
                        break;
                    }
                    case 1: {
                        remainsValue = BlocksManager.GetBlockIndex(array2[0]);
                        if (remainsValue == -1) {
                            continue;
                        }
                        break;
                    }
                    default: continue;
                }
                if (!int.TryParse(array[3], out int remainsCount)) {
                    continue;
                }
                if (!float.TryParse(array[4], out float requiredPlayerLevel)) {
                    continue;
                }
                string[] array3 = array[5].Split(',');
                Dictionary<int, int> ingredients = [];
                bool valid = true;
                foreach (string ingredientString in array3) {
                    string[] array4 = ingredientString.Split(':');
                    if (array4.Length == 2) {
                        int ingredientValue = BlocksManager.GetBlockIndex(array4[0]);
                        if (ingredientValue == -1
                            || !int.TryParse(array4[1], out int ingredientCount)) {
                            valid = false;
                            break;
                        }
                        ingredients.Add(ingredientValue, ingredientCount);
                    }
                    else if (array4.Length == 3) {
                        int ingredientContents = BlocksManager.GetBlockIndex(array4[0]);
                        if (ingredientContents == -1
                            || !int.TryParse(array4[1], out int ingredientData)
                            || !int.TryParse(array4[2], out int ingredientCount)) {
                            valid = false;
                            break;
                        }
                        ingredients.Add(Terrain.MakeBlockValue(ingredientContents, ingredientData == 0 ? 0 : 1, ingredientData), ingredientCount);
                    }
                    else {
                        valid = false;
                        break;
                    }
                }
                if (valid) {
                    FavoriteCookedRecipes.Add(
                        new CookedCraftingRecipe(
                            resultValue,
                            resultCount,
                            remainsValue,
                            remainsCount,
                            requiredPlayerLevel,
                            ingredients,
                            true
                        )
                    );
                }
            }
            Task.Run(async () => {
                    await Task.Delay(1000);
                    if (m_isDisposed) {
                        return;
                    }
                    Dictionary<string, HashSet<int>> craftingId2BlockContents = [];
                    HashSet<int> furnitureContents = [];
                    for (int contents = 0; contents < BlocksManager.Blocks.Length; contents++) {
                        Block block = BlocksManager.Blocks[contents];
                        if (block is not null and not AirBlock) {
                            if (block is FurnitureBlock) {
                                furnitureContents.Add(contents);
                            }
                            string craftingId = block.GetCraftingId(contents);
                            if (string.IsNullOrWhiteSpace(craftingId)) {
                                continue;
                            }
                            if (craftingId2BlockContents.TryGetValue(craftingId, out HashSet<int> hashSet)) {
                                hashSet.Add(contents);
                            }
                            else {
                                craftingId2BlockContents.Add(craftingId, [contents]);
                            }
                        }
                    }
                    CookedRecipes.Clear();
                    foreach (CraftingRecipe recipe in CraftingRecipesManager.Recipes) {
                        try {
                            if (m_isDisposed) {
                                return;
                            }
                            if (recipe.RequiredHeatLevel != 0
                                || furnitureContents.Contains(Terrain.ExtractContents(recipe.ResultValue))) {
                                continue;
                            }
                            Dictionary<string, int> rawIngredients = [];
                            foreach (string ingredient in recipe.Ingredients) {
                                if (string.IsNullOrWhiteSpace(ingredient)) {
                                    continue;
                                }
                                if (rawIngredients.TryGetValue(ingredient, out int count)) {
                                    rawIngredients[ingredient] = count + 1;
                                }
                                else {
                                    rawIngredients.Add(ingredient, 1);
                                }
                            }
                            foreach (Dictionary<int, int> cookedIngredients in GetCookedIngredientCombinations(
                                    rawIngredients,
                                    craftingId2BlockContents
                                )) {
                                CookedCraftingRecipe cookedRecipe = new(
                                    recipe.ResultValue,
                                    recipe.ResultCount,
                                    recipe.RemainsValue,
                                    recipe.RemainsCount,
                                    recipe.RequiredPlayerLevel,
                                    cookedIngredients
                                );
                                if (FavoriteCookedRecipes.Contains(cookedRecipe)) {
                                    cookedRecipe.IsFavorite = true;
                                }
                                CookedRecipes.Add(cookedRecipe);
                            }
                        }
                        catch (Exception e) {
                            Log.Error($"[TerrariaCraftingTable] Error on cooking recipe: {e.Message}]");
                        }
                    }
                    m_isInitialized = true;
                }
            );
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            base.Save(valuesDictionary);
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Favorites", valuesDictionary2);
            int i = 0;
            foreach (CookedCraftingRecipe recipe in FavoriteCookedRecipes) {
                StringBuilder stringBuilder = new();
                int resultContents = Terrain.ExtractContents(recipe.ResultValue);
                int resultData = Terrain.ExtractData(recipe.ResultValue);
                if (resultData == 0) {
                    stringBuilder.Append(BlocksManager.Blocks[resultContents].GetType().Name);
                }
                else {
                    stringBuilder.Append($"{BlocksManager.Blocks[resultContents].GetType().Name}:{resultData}");
                }
                stringBuilder.Append(";");
                stringBuilder.Append(recipe.ResultCount);
                stringBuilder.Append(";");
                int remainsContents = Terrain.ExtractContents(recipe.RemainsValue);
                int remainsData = Terrain.ExtractData(recipe.RemainsValue);
                if (remainsData == 0) {
                    stringBuilder.Append(BlocksManager.Blocks[remainsContents].GetType().Name);
                }
                else {
                    stringBuilder.Append($"{BlocksManager.Blocks[remainsContents].GetType().Name}:{remainsData}");
                }
                stringBuilder.Append(";");
                stringBuilder.Append(recipe.RemainsCount);
                stringBuilder.Append(";");
                stringBuilder.Append(recipe.RequiredPlayerLevel);
                stringBuilder.Append(";");
                foreach ((int ingredient, int count) in recipe.Ingredients) {
                    int ingredientContents = Terrain.ExtractContents(ingredient);
                    int ingredientData = Terrain.ExtractData(ingredient);
                    if (ingredientData == 0) {
                        stringBuilder.Append($"{BlocksManager.Blocks[ingredientContents].GetType().Name}:{count}");
                    }
                    else {
                        stringBuilder.Append($"{BlocksManager.Blocks[ingredientContents].GetType().Name}:{ingredientData}:{count}");
                    }
                    stringBuilder.Append(",");
                }
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
                valuesDictionary2.SetValue($"Favorite{i++}", stringBuilder.ToString());
            }
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            base.OnInteract(raycastResult, componentMiner);
            if (!m_isInitialized) {
                return true;
            }
            componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new TerrariaCraftingTableWidget(
                this,
                raycastResult.CellFace.Point,
                componentMiner.Inventory,
                componentMiner.ComponentPlayer
            );
            AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
            return true;
        }

        public override void Dispose() {
            m_isDisposed = true;
            m_isInitialized = false;
            CookedRecipes?.Clear();
            base.Dispose();
        }

        public Dictionary<CookedCraftingRecipe, int> GetAvailableRecipesFromNearbyInventories(Point3 start,
            out Dictionary<int, IngredientInfo> ingredients,
            ComponentInventoryBase extraInventory = null,
            bool includingZeroResult = false) {
            ingredients = [];
            if (extraInventory != null) {
                GetIngredientsFromInventory(extraInventory, ingredients);
            }
            foreach (ComponentInventoryBase inventory in GetNearbyInventories(start)) {
                GetIngredientsFromInventory(inventory, ingredients);
            }
            return GetAvailableRecipesFromIngredients(ingredients, includingZeroResult);
        }

        public HashSet<ComponentInventoryBase> GetNearbyInventories(Point3 start) {
            HashSet<ComponentInventoryBase> result = [];
            HashSet<Point3> scanned = [];
            Queue<Point3> toScan = [];
            foreach (Point3 direction in CellFace.m_faceToPoint3) {
                toScan.Enqueue(start + direction);
            }
            for (int i = 0; i < 1000 && toScan.Count > 0; i++) {
                Point3 point = toScan.Dequeue();
                scanned.Add(point);
                if (m_subsystemBlockEntities.m_blockEntities.TryGetValue(point, out ComponentBlockEntity blockEntity)) {
                    ComponentInventoryBase inventory = blockEntity.Entity.FindComponent<ComponentInventoryBase>(false);
                    if (inventory != null) {
                        result.Add(inventory);
                        foreach (Point3 direction in CellFace.m_faceToPoint3) {
                            Point3 next = point + direction;
                            if (!scanned.Contains(next)) {
                                toScan.Enqueue(next);
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static void GetIngredientsFromInventory(ComponentInventoryBase inventory, Dictionary<int, IngredientInfo> ingredients) {
            foreach (ComponentInventoryBase.Slot slot in inventory.m_slots) {
                int contents = Terrain.ExtractContents(slot.Value);
                int data = Terrain.ExtractData(slot.Value);
                if (data != 0) {
                    int newValue = Terrain.MakeBlockValue(contents, 1, data);
                    if (ingredients.TryGetValue(newValue, out IngredientInfo info)) {
                        info.Count += slot.Count;
                        info.Inventories.Add(inventory);
                    }
                    else {
                        ingredients.Add(newValue, new IngredientInfo(slot.Count, inventory));
                    }
                }
                {
                    if (ingredients.TryGetValue(contents, out IngredientInfo info)) {
                        info.Count += slot.Count;
                        info.Inventories.Add(inventory);
                    }
                    else {
                        ingredients.Add(contents, new IngredientInfo(slot.Count, inventory));
                    }
                }
            }
        }

        public Dictionary<CookedCraftingRecipe, int> GetAvailableRecipesFromIngredients(Dictionary<int, IngredientInfo> ingredients,
            bool includingZeroResult = false) {
            Dictionary<CookedCraftingRecipe, int> result = [];
            foreach (CookedCraftingRecipe recipe in CookedRecipes) {
                int resultCount = recipe.CalculateMaxResultCount(ingredients);
                if (includingZeroResult || resultCount > 0) {
                    if (result.TryGetValue(recipe, out int count)) {
                        result[recipe] = count + resultCount;
                    }
                    else {
                        result.Add(recipe, resultCount);
                    }
                }
            }
            return result;
        }

        public int Craft(CookedCraftingRecipe recipe,
            int times,
            Dictionary<int, IngredientInfo> ingredients,
            IInventory targetInventory,
            Vector3 positionToThrowOverflow,
            ComponentPlayer componentPlayer = null) {
            if (recipe == null
                || times <= 0
                || ingredients == null
                || ingredients.Count == 0) {
                return 0;
            }
            if (componentPlayer != null
                && componentPlayer.PlayerData.Level < recipe.RequiredPlayerLevel) {
                componentPlayer.ComponentGui.DisplaySmallMessage(
                    string.Format(LanguageControl.Get("CraftingRecipesManager", "2"), recipe.RequiredPlayerLevel),
                    Color.White,
                    true,
                    true
                );
                return 0;
            }
            times = Math.Min(recipe.CalculateMaxResultCount(ingredients) / recipe.ResultCount, times);
            if (times <= 0) {
                componentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "3"), Color.White, true, true);
                return 0;
            }
            bool allSatisfied = true;
            Dictionary<int, Dictionary<ComponentInventoryBase, int>> allRemoved = [];
            foreach ((int ingredient, int ingredientCount) in recipe.Ingredients) {
                bool satisfied = false;
                Dictionary<ComponentInventoryBase, int> removed = [];
                if (ingredients.TryGetValue(ingredient, out IngredientInfo info)) {
                    int requiredCount = ingredientCount * times;
                    bool judgeData = Terrain.ExtractLight(ingredient) == 1;
                    foreach (ComponentInventoryBase inventory in info.Inventories) {
                        for (int i = 0; i < inventory.SlotsCount; i++) {
                            int value = inventory.GetSlotValue(i);
                            if (judgeData ? value == ingredient : Terrain.ExtractContents(value) == ingredient) {
                                int removedCount = inventory.RemoveSlotItems(i, requiredCount);
                                if (removedCount > 0) {
                                    if (removed.TryGetValue(inventory, out int lastRemovedCount)) {
                                        removed[inventory] = lastRemovedCount + removedCount;
                                    }
                                    else {
                                        removed.Add(inventory, removedCount);
                                    }
                                }
                                requiredCount -= removedCount;
                                if (requiredCount <= 0) {
                                    satisfied = true;
                                    break;
                                }
                            }
                        }
                        if (satisfied) {
                            break;
                        }
                    }
                }
                if (removed.Count > 0) {
                    allRemoved.Add(ingredient, removed);
                }
                if (!satisfied) {
                    allSatisfied = false;
                    break;
                }
            }
            if (allSatisfied) {
                {
                    int toThrow = targetInventory == null
                        ? recipe.ResultCount * times
                        : ComponentInventoryBase.AcquireItems(targetInventory, recipe.ResultValue, recipe.ResultCount * times);
                    if (toThrow > 0) {
                        ThrowOverflow(positionToThrowOverflow, recipe.ResultValue, toThrow);
                    }
                }
                if (recipe.RemainsValue != 0
                    && recipe.RemainsCount > 0) {
                    int toThrow = targetInventory == null
                        ? recipe.RemainsCount * times
                        : ComponentInventoryBase.AcquireItems(targetInventory, recipe.RemainsValue, recipe.RemainsCount * times);
                    if (toThrow > 0) {
                        ThrowOverflow(positionToThrowOverflow, recipe.RemainsValue, toThrow);
                    }
                }
                if (componentPlayer != null) {
                    string resultDisplayNamee = BlocksManager.Blocks[Terrain.ExtractContents(recipe.ResultValue)]
                        .GetDisplayName(m_subsystemTerrain, recipe.ResultValue);
                    if (recipe.RemainsValue != 0
                        && recipe.RemainsCount > 0) {
                        string remainsDisplayName = BlocksManager.Blocks[Terrain.ExtractContents(recipe.RemainsValue)]
                            .GetDisplayName(m_subsystemTerrain, recipe.RemainsValue);
                        componentPlayer.ComponentGui.DisplaySmallMessage(
                            string.Format(
                                LanguageControl.Get(fName, "2"),
                                times,
                                resultDisplayNamee,
                                times * recipe.ResultCount,
                                remainsDisplayName,
                                times * recipe.RemainsCount
                            ),
                            Color.White,
                            true,
                            true
                        );
                    }
                    else {
                        componentPlayer.ComponentGui.DisplaySmallMessage(
                            string.Format(LanguageControl.Get(fName, "1"), times, resultDisplayNamee, times * recipe.ResultCount),
                            Color.White,
                            true,
                            true
                        );
                    }
                }
                return times;
            }
            foreach ((int ingredient, Dictionary<ComponentInventoryBase, int> removed) in allRemoved) {
                int toThrow = 0;
                foreach ((ComponentInventoryBase inventory, int removedCount) in removed) {
                    toThrow += ComponentInventoryBase.AcquireItems(inventory, ingredient, removedCount);
                }
                if (toThrow > 0) {
                    ThrowOverflow(positionToThrowOverflow, ingredient, toThrow);
                }
            }
            componentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "3"), Color.White, true, true);
            return 0;
        }

        public void ThrowOverflow(Vector3 position, int value, int count) {
            int maxStacking = BlocksManager.Blocks[Terrain.ExtractContents(value)].GetMaxStacking(value);
            while (count > 0) {
                int toThrow = Math.Min(count, maxStacking);
                count -= toThrow;
                m_subsystemPickables.AddPickable(value, toThrow, position, new Vector3(0f, 5f, 0f), null, null);
            }
            m_subsystemAudio.PlaySound("Audio/DispenserDispense", 1f, 0f, position, 3f, true);
        }

        //By ChatGPT
        public static IEnumerable<Dictionary<int, int>> GetCookedIngredientCombinations(Dictionary<string, int> rawIngredients,
            Dictionary<string, HashSet<int>> craftingId2BlockContents) {
            if (rawIngredients == null
                || craftingId2BlockContents == null
                || rawIngredients.Count == 0) {
                yield break;
            }

            // 预处理并校验所有 craftingId
            List<(int[] blockIds, int count)> items = new(rawIngredients.Count);
            foreach ((string craftingId, int count) in rawIngredients) {
                CraftingRecipesManager.DecodeIngredient(craftingId, out string craftingId2, out int? data);
                if (!craftingId2BlockContents.TryGetValue(craftingId2, out HashSet<int> blockSet)
                    || blockSet == null
                    || blockSet.Count == 0) {
                    // 任意一个 craftingId 无效，则整个结果无效
                    yield break;
                }
                int[] blockIds = blockSet.ToArray();
                if (data.HasValue) {
                    for (int i = 0; i < blockIds.Length; i++) {
                        blockIds[i] = Terrain.MakeBlockValue(blockIds[i], 1, data.Value);
                    }
                }
                items.Add((blockIds, count));
            }
            int n = items.Count;
            if (n == 0) {
                yield break;
            }

            // 每层当前索引
            int[] indices = new int[n];
            while (true) {
                // 生成当前组合
                Dictionary<int, int> result = new(n);
                for (int i = 0; i < n; i++) {
                    (int[] blockIds, int count) = items[i];
                    result.Add(blockIds[indices[i]], count);
                }
                yield return result;

                // 迭代器推进：从最后一层开始进位
                int pos = n - 1;
                while (pos >= 0) {
                    indices[pos]++;
                    if (indices[pos] < items[pos].blockIds.Length) {
                        break; // 当前层未溢出，继续
                    }
                    indices[pos] = 0;
                    pos--;
                }

                // 如果最高位也溢出，则结束
                if (pos < 0) {
                    yield break;
                }
            }
        }
    }
}