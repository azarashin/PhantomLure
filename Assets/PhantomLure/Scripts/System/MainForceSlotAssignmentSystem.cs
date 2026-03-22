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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            bool hasGrid = SystemAPI.HasSingleton<GridConfig>();
            GridConfig grid = default;

            if (hasGrid)
            {
                grid = SystemAPI.GetSingleton<GridConfig>();
            }

            foreach ((RefRW<AssignedSlot> assignedSlot, RefRO<FormationIndex> formationIndex, RefRO<FormationMember> formationMember)
                in SystemAPI.Query<RefRW<AssignedSlot>, RefRO<FormationIndex>, RefRO<FormationMember>>()
                    .WithAll<MainForceTag>())
            {
                Entity anchorEntity = formationMember.ValueRO.AnchorEntity;

                if (!SystemAPI.Exists(anchorEntity))
                {
                    assignedSlot.ValueRW.IsValid = 0;
                    continue;
                }

                if (!SystemAPI.HasComponent<MainForceFormationAnchor>(anchorEntity))
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

                assignedSlot.ValueRW.WorldPosition = slotWorldPosition;
                assignedSlot.ValueRW.IsValid = 1;

                if (hasGrid)
                {
                    assignedSlot.ValueRW.SlotCell = GridUtility.ToCell(grid, slotWorldPosition);
                }
                else
                {
                    assignedSlot.ValueRW.SlotCell = int2.zero;
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
