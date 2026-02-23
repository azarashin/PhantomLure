using Unity.Burst;
using Unity.Entities;

namespace PhantomLure.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct LureLifetimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LureTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (lure, entity) in SystemAPI.Query<RefRW<Lure>>().WithAll<LureTag>().WithEntityAccess())
            {
                lure.ValueRW.Age += dt;

                if (lure.ValueRW.Age >= lure.ValueRW.Lifetime)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}