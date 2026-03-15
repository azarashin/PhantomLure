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
                .WithAll<MainForceFormationAnchor>()
                .Build();

            state.RequireForUpdate(_commandQuery);
            state.RequireForUpdate(_anchorQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            using NativeArray<MainForceMoveCommand> commands = _commandQuery.ToComponentDataArray<MainForceMoveCommand>(Allocator.Temp);

            if (commands.Length == 0)
            {
                return;
            }

            MainForceMoveCommand latestCommand = commands[commands.Length - 1];

            foreach (RefRW<MainForceFormationAnchor> anchor in
                     SystemAPI.Query<RefRW<MainForceFormationAnchor>>())
            {
                float3 toDestination = latestCommand.Destination - anchor.ValueRO.Position;
                toDestination.y = 0.0f;

                if (math.lengthsq(toDestination) > 0.0001f)
                {
                    anchor.ValueRW.Forward = math.normalize(toDestination);
                }

                anchor.ValueRW.Destination = latestCommand.Destination;
                anchor.ValueRW.IsMoving = true;
            }

            state.EntityManager.DestroyEntity(_commandQuery);
        }


        /*
        private EntityQuery _commandQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _commandQuery = SystemAPI.QueryBuilder()
                .WithAll<MainForceMoveCommand>()
                .Build();

            state.RequireForUpdate(_commandQuery);
            state.RequireForUpdate<MainForceMoveCommand>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityManager entityManager = state.EntityManager;

            using NativeArray<MainForceMoveCommand> commands = _commandQuery.ToComponentDataArray<MainForceMoveCommand>(Allocator.Temp);

            if (commands.Length == 0)
            {
                return;
            }

            MainForceMoveCommand latestCommand = commands[commands.Length - 1];

            float3 forward = latestCommand.Forward;
            forward.y = 0.0f;

            if (math.lengthsq(forward) < 0.0001f)
            {
                forward = new float3(0.0f, 0.0f, 1.0f);
            }
            else
            {
                forward = math.normalize(forward);
            }

            float3 right = math.normalize(math.cross(new float3(0.0f, 1.0f, 0.0f), forward));
            int columnCount = math.max(1, latestCommand.ColumnCount);

            foreach ((
                RefRW<MoveTarget> moveTarget,
                RefRW<MoveState> moveState,
                RefRO<FormationIndex> formationIndex)
                in SystemAPI.Query<
                    RefRW<MoveTarget>,
                    RefRW<MoveState>,
                    RefRO<FormationIndex>>().WithAll<MainForceTag>())
            {
                int index = math.max(0, formationIndex.ValueRO.Value);
                int column = index % columnCount;
                int row = index / columnCount;

                float centeredColumn = column - ((columnCount - 1) * 0.5f);

                float3 offsetX = right * (centeredColumn * latestCommand.SpacingX);
                float3 offsetZ = -forward * (row * latestCommand.SpacingZ);
                float3 targetPosition = latestCommand.Destination + offsetX + offsetZ;

                moveTarget.ValueRW.Position = targetPosition;
                moveTarget.ValueRW.StoppingDistance = latestCommand.StoppingDistance;
                moveState.ValueRW.IsMoving = true;
            }

            // _commandQuery に 現在マッチしている命令エンティティ を削除
            // (_commandQuery という EntityQueryオブジェクト自体 を消しているのではない)
            // 性能面では毎フレーム多用しすぎない方がよい
            // このクラスはユーザの入力に対する処理を1Entity で行うだけなので現状維持。
            // 多数のEntity を使う場合はECB で命令エンティティを後段削除する。
            entityManager.DestroyEntity(_commandQuery);
        }
        */
    }
}
