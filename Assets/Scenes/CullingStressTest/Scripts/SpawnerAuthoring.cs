using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Scenes.CullingStressTest.Scripts
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject PrefabToSpawn;
        [Tooltip("How many times the prefab will be instantiated.")]
        public int Count;
        [Tooltip("Extents of the rectangle where prefabs will be instantiated.")]
        public float2 SpawningAreaExtents;
        [Tooltip("Spawned prefabs will be assigned a shared component for their map section.")]
        public int MapSectionSize;
    
        public class SpawnerAuthoringBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpawnerSettings
                {
                    PrefabToSpawn = GetEntity(authoring.PrefabToSpawn, TransformUsageFlags.None),
                    Count = authoring.Count,
                    SpawningAreaExtents = authoring.SpawningAreaExtents,
                    MapSectionsSize = authoring.MapSectionSize,
                });
            }
        }
    }

    public struct SpawnerSettings : IComponentData
    {
        public Entity PrefabToSpawn;
        public int Count;
        public float2 SpawningAreaExtents;
        public float MapSectionsSize;
    }
}