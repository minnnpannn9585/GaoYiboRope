﻿#if (OBI_BURST && OBI_MATHEMATICS && OBI_COLLECTIONS)
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;

namespace Obi
{
    public struct MovingEntity
    {
        public int4 oldCellCoord;
        public int4 newCellCoord;
        public int entity;
    }

    public class ParticleGrid : IDisposable
    {
        public NativeMultilevelGrid<int> grid;
        public NativeQueue<BurstContact> particleContactQueue;
        public NativeQueue<FluidInteraction> fluidInteractionQueue;

        [BurstCompile]
        struct UpdateGrid : IJob
        {
            public NativeMultilevelGrid<int> grid;
            [ReadOnly] public NativeArray<BurstAabb> simplexBounds;
            public NativeArray<int4> cellCoords;

            [ReadOnly] public Oni.SolverParameters parameters;
            [ReadOnly] public int simplexCount;

            public void Execute()
            {
                grid.Clear();

                for (int i = 0; i < simplexCount; ++i)
                {
                    int level = NativeMultilevelGrid<int>.GridLevelForSize(simplexBounds[i].MaxAxisLength());
                    float cellSize = NativeMultilevelGrid<int>.CellSizeOfLevel(level);

                    // get new cell coordinate:
                    int4 newCellCoord = new int4(GridHash.Quantize(simplexBounds[i].center.xyz, cellSize), level);

                    // if the solver is 2D, project to the z = 0 cell.
                    if (parameters.mode == Oni.SolverParameters.Mode.Mode2D) newCellCoord[2] = 0;

                    cellCoords[i] = newCellCoord;

                    // add to new cell:
                    int cellIndex = grid.GetOrCreateCell(cellCoords[i]);
                    var newCell = grid.usedCells[cellIndex];
                    newCell.Add(i);
                    grid.usedCells[cellIndex] = newCell;
                }
            }
        }

        [BurstCompile]
        public struct GenerateParticleParticleContactsJob : IJobParallelFor
        {
            [ReadOnly] public NativeMultilevelGrid<int> grid;

            [DeallocateOnJobCompletion]
            [ReadOnly] public NativeArray<int> gridLevels;

            [ReadOnly] public NativeArray<float4> positions;
            [ReadOnly] public NativeArray<quaternion> orientations;
            [ReadOnly] public NativeArray<float4> restPositions;
            [ReadOnly] public NativeArray<quaternion> restOrientations;
            [ReadOnly] public NativeArray<float4> velocities;
            [ReadOnly] public NativeArray<float> invMasses;
            [ReadOnly] public NativeArray<float4> radii;
            [ReadOnly] public NativeArray<float4> normals;
            [ReadOnly] public NativeArray<float4> fluidMaterials;
            [ReadOnly] public NativeArray<int> phases;
            [ReadOnly] public NativeArray<int> filters;

            // simplex arrays:
            [ReadOnly] public NativeArray<int> simplices;
            [ReadOnly] public SimplexCounts simplexCounts;

            [ReadOnly] public NativeArray<int> particleMaterialIndices;
            [ReadOnly] public NativeArray<BurstCollisionMaterial> collisionMaterials;

            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeQueue<BurstContact>.ParallelWriter contactsQueue;

            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeQueue<FluidInteraction>.ParallelWriter fluidInteractionsQueue;

            [ReadOnly] public float dt;
            [ReadOnly] public float collisionMargin;
            [ReadOnly] public int optimizationIterations;
            [ReadOnly] public float optimizationTolerance;

            public void Execute(int i)
            {
                BurstSimplex simplexShape = new BurstSimplex()
                {
                    positions = restPositions,
                    radii = radii,
                    simplices = simplices,
                };

                // Looks for close particles in the same cell:
                IntraCellSearch(i, ref simplexShape);

                // Looks for close particles in neighboring cells, in the same level or higher levels.
                IntraLevelSearch(i, ref simplexShape);
            }

            private void IntraCellSearch(int cellIndex, ref BurstSimplex simplexShape)
            {
                int cellLength = grid.usedCells[cellIndex].Length;

                for (int p = 0; p < cellLength; ++p)
                {
                    for (int n = p + 1; n < cellLength; ++n)
                    {
                        InteractionTest(grid.usedCells[cellIndex][p], grid.usedCells[cellIndex][n], ref simplexShape);
                    }
                }
            }

            private void InterCellSearch(int cellIndex, int neighborCellIndex, ref BurstSimplex simplexShape)
            {
                int cellLength = grid.usedCells[cellIndex].Length;
                int neighborCellLength = grid.usedCells[neighborCellIndex].Length;

                for (int p = 0; p < cellLength; ++p)
                {
                    for (int n = 0; n < neighborCellLength; ++n)
                    {
                        InteractionTest(grid.usedCells[cellIndex][p], grid.usedCells[neighborCellIndex][n], ref simplexShape);
                    }
                }
            }

