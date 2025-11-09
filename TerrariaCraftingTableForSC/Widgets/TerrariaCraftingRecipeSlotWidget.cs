using System.Xml.Linq;
using Engine;

namespace Game {
    public class TerrariaCraftingRecipeSlotWidget : CanvasWidget {
        public RectangleWidget m_backgroundWidget;
        public BevelledRectangleWidget m_bevelledRectangleWidget;
        public BlockIconWidget m_blockIconWidget;
        public LabelWidget m_labelWidget;
        public RectangleWidget m_starWidget;

        public static Color InvalidColor = new(70, 10, 0, 90);

        public int Value {
            get => m_blockIconWidget.Value;
            set {
                if (Terrain.ExtractContents(value) == 0) {
                    m_blockIconWidget.Value = 0;
                    m_blockIconWidget.IsVisible = false;
                }
                else {
                    m_blockIconWidget.Value = value;
                    m_blockIconWidget.Light = 15;
                    m_blockIconWidget.IsVisible = true;
                }
            }
        }

        public int Count {
            get => field;
            set {
                field = value;
                if (value < 0) {
                    m_labelWidget.IsVisible = false;
                }
                else {
                    m_labelWidget.Text = value > 9999 ? "9999+" : value.ToString();
                    m_labelWidget.IsVisible = true;
                }
            }
        }

        public bool IsBevelledRectangleVisible {
            get => m_bevelledRectangleWidget.IsVisible;
            set {
                m_bevelledRectangleWidget.IsVisible = value;
                IsInvalid = IsInvalid;
            }
        }

        public bool IsInvalid {
            get => field;
            set {
                field = value;
                if (IsBevelledRectangleVisible) {
                    m_bevelledRectangleWidget.CenterColor = value ? InvalidColor : Color.Transparent;
                }
                else {
                    m_backgroundWidget.IsVisible = value;
                }
            }
        }

        public bool IsStarVisible {
            get => m_starWidget.IsVisible;
            set => m_starWidget.IsVisible = value;
        }

        public TerrariaCraftingRecipeSlotWidget() {
            XElement node = ContentManager.Get<XElement>("Widgets/TerrariaCraftingRecipeSlotWidget");
            LoadContents(this, node);
            m_backgroundWidget = Children.Find<RectangleWidget>("TerrariaCraftingRecipeSlotWidget.Background");
            m_bevelledRectangleWidget = Children.Find<BevelledRectangleWidget>("TerrariaCraftingRecipeSlotWidget.BevelledRectangle");
            m_blockIconWidget = Children.Find<BlockIconWidget>("TerrariaCraftingRecipeSlotWidget.Icon");
            m_labelWidget = Children.Find<LabelWidget>("TerrariaCraftingRecipeSlotWidget.Count");
            m_starWidget = Children.Find<RectangleWidget>("TerrariaCraftingRecipeSlotWidget.Star");
        }
    }
}