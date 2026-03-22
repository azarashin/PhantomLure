using PhantomLure.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
            Entity gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
            GridConfig grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
            DynamicBuffer<GridCell> gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRO<PathRequest> request, Entity entity)
                in SystemAPI.Query<RefRO<PathRequest>>()
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

                if (SystemAPI.HasComponent<MainForcePathState>(entity))
                {
                    MainForcePathState pathState = SystemAPI.GetComponent<MainForcePathState>(entity);
                    pathState.CurrentPathIndex = 0;
                    pathState.WaitingForPath = 1;
                    ecb.SetComponent(entity, pathState);
                }

                if (SystemAPI.HasComponent<UnitPathState>(entity))
                {
                    UnitPathState unitPathState = SystemAPI.GetComponent<UnitPathState>(entity);
                    unitPathState.CurrentPathIndex = 0;
                    unitPathState.WaitingForPath = 1;
                    ecb.SetComponent(entity, unitPathState);
                }

                NativeList<float3> path = new NativeList<float3>(Allocator.Temp);

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
                        pathBuffer.Add(new PathPoint
                        {
                            Value = path[i]
                        });
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
    public struct SystemHandleMarker : IComponentData
    {
    }
}
