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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
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
                    float3 direct = assignedSlot.ValueRO.WorldPosition - localTransform.ValueRO.Position;
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

                float3 targetPosition = assignedSlot.ValueRO.WorldPosition;

                if (unitPathState.ValueRO.CurrentPathIndex < pathBuffer.Length)
                {
                    targetPosition = pathBuffer[unitPathState.ValueRO.CurrentPathIndex].Value;
                }

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
    }
}
