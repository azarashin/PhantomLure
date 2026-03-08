using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    public static class GridEditUtility
    {
        public static void SetWalkable(
            EntityManager em,
            Entity gridEntity,
            int2 cell,
            bool walkable)
        {
            var grid = em.GetComponentData<GridConfig>(gridEntity);
            if (!GridUtility.IsInBounds(grid, cell))
                return;

            var buffer = em.GetBuffer<GridCell>(gridEntity);
            int index = GridUtility.ToIndex(grid, cell);

            var c = buffer[index];
            c.Walkable = (byte)(walkable ? 1 : 0);
            buffer[index] = c;
        }

        public static void SetCost(
            EntityManager em,
            Entity gridEntity,
            int2 cell,
            float cost)
        {
            var grid = em.GetComponentData<GridConfig>(gridEntity);
            if (!GridUtility.IsInBounds(grid, cell))
                return;

            var buffer = em.GetBuffer<GridCell>(gridEntity);
            int index = GridUtility.ToIndex(grid, cell);

            var c = buffer[index];
            c.Cost = cost;
            buffer[index] = c;
        }
    }
}
