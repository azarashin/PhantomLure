using PhantomLure.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MainForceUnitRepathDecisionSystem))]
    public partial struct MainForceUnitPathRequestBuildSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainForceTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRO<LocalTransform> localTransform,
                      RefRO<AssignedSlot> assignedSlot,
                      RefRW<UnitPathState> unitPathState,
                      RefRW<UnitRepathState> unitRepathState,
                      RefRW<UnitStuckState> unitStuckState,
                      Entity entity)
                in SystemAPI.Query<RefRO<LocalTransform>,
                                   RefRO<AssignedSlot>,
                                   RefRW<UnitPathState>,
                                   RefRW<UnitRepathState>,
                                   RefRW<UnitStuckState>>()
                    .WithAll<MainForceTag, NeedsUnitRepathTag>()
                    .WithEntityAccess())
            {
                if (assignedSlot.ValueRO.IsValid == 0)
                {
                    ecb.RemoveComponent<NeedsUnitRepathTag>(entity);
                    continue;
                }

                if (!SystemAPI.HasBuffer<PathPoint>(entity))
                {
                    ecb.AddBuffer<PathPoint>(entity);
                }

                PathRequest request = new PathRequest
                {
                    StartWorld = localTransform.ValueRO.Position,
                    GoalWorld = assignedSlot.ValueRO.WorldPosition
                };

                if (SystemAPI.HasComponent<PathRequest>(entity))
                {
                    ecb.SetComponent(entity, request);
                }
                else
                {
                    ecb.AddComponent(entity, request);
                }

                if (!SystemAPI.HasComponent<PathRequestTag>(entity))
                {
                    ecb.AddComponent<PathRequestTag>(entity);
                }

                if (SystemAPI.HasComponent<PathFailedTag>(entity))
                {
                    ecb.RemoveComponent<PathFailedTag>(entity);
                }

                // PathReadyTag はここでは消さない。
                // 新しい path が解けるまでは古い path を使い続ける。
                unitPathState.ValueRW.WaitingForPath = 1;

                unitRepathState.ValueRW.LastRepathTime = elapsedTime;
                unitRepathState.ValueRW.LastRequestedGoal = assignedSlot.ValueRO.WorldPosition;

                unitStuckState.ValueRW.AccumulatedTime = 0.0f;
                unitStuckState.ValueRW.IsStuck = 0;

                ecb.RemoveComponent<NeedsUnitRepathTag>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