            private void IntraLevelSearch(int cellIndex, ref BurstSimplex simplexShape)
            {
                int4 cellCoords = grid.usedCells[cellIndex].Coords;

                // neighboring cells in the current level:
                for (int i = 0; i < 13; ++i)
                {
                    int4 neighborCellCoords = new int4(cellCoords.xyz + GridHash.cellOffsets3D[i], cellCoords.w);

                    int neighborCellIndex;
                    if (grid.TryGetCellIndex(neighborCellCoords, out neighborCellIndex))
                    {
                        InterCellSearch(cellIndex, neighborCellIndex, ref simplexShape);
                    }
                }

                // neighboring cells in levels above the current one:
                int levelIndex = gridLevels.IndexOf<int, int>(cellCoords.w);
                if (levelIndex >= 0)
                {
                    levelIndex++;
                    for (; levelIndex < gridLevels.Length; ++levelIndex)
                    {
                        int level = gridLevels[levelIndex];

                        // calculate index of parent cell in parent level:
                        int4 parentCellCoords = NativeMultilevelGrid<int>.GetParentCellCoords(cellCoords, level);

                        // search in all neighbouring cells:
                        for (int x = -1; x <= 1; ++x)
                            for (int y = -1; y <= 1; ++y)
                                for (int z = -1; z <= 1; ++z)
                                {
                                    int4 neighborCellCoords = parentCellCoords + new int4(x, y, z, 0);

                                    int neighborCellIndex;
                                    if (grid.TryGetCellIndex(neighborCellCoords, out neighborCellIndex))
                                    {
                                        InterCellSearch(cellIndex, neighborCellIndex, ref simplexShape);
                                    }
                                }
                    }
                }
            }

            private int GetSimplexGroup(int simplexStart, int simplexSize, out ObiUtils.ParticleFlags flags, out int category, out int mask, ref bool restPositionsEnabled)
            {
                flags = 0;
                int group = 0;
                category = 0;
                mask = 0;
                for (int j = 0; j < simplexSize; ++j)
                {
                    int particleIndex = simplices[simplexStart + j];
                    group = math.max(group, ObiUtils.GetGroupFromPhase(phases[particleIndex]));
                    flags |= ObiUtils.GetFlagsFromPhase(phases[particleIndex]);
                    category |= filters[particleIndex] & ObiUtils.FilterCategoryBitmask;
                    mask |= (filters[particleIndex] & ObiUtils.FilterMaskBitmask) >> 16;
                    restPositionsEnabled |= restPositions[particleIndex].w > 0.5f;
                }

                return group;
            }

