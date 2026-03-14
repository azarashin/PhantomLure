using Unity.Burst;
using Unity.Entities;

namespace PhantomLure.ECS
{
    /// <summary>
    /// ユーザの入力に対する処理を実行する
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerInputSystem))]
    public partial struct PlayerCommandConsumeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var command in
                     SystemAPI.Query<RefRW<PlayerCommandData>>())
            {
                // command.ValueRO.Move を読む
                // command.ValueRO.ClickRequested / ClickWorldPosition を読む

                // クリックを1回処理したいなら最後に落とす
                command.ValueRW.ClickRequested = 0;
            }
        }
    }
}