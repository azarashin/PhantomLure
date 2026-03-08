using Unity.Entities;
using UnityEngine;

namespace PhantomLure.ECS
{
    public struct PathDebugLoggedTag : IComponentData { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PathDebugLogSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (request, entity) in
                     SystemAPI.Query<RefRO<PathRequest>>()
                     .WithAll<PathReadyTag>()
                     .WithNone<PathDebugLoggedTag>()
                     .WithEntityAccess())
            {
                int count = 0;
                if (SystemAPI.HasBuffer<PathPoint>(entity))
                {
                    count = SystemAPI.GetBuffer<PathPoint>(entity).Length;
                }

                Debug.Log(
                    $"[PathDebug] READY Entity({entity.Index}:{entity.Version}) " +
                    $"Start={request.ValueRO.StartWorld} Goal={request.ValueRO.GoalWorld} " +
                    $"Points={count}");

                ecb.AddComponent<PathDebugLoggedTag>(entity);
            }

            foreach (var (request, entity) in
                     SystemAPI.Query<RefRO<PathRequest>>()
                     .WithAll<PathFailedTag>()
                     .WithNone<PathDebugLoggedTag>()
                     .WithEntityAccess())
            {
                Debug.LogWarning(
                    $"[PathDebug] FAILED Entity({entity.Index}:{entity.Version}) " +
                    $"Start={request.ValueRO.StartWorld} Goal={request.ValueRO.GoalWorld}");

                ecb.AddComponent<PathDebugLoggedTag>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
