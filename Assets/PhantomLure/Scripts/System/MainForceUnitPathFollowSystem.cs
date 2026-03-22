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
                float3 previousDesiredVelocity = desiredVelocity.ValueRO.Value;
                float3 newDesiredVelocity = float3.zero;

                if (assignedSlot.ValueRO.IsValid == 0)
                {
                    desiredVelocity.ValueRW.Value = float3.zero;
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                if (SystemAPI.HasComponent<PathFailedTag>(entity))
                {
                    float3 direct = assignedSlot.ValueRO.WorldPosition - localTransform.ValueRO.Position;
                    direct.y = 0.0f;

                    if (math.lengthsq(direct) > 0.0001f)
                    {
                        newDesiredVelocity = math.normalize(direct) * moveSpeed.ValueRO.Value;
                        desiredVelocity.ValueRW.Value = newDesiredVelocity;
                        moveState.ValueRW.IsMoving = true;
                    }
                    else
                    {
                        desiredVelocity.ValueRW.Value = float3.zero;
                        moveState.ValueRW.IsMoving = false;
                    }

                    continue;
                }

                if (!SystemAPI.HasComponent<PathReadyTag>(entity))
                {
                    desiredVelocity.ValueRW.Value = float3.zero;
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                if (!SystemAPI.HasBuffer<PathPoint>(entity))
                {
                    desiredVelocity.ValueRW.Value = float3.zero;
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                DynamicBuffer<PathPoint> pathBuffer = SystemAPI.GetBuffer<PathPoint>(entity);

                if (pathBuffer.Length == 0)
                {
                    desiredVelocity.ValueRW.Value = float3.zero;
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                unitPathState.ValueRW.WaitingForPath = 0;

                float waypointSkipDistance = unitPathState.ValueRO.WaypointReachDistance * 2.0f;
                bool hasPreviousDirection = math.lengthsq(previousDesiredVelocity) > 0.0001f;
                float3 previousDirection = hasPreviousDirection
                    ? math.normalize(previousDesiredVelocity)
                    : float3.zero;

                while (unitPathState.ValueRO.CurrentPathIndex < pathBuffer.Length)
                {
                    float3 waypoint = pathBuffer[unitPathState.ValueRO.CurrentPathIndex].Value;
                    float3 toWaypoint = waypoint - localTransform.ValueRO.Position;
                    toWaypoint.y = 0.0f;

                    float distanceToWaypointSq = math.lengthsq(toWaypoint);

                    if (distanceToWaypointSq <= (waypointSkipDistance * waypointSkipDistance))
                    {
                        unitPathState.ValueRW.CurrentPathIndex += 1;
                        continue;
                    }

                    if (hasPreviousDirection)
                    {
                        float3 toWaypointDir = math.normalize(toWaypoint);
                        float dot = math.dot(previousDirection, toWaypointDir);

                        // 現在の進行方向に対してかなり後ろならスキップ
                        if (dot < -0.2f)
                        {
                            unitPathState.ValueRW.CurrentPathIndex += 1;
                            continue;
                        }
                    }

                    break;
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
                    desiredVelocity.ValueRW.Value = float3.zero;
                    moveState.ValueRW.IsMoving = false;
                    continue;
                }

                newDesiredVelocity = math.normalize(toTarget) * moveSpeed.ValueRO.Value;
                desiredVelocity.ValueRW.Value = newDesiredVelocity;
                moveState.ValueRW.IsMoving = true;
            }
        }
    }
}
