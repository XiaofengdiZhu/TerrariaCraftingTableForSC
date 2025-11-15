using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Engine;
using IngredientInfo = Game.CookedCraftingRecipe.IngredientInfo;
using FilterMethod1 = Game.SubsystemTerrariaCraftingTableBlockBehavior.FilterMethod1;
using FilterMethod2 = Game.SubsystemTerrariaCraftingTableBlockBehavior.FilterMethod2;

namespace Game {
    public class TerrariaCraftingTableWidget : CanvasWidget {
        public enum SortOrder {
            DisplayOrderAscending,
            DisplayOrderDescending,
            ContentsAscending,
            ContentsDescending,
            NameAscending,
            NameDescending
        }

        public enum GroupMethod {
            None,
            Craftable,
            ResultCategory
        }

        public ButtonWidget m_sortButton;
        public ButtonWidget m_groupButton;
        public ButtonWidget m_filter1Button;
        public ButtonWidget m_filter2Button;
        public CanvasWidget m_recipeSelectorContainer;
        public SelectiveFlexPanelWidget m_recipeSelector;
        public CanvasWidget m_ingredientSlotsContainer;
        public StackPanelWidget m_ingredientSlots;
        public TerrariaCraftingRecipeSlotWidget m_resultSlot;
        public TerrariaCraftingRecipeSlotWidget m_remainsSlot;
        public ButtonWidget m_craftingX1Button;
        public ButtonWidget m_craftingX10Button;
        public ButtonWidget m_craftingX40Button;
        public ButtonWidget m_craftingXMaxButton;
        public ButtonWidget m_addToFavoritesButton;
        public RectangleWidget m_addToFavoritesButtonStar;

        public SubsystemTerrariaCraftingTableBlockBehavior m_subsystem;
        public SubsystemTerrain m_subsystemTerrain;
        public Point3 m_position;
        public IInventory m_minerInventory;
        public ComponentPlayer m_componentPlayer;

        public Dictionary<int, IngredientInfo> m_nearbyIngredients;
        public FilterMethod1 m_filter1 = FilterMethod1.Craftable;
        public FilterMethod2 m_filter2 = FilterMethod2.All;
        public string m_filter2String;

        public static SortOrder m_sortOrder = SortOrder.DisplayOrderAscending;
        public static StringComparer m_stringComparer;
        public static GroupMethod m_groupMethod = GroupMethod.Craftable;

        public static Color CraftableColor = Color.Transparent;
        public static Color NoIngredientsColor = new(70, 10, 0, 90);
        public static Color PartialIngredientsColor = new(70, 70, 10, 90);
        public const string fName = "TerrariaCraftingTableWidget";

