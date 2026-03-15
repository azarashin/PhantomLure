using PhantomLure.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ApplyMainForceMoveCommandSystem : ISystem
    {
        private EntityQuery _commandQuery;
        private EntityQuery _anchorQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _commandQuery = SystemAPI.QueryBuilder()
                .WithAll<MainForceMoveCommand>()
                .Build();

            _anchorQuery = SystemAPI.QueryBuilder()
                .WithAll<MainForceFormationAnchor, MainForcePathState>()
                .Build();

            state.RequireForUpdate(_commandQuery);
            state.RequireForUpdate(_anchorQuery);
            state.RequireForUpdate<GridConfig>();
            state.RequireForUpdate<SystemHandleMarker>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            using NativeArray<MainForceMoveCommand> commands =
                _commandQuery.ToComponentDataArray<MainForceMoveCommand>(Allocator.Temp);

            if (commands.Length == 0)
            {
                return;
            }

            MainForceMoveCommand latestCommand = commands[commands.Length - 1];
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRW<MainForceFormationAnchor> anchor, RefRW<MainForcePathState> pathState, Entity entity) in
                     SystemAPI.Query<
                         RefRW<MainForceFormationAnchor>,
                         RefRW<MainForcePathState>>().WithEntityAccess())
            {
                float3 toDestination = latestCommand.Destination - anchor.ValueRO.Position;
                toDestination.y = 0.0f;

                if (math.lengthsq(toDestination) > 0.0001f)
                {
                    anchor.ValueRW.Forward = math.normalize(toDestination);
                }

                anchor.ValueRW.Destination = latestCommand.Destination;
                anchor.ValueRW.IsMoving = true;

                pathState.ValueRW.CurrentPathIndex = 0;
                pathState.ValueRW.WaitingForPath = 1;

                if (SystemAPI.HasBuffer<PathPoint>(entity))
                {
                    DynamicBuffer<PathPoint> pathBuffer = SystemAPI.GetBuffer<PathPoint>(entity);
                    pathBuffer.Clear();
                }
                else
                {
                    ecb.AddBuffer<PathPoint>(entity);
                }

                if (SystemAPI.HasComponent<PathRequest>(entity))
                {
                    ecb.SetComponent(entity, new PathRequest
                    {
                        StartWorld = anchor.ValueRO.Position,
                        GoalWorld = latestCommand.Destination
                    });
                }
                else
                {
                    ecb.AddComponent(entity, new PathRequest
                    {
                        StartWorld = anchor.ValueRO.Position,
                        GoalWorld = latestCommand.Destination
                    });
                }

                if (!SystemAPI.HasComponent<PathRequestTag>(entity))
                {
                    ecb.AddComponent<PathRequestTag>(entity);
                }

                if (SystemAPI.HasComponent<PathReadyTag>(entity))
                {
                    ecb.RemoveComponent<PathReadyTag>(entity);
                }

                if (SystemAPI.HasComponent<PathFailedTag>(entity))
                {
                    ecb.RemoveComponent<PathFailedTag>(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            state.EntityManager.DestroyEntity(_commandQuery);
        }
    }
}
