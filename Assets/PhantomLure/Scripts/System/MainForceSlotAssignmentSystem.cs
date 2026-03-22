using PhantomLure.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FormationAnchorMoveSystem))]
    public partial struct MainForceSlotAssignmentSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainForceTag>();
            state.RequireForUpdate<GridConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            GridConfig grid = SystemAPI.GetSingleton<GridConfig>();
            Entity gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
            DynamicBuffer<GridCell> gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);

            foreach ((RefRW<AssignedSlot> assignedSlot, RefRO<FormationIndex> formationIndex, RefRO<FormationMember> formationMember)
                in SystemAPI.Query<RefRW<AssignedSlot>, RefRO<FormationIndex>, RefRO<FormationMember>>()
                    .WithAll<MainForceTag>())
            {
                Entity anchorEntity = formationMember.ValueRO.AnchorEntity;

                if (!SystemAPI.Exists(anchorEntity) || !SystemAPI.HasComponent<MainForceFormationAnchor>(anchorEntity))
                {
                    assignedSlot.ValueRW.IsValid = 0;
                    continue;
                }

                MainForceFormationAnchor anchor = SystemAPI.GetComponent<MainForceFormationAnchor>(anchorEntity);

                float3 anchorForward = anchor.Forward;
                anchorForward.y = 0.0f;

                if (math.lengthsq(anchorForward) < 0.0001f)
                {
                    anchorForward = new float3(0.0f, 0.0f, 1.0f);
                }
                else
                {
                    anchorForward = math.normalize(anchorForward);
                }

                float3 anchorRight = math.normalize(math.cross(new float3(0.0f, 1.0f, 0.0f), anchorForward));

                float3 slotWorldPosition = CalculateSlotWorldPosition(anchor, formationIndex.ValueRO.Value, anchorForward, anchorRight);
                int2 slotCell = GridUtility.ToCell(grid, slotWorldPosition);
                bool isSlotWalkable = GridUtility.IsWalkable(grid, gridCells, slotCell);

                assignedSlot.ValueRW.WorldPosition = slotWorldPosition;
                assignedSlot.ValueRW.SlotCell = slotCell;
                assignedSlot.ValueRW.IsValid = 1;
                assignedSlot.ValueRW.IsSlotWalkable = isSlotWalkable ? (byte)1 : (byte)0;

                if (isSlotWalkable)
                {
                    assignedSlot.ValueRW.NavigationTargetWorld = slotWorldPosition;
                    assignedSlot.ValueRW.NavigationTargetCell = slotCell;
                }
                else
                {
                    float3 fallbackTarget = anchor.Position;
                    int2 fallbackCell = GridUtility.ToCell(grid, fallbackTarget);

                    assignedSlot.ValueRW.NavigationTargetWorld = fallbackTarget;
                    assignedSlot.ValueRW.NavigationTargetCell = fallbackCell;
                }
            }
        }

        [BurstCompile]
        private static float3 CalculateSlotWorldPosition(MainForceFormationAnchor anchor, int formationIndex, float3 anchorForward, float3 anchorRight)
        {
            int columns = math.max(1, anchor.ColumnCount);
            int index = math.max(0, formationIndex);
            int column = index % columns;
            int row = index / columns;
            float centeredColumn = column - ((columns - 1) * 0.5f);

            float3 offset =
                (anchorRight * (centeredColumn * anchor.SpacingSide)) -
                (anchorForward * (row * anchor.SpacingBack));

            return anchor.Position + offset;
        }
    }
}