        public TerrariaCraftingTableWidget(SubsystemTerrariaCraftingTableBlockBehavior subsystem,
            Point3 position,
            IInventory minerInventory,
            ComponentPlayer componentPlayer) {
            m_subsystem = subsystem;
            m_subsystemTerrain = subsystem.m_subsystemTerrain;
            m_position = position;
            m_minerInventory = minerInventory;
            m_componentPlayer = componentPlayer;
            try {
                m_stringComparer ??= StringComparer.Create(new CultureInfo(ModsManager.Configs["Language"]), false);
            }
            catch {
                m_stringComparer = StringComparer.InvariantCulture;
            }
            LoadContents(this, ContentManager.Get<XElement>("Widgets/TerrariaCraftingTableWidget"));
            m_groupButton = Children.Find<ButtonWidget>("GroupButton");
            m_sortButton = Children.Find<ButtonWidget>("SortButton");
            m_filter1Button = Children.Find<ButtonWidget>("Filter1Button");
            m_filter2Button = Children.Find<ButtonWidget>("Filter2Button");
            m_recipeSelectorContainer = Children.Find<CanvasWidget>("RecipeSelectorContainer");
            m_recipeSelector = Children.Find<SelectiveFlexPanelWidget>("RecipeSelector");
            m_recipeSelector.ItemWidgetFactory = o => o is CookedCraftingRecipe recipe
                ? new TerrariaCraftingRecipeSlotWidget {
                    Value = recipe.ResultValue,
                    Count = recipe.CraftableTimes * recipe.ResultCount,
                    NoBevel = true,
                    CenterColor = recipe.AnyIngredientInInventory
                        ? recipe.CraftableTimes > 0 ? CraftableColor : PartialIngredientsColor
                        : NoIngredientsColor,
                    IsStarVisible = recipe.IsFavorite
                }
                : null;
            m_recipeSelector.ItemClicked = o => {
                if (o is not CookedCraftingRecipe recipe) {
                    return;
                }
                m_ingredientSlots.ClearChildren();
                foreach ((int ingredient, int count) in recipe.Ingredients) {
                    int countInInventory = m_nearbyIngredients.TryGetValue(ingredient, out IngredientInfo ingredientInfo) ? ingredientInfo.Count : 0;
                    m_ingredientSlots.Children.Add(
                        new TerrariaCraftingRecipeSlotWidget {
                            Value = ingredient,
                            Count = count,
                            CenterColor = countInInventory == 0 ? NoIngredientsColor :
                                countInInventory < count ? PartialIngredientsColor : CraftableColor,
                            IsClickable = true
                        }
                    );
                }
                m_resultSlot.Value = recipe.ResultValue;
                m_resultSlot.Count = recipe.ResultCount;
                m_resultSlot.CenterColor = recipe.CraftableTimes > 0 ? CraftableColor : NoIngredientsColor;
                m_resultSlot.IsClickable = true;
                if (recipe.RemainsValue != 0
                    && recipe.RemainsCount > 0) {
                    m_ingredientSlotsContainer.Size = new Vector2(232f, 80f);
                    m_remainsSlot.Value = recipe.RemainsValue;
                    m_remainsSlot.Count = recipe.RemainsCount;
                    m_remainsSlot.CenterColor = recipe.CraftableTimes > 0 ? CraftableColor : NoIngredientsColor;
                    m_remainsSlot.IsClickable = true;
                    m_remainsSlot.IsVisible = true;
                }
                else {
                    m_ingredientSlotsContainer.Size = new Vector2(304f, 80f);
                    m_remainsSlot.IsVisible = false;
                }
                m_addToFavoritesButtonStar.FillColor = recipe.IsFavorite ? Color.Gray : Color.Yellow;
            };
            UpdateSelector();
            m_ingredientSlotsContainer = Children.Find<CanvasWidget>("IngredientSlotsContainer");
            m_ingredientSlots = Children.Find<StackPanelWidget>("IngredientSlots");
            m_resultSlot = Children.Find<TerrariaCraftingRecipeSlotWidget>("ResultSlot");
            m_remainsSlot = Children.Find<TerrariaCraftingRecipeSlotWidget>("RemainsSlot");
            m_craftingX1Button = Children.Find<ButtonWidget>("CraftingX1Button");
            m_craftingX10Button = Children.Find<ButtonWidget>("CraftingX10Button");
            m_craftingX40Button = Children.Find<ButtonWidget>("CraftingX40Button");
            m_craftingXMaxButton = Children.Find<ButtonWidget>("CraftingXMaxButton");
            m_addToFavoritesButton = Children.Find<ButtonWidget>("AddToFavoritesButton");
            m_addToFavoritesButtonStar = Children.Find<RectangleWidget>("AddToFavoritesButtonStar");
            GridPanelWidget inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
            int inventorySlotsCount = minerInventory is ComponentCreativeInventory creativeInventory
                ? creativeInventory.OpenSlotsCount
                : minerInventory.SlotsCount;
            int maxRowsCount = SettingsManager.UIScale switch {
                >= 1f => 4,
                >= 0.85f => 5,
                _ => 6
            };
            //inventoryGrid.RowsCount和ColumnsCount默认值是6
            switch (inventorySlotsCount) {
                case <= 26:
                    inventoryGrid.RowsCount = 4;
                    inventoryGrid.ColumnsCount = 4;
                    break;
                case <= 30:
                    inventoryGrid.RowsCount = 4;
                    inventoryGrid.ColumnsCount = 5;
                    break;
                default:
                    switch (maxRowsCount) {
                        case 4: inventoryGrid.RowsCount = 4; break;
                        case 5:
                            inventoryGrid.RowsCount = 5;
                            if (inventorySlotsCount <= 35) {
                                inventoryGrid.ColumnsCount = 5;
                            }
                            break;
                        case 6:
                            switch (inventorySlotsCount) {
                                case <= 35:
                                    inventoryGrid.RowsCount = 5;
                                    inventoryGrid.ColumnsCount = 5;
                                    break;
                                case <= 40: inventoryGrid.RowsCount = 5; break;
                            }
                            break;
                    }
                    break;
            }
            int maxI = Math.Min(inventorySlotsCount - 10, inventoryGrid.RowsCount * inventoryGrid.ColumnsCount);
            for (int i = 0; i < maxI; i++) {
                InventorySlotWidget inventorySlot = new();
                inventorySlot.AssignInventorySlot(minerInventory, i + 10);
                inventoryGrid.Children.Add(inventorySlot);
                inventoryGrid.SetWidgetCell(inventorySlot, new Point2(i % inventoryGrid.ColumnsCount, i / inventoryGrid.ColumnsCount));
            }
            Size = new Vector2(inventoryGrid.ColumnsCount * 72f + 480f, maxRowsCount * 72f + 110f);
            m_recipeSelectorContainer.Size = new Vector2(444f, maxRowsCount * 72f - 112f);
        }

