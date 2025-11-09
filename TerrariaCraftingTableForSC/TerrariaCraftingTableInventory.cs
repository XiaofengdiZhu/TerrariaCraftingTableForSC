using Engine;
using GameEntitySystem;

namespace Game {
    public class TerrariaCraftingTableInventory : IInventory {
        public Project m_project;
        public int m_slotsCount;
        public int m_activeSlotIndex;

        public Project Project => m_project;
        public int SlotsCount => m_slotsCount;

        public int VisibleSlotsCount {
            get => m_slotsCount;
            set { }
        }

        public int ActiveSlotIndex {
            get => m_activeSlotIndex;
            set => m_activeSlotIndex = value;
        }

        public int GetSlotValue(int slotIndex) => throw new System.NotImplementedException();

        public int GetSlotCount(int slotIndex) => throw new System.NotImplementedException();

        public int GetSlotCapacity(int slotIndex, int value) => 0;

        public int GetSlotProcessCapacity(int slotIndex, int value) => 0;

        public void AddSlotItems(int slotIndex, int value, int count) {
            throw new System.NotImplementedException();
        }

        public void ProcessSlotItems(int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount) {
            throw new System.NotImplementedException();
        }

        public int RemoveSlotItems(int slotIndex, int count) => throw new System.NotImplementedException();

        public void DropAllItems(Vector3 position) { }
    }
}