using System.Collections.Generic;
using Unbegames.Noise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Core.Static {
    public static class VoxelNoise {
        //3D Noise Functions
        private static float[,,] Get3DNoiseMap(int size, int layerToMake, PlanetSettings settings) {
            var octaves = settings.noiseLayers[layerToMake].octaves;
            var lacunarity = settings.noiseLayers[layerToMake].lacunarity;
            var gain = settings.noiseLayers[layerToMake].gain;
            var strength = settings.noiseLayers[layerToMake].strength;
            var noise = new Perlin3D();
            var noiseBillow = new FractalBillow<Value3D>(octaves, lacunarity, gain, strength);
            var noiseRigid = new FractalRiged<ValueCubic3D>(octaves, lacunarity, gain, strength);
            float[,,] map3D = new float[size + 1, size + 1, size + 1];
            for (int x = 0; x <= size; x++) {
                for (int y = 0; y <= size; y++) {
                    for (int z = 0; z <= size; z++) {
                        float sampleX = (x / (45 * settings.noiseLayers[layerToMake].scale)) + settings.noiseLayers[layerToMake].offset.x;
                        float sampleY = (y / (45 * settings.noiseLayers[layerToMake].scale)) + settings.noiseLayers[layerToMake].offset.y;
                        float sampleZ = (z / (45 * settings.noiseLayers[layerToMake].scale)) + settings.noiseLayers[layerToMake].offset.z;
                        var value = settings.noiseLayers[layerToMake].amplitude;
                        var frequency = settings.noiseLayers[layerToMake].frequency;
                        var amplitude = settings.noiseLayers[layerToMake].amplitude;
                        if (settings.noiseLayers[layerToMake].type == NoiseType.Normal) {
                            for (int i = 0; i < octaves; i++) {
                                sampleX *= frequency;
                                sampleY *= frequency;
                                sampleZ *= frequency;
                                value += noise.GetValue(settings.seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                                frequency *= lacunarity;
                                amplitude *= settings.noiseLayers[layerToMake].persistence;
                            }
                        }else {
                            if (settings.noiseLayers[layerToMake].type == NoiseType.Billow) value += noiseBillow.GetValue(settings.seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                            if (settings.noiseLayers[layerToMake].type == NoiseType.Ridged) value += noiseRigid.GetValue(settings.seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                        }
                        map3D[x, y, z] = value * settings.noiseLayers[layerToMake].layerPower;
                    }
                }
            }
            return map3D;
        }
        private static float[,,] Get3DNoiseMapJob(int size, int layerToMake, PlanetSettings settings) {
            var octaves = settings.noiseLayers[layerToMake].octaves;
            var lacunarity = settings.noiseLayers[layerToMake].lacunarity;
            var gain = settings.noiseLayers[layerToMake].gain;
            var strength = settings.noiseLayers[layerToMake].strength;
            float[,,] map3D = new float[size + 1, size + 1, size + 1];
            int noiseTypeToUse = 0;
            if (settings.noiseLayers[layerToMake].type == NoiseType.Billow) noiseTypeToUse = 1;
            if (settings.noiseLayers[layerToMake].type == NoiseType.Ridged) noiseTypeToUse = 2;
            var values = new NativeArray<float>((size + 1) * (size + 1) * (size + 1), Allocator.TempJob);
            var job = new Get3DNoise() {
                Size = size,
                Offset = settings.noiseLayers[layerToMake].offset,
                Scale = (45 * settings.noiseLayers[layerToMake].scale),
                Seed = settings.seed,
                Power = settings.noiseLayers[layerToMake].layerPower,
                Amplitude = settings.noiseLayers[layerToMake].amplitude,
                Frequency = settings.noiseLayers[layerToMake].frequency,
                Lacunarity = lacunarity,
                Persistence = settings.noiseLayers[layerToMake].persistence,
                Octaves = octaves,
                MapPlanet1D = values,
                UseNoise = noiseTypeToUse,
                Noise = new Perlin3D(),
                NoiseBillow = new FractalBillow<Value3D>(octaves, lacunarity, gain, strength),
                NoiseRigid = new FractalRiged<ValueCubic3D>(octaves, lacunarity, gain, strength)
            };
            var handle = job.Schedule(values.Length , 128);
            handle.Complete();
            int index = 0;
            for (int x = 0; x <= size; x++) {
                for (int y = 0; y <= size; y++) {
                    for (int z = 0; z <= size; z++) {
                        map3D[x, y, z] = values[index];
                        index++;
                    }
                }
            }
            values.Dispose();
            return map3D;
        }
        //Planet Noise Functions
        public static float[,,] GetPlanetNoiseMap(int mapSize, float planetRadius, PlanetSettings settings) {
            var center = Vector3.one * (mapSize * .5f);
            var noiseMaps = new List<float[,,]>(settings.noiseLayers.Length - 1);
            if (settings.useNoiseMap && settings.noiseLayers.Length > 0) {
                for (int i = 0; i < settings.noiseLayers.Length; i++) {
                    noiseMaps.Add(Get3DNoiseMap(mapSize, i, settings));
                }
            }
            float[,,] mapPlanet = new float[mapSize + 1, mapSize + 1, mapSize + 1];
            var maskValue = 0f;
            for (int x = 0; x <= mapSize; x++) {
                for (int y = 0; y <= mapSize; y++) {
                    for (int z = 0; z <= mapSize; z++) {
                        var position = new Vector3(x, y, z);
                        var distanceFromCenter = Vector3.Distance(position, center);
                        var distanceValue = (1 - ((distanceFromCenter + 15) - planetRadius));
                        mapPlanet[x, y, z] = distanceValue;
                        for (int i = 0; i < noiseMaps.Count; i++) {
                            if (i > 0 && settings.noiseLayers[i].useLastLayerAsMask) maskValue = noiseMaps[i - 1][x, y, z];
                            mapPlanet[x, y, z] += noiseMaps[i][x,y,z] - maskValue;
                        }
                    }
                }
            }
            
            return mapPlanet;
        }
        public static float[,,] GetPlanetNoiseMapJob(int mapSize, float planetRadius, PlanetSettings settings) {
            var map3D = new float[mapSize + 1, mapSize + 1, mapSize + 1];
            var noiseMaps = new List<float[,,]>(settings.noiseLayers.Length - 1);
            if (settings.useNoiseMap && settings.noiseLayers.Length > 0) {
                for (int i = 0; i < settings.noiseLayers.Length; i++) {
                    noiseMaps.Add(Get3DNoiseMapJob(mapSize, i, settings));
                }
            }
            var values = new NativeArray<float>((mapSize + 1) * (mapSize + 1) * (mapSize + 1), Allocator.TempJob);
            var job = new GetPlanetNoise {
                MapSize = mapSize,
                Radius = planetRadius,
                Center = new float3(1, 1, 1) * (mapSize * .5f),
                MapPlanet1D = values
            };
            var handle = job.Schedule(values.Length, mapSize + 1);
            handle.Complete();
            var maskValue = 0f;
            int index = 0;
            for (int x = 0; x <= mapSize; x++) {
                for (int y = 0; y <= mapSize; y++) {
                    for (int z = 0; z <= mapSize; z++) {
                        map3D[x, y, z] = values[index];
                        for (int i = 0; i < noiseMaps.Count; i++) {
                            if (i > 0 && settings.noiseLayers[i].useLastLayerAsMask) maskValue = noiseMaps[i - 1][x, y, z];
                            map3D[x, y, z] += noiseMaps[i][x,y,z] - maskValue;
                        }
                        index++;
                    }
                }
            }
            values.Dispose();
            return map3D;
        }
        //Colors
        public static Color[] GetLayerColors3DPlanet(float[,,] map, int layer, Gradient mapColors, out float min, out float max, bool isPlanet) {
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
            var handle = job.Schedule(values.Length, size + 1);
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
            [ReadOnly] public int Size;
            [ReadOnly] public float3 Offset;
            [ReadOnly] public float Scale;
            [ReadOnly] public int Seed;
            [ReadOnly] public float Power;

            [ReadOnly] public float Amplitude;
            [ReadOnly] public float Frequency;
            [ReadOnly] public float Lacunarity;
            [ReadOnly] public float Persistence;
            [ReadOnly] public int Octaves;
            [ReadOnly] public int UseNoise;
            [ReadOnly] public Perlin3D Noise; // UseNoise == 0
            [ReadOnly] public FractalBillow<Value3D> NoiseBillow; // UseNoise == 1
            [ReadOnly] public FractalRiged<ValueCubic3D> NoiseRigid; // UseNoise == 2;
            public NativeArray<float> MapPlanet1D;

            public void Execute(int index) {
                int z = index % (Size + 1);
                int y = (index / (Size + 1)) % (Size + 1);
                int x = index / ((Size + 1) * (Size + 1));
                float sampleX = (x / Scale) + Offset.x;
                float sampleY = (y / Scale) + Offset.y;
                float sampleZ = (z / Scale) + Offset.z;
                var value = Amplitude;
                var frequency = Frequency;
                var amplitude = Amplitude;
                if (UseNoise == 0) {
                    for (int i = 0; i < Octaves; i++) {
                        sampleX *= frequency;
                        sampleY *= frequency;
                        sampleZ *= frequency;
                        value += Noise.GetValue(Seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                        frequency *= Lacunarity;
                        amplitude *= Persistence;
                    }
                }else {
                    if (UseNoise == 1) value += NoiseBillow.GetValue(Seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                    if (UseNoise == 2) value += NoiseRigid.GetValue(Seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                }
                MapPlanet1D[index] = value * Power;
            }
        }
        [BurstCompile]
        private struct GetPlanetNoise : IJobParallelFor {
            [ReadOnly] public int MapSize;
            [ReadOnly] public float Radius;
            [ReadOnly] public float3 Center;
            public NativeArray<float> MapPlanet1D;

            public void Execute(int index) {
                int z = index % (MapSize + 1);
                int y = (index / (MapSize + 1)) % (MapSize + 1);
                int x = index / ((MapSize + 1) * (MapSize + 1));
                var position = new float3(x, y, z);
                var distanceFromCenter = math.distance(position, Center);
                float distanceValue = 1 - ((distanceFromCenter + 15) - Radius);
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
                    var weight = SmoothStep(EffectRadius, EffectRadius * 0.7f, distanceFromPoint);
                    Map1D[index] += -(EffectSpeed * weight * DeltaTime) * Mult;
                }
            }
            private static float SmoothStep(float min, float max, float time) {
                time = math.saturate((time - min) / (max - min));
                return time * time * (3 - 2 * time);
            }
        }
    }
}