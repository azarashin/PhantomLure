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
                0f,
                (cell.y + 0.5f) * grid.CellSize
            );
        }

        public static float Heuristic(int2 a, int2 b)
        {
            // 4近傍用マンハッタン距離
            int2 d = math.abs(a - b);
            return d.x + d.y;
        }
    }
}
