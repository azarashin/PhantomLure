using PhantomLure.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct MainForceCommandDebugLifetimeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRW<MainForceCommandDebug> debug, Entity entity) in
                     SystemAPI.Query<RefRW<MainForceCommandDebug>>().WithEntityAccess())
            {
                debug.ValueRW.LifeTime -= deltaTime;

                if (debug.ValueRW.LifeTime <= 0.0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
