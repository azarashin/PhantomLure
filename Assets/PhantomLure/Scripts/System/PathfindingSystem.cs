using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PhantomLure.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PathfindingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<SystemHandleMarker>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
            var grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
            var gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, entity) in
                     SystemAPI.Query<RefRO<PathRequest>>()
                     .WithAll<PathRequestTag>()
                     .WithEntityAccess())
            {
                DynamicBuffer<PathPoint> pathBuffer;
                if (SystemAPI.HasBuffer<PathPoint>(entity))
                {
                    pathBuffer = SystemAPI.GetBuffer<PathPoint>(entity);
                    pathBuffer.Clear();
                }
                else
                {
                    pathBuffer = ecb.AddBuffer<PathPoint>(entity);
                }

                var path = new NativeList<Unity.Mathematics.float3>(Allocator.Temp);

                bool ok = AStarPathfinder.TryFindPath(
                    grid,
                    gridCells,
                    request.ValueRO.StartWorld,
                    request.ValueRO.GoalWorld,
                    ref path);

                ecb.RemoveComponent<PathRequestTag>(entity);
                ecb.RemoveComponent<PathReadyTag>(entity);
                ecb.RemoveComponent<PathFailedTag>(entity);

                if (ok)
                {
                    for (int i = 0; i < path.Length; i++)
                    {
                        pathBuffer.Add(new PathPoint { Value = path[i] });
                    }

                    ecb.AddComponent<PathReadyTag>(entity);
                }
                else
                {
                    ecb.AddComponent<PathFailedTag>(entity);
                }

                path.Dispose();
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>
    /// RequireForUpdate 用のダミー singleton
    /// プロジェクトの初期化時に1個だけ作る
    /// </summary>
    public struct SystemHandleMarker : IComponentData { }
}
