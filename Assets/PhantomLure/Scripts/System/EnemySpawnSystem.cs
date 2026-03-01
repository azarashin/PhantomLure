using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PhantomLure.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EnemySpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemySpawnConfig>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var configEntity = SystemAPI.GetSingletonEntity<EnemySpawnConfig>();
            var config = SystemAPI.GetSingleton<EnemySpawnConfig>();

            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            int total = math.max(0, config.TotalCount);
            int squadCount = math.max(1, config.SquadCount);

            var rng = new Unity.Mathematics.Random(config.RandomSeed);

            switch (config.Pattern)
            {
                case EnemySpawnPattern.Circle:
                    {
                        SpawnCircle(ref ecb, ref rng, config, total, squadCount);
                        break;
                    }
                case EnemySpawnPattern.FrontLine:
                    {
                        SpawnFrontLine(ref ecb, ref rng, config, total, squadCount);
                        break;
                    }
                case EnemySpawnPattern.RandomRect:
                default:
                    {
                        SpawnRandomRect(ref ecb, ref rng, config, total, squadCount);
                        break;
                    }
            }

            // 1回だけ実行するため設定エンティティを破棄（ECB）
            ecb.DestroyEntity(configEntity);
        }

        private static void SpawnCircle(ref EntityCommandBuffer ecb, ref Unity.Mathematics.Random rng, in EnemySpawnConfig config, int total, int squadCount)
        {
            float baseRadius = math.max(0.01f, config.CircleRadius);
            float radiusStep = config.CircleSquadRadiusStep;

            // squadごとに半径を変えてリングを分ける（分断が分かりやすい）
            for (int i = 0; i < total; i++)
            {
                int squadId = i % squadCount;

                // squad内のインデックス
                int indexInSquad = i / squadCount;

                // squadごとの概算人数（均等割り）
                int countInSquad = (total + squadCount - 1) / squadCount;

                float t = 0f;
                if (countInSquad > 0)
                {
                    t = (float)indexInSquad / (float)countInSquad;
                }

                float angle = t * math.PI * 2f;

                // 角度に少しランダムノイズ（完全な円周だと重なりやすい）
                float angleJitter = rng.NextFloat(-0.02f, 0.02f);
                angle += angleJitter;

                float radius = baseRadius + radiusStep * squadId;
                // 半径にも微小ノイズ
                radius += rng.NextFloat(-0.2f, 0.2f);

                float3 pos = config.Center + new float3(math.cos(angle) * radius, 0f, math.sin(angle) * radius);

                Entity enemy = ecb.Instantiate(config.EnemyPrefab);

                ecb.SetComponent(enemy, LocalTransform.FromPosition(pos));

                // SquadId を上書き（Prefab側にあってもOK）
                ecb.AddComponent(enemy, new SquadMember
                {
                    SquadId = squadId,
                    IsDetached = 0
                });
            }
        }

        private static void SpawnFrontLine(ref EntityCommandBuffer ecb, ref Unity.Mathematics.Random rng, in EnemySpawnConfig config, int total, int squadCount)
        {
            float length = math.max(0.01f, config.FrontLineLength);
            float spacing = math.max(0.01f, config.FrontLineSpacing);

            float zBase = config.Center.z + config.FrontLineZOffset;
            float zStep = config.FrontLineSquadZStep;

            // 1 squad = 1列（Z方向にずらす）にし、X方向に並べる
            for (int i = 0; i < total; i++)
            {
                int squadId = i % squadCount;
                int indexInSquad = i / squadCount;

                // X方向に等間隔配置。line長を超えたら折り返す（2列目）でも良いが、まずははみ出してもOK版
                float x = config.Center.x - length * 0.5f + (indexInSquad * spacing);

                // 少し揺らして密着を避ける
                float xJ = rng.NextFloat(-0.15f, 0.15f);
                float zJ = rng.NextFloat(-0.15f, 0.15f);

                float z = zBase + zStep * squadId;

                float3 pos = new float3(x + xJ, 0f, z + zJ);

                Entity enemy = ecb.Instantiate(config.EnemyPrefab);

                ecb.SetComponent(enemy, LocalTransform.FromPosition(pos));

                ecb.AddComponent(enemy, new SquadMember
                {
                    SquadId = squadId,
                    IsDetached = 0
                });
            }
        }

        private static void SpawnRandomRect(ref EntityCommandBuffer ecb, ref Unity.Mathematics.Random rng, in EnemySpawnConfig config, int total, int squadCount)
        {
            float2 size = config.RandomRectSize;
            size.x = math.max(0.01f, size.x);
            size.y = math.max(0.01f, size.y);

            float minJitter = config.RandomMinDistanceJitter;

            // squadごとに矩形領域を分割して置く（混ざりすぎない）
            // 例：Z方向に帯で分割
            float bandHeight = size.y / squadCount;

            for (int i = 0; i < total; i++)
            {
                int squadId = i % squadCount;

                float xMin = config.Center.x - size.x * 0.5f;
                float xMax = config.Center.x + size.x * 0.5f;

                float zMinAll = config.Center.z - size.y * 0.5f;
                float zMin = zMinAll + bandHeight * squadId;
                float zMax = zMinAll + bandHeight * (squadId + 1);

                float x = rng.NextFloat(xMin, xMax);
                float z = rng.NextFloat(zMin, zMax);

                if (minJitter > 0f)
                {
                    x += rng.NextFloat(-minJitter, minJitter);
                    z += rng.NextFloat(-minJitter, minJitter);
                }

                float3 pos = new float3(x, 0f, z);

                Entity enemy = ecb.Instantiate(config.EnemyPrefab);

                ecb.SetComponent(enemy, LocalTransform.FromPosition(pos));

                ecb.AddComponent(enemy, new SquadMember
                {
                    SquadId = squadId,
                    IsDetached = 0
                });
            }
        }
    }
}
