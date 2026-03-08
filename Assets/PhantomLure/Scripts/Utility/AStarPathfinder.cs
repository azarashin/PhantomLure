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

            if (!GridUtility.IsInBounds(grid, startCell) || !GridUtility.IsInBounds(grid, goalCell))
                return false;

            int startIndex = GridUtility.ToIndex(grid, startCell);
            int goalIndex = GridUtility.ToIndex(grid, goalCell);

            if (cells[startIndex].Walkable == 0 || cells[goalIndex].Walkable == 0)
                return false;

            int total = grid.Width * grid.Height;

            var nodes = new NativeArray<AStarNode>(total, allocator);
            var openList = new NativeList<int>(allocator);

            for (int i = 0; i < total; i++)
            {
                nodes[i] = new AStarNode
                {
                    ParentIndex = -1,
                    G = float.MaxValue,
                    H = 0f,
                    Open = 0,
                    Closed = 0
                };
            }

            nodes[startIndex] = new AStarNode
            {
                ParentIndex = -1,
                G = 0f,
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

                var currentNode = nodes[currentIndex];
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
                    if (!GridUtility.IsInBounds(grid, nextCell))
                        continue;

                    int nextIndex = GridUtility.ToIndex(grid, nextCell);
                    if (cells[nextIndex].Walkable == 0)
                        continue;

                    var nextNode = nodes[nextIndex];
                    if (nextNode.Closed != 0)
                        continue;

                    float moveCost = cells[nextIndex].Cost <= 0f ? 1f : cells[nextIndex].Cost;
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

            ReconstructPath(grid, nodes, goalIndex, ref outPath);

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
                var node = nodes[openList[i]];
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

        private static void ReconstructPath(
            in GridConfig grid,
            NativeArray<AStarNode> nodes,
            int goalIndex,
            ref NativeList<float3> outPath)
        {
            var reverse = new NativeList<float3>(Allocator.Temp);

            int current = goalIndex;
            while (current >= 0)
            {
                int2 cell = new int2(current % grid.Width, current / grid.Width);
                reverse.Add(GridUtility.ToWorldCenter(grid, cell));
                current = nodes[current].ParentIndex;
            }

            for (int i = reverse.Length - 1; i >= 0; i--)
            {
                outPath.Add(reverse[i]);
            }

            reverse.Dispose();
        }
    }
}
