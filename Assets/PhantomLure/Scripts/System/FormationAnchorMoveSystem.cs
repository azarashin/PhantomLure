using PhantomLure.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ApplyMainForceMoveCommandSystem))]
    public partial struct FormationAnchorMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainForceFormationAnchor>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (RefRW<MainForceFormationAnchor> anchor in
                     SystemAPI.Query<RefRW<MainForceFormationAnchor>>())
            {
                if (!anchor.ValueRO.IsMoving)
                {
                    continue;
                }

                float3 toDestination = anchor.ValueRO.Destination - anchor.ValueRO.Position;
                toDestination.y = 0.0f;

                float distance = math.length(toDestination);

                if (distance <= anchor.ValueRO.ArriveDistance)
                {
                    anchor.ValueRW.Position = anchor.ValueRO.Destination;
                    anchor.ValueRW.IsMoving = false;
                    continue;
                }

                float3 forward = toDestination / math.max(distance, 0.0001f);
                float step = anchor.ValueRO.MoveSpeed * deltaTime;

                anchor.ValueRW.Forward = forward;

                if (step >= distance)
                {
                    anchor.ValueRW.Position = anchor.ValueRO.Destination;
                    anchor.ValueRW.IsMoving = false;
                }
                else
                {
                    anchor.ValueRW.Position = anchor.ValueRO.Position + (forward * step);
                }
            }
        }
    }
}