            private void InteractionTest(int A, int B, ref BurstSimplex simplexShape)
            {
                // get the start index and size of each simplex:
                int simplexStartA = simplexCounts.GetSimplexStartAndSize(A, out int simplexSizeA);
                int simplexStartB = simplexCounts.GetSimplexStartAndSize(B, out int simplexSizeB);

                // immediately reject simplex pairs that share particles:
                for (int a = 0; a < simplexSizeA; ++a)
                    for (int b = 0; b < simplexSizeB; ++b)
                        if (simplices[simplexStartA + a] == simplices[simplexStartB + b])
                            return;

                // get group for each simplex:
                bool restPositionsEnabled = false;
                int groupA = GetSimplexGroup(simplexStartA, simplexSizeA, out ObiUtils.ParticleFlags flagsA, out int categoryA, out int maskA, ref restPositionsEnabled);
                int groupB = GetSimplexGroup(simplexStartB, simplexSizeB, out ObiUtils.ParticleFlags flagsB, out int categoryB, out int maskB, ref restPositionsEnabled);

                // if all particles are in the same group:
                if (groupA == groupB)
                {
                    // if none are self-colliding, reject the pair.
                    if ((flagsA & flagsB & ObiUtils.ParticleFlags.SelfCollide) == 0)
                        return;
                }
                // category-based filtering:
                else if ((maskA & categoryB) == 0 || (maskB & categoryA) == 0)
                    return;

                // if all simplices are fluid, check their smoothing radii:
                if ((flagsA & ObiUtils.ParticleFlags.Fluid) != 0 && (flagsB & ObiUtils.ParticleFlags.Fluid) != 0)
                {
                    // for fluid we only consider the first particle in each simplex.
                    int particleA = simplices[simplexStartA];
                    int particleB = simplices[simplexStartB];

                    // Calculate particle center distance:
                    float d2 = math.lengthsq(positions[particleA].xyz - positions[particleB].xyz);

                    float fluidDistance = math.max(fluidMaterials[particleA].x, fluidMaterials[particleB].x) + collisionMargin;
                    if (d2 <= fluidDistance * fluidDistance)
                    {
                        fluidInteractionsQueue.Enqueue(new FluidInteraction { particleA = particleA, particleB = particleB });
                    }
                }
                else // at least one solid particle is present:
                {
                    // swap simplices so that B is always the one-sided one.
                    if ((flagsA & ObiUtils.ParticleFlags.OneSided) != 0 && categoryA < categoryB)
                    {
                        ObiUtils.Swap(ref A, ref B);
                        ObiUtils.Swap(ref simplexStartA, ref simplexStartB);
                        ObiUtils.Swap(ref simplexSizeA, ref simplexSizeB);
                        ObiUtils.Swap(ref flagsA, ref flagsB);
                        ObiUtils.Swap(ref groupA, ref groupB);
                    }

                    float4 simplexBary = BurstMath.BarycenterForSimplexOfSize(simplexSizeA);
                    float4 simplexPoint;

                    simplexShape.simplexStart = simplexStartB;
                    simplexShape.simplexSize = simplexSizeB;
                    simplexShape.positions = restPositions;
                    simplexShape.CacheData();

                    float simplexRadiusA = 0, simplexRadiusB = 0;

                    // skip the contact if there's self-intersection at rest:
                    if (groupA == groupB && restPositionsEnabled)
                    {
                        var restPoint = BurstLocalOptimization.Optimize<BurstSimplex>(ref simplexShape, restPositions, restOrientations, radii,
                                                    simplices, simplexStartA, simplexSizeA, ref simplexBary, out simplexPoint, 4, 0);

                        for (int j = 0; j < simplexSizeA; ++j)
                            simplexRadiusA += radii[simplices[simplexStartA + j]].x * simplexBary[j];

                        for (int j = 0; j < simplexSizeB; ++j)
                            simplexRadiusB += radii[simplices[simplexStartB + j]].x * restPoint.bary[j];

                        // compare distance along contact normal with radius.
                        if (math.dot(simplexPoint - restPoint.point, restPoint.normal) < simplexRadiusA + simplexRadiusB)
                            return;
                    }

                    simplexBary = BurstMath.BarycenterForSimplexOfSize(simplexSizeA);
                    simplexShape.positions = positions;
                    simplexShape.CacheData();

                    var surfacePoint = BurstLocalOptimization.Optimize<BurstSimplex>(ref simplexShape, positions, orientations, radii,
                                        simplices, simplexStartA, simplexSizeA, ref simplexBary, out simplexPoint, optimizationIterations, optimizationTolerance);

                    simplexRadiusA = 0; simplexRadiusB = 0;
                    float4 velocityA = float4.zero, velocityB = float4.zero, normalA = float4.zero, normalB = float4.zero;
                    float invMassA = 0, invMassB = 0; 

                    for (int j = 0; j < simplexSizeA; ++j)
                    {
                        int particleIndex = simplices[simplexStartA + j];
                        simplexRadiusA += radii[particleIndex].x * simplexBary[j];
                        velocityA += velocities[particleIndex] * simplexBary[j];
                        normalA += (normals[particleIndex].w < 0 ? new float4(math.rotate(orientations[particleIndex],normals[particleIndex].xyz), normals[particleIndex].w) : normals[particleIndex]) * simplexBary[j];
                        invMassA += invMasses[particleIndex] * simplexBary[j];
                    }

                    for (int j = 0; j < simplexSizeB; ++j)
                    {
                        int particleIndex = simplices[simplexStartB + j];
                        simplexRadiusB += radii[particleIndex].x * surfacePoint.bary[j];
                        velocityB += velocities[particleIndex] * surfacePoint.bary[j];
                        normalB += (normals[particleIndex].w < 0 ? new float4(math.rotate(orientations[particleIndex], normals[particleIndex].xyz), normals[particleIndex].w) : normals[particleIndex]) * surfacePoint.bary[j];
                        invMassB += invMasses[particleIndex] * simplexBary[j];
                    }

                    // no contact between fixed simplices:
                    //if (!(invMassA > 0 || invMassB > 0)) 
                      //  return;

                    float dAB = math.dot(simplexPoint - surfacePoint.point, surfacePoint.normal);
                    float vel = math.dot(velocityA - velocityB, surfacePoint.normal);

                    // check if the projected velocity along the contact normal will get us within collision distance.
                    if (vel * dt + dAB <= simplexRadiusA + simplexRadiusB + collisionMargin)
                    {
                        // adapt collision normal for one-sided simplices:
                        if ((flagsB & ObiUtils.ParticleFlags.OneSided) != 0 && categoryA < categoryB)
                            BurstMath.OneSidedNormal(normalB, ref surfacePoint.normal); 

                        // during inter-collision, if either particle contains SDF data and they overlap:
                        if (groupA != groupB && (normalB.w < 0 || normalA.w < 0) && dAB * 1.05f <= simplexRadiusA + simplexRadiusB)
                        {
                            // as normal, pick SDF gradient belonging to least penetration distance:
                            float4 nij = normalB;
                            if (normalB.w >= 0 || (normalA.w < 0 && normalB.w < normalA.w))
                                nij = new float4(-normalA.xyz, normalA.w);

                            // for boundary particles, use one sided sphere normal:
                            if (math.abs(nij.w) <= math.max(simplexRadiusA, simplexRadiusB) * 1.5f)
                                BurstMath.OneSidedNormal(nij, ref surfacePoint.normal);
                            else
                                surfacePoint.normal = nij;
                        }

                        surfacePoint.normal.w = 0;
                        contactsQueue.Enqueue(new BurstContact
                        {
                            bodyA = A,
                            bodyB = B,
                            pointA = simplexBary,
                            pointB = surfacePoint.bary,
                            normal = surfacePoint.normal
                        }); 
                    }
                }
            }
        }

