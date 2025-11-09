using Engine;
using Engine.Graphics;

namespace Game {
    public class TerrariaCraftingTableBlock : Block {
        public BlockMesh m_blockMesh = new();
        public BlockMesh m_standaloneBlockMesh = new();
        public BoundingBox[] m_collisionBoxes = [new(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f))];

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/CraftingTable");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("CraftingTable").ParentBone);
            m_blockMesh.AppendModelMeshPart(
                model.FindMesh("CraftingTable").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateScale(1f, 0.5f, 1f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("CraftingTable").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateScale(1f, 0.5f, 1f) * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            generator.GenerateShadedMeshVertices(
                this,
                x,
                y,
                z,
                m_blockMesh,
                Color.White,
                null,
                null,
                geometry.SubsetOpaque
            );
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) => m_collisionBoxes;
    }
}