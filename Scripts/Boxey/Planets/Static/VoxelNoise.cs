using Boxey.Planets.Generation.Data_Classes;
using Unbegames.Noise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Planets.Static {
    public static class VoxelNoise {
        //3D Noise Functions
        private static float[,,] Get3DNoiseMap(int width, int height, int depth, int seed, NoiseSettings settings) {
            var noise = new Perlin3D();
            float[,,] map3D = new float[width + 1, height + 1, depth + 1];
            for (int x = 0; x <= width; x++) {
                for (int y = 0; y <= height; y++) {
                    for (int z = 0; z <= depth; z++) {
                        float sampleX = (x / (45 * settings.scale)) + settings.offset.x;
                        float sampleY = (y / (45 * settings.scale)) + settings.offset.y;
                        float sampleZ = (z / (45 * settings.scale)) + settings.offset.z;
                        var value = settings.amplitude;
                        var frequency = settings.frequency;
                        var amplitude = settings.amplitude;
                        for (int i = 0; i < settings.octaves; i++) {
                            sampleX *= frequency;
                            sampleY *= frequency;
                            sampleZ *= frequency;
                            value += noise.GetValue(seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                            frequency *= settings.lacunarity;
                            amplitude *= settings.persistence;
                        }
                        map3D[x, y, z] = value;
                    }
                }
            }
            return map3D;
        }
        private static float[,,] Get3DNoiseMapJob(int width, int height, int depth, int seed, NoiseSettings settings) {
            float[,,] map3D = new float[width + 1, height + 1, depth + 1];
            var values = new NativeArray<float>((map3D.GetLength(0) * map3D.GetLength(1) * map3D.GetLength(2)), Allocator.TempJob);
            var job = new Get3DNoise() {
                Width = width,
                Height = height,
                Depth = depth,
                Offset = settings.offset,
                Scale = (45 * settings.scale),
                Seed = seed,
                Amplitude = settings.amplitude,
                Frequency = settings.frequency,
                Lacunarity = settings.lacunarity,
                Persistence = settings.persistence,
                Octaves = settings.octaves,
                MapPlanet1D = values,
                Noise = new Perlin3D()
            };
            var handle = job.Schedule(values.Length , 64);
            handle.Complete();
            int index = 0;
            for (int x = 0; x <= width; x++) {
                for (int y = 0; y <= height; y++) {
                    for (int z = 0; z <= depth; z++) {
                        map3D[x, y, z] = values[index];
                        index++;
                    }
                }
            }
            values.Dispose();
            return map3D;
        }
        //Planet Noise Functions
        public static float[,,] GetPlanetNoiseMap(int radius, int additive, int seed, NoiseSettings settings) {
            var diameter = (radius) * 2;
            var center = Vector3.one * radius;
            var noiseMap = Get3DNoiseMap(diameter, diameter, diameter, seed, settings);
            float[,,] mapPlanet = new float[diameter + 1, diameter + 1, diameter + 1];
            for (int x = 0; x <= diameter; x++) {
                for (int y = 0; y <= diameter; y++) {
                    for (int z = 0; z <= diameter; z++) {
                        var position = new Vector3(x, y, z);
                        var distanceFromCenter = Vector3.Distance(position, center);
                        float distanceValue = (1 - ((distanceFromCenter + additive) - radius));
                        mapPlanet[x, y, z] = distanceValue + noiseMap[x,y,z];
                    }
                }
            }
            
            return mapPlanet;
        }
        public static float[,,] GetPlanetNoiseMapJob(int radius, int additive, int seed, NoiseSettings settings) {
            var diameter = (radius) * 2;
            var map3D = new float[diameter + 1, diameter + 1, diameter + 1];
            var noiseMap = Get3DNoiseMapJob(diameter, diameter, diameter, seed, settings);
            var values = new NativeArray<float>((diameter + 1) * (diameter + 1) * (diameter + 1), Allocator.TempJob);
            var job = new GetPlanetNoise {
                Diameter = (radius) * 2,
                Radius = radius,
                DistanceOffset = additive,
                Center = new float3(1, 1, 1) * (radius),
                MapPlanet1D = values
            };
            var handle = job.Schedule(values.Length, 64);
            handle.Complete();
            int index = 0;
            for (int x = 0; x <= diameter; x++) {
                for (int y = 0; y <= diameter; y++) {
                    for (int z = 0; z <= diameter; z++) {
                        map3D[x, y, z] = values[index] + noiseMap[x, y, z];
                        index++;
                    }
                }
            }
            values.Dispose();
            return map3D;
        }
        //Colors
        public static Color[] GetLayerColors3D(float[,,] map, int layer, Gradient mapColors, out float min, out float max, bool isPlanet) {
            max = float.MinValue; min = float.MaxValue;
            int sizeX = map.GetLength(0);
            int sizeZ = map.GetLength(1);
            Color[] colors = new Color[sizeX * sizeZ];
            for (int width = 0; width < sizeX; width++) {
                for (int height = 0; height < sizeZ; height++) {
                    var value = map[width, height, layer];
                    if (value > max) max = value;
                    if (value < min) min = value;
                    colors[height * sizeX + width] = isPlanet ? Color.red: mapColors.Evaluate(value);
                }
            }
            for (int width = 0; width < sizeX; width++) {
                for (int height = 0; height < sizeZ; height++) {
                    colors[height * sizeX + width] = mapColors.Evaluate(Mathf.InverseLerp(min, max, map[width, height, layer]));
                    if (isPlanet && map[width, height, layer] >= -.75f && map[width, height, layer] <= .75f) colors[height * sizeX + width] = Color.red;
                }
            }
            return colors;
        }
        //Terraforming
        public static void Terraform(ref float[,,] map, int size, bool addTerrain, float brushRadius, float brushSpeed, float3 centerPoint) {
            var values = new NativeArray<float>((size + 1) * (size + 1) * (size + 1), Allocator.TempJob);
            int index = 0;
            for (int x = 0; x <= size; x++) {
                for (int y = 0; y <= size; y++) {
                    for (int z = 0; z <= size; z++) {
                        values[index] = map[x, y, z];
                        index++;
                    }
                }
            }
            var job = new Terrafrom() {
                Size = size,
                Mult = addTerrain ? 1 : -1,
                EffectRadius = brushRadius,
                EffectSpeed = brushSpeed,
                DeltaTime = Time.deltaTime,
                EffectCenter = centerPoint,
                Map1D = values
            };
            var handle = job.Schedule(values.Length, 64);
            handle.Complete();
            var i = 0;
            for (int x = 0; x <= size; x++) {
                for (int y = 0; y <= size; y++) {
                    for (int z = 0; z <= size; z++) {
                        map[x, y, z] = values[i];
                        i++;
                    }
                }
            }
            values.Dispose();
        }
        //Jobs
        [BurstCompile]
        private struct Get3DNoise : IJobParallelFor {
            [ReadOnly] public int Width;
            [ReadOnly] public int Height;
            [ReadOnly] public int Depth;
            [ReadOnly] public float3 Offset;
            [ReadOnly] public float Scale;
            [ReadOnly] public int Seed;

            [ReadOnly] public float Amplitude;
            [ReadOnly] public float Frequency;
            [ReadOnly] public float Lacunarity;
            [ReadOnly] public float Persistence;
            [ReadOnly] public int Octaves;
            [ReadOnly] public Perlin3D Noise;
            public NativeArray<float> MapPlanet1D;

            public void Execute(int index) {
                int z = index % (Width + 1);
                int y = (index / (Depth + 1)) % (Height + 1);
                int x = index / ((Depth + 1) * (Height + 1));
                float sampleX = (x / Scale) + Offset.x;
                float sampleY = (y / Scale) + Offset.y;
                float sampleZ = (z / Scale) + Offset.z;
                var value = Amplitude;
                var frequency = Frequency;
                var amplitude = Amplitude;
                for (int i = 0; i < Octaves; i++) {
                    sampleX *= frequency;
                    sampleY *= frequency;
                    sampleZ *= frequency;
                    value += Noise.GetValue(Seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                    frequency *= Lacunarity;
                    amplitude *= Persistence;
                }
                MapPlanet1D[index] = value;
            }
        }
        [BurstCompile]
        private struct GetPlanetNoise : IJobParallelFor {
            [ReadOnly] public int Diameter;
            [ReadOnly] public int Radius;
            [ReadOnly] public int DistanceOffset;
            [ReadOnly] public float3 Center;
            public NativeArray<float> MapPlanet1D;

            public void Execute(int index) {
                int z = index % (Diameter + 1);
                int y = (index / (Diameter + 1)) % (Diameter + 1);
                int x = index / ((Diameter + 1) * (Diameter + 1));
                var position = new float3(x, y, z);
                var distanceFromCenter = math.distance(position, Center);
                float distanceValue = 1 - ((distanceFromCenter + DistanceOffset) - Radius);
                MapPlanet1D[index] = distanceValue;
            }
        }
        [BurstCompile]
        private struct Terrafrom : IJobParallelFor {
            [ReadOnly] public int Size;
            [ReadOnly] public int Mult;
            [ReadOnly] public float EffectRadius;
            [ReadOnly] public float EffectSpeed;
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public float3 EffectCenter;
            public NativeArray<float> Map1D;

            public void Execute(int index) {
                int z = index % (Size + 1);
                int y = (index / (Size + 1)) % (Size + 1);
                int x = index / ((Size + 1) * (Size + 1));
                var position = new float3(x, y, z);
                var distanceFromPoint =  math.distance(position, EffectCenter);
                if (distanceFromPoint < EffectRadius && distanceFromPoint > -EffectRadius) {
                    var weight = Step(EffectRadius, EffectRadius * 0.7f, distanceFromPoint);
                    Map1D[index] += -(EffectSpeed * weight * DeltaTime) * Mult;
                }
            }
            private static float Step(float min, float max, float time) {
                time = math.saturate((time - min) / (max - min));
                return time * time * (3 - 2 * time);
            }
        }
    }
}