        public ParticleGrid()
        {
            this.grid = new NativeMultilevelGrid<int>(1000, Allocator.Persistent);
            this.particleContactQueue = new NativeQueue<BurstContact>(Allocator.Persistent);
            this.fluidInteractionQueue = new NativeQueue<FluidInteraction>(Allocator.Persistent);
        }

        public void Update(BurstSolverImpl solver, JobHandle inputDeps)
        {
            var updateGrid = new UpdateGrid
            {
                grid = grid,
                simplexBounds = solver.simplexBounds,
                simplexCount = solver.simplexCounts.simplexCount,
                cellCoords = solver.cellCoords,
                parameters = solver.abstraction.parameters
            };
            updateGrid.Schedule(inputDeps).Complete();
        }

        public JobHandle GenerateContacts(BurstSolverImpl solver, float deltaTime)
        {

            var generateParticleContactsJob = new GenerateParticleParticleContactsJob
            {
                grid = grid,
                gridLevels = grid.populatedLevels.GetKeyArray(Allocator.TempJob),

                positions = solver.positions,
                orientations = solver.orientations,
                restPositions = solver.restPositions,
                restOrientations = solver.restOrientations,
                velocities = solver.velocities,
                invMasses = solver.invMasses,
                radii = solver.principalRadii,
                normals = solver.normals,
                fluidMaterials = solver.fluidMaterials,
                phases = solver.phases,
                filters = solver.filters,

                simplices = solver.simplices,
                simplexCounts = solver.simplexCounts,

                particleMaterialIndices = solver.abstraction.collisionMaterials.AsNativeArray<int>(),
                collisionMaterials = ObiColliderWorld.GetInstance().collisionMaterials.AsNativeArray<BurstCollisionMaterial>(),

                contactsQueue = particleContactQueue.AsParallelWriter(),
                fluidInteractionsQueue = fluidInteractionQueue.AsParallelWriter(),
                dt = deltaTime,
                collisionMargin = solver.abstraction.parameters.collisionMargin,
                optimizationIterations = solver.abstraction.parameters.surfaceCollisionIterations,
                optimizationTolerance = solver.abstraction.parameters.surfaceCollisionTolerance,
            };

            return generateParticleContactsJob.Schedule(grid.CellCount, 1);
        }

        public JobHandle SpatialQuery(BurstSolverImpl solver,
                                      NativeArray<BurstQueryShape> shapes,
                                      NativeArray<BurstAffineTransform> transforms,
                                      NativeQueue<BurstQueryResult> results)
        {
            var job = new SpatialQueryJob
            {
                grid = grid,

                positions = solver.abstraction.prevPositions.AsNativeArray<float4>(),
                orientations = solver.abstraction.prevOrientations.AsNativeArray<quaternion>(),
                radii = solver.abstraction.principalRadii.AsNativeArray<float4>(),
                filters = solver.abstraction.filters.AsNativeArray<int>(),

                simplices = solver.simplices,
                simplexCounts = solver.simplexCounts,

                shapes = shapes,
                transforms = transforms,

                results = results.AsParallelWriter(),
                worldToSolver = solver.worldToSolver,
                parameters = solver.abstraction.parameters
            };

            return job.Schedule(shapes.Length, 4);
        }

        public void GetCells(ObiNativeAabbList cells)
        {
            if (cells.count == grid.usedCells.Length)
            {
                for (int i = 0; i < grid.usedCells.Length; ++i)
                {
                    var cell = grid.usedCells[i];
                    float size = NativeMultilevelGrid<int>.CellSizeOfLevel(cell.Coords.w);

                    float4 min = (float4)cell.Coords * size;
                    min[3] = 0;

                    cells[i] = new Aabb(min, min + new float4(size, size, size, 0));
                }
            }
        }

        public void Dispose()
        {
            grid.Dispose();
            particleContactQueue.Dispose();
            fluidInteractionQueue.Dispose();
        }

    }
}
#endif