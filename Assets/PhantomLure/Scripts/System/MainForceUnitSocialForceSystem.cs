using PhantomLure.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MainForceUnitPathFollowSystem))]
    public partial struct MainForceUnitSocialForceSystem : ISystem
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
                      RefRO<MainForceSocialForceAgent> socialForceAgent,
                      RefRW<AvoidanceVelocity> avoidanceVelocity,
                      Entity entity)
                in SystemAPI.Query<RefRO<LocalTransform>,
                                   RefRO<MainForceSocialForceAgent>,
                                   RefRW<AvoidanceVelocity>>()
                    .WithAll<MainForceTag>()
                    .WithEntityAccess())
            {
                float3 repulsion = float3.zero;
                float3 selfPosition = localTransform.ValueRO.Position;

                foreach ((RefRO<LocalTransform> otherTransform,
                          RefRO<MainForceSocialForceAgent> otherAgent,
                          Entity otherEntity)
                    in SystemAPI.Query<RefRO<LocalTransform>,
                                       RefRO<MainForceSocialForceAgent>>()
                        .WithAll<MainForceTag>()
                        .WithEntityAccess())
                {
                    if (entity == otherEntity)
                    {
                        continue;
                    }

                    float3 offset = selfPosition - otherTransform.ValueRO.Position;
                    offset.y = 0.0f;

                    float distance = math.length(offset);

                    if (distance <= 0.0001f)
                    {
                        continue;
                    }

                    if (distance > socialForceAgent.ValueRO.NeighborRadius)
                    {
                        continue;
                    }

                    float combinedRadius =
                        socialForceAgent.ValueRO.PersonalSpaceRadius +
                        otherAgent.ValueRO.PersonalSpaceRadius;

                    float penetration = combinedRadius - distance;

                    if (penetration <= 0.0f)
                    {
                        continue;
                    }

                    float normalizedPenetration =
                        penetration / math.max(0.01f, socialForceAgent.ValueRO.FalloffDistance);

                    float weight = math.saturate(normalizedPenetration);
                    float3 direction = offset / distance;

                    repulsion += direction * (weight * socialForceAgent.ValueRO.RepulsionStrength);
                }

                float repulsionLength = math.length(repulsion);

                if (repulsionLength > socialForceAgent.ValueRO.MaxRepulsionSpeed)
                {
                    repulsion = (repulsion / repulsionLength) * socialForceAgent.ValueRO.MaxRepulsionSpeed;
                }

                avoidanceVelocity.ValueRW.Value = repulsion;
            }
        }
    }
}
