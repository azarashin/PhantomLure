using PhantomLure.ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ApplyMainForceMoveCommandSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainForceMoveCommand>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityManager entityManager = state.EntityManager;
            EntityQuery commandQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MainForceMoveCommand>());

            using NativeArray<MainForceMoveCommand> commands = commandQuery.ToComponentDataArray<MainForceMoveCommand>(Allocator.Temp);

            if (commands.Length == 0)
            {
                return;
            }

            MainForceMoveCommand latestCommand = commands[commands.Length - 1];

            foreach ((
                RefRW<MoveTarget> moveTarget,
                RefRW<MoveState> moveState)
                in SystemAPI.Query<
                    RefRW<MoveTarget>,
                    RefRW<MoveState>>().WithAll<MainForceTag>())
            {
                moveTarget.ValueRW.Position = latestCommand.Destination;
                moveTarget.ValueRW.StoppingDistance = latestCommand.StoppingDistance;
                moveState.ValueRW.IsMoving = true;
            }

            entityManager.DestroyEntity(commandQuery);
        }
    }
}