        public override void Update() {
            base.Update();
            if (m_sortButton.IsClicked) {
                DialogsManager.ShowDialog(
                    m_componentPlayer.GuiWidget,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "1"),
                        (SortOrder[])Enum.GetValues(typeof(SortOrder)),
                        56f,
                        o => o is SortOrder sortOrder
                            ? new LabelWidget {
                                Text = LanguageControl.Get(fName, "SortOrder", sortOrder.ToString()),
                                Color = sortOrder == m_sortOrder ? new Color(50, 150, 35) : Color.White,
                                HorizontalAlignment = WidgetAlignment.Center,
                                VerticalAlignment = WidgetAlignment.Center
                            }
                            : null,
                        o => {
                            if (o is SortOrder sortOrder) {
                                m_sortOrder = sortOrder;
                                UpdateSelector();
                            }
                        }
                    )
                );
            }
            if (m_groupButton.IsClicked) {
                DialogsManager.ShowDialog(
                    m_componentPlayer.GuiWidget,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "8"),
                        (GroupMethod[])Enum.GetValues(typeof(GroupMethod)),
                        56f,
                        o => o is GroupMethod group
                            ? new LabelWidget {
                                Text = LanguageControl.Get(fName, "GroupMethod", group.ToString()),
                                Color = group == m_groupMethod ? new Color(50, 150, 35) : Color.White,
                                HorizontalAlignment = WidgetAlignment.Center,
                                VerticalAlignment = WidgetAlignment.Center
                            }
                            : null,
                        o => {
                            if (o is GroupMethod group) {
                                m_groupMethod = group;
                                UpdateSelector();
                            }
                        }
                    )
                );
            }
            if (m_filter1Button.IsClicked) {
                DialogsManager.ShowDialog(
                    m_componentPlayer.GuiWidget,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "7"),
                        (FilterMethod1[])Enum.GetValues(typeof(FilterMethod1)),
                        56f,
                        o => o is FilterMethod1 filter1
                            ? new LabelWidget {
                                Text = LanguageControl.Get("SubsystemTerrariaCraftingTableBlockBehavior", "FilterMethod1", filter1.ToString()),
                                Color = filter1 == m_filter1 ? new Color(50, 150, 35) : Color.White,
                                HorizontalAlignment = WidgetAlignment.Center,
                                VerticalAlignment = WidgetAlignment.Center
                            }
                            : null,
                        o => {
                            if (o is FilterMethod1 filterMethod1) {
                                m_filter1 = filterMethod1;
                                UpdateSelector();
                            }
                        }
                    )
                );
            }
            if (m_filter2Button.IsClicked) {
                List<string> items = ["All", "ResultName", "ResultContents", "IngredientsName", "IngredientsContents", "Favorite"];
                items.AddRange(BlocksManager.Categories);
                DialogsManager.ShowDialog(
                    m_componentPlayer.GuiWidget,
                    new ListSelectionDialog(
                        LanguageControl.Get(fName, "2"),
                        items,
                        56f,
                        o => o is string str
                            ? new LabelWidget {
                                Text = str switch {
                                    "All" => LanguageControl.Get("SubsystemTerrariaCraftingTableBlockBehavior", "FilterMethod2", "All"),
                                    "ResultName" => LanguageControl.Get("SubsystemTerrariaCraftingTableBlockBehavior", "FilterMethod2", "ResultName"),
                                    "ResultContents" => LanguageControl.Get(
                                        "SubsystemTerrariaCraftingTableBlockBehavior",
                                        "FilterMethod2",
                                        "ResultContents"
                                    ),
                                    "IngredientsName" => LanguageControl.Get(
                                        "SubsystemTerrariaCraftingTableBlockBehavior",
                                        "FilterMethod2",
                                        "IngredientsName"
                                    ),
                                    "IngredientsContents" => LanguageControl.Get(
                                        "SubsystemTerrariaCraftingTableBlockBehavior",
                                        "FilterMethod2",
                                        "IngredientsContents"
                                    ),
                                    "Favorite" => LanguageControl.Get("SubsystemTerrariaCraftingTableBlockBehavior", "FilterMethod2", "Favorite"),
                                    _ => LanguageControl.Get("BlocksManager", str)
                                },
                                Color = str switch {
                                    "Favorite" => new Color(255, 255, 0),
                                    "Minerals" => new Color(128, 128, 128),
                                    "Electrics" => new Color(128, 140, 255),
                                    "Plants" => new Color(64, 160, 64),
                                    "Weapons" => new Color(255, 128, 112),
                                    _ => Color.White
                                },
                                HorizontalAlignment = WidgetAlignment.Center,
                                VerticalAlignment = WidgetAlignment.Center
                            }
                            : null,
                        o => {
                            if (o is string str) {
                                switch (str) {
                                    case "All":
                                        m_filter2 = FilterMethod2.All;
                                        m_filter2String = null;
                                        UpdateSelector();
                                        break;
                                    case "ResultName": PopupSearchDialog(FilterMethod2.ResultName); break;
                                    case "ResultContents": PopupSearchDialog(FilterMethod2.ResultContents); break;
                                    case "IngredientsName": PopupSearchDialog(FilterMethod2.IngredientsName); break;
                                    case "IngredientsContents": PopupSearchDialog(FilterMethod2.IngredientsContents); break;
                                    case "Favorite":
                                        m_filter2 = FilterMethod2.Favorite;
                                        m_filter2String = null;
                                        UpdateSelector();
                                        break;
                                    default:
                                        m_filter2 = FilterMethod2.ResultCategory;
                                        m_filter2String = str;
                                        UpdateSelector();
                                        break;
                                }
                            }
                        }
                    )
                );
            }
            if (m_craftingX1Button.IsClicked) {
                CraftAndUpdateSelector(1);
            }
            if (m_craftingX10Button.IsClicked) {
                CraftAndUpdateSelector(10);
            }
            if (m_craftingX40Button.IsClicked) {
                CraftAndUpdateSelector(40);
            }
            if (m_craftingXMaxButton.IsClicked) {
                CraftAndUpdateSelector(int.MaxValue);
            }
            if (m_addToFavoritesButton.IsClicked
                && m_recipeSelector.SelectedItem is CookedCraftingRecipe selected) {
                if (m_subsystem.FavoriteCookedRecipes.Add(selected)) {
                    selected.IsFavorite = true;
                    m_addToFavoritesButtonStar.FillColor = Color.Gray;
                    if (m_recipeSelector.SelectedIndex.HasValue
                        && m_recipeSelector.m_widgetsByIndex.TryGetValue(m_recipeSelector.SelectedIndex.Value, out Widget widget)
                        && widget is TerrariaCraftingRecipeSlotWidget slotWidget) {
                        slotWidget.IsStarVisible = true;
                    }
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "5"), Color.White, true, true);
                }
                else if (m_subsystem.FavoriteCookedRecipes.Remove(selected)) {
                    selected.IsFavorite = false;
                    m_addToFavoritesButtonStar.FillColor = Color.Yellow;
                    if (m_recipeSelector.SelectedIndex.HasValue
                        && m_recipeSelector.m_widgetsByIndex.TryGetValue(m_recipeSelector.SelectedIndex.Value, out Widget widget)
                        && widget is TerrariaCraftingRecipeSlotWidget slotWidget) {
                        slotWidget.IsStarVisible = false;
                    }
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "6"), Color.White, true, true);
                }
            }
        }

        public void CraftAndUpdateSelector(int times) {
            if (m_recipeSelector.SelectedItem is CookedCraftingRecipe selected) {
                times = m_subsystem.Craft(
                    selected,
                    times,
                    m_nearbyIngredients,
                    m_minerInventory,
                    new Vector3(m_position.X + 0.5f, m_position.Y + 0.5f, m_position.Z + 0.5f),
                    m_componentPlayer
                );
                if (times > 0) {
                    UpdateSelector();
                }
            }
        }

        public void UpdateSelector() {
            CookedCraftingRecipe selected = m_recipeSelector.SelectedItem as CookedCraftingRecipe;
            m_recipeSelector.SelectedItem = null;
            m_recipeSelector.ClearItems();
            List<CookedCraftingRecipe> recipes = m_subsystem.GetAvailableRecipesFromNearbyInventories(
                m_position,
                out m_nearbyIngredients,
                m_minerInventory is ComponentInventoryBase inventoryBase ? inventoryBase : null,
                m_filter1,
                m_filter2,
                m_filter2String
            );
            IOrderedEnumerable<CookedCraftingRecipe> sortedRecipes = null;
            bool needThenBy = false;
            if (m_groupMethod == GroupMethod.Craftable) {
                switch (m_filter1) {
                    case FilterMethod1.All:
                        sortedRecipes = recipes.OrderBy(recipe => recipe.CraftableTimes > 0 ? 0 :
                            recipe.AnyIngredientInInventory ? 1 : 2
                        );
                        needThenBy = true;
                        break;
                    case FilterMethod1.CraftableAndPartialIngredients:
                        sortedRecipes = recipes.OrderBy(recipe => recipe.CraftableTimes > 0 ? 0 : 1);
                        needThenBy = true;
                        break;
                }
            }
            else if (m_groupMethod == GroupMethod.ResultCategory) {
                Dictionary<string, int> categoryToIndex = [];
                for (int i = 0; i < BlocksManager.Categories.Count; i++) {
                    categoryToIndex.Add(BlocksManager.Categories[i], i);
                }
                sortedRecipes = recipes.OrderBy(recipe => categoryToIndex[BlocksManager.Blocks[Terrain.ExtractContents(recipe.ResultValue)]
                    .GetCategory(recipe.ResultValue)]
                );
                needThenBy = true;
            }
            sortedRecipes = needThenBy
                ? sortedRecipes.ThenByDescending(recipe => recipe.IsFavorite)
                : recipes.OrderByDescending(recipe => recipe.IsFavorite);
            sortedRecipes = m_sortOrder switch {
                SortOrder.DisplayOrderDescending => sortedRecipes.ThenByDescending(recipe => BlocksManager
                    .Blocks[Terrain.ExtractContents(recipe.ResultValue)]
                    .GetDisplayOrder(recipe.ResultValue)
                ),
                SortOrder.ContentsAscending => sortedRecipes.ThenBy(recipe => Terrain.ExtractContents(recipe.ResultValue)),
                SortOrder.ContentsDescending => sortedRecipes.ThenByDescending(recipe => Terrain.ExtractContents(recipe.ResultValue)),
                SortOrder.NameAscending => sortedRecipes.ThenBy(
                    recipe => BlocksManager.Blocks[Terrain.ExtractContents(recipe.ResultValue)]
                        .GetDisplayName(m_subsystemTerrain, recipe.ResultValue),
                    m_stringComparer
                ),
                SortOrder.NameDescending => sortedRecipes.ThenByDescending(
                    recipe => BlocksManager.Blocks[Terrain.ExtractContents(recipe.ResultValue)]
                        .GetDisplayName(m_subsystemTerrain, recipe.ResultValue),
                    m_stringComparer
                ),
                _ => sortedRecipes.ThenBy(recipe => BlocksManager.Blocks[Terrain.ExtractContents(recipe.ResultValue)]
                    .GetDisplayOrder(recipe.ResultValue)
                )
            };
            foreach (CookedCraftingRecipe recipe in sortedRecipes) {
                m_recipeSelector.AddItem(recipe);
                if (selected != null
                    && selected.Equals(recipe)) {
                    m_recipeSelector.SelectedItem = recipe;
                }
            }
            if (m_recipeSelector.SelectedItem != null) {
                m_recipeSelector.ScrollToItem(m_recipeSelector.SelectedItem);
            }
        }

        public void PopupSearchDialog(FilterMethod2 filterMethod2) {
            string text = null;
            bool isContents = false;
            switch (filterMethod2) {
                case FilterMethod2.ResultContents:
                case FilterMethod2.IngredientsContents: {
                    isContents = true;
                    if (int.TryParse(m_filter2String, out _)) {
                        text = m_filter2String;
                    }
                    break;
                }
                case FilterMethod2.ResultName:
                case FilterMethod2.IngredientsName: {
                    if (!int.TryParse(m_filter2String, out _)) {
                        text = m_filter2String;
                    }
                    break;
                }
                default: return;
            }
            string title = LanguageControl.Get(fName, "FilterMethod", filterMethod2.ToString());
            if (!isContents) {
                title += LanguageControl.Get(fName, "4");
            }
            DialogsManager.ShowDialog(
                m_componentPlayer.GuiWidget,
                new TextBoxDialog(
                    title,
                    text,
                    isContents ? 4 : 512,
                    str => {
                        str = str?.Trim();
                        if (string.IsNullOrEmpty(str)) {
                            return;
                        }
                        if (isContents) {
                            if (int.TryParse(str, out int num)
                                || num > 1023) {
                                m_filter2 = filterMethod2;
                                m_filter2String = str;
                                UpdateSelector();
                            }
                            else {
                                DialogsManager.ShowDialog(
                                    m_componentPlayer.GuiWidget,
                                    new MessageDialog(LanguageControl.Error, LanguageControl.Get(fName, "3"), LanguageControl.Ok, null, null)
                                );
                            }
                        }
                        else {
                            m_filter2 = filterMethod2;
                            m_filter2String = str;
                            UpdateSelector();
                        }
                    }
                )
            );
        }
    }
}