using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    public static class GridUtility
    {
        public static bool IsInBounds(in GridConfig grid, int2 cell)
        {
            return cell.x >= 0 && cell.x < grid.Width
                && cell.y >= 0 && cell.y < grid.Height;
        }

        public static int ToIndex(in GridConfig grid, int2 cell)
        {
            return cell.y * grid.Width + cell.x;
        }

        public static int2 ToCell(in GridConfig grid, float3 world)
        {
            float3 local = world - grid.Origin;

            int x = (int)math.floor(local.x / grid.CellSize);
            int y = (int)math.floor(local.z / grid.CellSize);

            return new int2(x, y);
        }

        public static float3 ToWorldCenter(in GridConfig grid, int2 cell)
        {
            return grid.Origin + new float3(
                (cell.x + 0.5f) * grid.CellSize,
                0.0f,
                (cell.y + 0.5f) * grid.CellSize);
        }

        public static float Heuristic(int2 a, int2 b)
        {
            int2 d = math.abs(a - b);
            return d.x + d.y;
        }

        public static bool IsWalkable(in GridConfig grid, DynamicBuffer<GridCell> cells, int2 cell)
        {
            if (!IsInBounds(grid, cell))
            {
                return false;
            }

            int index = ToIndex(grid, cell);
            return cells[index].Walkable != 0;
        }

        public static bool TryFindNearestWalkableCell(
            in GridConfig grid,
            DynamicBuffer<GridCell> cells,
            int2 startCell,
            int maxRadius,
            out int2 result)
        {
            if (IsWalkable(grid, cells, startCell))
            {
                result = startCell;
                return true;
            }

            for (int radius = 1; radius <= maxRadius; radius++)
            {
                int minX = startCell.x - radius;
                int maxX = startCell.x + radius;
                int minY = startCell.y - radius;
                int maxY = startCell.y + radius;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        bool isBorder =
                            x == minX
                            || x == maxX
                            || y == minY
                            || y == maxY;

                        if (!isBorder)
                        {
                            continue;
                        }

                        int2 cell = new int2(x, y);

                        if (!IsWalkable(grid, cells, cell))
                        {
                            continue;
                        }

                        result = cell;
                        return true;
                    }
                }
            }

            result = startCell;
            return false;
        }
    }
}
