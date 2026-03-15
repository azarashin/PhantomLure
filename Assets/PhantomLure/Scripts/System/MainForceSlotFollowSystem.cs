using PhantomLure.ECS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FormationAnchorMoveSystem))]
    public partial struct MainForceSlotFollowSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainForceFormationAnchor>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            bool hasGrid = SystemAPI.HasSingleton<GridConfig>();

            GridConfig grid = default;
            DynamicBuffer<GridCell> gridCells = default;

            if (hasGrid)
            {
                Entity gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
                grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
                gridCells = SystemAPI.GetBuffer<GridCell>(gridEntity);
            }

            foreach ((
                RefRW<LocalTransform> localTransform,
                RefRO<MoveSpeed> moveSpeed,
                RefRO<FormationIndex> formationIndex,
                RefRO<FormationMember> formationMember)
                in SystemAPI.Query<
                    RefRW<LocalTransform>,
                    RefRO<MoveSpeed>,
                    RefRO<FormationIndex>,
                    RefRO<FormationMember>>().WithAll<MainForceTag>())
            {
                if (!SystemAPI.Exists(formationMember.ValueRO.AnchorEntity))
                {
                    continue;
                }

                RefRO<MainForceFormationAnchor> anchor =
                    SystemAPI.GetComponentRO<MainForceFormationAnchor>(formationMember.ValueRO.AnchorEntity);

                RefRO<MainForceFormationSettings> settings =
                    SystemAPI.GetComponentRO<MainForceFormationSettings>(formationMember.ValueRO.AnchorEntity);

                RefRO<MainForceAvoidanceSettings> avoidanceSettings =
                    SystemAPI.GetComponentRO<MainForceAvoidanceSettings>(formationMember.ValueRO.AnchorEntity);

                float3 anchorForward = anchor.ValueRO.Forward;
                anchorForward.y = 0.0f;

                if (math.lengthsq(anchorForward) < 0.0001f)
                {
                    anchorForward = new float3(0.0f, 0.0f, 1.0f);
                }
                else
                {
                    anchorForward = math.normalize(anchorForward);
                }

                float3 anchorRight = math.normalize(math.cross(new float3(0.0f, 1.0f, 0.0f), anchorForward));

                int columnCount = math.max(1, settings.ValueRO.ColumnCount);
                int index = math.max(0, formationIndex.ValueRO.Value);
                int column = index % columnCount;
                int row = index / columnCount;
                float centeredColumn = column - ((columnCount - 1) * 0.5f);

                float3 idealSlotOffset =
                    (anchorRight * (centeredColumn * settings.ValueRO.SpacingSide)) -
                    (anchorForward * (row * settings.ValueRO.SpacingBack));

                float3 idealSlotPosition = anchor.ValueRO.Position + idealSlotOffset;
                float3 currentPosition = localTransform.ValueRO.Position;

                float3 resolvedSlotPosition = idealSlotPosition;

                if (hasGrid)
                {
                    resolvedSlotPosition = FindReachableSlotPosition(
                        grid,
                        gridCells,
                        currentPosition,
                        idealSlotPosition,
                        anchorForward,
                        anchorRight,
                        settings.ValueRO.SpacingSide,
                        settings.ValueRO.SpacingBack);
                }

                float3 toSlot = resolvedSlotPosition - currentPosition;
                toSlot.y = 0.0f;

                float distanceToSlot = math.length(toSlot);

                if (distanceToSlot <= 0.02f)
                {
                    continue;
                }

                float3 desiredDirection = toSlot / math.max(distanceToSlot, 0.0001f);

                float forwardOffset = math.dot(currentPosition - resolvedSlotPosition, anchorForward);
                float speedMultiplier = 1.0f;

                if (forwardOffset > 0.0f)
                {
                    speedMultiplier *= 0.75f;
                }
                else
                {
                    float catchUpT = math.saturate(
                        distanceToSlot / math.max(0.01f, settings.ValueRO.SlotCatchUpDistance));
                    float catchUpMultiplier = math.lerp(
                        1.0f,
                        settings.ValueRO.MaxCatchUpMultiplier,
                        catchUpT);
                    speedMultiplier *= catchUpMultiplier;
                }

                float slowDownT = math.saturate(distanceToSlot / settings.ValueRO.SlowDownDistance);
                float slowDownMultiplier = math.lerp(0.15f, 1.0f, slowDownT);
                speedMultiplier *= slowDownMultiplier;

                float step = moveSpeed.ValueRO.Value * speedMultiplier * deltaTime;
                float3 finalDirection = desiredDirection;

                if (hasGrid)
                {
                    finalDirection = ChooseBestMoveDirection(
                        grid,
                        gridCells,
                        currentPosition,
                        resolvedSlotPosition,
                        desiredDirection,
                        anchorForward,
                        step);
                }

                float3 nextPosition = currentPosition + (finalDirection * step);

                if (hasGrid)
                {
                    int2 nextCell = GridUtility.ToCell(grid, nextPosition);

                    if (!GridUtility.IsWalkable(grid, gridCells, nextCell))
                    {
                        nextPosition = currentPosition;
                    }
                }

                localTransform.ValueRW.Position = nextPosition;

                float3 flatVelocity = finalDirection;
                flatVelocity.y = 0.0f;

                if (math.lengthsq(flatVelocity) > 0.0001f)
                {
                    localTransform.ValueRW.Rotation =
                        quaternion.LookRotationSafe(math.normalize(flatVelocity), math.up());
                }
            }
        }

        private static float3 FindReachableSlotPosition(
            in GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 currentPosition,
            float3 idealSlotPosition,
            float3 anchorForward,
            float3 anchorRight,
            float spacingSide,
            float spacingBack)
        {
            if (IsWalkableWorld(grid, gridCells, idealSlotPosition))
            {
                return idealSlotPosition;
            }

            float sideStep = math.max(grid.CellSize, spacingSide * 0.5f);
            float backStep = math.max(grid.CellSize, spacingBack * 0.5f);

            float3 bestPosition = currentPosition;
            float bestScore = float.MaxValue;

            for (int back = 0; back <= 3; back++)
            {
                for (int side = -3; side <= 3; side++)
                {
                    float3 candidate =
                        idealSlotPosition
                        + (anchorRight * (side * sideStep))
                        - (anchorForward * (back * backStep));

                    if (!IsWalkableWorld(grid, gridCells, candidate))
                    {
                        continue;
                    }

                    float score =
                        math.distance(candidate, idealSlotPosition) * 2.0f
                        + math.distance(candidate, currentPosition);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPosition = candidate;
                    }
                }
            }

            return bestPosition;
        }

        private static float3 ChooseBestMoveDirection(
            in GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 currentPosition,
            float3 targetPosition,
            float3 desiredDirection,
            float3 anchorForward,
            float step)
        {
            float3 up = new float3(0.0f, 1.0f, 0.0f);

            float3 dir0 = math.normalize(desiredDirection);
            float3 dirL1 = RotateOnPlane(dir0, up, math.radians(25.0f));
            float3 dirR1 = RotateOnPlane(dir0, up, math.radians(-25.0f));
            float3 dirL2 = RotateOnPlane(dir0, up, math.radians(50.0f));
            float3 dirR2 = RotateOnPlane(dir0, up, math.radians(-50.0f));
            float3 dirBackL = math.normalize((-anchorForward * 0.35f) + math.cross(up, dir0));
            float3 dirBackR = math.normalize((-anchorForward * 0.35f) - math.cross(up, dir0));

            float3 bestDirection = float3.zero;
            float bestScore = float.MaxValue;

            EvaluateCandidate(grid, gridCells, currentPosition, targetPosition, dir0, step, ref bestDirection, ref bestScore);
            EvaluateCandidate(grid, gridCells, currentPosition, targetPosition, dirL1, step, ref bestDirection, ref bestScore);
            EvaluateCandidate(grid, gridCells, currentPosition, targetPosition, dirR1, step, ref bestDirection, ref bestScore);
            EvaluateCandidate(grid, gridCells, currentPosition, targetPosition, dirL2, step, ref bestDirection, ref bestScore);
            EvaluateCandidate(grid, gridCells, currentPosition, targetPosition, dirR2, step, ref bestDirection, ref bestScore);
            EvaluateCandidate(grid, gridCells, currentPosition, targetPosition, dirBackL, step, ref bestDirection, ref bestScore);
            EvaluateCandidate(grid, gridCells, currentPosition, targetPosition, dirBackR, step, ref bestDirection, ref bestScore);

            if (bestScore == float.MaxValue)
            {
                return float3.zero;
            }

            return bestDirection;
        }

        private static void EvaluateCandidate(
            in GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 currentPosition,
            float3 targetPosition,
            float3 direction,
            float step,
            ref float3 bestDirection,
            ref float bestScore)
        {
            if (math.lengthsq(direction) < 0.0001f)
            {
                return;
            }

            float3 normalizedDirection = math.normalize(direction);
            float3 candidatePosition = currentPosition + (normalizedDirection * step);

            if (!IsWalkableWorld(grid, gridCells, candidatePosition))
            {
                return;
            }

            float score = math.distance(candidatePosition, targetPosition);

            if (score < bestScore)
            {
                bestScore = score;
                bestDirection = normalizedDirection;
            }
        }

        private static float3 RotateOnPlane(float3 direction, float3 axis, float radians)
        {
            quaternion rotation = quaternion.AxisAngle(axis, radians);
            float3 rotated = math.rotate(rotation, direction);
            rotated.y = 0.0f;

            if (math.lengthsq(rotated) < 0.0001f)
            {
                return direction;
            }

            return math.normalize(rotated);
        }

        private static bool IsWalkableWorld(
            in GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 position)
        {
            int2 cell = GridUtility.ToCell(grid, position);
            return GridUtility.IsWalkable(grid, gridCells, cell);
        }
    }
}
