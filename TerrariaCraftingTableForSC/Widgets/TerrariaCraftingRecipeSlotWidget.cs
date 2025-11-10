using System.Xml.Linq;
using Engine;

namespace Game {
    public class TerrariaCraftingRecipeSlotWidget : CanvasWidget {
        public RectangleWidget m_noBevelBackground;
        public BevelledRectangleWidget m_bevelledRectangleWidget;
        public BlockIconWidget m_blockIconWidget;
        public LabelWidget m_labelWidget;
        public RectangleWidget m_starWidget;
        public ClickableWidget m_clickableWidget;

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

        public bool NoBevel {
            get => field;
            set {
                field = value;
                if (value) {
                    m_bevelledRectangleWidget.IsVisible = false;
                    m_noBevelBackground.IsVisible = true;
                }
                else {
                    m_bevelledRectangleWidget.IsVisible = true;
                    m_noBevelBackground.IsVisible = false;
                }
                CenterColor = CenterColor;
            }
        }

        public Color CenterColor {
            get => field;
            set {
                field = value;
                if (NoBevel) {
                    m_noBevelBackground.FillColor = value;
                }
                else {
                    m_bevelledRectangleWidget.CenterColor = value;
                }
            }
        }

        public bool IsStarVisible {
            get => m_starWidget.IsVisible;
            set => m_starWidget.IsVisible = value;
        }

        public bool IsClickable {
            get => m_clickableWidget.IsVisible;
            set => m_clickableWidget.IsVisible = value;
        }

        public TerrariaCraftingRecipeSlotWidget() {
            XElement node = ContentManager.Get<XElement>("Widgets/TerrariaCraftingRecipeSlotWidget");
            LoadContents(this, node);
            m_noBevelBackground = Children.Find<RectangleWidget>("TerrariaCraftingRecipeSlotWidget.NoBevelBackground");
            m_bevelledRectangleWidget = Children.Find<BevelledRectangleWidget>("TerrariaCraftingRecipeSlotWidget.BevelledRectangle");
            m_blockIconWidget = Children.Find<BlockIconWidget>("TerrariaCraftingRecipeSlotWidget.Icon");
            m_labelWidget = Children.Find<LabelWidget>("TerrariaCraftingRecipeSlotWidget.Count");
            m_starWidget = Children.Find<RectangleWidget>("TerrariaCraftingRecipeSlotWidget.Star");
            m_clickableWidget = Children.Find<ClickableWidget>("TerrariaCraftingRecipeSlotWidget.Clickable");
        }

        public override void Update() {
            base.Update();
            if (m_clickableWidget.IsClicked) {
                ScreensManager.SwitchScreen(
                    BlocksManager.Blocks[Terrain.ExtractContents(Value)].GetBlockDescriptionScreen(Value),
                    Value,
                    new int[1] { Value }
                );
            }
        }
    }
}