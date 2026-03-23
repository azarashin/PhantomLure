using PhantomLure.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PathfindingSystem))]
    [UpdateAfter(typeof(ApplyMainForceMoveCommandSystem))]
    public partial struct FormationAnchorMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainForceFormationAnchor>();
            state.RequireForUpdate<GridConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach ((RefRW<MainForceFormationAnchor> anchor, RefRW<MainForcePathState> pathState, Entity entity) in
                     SystemAPI.Query<
                         RefRW<MainForceFormationAnchor>,
                         RefRW<MainForcePathState>>().WithEntityAccess())
            {
                if (!anchor.ValueRO.IsMoving)
                {
                    continue;
                }

                if (SystemAPI.HasComponent<PathFailedTag>(entity))
                {
                    anchor.ValueRW.IsMoving = false;
                    pathState.ValueRW.WaitingForPath = 0;
                    continue;
                }

                if (!SystemAPI.HasComponent<PathReadyTag>(entity) || !SystemAPI.HasBuffer<PathPoint>(entity))
                {
                    continue;
                }

                DynamicBuffer<PathPoint> pathBuffer = SystemAPI.GetBuffer<PathPoint>(entity);

                if (pathBuffer.Length == 0)
                {
                    anchor.ValueRW.IsMoving = false;
                    pathState.ValueRW.WaitingForPath = 0;
                    continue;
                }

                pathState.ValueRW.WaitingForPath = 0;

                while (pathState.ValueRO.CurrentPathIndex < pathBuffer.Length)
                {
                    float3 waypoint = pathBuffer[pathState.ValueRO.CurrentPathIndex].Value;
                    float3 toWaypoint = waypoint - anchor.ValueRO.Position;
                    toWaypoint.y = 0.0f;

                    float waypointDistance = math.length(toWaypoint);

                    if (waypointDistance > pathState.ValueRO.WaypointReachDistance)
                    {
                        break;
                    }

                    pathState.ValueRW.CurrentPathIndex += 1;
                }

                if (pathState.ValueRO.CurrentPathIndex >= pathBuffer.Length)
                {
                    float3 toDestination = anchor.ValueRO.Destination - anchor.ValueRO.Position;
                    toDestination.y = 0.0f;

                    if (math.length(toDestination) <= anchor.ValueRO.ArriveDistance)
                    {
                        anchor.ValueRW.Position = anchor.ValueRO.Destination;
                        anchor.ValueRW.IsMoving = false;
                        continue;
                    }

                    pathState.ValueRW.CurrentPathIndex = pathBuffer.Length - 1;
                }

                float3 target = pathBuffer[pathState.ValueRO.CurrentPathIndex].Value;
                float3 toTarget = target - anchor.ValueRO.Position;
                toTarget.y = 0.0f;

                float distance = math.length(toTarget);

                if (distance <= 0.0001f)
                {
                    continue;
                }

                float3 forward = toTarget / distance;
                float step = anchor.ValueRO.MoveSpeed * deltaTime;

                anchor.ValueRW.Forward = forward;

                if (step >= distance)
                {
                    anchor.ValueRW.Position = target;
                }
                else
                {
                    anchor.ValueRW.Position = anchor.ValueRO.Position + (forward * step);
                }
            }
        }
    }
}
