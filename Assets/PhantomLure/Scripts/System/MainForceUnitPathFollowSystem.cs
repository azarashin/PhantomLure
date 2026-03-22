using PhantomLure.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PathfindingSystem))]
    public partial struct MainForceUnitPathFollowSystem : ISystem
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

            foreach ((RefRO<LocalTransform> localTransform,
                      RefRO<MoveSpeed> moveSpeed,
                      RefRO<AssignedSlot> assignedSlot,
                      RefRW<UnitPathState> unitPathState,
                      RefRW<DesiredVelocity> desiredVelocity,
                      RefRW<MoveState> moveState,
                      Entity entity)
                in SystemAPI.Query<RefRO<LocalTransform>,
                                   RefRO<MoveSpeed>,
                                   RefRO<AssignedSlot>,
                                   RefRW<UnitPathState>,
                                   RefRW<DesiredVelocity>,
                                   RefRW<MoveState>>()
                    .WithAll<MainForceTag>()
                    .WithEntityAccess())
            {
                desiredVelocity.ValueRW.Value = float3.zero;

                if (assignedSlot.ValueRO.IsValid == 0)
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                if (SystemAPI.HasComponent<PathFailedTag>(entity))
                {
                    float3 direct = assignedSlot.ValueRO.NavigationTargetWorld - localTransform.ValueRO.Position;
                    direct.y = 0.0f;

                    if (math.lengthsq(direct) > 0.0001f)
                    {
                        desiredVelocity.ValueRW.Value = math.normalize(direct) * moveSpeed.ValueRO.Value;
                        moveState.ValueRW.IsMoving = true;
                    }
                    else
                    {
                        moveState.ValueRW.IsMoving = false;
                    }

                    continue;
                }

                if (!SystemAPI.HasComponent<PathReadyTag>(entity))
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                if (!SystemAPI.HasBuffer<PathPoint>(entity))
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                DynamicBuffer<PathPoint> pathBuffer = SystemAPI.GetBuffer<PathPoint>(entity);

                if (pathBuffer.Length == 0)
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                unitPathState.ValueRW.WaitingForPath = 0;

                // 1. 到達済みの waypoint だけ順に消化する
                while (unitPathState.ValueRO.CurrentPathIndex < pathBuffer.Length)
                {
                    float3 waypoint = pathBuffer[unitPathState.ValueRO.CurrentPathIndex].Value;
                    float3 toWaypoint = waypoint - localTransform.ValueRO.Position;
                    toWaypoint.y = 0.0f;

                    if (math.length(toWaypoint) > unitPathState.ValueRO.WaypointReachDistance)
                    {
                        break;
                    }

                    unitPathState.ValueRW.CurrentPathIndex += 1;
                }

                // 2. 現在位置から直線で見通せる範囲で、最も先の waypoint を target にする
                int targetIndex = math.min(unitPathState.ValueRO.CurrentPathIndex, pathBuffer.Length - 1);

                for (int i = targetIndex; i < pathBuffer.Length; i++)
                {
                    float3 candidate = pathBuffer[i].Value;

                    if (HasLineOfSight(grid, gridCells, localTransform.ValueRO.Position, candidate))
                    {
                        targetIndex = i;
                    }
                    else
                    {
                        break;
                    }
                }

                float3 targetPosition = pathBuffer[targetIndex].Value;
                float3 toTarget = targetPosition - localTransform.ValueRO.Position;
                toTarget.y = 0.0f;

                if (math.lengthsq(toTarget) <= 0.0001f)
                {
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                desiredVelocity.ValueRW.Value = math.normalize(toTarget) * moveSpeed.ValueRO.Value;
                moveState.ValueRW.IsMoving = true;
            }
        }

        [BurstCompile]
        private static bool HasLineOfSight(GridConfig grid, DynamicBuffer<GridCell> gridCells, float3 from, float3 to)
        {
            float3 delta = to - from;
            delta.y = 0.0f;

            float distance = math.length(delta);

            if (distance <= 0.001f)
            {
                return true;
            }

            float3 direction = delta / distance;
            float sampleStep = math.max(0.1f, grid.CellSize * 0.4f);
            int sampleCount = math.max(1, (int)math.ceil(distance / sampleStep));

            for (int i = 1; i <= sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float3 sample = from + (delta * t);
                int2 cell = GridUtility.ToCell(grid, sample);

                if (!GridUtility.IsWalkable(grid, gridCells, cell))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
