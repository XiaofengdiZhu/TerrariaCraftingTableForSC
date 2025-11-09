using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Engine;
using IngredientInfo = Game.CookedCraftingRecipe.IngredientInfo;

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

        public enum FilterMethod {
            None,
            ResultName,
            ResultContents,
            ResultCategory,
            IngredientsName,
            IngredientsContents,
            Favorite
        }

        public ButtonWidget m_sortButton;
        public ButtonWidget m_categoryButton;
        public CanvasWidget m_recipeSelectorContainer;
        public SelectiveFlexPanelWidget m_recipeSelector;
        public StackPanelWidget m_ingredientSlots;
        public TerrariaCraftingRecipeSlotWidget m_resultSlot;
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
        public bool m_includingZeroResult;
        public FilterMethod m_filterMethod = FilterMethod.None;
        public string m_filterString;

        public static SortOrder m_sortOrder = SortOrder.DisplayOrderAscending;
        public static StringComparer m_stringComparer;
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
            m_sortButton = Children.Find<ButtonWidget>("SortButton");
            m_categoryButton = Children.Find<ButtonWidget>("CategoryButton");
            m_recipeSelectorContainer = Children.Find<CanvasWidget>("RecipeSelectorContainer");
            m_recipeSelector = Children.Find<SelectiveFlexPanelWidget>("RecipeSelector");
            m_recipeSelector.ItemWidgetFactory = o => o is KeyValuePair<CookedCraftingRecipe, int> pair
                ? new TerrariaCraftingRecipeSlotWidget {
                    Value = pair.Key.ResultValue,
                    Count = pair.Value,
                    IsBevelledRectangleVisible = false,
                    IsInvalid = pair.Value == 0,
                    IsStarVisible = pair.Key.IsFavorite
                }
                : null;
            m_recipeSelector.ItemClicked = o => {
                if (o is not KeyValuePair<CookedCraftingRecipe, int> pair) {
                    return;
                }
                m_ingredientSlots.ClearChildren();
                foreach ((int ingredient, int count) in pair.Key.Ingredients) {
                    bool notEnough = true;
                    if (m_nearbyIngredients.TryGetValue(ingredient, out IngredientInfo ingredientInfo)) {
                        if (ingredientInfo.Count > count) {
                            notEnough = false;
                        }
                    }
                    m_ingredientSlots.Children.Add(new TerrariaCraftingRecipeSlotWidget { Value = ingredient, Count = count, IsInvalid = notEnough });
                }
                m_resultSlot.Value = pair.Key.ResultValue;
                m_resultSlot.Count = pair.Key.ResultCount;
                m_resultSlot.IsInvalid = pair.Value == 0;
                m_addToFavoritesButtonStar.FillColor = pair.Key.IsFavorite ? Color.Gray : Color.Yellow;
            };
            UpdateSelector();
            m_ingredientSlots = Children.Find<StackPanelWidget>("IngredientSlots");
            m_resultSlot = Children.Find<TerrariaCraftingRecipeSlotWidget>("ResultSlot");
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
                        o => LanguageControl.Get(fName, "SortOrder", o.ToString()),
                        o => {
                            if (o is SortOrder sortOrder) {
                                m_sortOrder = sortOrder;
                                UpdateSelector();
                            }
                        }
                    )
                );
            }
            if (m_categoryButton.IsClicked) {
                List<string> items = [
                    "None",
                    "IncludingZeroResult",
                    "ResultName",
                    "ResultContents",
                    "IngredientsName",
                    "IngredientsContents",
                    "Favorite"
                ];
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
                                    "None" => LanguageControl.Get(fName, "FilterMethod", "None"),
                                    "IncludingZeroResult" => LanguageControl.Get(fName, m_includingZeroResult ? "6" : "5"),
                                    "ResultName" => LanguageControl.Get(fName, "FilterMethod", "ResultName"),
                                    "ResultContents" => LanguageControl.Get(fName, "FilterMethod", "ResultContents"),
                                    "IngredientsName" => LanguageControl.Get(fName, "FilterMethod", "IngredientsName"),
                                    "IngredientsContents" => LanguageControl.Get(fName, "FilterMethod", "IngredientsContents"),
                                    "Favorite" => LanguageControl.Get(fName, "FilterMethod", "Favorite"),
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
                                    case "None":
                                        m_filterMethod = FilterMethod.None;
                                        m_filterString = null;
                                        UpdateSelector();
                                        break;
                                    case "IncludingZeroResult":
                                        m_includingZeroResult = !m_includingZeroResult;
                                        UpdateSelector();
                                        break;
                                    case "ResultName": PopupSearchDialog(FilterMethod.ResultName); break;
                                    case "ResultContents": PopupSearchDialog(FilterMethod.ResultContents); break;
                                    case "IngredientsName": PopupSearchDialog(FilterMethod.IngredientsName); break;
                                    case "IngredientsContents": PopupSearchDialog(FilterMethod.IngredientsContents); break;
                                    case "Favorite":
                                        m_filterMethod = FilterMethod.Favorite;
                                        m_filterString = null;
                                        UpdateSelector();
                                        break;
                                    default:
                                        m_filterMethod = FilterMethod.ResultCategory;
                                        m_filterString = str;
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
                && m_recipeSelector.SelectedItem is KeyValuePair<CookedCraftingRecipe, int> selected) {
                if (m_subsystem.FavoriteCookedRecipes.Add(selected.Key)) {
                    selected.Key.IsFavorite = true;
                    m_addToFavoritesButtonStar.FillColor = Color.Gray;
                    if (m_recipeSelector.SelectedIndex.HasValue
                        && m_recipeSelector.m_widgetsByIndex.TryGetValue(m_recipeSelector.SelectedIndex.Value, out Widget widget)
                        && widget is TerrariaCraftingRecipeSlotWidget slotWidget) {
                        slotWidget.IsStarVisible = true;
                    }
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "7"), Color.White, true, true);
                }
                else if (m_subsystem.FavoriteCookedRecipes.Remove(selected.Key)) {
                    selected.Key.IsFavorite = false;
                    m_addToFavoritesButtonStar.FillColor = Color.Yellow;
                    if (m_recipeSelector.SelectedIndex.HasValue
                        && m_recipeSelector.m_widgetsByIndex.TryGetValue(m_recipeSelector.SelectedIndex.Value, out Widget widget)
                        && widget is TerrariaCraftingRecipeSlotWidget slotWidget) {
                        slotWidget.IsStarVisible = false;
                    }
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "8"), Color.White, true, true);
                }
            }
        }

        public void CraftAndUpdateSelector(int times) {
            if (m_recipeSelector.SelectedItem is KeyValuePair<CookedCraftingRecipe, int> selected) {
                times = m_subsystem.Craft(
                    selected.Key,
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
            CookedCraftingRecipe selected = m_recipeSelector.SelectedItem is KeyValuePair<CookedCraftingRecipe, int> pair1 ? pair1.Key : null;
            m_recipeSelector.SelectedItem = null;
            m_recipeSelector.ClearItems();
            Dictionary<CookedCraftingRecipe, int> dictionary = m_subsystem.GetAvailableRecipesFromNearbyInventories(
                m_position,
                out m_nearbyIngredients,
                m_minerInventory is ComponentInventoryBase inventoryBase ? inventoryBase : null,
                m_includingZeroResult
            );
            Regex regex = m_filterMethod is FilterMethod.ResultName or FilterMethod.IngredientsName
                ? new Regex(m_filterString, RegexOptions.IgnoreCase)
                : null;
            IEnumerable<KeyValuePair<CookedCraftingRecipe, int>> filteredDictionary = m_filterMethod switch {
                FilterMethod.ResultName => dictionary.Where(pair2 => regex!.IsMatch(
                        BlocksManager.Blocks[Terrain.ExtractContents(pair2.Key.ResultValue)].GetDisplayName(m_subsystemTerrain, pair2.Key.ResultValue)
                    )
                ),
                FilterMethod.ResultContents => int.TryParse(m_filterString, out int num)
                    ? dictionary.Where(pair2 => Terrain.ExtractContents(pair2.Key.ResultValue) == num)
                    : dictionary,
                FilterMethod.ResultCategory => dictionary.Where(pair2 => BlocksManager.Blocks[Terrain.ExtractContents(pair2.Key.ResultValue)]
                        .GetCategory(pair2.Key.ResultValue)
                    == m_filterString
                ),
                FilterMethod.IngredientsName => dictionary.Where(pair2 => pair2.Key.Ingredients.Any(pair3 => regex!.IsMatch(
                            BlocksManager.Blocks[Terrain.ExtractContents(pair3.Key)].GetDisplayName(m_subsystemTerrain, pair3.Key)
                        )
                    )
                ),
                FilterMethod.IngredientsContents => int.TryParse(m_filterString, out int num2)
                    ? dictionary.Where(pair2 => pair2.Key.Ingredients.Any(pair3 => Terrain.ExtractContents(pair3.Key) == num2))
                    : dictionary,
                FilterMethod.Favorite => dictionary.Where(pair2 => pair2.Key.IsFavorite),
                _ => dictionary
            };
            IOrderedEnumerable<KeyValuePair<CookedCraftingRecipe, int>> sortedDictionary = m_sortOrder switch {
                SortOrder.DisplayOrderDescending => filteredDictionary.OrderByDescending(pair3 => BlocksManager
                    .Blocks[Terrain.ExtractContents(pair3.Key.ResultValue)]
                    .GetDisplayOrder(pair3.Key.ResultValue)
                ),
                SortOrder.ContentsAscending => filteredDictionary.OrderBy(pair3 => Terrain.ExtractContents(pair3.Key.ResultValue)),
                SortOrder.ContentsDescending => filteredDictionary.OrderByDescending(pair3 => Terrain.ExtractContents(pair3.Key.ResultValue)),
                SortOrder.NameAscending => filteredDictionary.OrderBy(
                    pair3 => BlocksManager.Blocks[Terrain.ExtractContents(pair3.Key.ResultValue)]
                        .GetDisplayName(m_subsystemTerrain, pair3.Key.ResultValue),
                    m_stringComparer
                ),
                SortOrder.NameDescending => filteredDictionary.OrderByDescending(
                    pair3 => BlocksManager.Blocks[Terrain.ExtractContents(pair3.Key.ResultValue)]
                        .GetDisplayName(m_subsystemTerrain, pair3.Key.ResultValue),
                    m_stringComparer
                ),
                _ => filteredDictionary.OrderBy(pair3 => BlocksManager.Blocks[Terrain.ExtractContents(pair3.Key.ResultValue)]
                    .GetDisplayOrder(pair3.Key.ResultValue)
                )
            };
            foreach (KeyValuePair<CookedCraftingRecipe, int> pair4 in sortedDictionary) {
                m_recipeSelector.AddItem(pair4);
                if (selected != null
                    && selected.Equals(pair4.Key)) {
                    m_recipeSelector.SelectedItem = pair4;
                }
            }
            if (m_recipeSelector.SelectedItem != null) {
                m_recipeSelector.ScrollToItem(m_recipeSelector.SelectedItem);
            }
        }

        public void PopupSearchDialog(FilterMethod filterMethod) {
            string text = null;
            bool isContents = false;
            switch (filterMethod) {
                case FilterMethod.ResultContents:
                case FilterMethod.IngredientsContents: {
                    isContents = true;
                    if (int.TryParse(m_filterString, out _)) {
                        text = m_filterString;
                    }
                    break;
                }
                case FilterMethod.ResultName:
                case FilterMethod.IngredientsName: {
                    if (!int.TryParse(m_filterString, out _)) {
                        text = m_filterString;
                    }
                    break;
                }
                default: return;
            }
            string title = LanguageControl.Get(fName, "FilterMethod", filterMethod.ToString());
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
                                m_filterMethod = filterMethod;
                                m_filterString = str;
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
                            m_filterMethod = filterMethod;
                            m_filterString = str;
                            UpdateSelector();
                        }
                    }
                )
            );
        }
    }
}