using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Scenes.CullingStressTest.Scripts
{
    public partial struct SpawningSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnerSettings>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var spawnerSettings = SystemAPI.GetSingleton<SpawnerSettings>();

            var newEntities = new NativeArray<Entity>(spawnerSettings.Count, Allocator.Temp);
            state.EntityManager.Instantiate(spawnerSettings.PrefabToSpawn, newEntities);

            var random = Random.CreateFromIndex(0);
            
            foreach (var newEntity in newEntities)
            {
                float3 position = new float3(random.NextFloat(-spawnerSettings.SpawningAreaExtents.x, spawnerSettings.SpawningAreaExtents.x), 0f, random.NextFloat(-spawnerSettings.SpawningAreaExtents.y, spawnerSettings.SpawningAreaExtents.y));
                state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));
                state.EntityManager.AddSharedComponent(newEntity, new MapSection{ Section = new int2((int) (position.x / spawnerSettings.MapSectionsSize), (int) (position.z / spawnerSettings.MapSectionsSize)) });
            }

            state.Enabled = false;
        }
    }

    public struct MapSection : ISharedComponentData
    {
        public int2 Section;
    }
}