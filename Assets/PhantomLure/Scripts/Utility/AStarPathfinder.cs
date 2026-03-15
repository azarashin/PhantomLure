using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    public static class AStarPathfinder
    {
        private static readonly int2[] Neighbor4 = new int2[]
        {
            new int2( 1,  0),
            new int2(-1,  0),
            new int2( 0,  1),
            new int2( 0, -1),
        };

        public static bool TryFindPath(
            in GridConfig grid,
            DynamicBuffer<GridCell> cells,
            float3 startWorld,
            float3 goalWorld,
            ref NativeList<float3> outPath,
            Allocator allocator = Allocator.Temp)
        {
            outPath.Clear();

            int2 startCell = GridUtility.ToCell(grid, startWorld);
            int2 goalCell = GridUtility.ToCell(grid, goalWorld);

            if (!GridUtility.IsInBounds(grid, startCell))
            {
                return false;
            }

            if (!GridUtility.IsInBounds(grid, goalCell))
            {
                return false;
            }

            if (!GridUtility.TryFindNearestWalkableCell(grid, cells, startCell, 4, out startCell))
            {
                return false;
            }

            if (!GridUtility.TryFindNearestWalkableCell(grid, cells, goalCell, 4, out goalCell))
            {
                return false;
            }

            int startIndex = GridUtility.ToIndex(grid, startCell);
            int goalIndex = GridUtility.ToIndex(grid, goalCell);

            int total = grid.Width * grid.Height;

            NativeArray<AStarNode> nodes = new NativeArray<AStarNode>(total, allocator);
            NativeList<int> openList = new NativeList<int>(allocator);

            for (int i = 0; i < total; i++)
            {
                nodes[i] = new AStarNode
                {
                    ParentIndex = -1,
                    G = float.MaxValue,
                    H = 0.0f,
                    Open = 0,
                    Closed = 0
                };
            }

            nodes[startIndex] = new AStarNode
            {
                ParentIndex = -1,
                G = 0.0f,
                H = GridUtility.Heuristic(startCell, goalCell),
                Open = 1,
                Closed = 0
            };

            openList.Add(startIndex);

            bool found = false;

            while (openList.Length > 0)
            {
                int currentOpenListIndex = FindLowestF(openList, nodes);
                int currentIndex = openList[currentOpenListIndex];

                RemoveAtSwapBack(ref openList, currentOpenListIndex);

                AStarNode currentNode = nodes[currentIndex];
                currentNode.Open = 0;
                currentNode.Closed = 1;
                nodes[currentIndex] = currentNode;

                if (currentIndex == goalIndex)
                {
                    found = true;
                    break;
                }

                int2 currentCell = new int2(currentIndex % grid.Width, currentIndex / grid.Width);

                for (int i = 0; i < Neighbor4.Length; i++)
                {
                    int2 nextCell = currentCell + Neighbor4[i];

                    if (!GridUtility.IsWalkable(grid, cells, nextCell))
                    {
                        continue;
                    }

                    int nextIndex = GridUtility.ToIndex(grid, nextCell);
                    AStarNode nextNode = nodes[nextIndex];

                    if (nextNode.Closed != 0)
                    {
                        continue;
                    }

                    float moveCost = cells[nextIndex].Cost <= 0.0f ? 1.0f : cells[nextIndex].Cost;
                    float tentativeG = currentNode.G + moveCost;

                    if (nextNode.Open == 0 || tentativeG < nextNode.G)
                    {
                        nextNode.ParentIndex = currentIndex;
                        nextNode.G = tentativeG;
                        nextNode.H = GridUtility.Heuristic(nextCell, goalCell);

                        if (nextNode.Open == 0)
                        {
                            nextNode.Open = 1;
                            openList.Add(nextIndex);
                        }

                        nodes[nextIndex] = nextNode;
                    }
                }
            }

            if (!found)
            {
                nodes.Dispose();
                openList.Dispose();
                return false;
            }

            ReconstructSimplifiedPath(grid, nodes, goalIndex, ref outPath);

            nodes.Dispose();
            openList.Dispose();

            return outPath.Length > 0;
        }

        private static int FindLowestF(NativeList<int> openList, NativeArray<AStarNode> nodes)
        {
            int bestListIndex = 0;
            float bestF = nodes[openList[0]].F;
            float bestH = nodes[openList[0]].H;

            for (int i = 1; i < openList.Length; i++)
            {
                AStarNode node = nodes[openList[i]];
                float f = node.F;

                if (f < bestF || (math.abs(f - bestF) < 0.0001f && node.H < bestH))
                {
                    bestF = f;
                    bestH = node.H;
                    bestListIndex = i;
                }
            }

            return bestListIndex;
        }

        private static void RemoveAtSwapBack(ref NativeList<int> list, int index)
        {
            int last = list.Length - 1;
            list[index] = list[last];
            list.RemoveAt(last);
        }

        private static void ReconstructSimplifiedPath(
            in GridConfig grid,
            NativeArray<AStarNode> nodes,
            int goalIndex,
            ref NativeList<float3> outPath)
        {
            NativeList<int2> reverseCells = new NativeList<int2>(Allocator.Temp);

            int current = goalIndex;

            while (current >= 0)
            {
                int2 cell = new int2(current % grid.Width, current / grid.Width);
                reverseCells.Add(cell);
                current = nodes[current].ParentIndex;
            }

            NativeList<int2> orderedCells = new NativeList<int2>(Allocator.Temp);

            for (int i = reverseCells.Length - 1; i >= 0; i--)
            {
                orderedCells.Add(reverseCells[i]);
            }

            if (orderedCells.Length > 0)
            {
                outPath.Add(GridUtility.ToWorldCenter(grid, orderedCells[0]));

                for (int i = 1; i < orderedCells.Length - 1; i++)
                {
                    int2 prev = orderedCells[i - 1];
                    int2 currentCell = orderedCells[i];
                    int2 next = orderedCells[i + 1];

                    int2 dirA = currentCell - prev;
                    int2 dirB = next - currentCell;

                    if (!dirA.Equals(dirB))
                    {
                        outPath.Add(GridUtility.ToWorldCenter(grid, currentCell));
                    }
                }

                if (orderedCells.Length >= 2)
                {
                    outPath.Add(GridUtility.ToWorldCenter(grid, orderedCells[orderedCells.Length - 1]));
                }
            }

            reverseCells.Dispose();
            orderedCells.Dispose();
        }
    }
}
