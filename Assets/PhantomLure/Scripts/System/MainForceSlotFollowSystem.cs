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

                float3 forward = anchor.ValueRO.Forward;
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

                int columnCount = math.max(1, settings.ValueRO.ColumnCount);
                int index = math.max(0, formationIndex.ValueRO.Value);
                int column = index % columnCount;
                int row = index / columnCount;
                float centeredColumn = column - ((columnCount - 1) * 0.5f);

                float3 slotOffset =
                    (right * (centeredColumn * settings.ValueRO.SpacingSide)) -
                    (forward * (row * settings.ValueRO.SpacingBack));

                float3 slotPosition = anchor.ValueRO.Position + slotOffset;
                float3 currentPosition = localTransform.ValueRO.Position;

                float3 toSlot = slotPosition - currentPosition;
                toSlot.y = 0.0f;

                float distanceToSlot = math.length(toSlot);

                if (distanceToSlot <= 0.02f)
                {
                    continue;
                }

                float3 desiredDirection = toSlot / math.max(distanceToSlot, 0.0001f);

                float forwardOffset = math.dot(currentPosition - slotPosition, forward);
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

                float3 finalDirection = desiredDirection;

                if (hasGrid)
                {
                    float3 repulsion = CalculateObstacleRepulsion(
                        grid,
                        gridCells,
                        currentPosition,
                        avoidanceSettings.ValueRO.ObstacleRepulsionRadius);

                    float3 detour = float3.zero;

                    if (IsBlockedAhead(
                        grid,
                        gridCells,
                        currentPosition,
                        desiredDirection,
                        avoidanceSettings.ValueRO.BlockProbeDistance))
                    {
                        float3 left = math.normalize(math.cross(new float3(0.0f, 1.0f, 0.0f), desiredDirection));
                        float3 rightDir = -left;

                        bool leftBlocked = IsBlockedAhead(
                            grid,
                            gridCells,
                            currentPosition,
                            left,
                            avoidanceSettings.ValueRO.LateralProbeDistance);

                        bool rightBlocked = IsBlockedAhead(
                            grid,
                            gridCells,
                            currentPosition,
                            rightDir,
                            avoidanceSettings.ValueRO.LateralProbeDistance);

                        if (!leftBlocked && rightBlocked)
                        {
                            detour = left;
                        }
                        else if (leftBlocked && !rightBlocked)
                        {
                            detour = rightDir;
                        }
                        else
                        {
                            float leftScore = ClearanceScore(
                                grid,
                                gridCells,
                                currentPosition,
                                left,
                                avoidanceSettings.ValueRO.LateralProbeDistance);

                            float rightScore = ClearanceScore(
                                grid,
                                gridCells,
                                currentPosition,
                                rightDir,
                                avoidanceSettings.ValueRO.LateralProbeDistance);

                            detour = leftScore >= rightScore ? left : rightDir;
                        }
                    }

                    float3 blended =
                        desiredDirection
                        + (repulsion * avoidanceSettings.ValueRO.ObstacleRepulsionWeight)
                        + (detour * avoidanceSettings.ValueRO.LateralProbeWeight);

                    blended.y = 0.0f;

                    if (math.lengthsq(blended) > 0.0001f)
                    {
                        finalDirection = math.normalize(blended);
                    }
                }

                float step = moveSpeed.ValueRO.Value * speedMultiplier * deltaTime;
                float3 nextPosition = currentPosition + (finalDirection * step);

                if (hasGrid)
                {
                    int2 nextCell = GridUtility.ToCell(grid, nextPosition);

                    if (!GridUtility.IsWalkable(grid, gridCells, nextCell))
                    {
                        float3 lateral = math.normalize(math.cross(new float3(0.0f, 1.0f, 0.0f), finalDirection));

                        float3 leftCandidate = currentPosition + (lateral * step);
                        float3 rightCandidate = currentPosition - (lateral * step);

                        int2 leftCell = GridUtility.ToCell(grid, leftCandidate);
                        int2 rightCell = GridUtility.ToCell(grid, rightCandidate);

                        bool canLeft = GridUtility.IsWalkable(grid, gridCells, leftCell);
                        bool canRight = GridUtility.IsWalkable(grid, gridCells, rightCell);

                        if (canLeft && !canRight)
                        {
                            nextPosition = leftCandidate;
                            finalDirection = lateral;
                        }
                        else if (!canLeft && canRight)
                        {
                            nextPosition = rightCandidate;
                            finalDirection = -lateral;
                        }
                        else if (canLeft && canRight)
                        {
                            float leftScore = ClearanceScore(
                                grid,
                                gridCells,
                                currentPosition,
                                lateral,
                                step);

                            float rightScore = ClearanceScore(
                                grid,
                                gridCells,
                                currentPosition,
                                -lateral,
                                step);

                            if (leftScore >= rightScore)
                            {
                                nextPosition = leftCandidate;
                                finalDirection = lateral;
                            }
                            else
                            {
                                nextPosition = rightCandidate;
                                finalDirection = -lateral;
                            }
                        }
                        else
                        {
                            nextPosition = currentPosition;
                        }
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

        private static float3 CalculateObstacleRepulsion(
            in GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 currentPosition,
            float radius)
        {
            float3 repulsion = float3.zero;

            int2 centerCell = GridUtility.ToCell(grid, currentPosition);
            int searchRadius = math.max(1, (int)math.ceil(radius / math.max(0.01f, grid.CellSize)));

            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                for (int x = -searchRadius; x <= searchRadius; x++)
                {
                    int2 cell = centerCell + new int2(x, y);

                    if (!GridUtility.IsInBounds(grid, cell))
                    {
                        continue;
                    }

                    int index = GridUtility.ToIndex(grid, cell);

                    if (gridCells[index].Walkable != 0)
                    {
                        continue;
                    }

                    float3 obstacleCenter = GridUtility.ToWorldCenter(grid, cell);
                    float3 away = currentPosition - obstacleCenter;
                    away.y = 0.0f;

                    float distance = math.length(away);

                    if (distance <= 0.0001f || distance > radius)
                    {
                        continue;
                    }

                    float t = 1.0f - math.saturate(distance / radius);
                    repulsion += math.normalize(away) * t;
                }
            }

            return repulsion;
        }

        private static bool IsBlockedAhead(
            in GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 currentPosition,
            float3 direction,
            float distance)
        {
            float3 probePosition = currentPosition + (math.normalize(direction) * distance);
            int2 probeCell = GridUtility.ToCell(grid, probePosition);
            return !GridUtility.IsWalkable(grid, gridCells, probeCell);
        }

        private static float ClearanceScore(
            in GridConfig grid,
            DynamicBuffer<GridCell> gridCells,
            float3 currentPosition,
            float3 direction,
            float distance)
        {
            float3 probePosition = currentPosition + (math.normalize(direction) * distance);
            int2 probeCell = GridUtility.ToCell(grid, probePosition);

            if (!GridUtility.IsInBounds(grid, probeCell))
            {
                return -1000.0f;
            }

            if (GridUtility.IsWalkable(grid, gridCells, probeCell))
            {
                return 1.0f;
            }

            return -1.0f;
        }
    }
}
