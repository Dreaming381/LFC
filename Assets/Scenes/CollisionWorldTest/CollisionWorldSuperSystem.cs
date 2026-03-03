using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Scenes.UniqueMeshTests.UniqueMeshTests;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Collider = Latios.Psyshock.Collider;
using Physics = Latios.Psyshock.Physics;

namespace CollisionWorldTest
{
    public partial class CollisionWorldSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<BuildCollisionWorldSystem>();
            GetOrCreateAndAddUnmanagedSystem<FindCollisionPairsSystem>();
            GetOrCreateAndAddUnmanagedSystem<SpawnerSystem>();
        }
    }
    
    
    public struct BodyOne : IComponentData { }
    public struct BodyTwo : IComponentData { }
    public struct Raycaster : IComponentData { }

    public struct Spawner : IComponentData
    {
        public Entity Prefab;
        public bool Spawned;
    }
    
    public partial struct CollisionWorldHolder : ICollectionComponent
    {
        public CollisionWorld World;

        public JobHandle TryDispose(JobHandle inputDeps) => World.IsCreated ? World.Dispose(inputDeps) : inputDeps;
    }
    
    [BurstCompile]
    public partial struct BuildCollisionWorldSystem : ISystem, ISystemNewScene
    {
        LatiosWorldUnmanaged latiosWorld;

        EntityQuery query;
        BuildCollisionWorldTypeHandles handles;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
            handles = new BuildCollisionWorldTypeHandles(ref state);
            query = state.Fluent()
                .WithAnyEnabled<BodyOne, BodyTwo>(true)
                .PatchQueryForBuildingCollisionWorld()
                .Build();
        }

        [BurstCompile]
        public void OnNewScene(ref SystemState state)
        {
            latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<CollisionWorldHolder>(default);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            handles.Update(ref state);
            state.Dependency = Physics.BuildCollisionWorld(query, in handles)
                // .WithSettings(CollisionSettings.Default)
                .ScheduleParallel(out var collisionWorld, state.WorldUpdateAllocator, state.Dependency);
            latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new CollisionWorldHolder { World = collisionWorld });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            latiosWorld.sceneBlackboardEntity.RemoveCollectionComponentAndDispose<CollisionWorldHolder>();
        }
    }
    
    [BurstCompile]
    public partial struct SpawnerSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SpawnJob()
            {
                Icb = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<WorldTransform>(),
            }.Schedule();
        }

        partial struct SpawnJob : IJobEntity
        {
            public InstantiateCommandBuffer<WorldTransform> Icb;
            
            void Execute([ChunkIndexInQuery] int chunkIndexInQuery, ref Spawner spawner)
            {
                if (spawner.Spawned) return;
                spawner.Spawned = true;

                for (int i = -1; i < 2; i++)
                for (int j = -1; j < 2; j++)
                {
                    Icb.Add(spawner.Prefab, new WorldTransform
                    {
                        worldTransform = new TransformQvvs(new float3(i *100, 0,  j *100), quaternion.identity),
                    });
                }
                Debug.Log("spawner.Spawned");
            }
        }
    }

    [BurstCompile]
    public partial struct FindCollisionPairsSystem : ISystem, ISystemShouldUpdate
    {
        LatiosWorldUnmanaged latiosWorld;
        EntityQueryMask bodiesOneQueryMask;
        EntityQueryMask bodiesTwoQueryMask;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
            bodiesOneQueryMask = state.Fluent().With<BodyOne>().Build().GetEntityQueryMask();
            bodiesTwoQueryMask = state.Fluent().With<BodyTwo>().Build().GetEntityQueryMask();
        }

        [BurstCompile]
        public bool ShouldUpdateSystem(ref SystemState state) => latiosWorld.sceneBlackboardEntity.HasCollectionComponent<CollisionWorldHolder>();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var collisionWorld = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<CollisionWorldHolder>(true).World;

            state.Dependency = Physics.FindPairs(in collisionWorld, in bodiesOneQueryMask, in bodiesTwoQueryMask, new OneVsTwoBodiesProcessor
            {
            }).ScheduleSingle(state.Dependency);

            // state.Dependency = PhysicsDebug.DrawFindPairs(collisionWorld.collisionLayer).ScheduleParallel(state.Dependency);

            state.Dependency = Physics.FindPairs(in collisionWorld, in bodiesOneQueryMask, new OneVsOneBodiesProcessor
            {
            }).ScheduleSingle(state.Dependency);
            
            // state.Dependency = new RaycastJob()
            // {
            //     World = collisionWorld,
            //     QueryMask = bodiesTwoQueryMask
            // }.Schedule(state.Dependency);
            
            state.Dependency = new DistanceBetweenJob()
            {
                World = collisionWorld,
                QueryMask = bodiesTwoQueryMask
            }.Schedule(state.Dependency);
        }

        struct OneVsTwoBodiesProcessor : IFindPairsProcessor
        {
            public void Execute(in FindPairsResult result)
            {
                PhysicsDebug.DrawAabb(result.aabbA, Color.red);
                PhysicsDebug.DrawAabb(result.aabbB, Color.red);
            }
        }

        [BurstCompile]
        struct OneVsOneBodiesProcessor : IFindPairsProcessor
        {
            [BurstCompile]
            public void Execute(in FindPairsResult result)
            {
                PhysicsDebug.DrawAabb(result.aabbA, Color.green);
                PhysicsDebug.DrawAabb(result.aabbB, Color.green);
            }
        }
        
        [BurstCompile]
        partial struct RaycastJob : IJobEntity
        {
            [ReadOnly] public CollisionWorld World;
            [ReadOnly] public EntityQueryMask QueryMask;
            
            CollisionWorld.Mask mask;
            void Execute(in WorldTransform transform, in Raycaster raycaster)
            {
                if (!mask.isCreated) mask = World.CreateMask(QueryMask);
                
                var start = transform.position;
                var end = transform.position + transform.forwardDirection * 20;
                Debug.DrawLine(start, end, Color.red);

                if (!Physics.RaycastAny(start, end, in World, in mask, out var result, out var info))
                {
                    var enumerator = Physics.FindObjects(Physics.AabbFrom(start, end), World, mask);
                    foreach (var r in enumerator)
                    {
                        PhysicsDebug.DrawAabb(r.aabb, Color.red);
                        Debug.Log($"RayCast fails. FindObjects: Aabb from ray {start}, {end}; bodyIndex: {r.bodyIndex}, entity {r.entity.entity.Index}");
                    }
                    return;
                }
                PhysicsDebug.DrawAabb(info.aabb, Color.green);
                PhysicsDebug.DrawCollider(info.collider, info.transform, Color.chocolate);
                Debug.Log($"hit entity {info.entity.Index}, bodyIndex {info.bodyIndex}, aabb from {info.aabb.min} to {info.aabb.max}");
            }
        }
        
        
        [BurstCompile]
        partial struct DistanceBetweenJob : IJobEntity
        {
            [ReadOnly] public CollisionWorld World;
            [ReadOnly] public EntityQueryMask QueryMask;
            
            CollisionWorld.Mask mask;
            void Execute(in Collider collider, in WorldTransform transform, in Raycaster raycaster)
            {
                if (!mask.isCreated) mask = World.CreateMask(QueryMask);
                
                var found = Physics.DistanceBetween(collider, in transform.worldTransform, in World, mask, 0f, out var result, out var info);
                PhysicsDebug.DrawCollider(collider, transform.worldTransform, found ? Color.chartreuse : Color.red);
            }
        }
    }
}