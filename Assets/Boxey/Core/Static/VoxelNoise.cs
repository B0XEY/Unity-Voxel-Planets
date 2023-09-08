using System.Collections.Generic;
using Unbegames.Noise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boxey.Core.Static {
    public static class VoxelNoise{
        private static Perlin3D _noise;
        public static void SetValues(){
            _noise = new Perlin3D();
        }
        
        //3D Noise Functions
        private static float[,,] Get3DNoiseMap(int size, float3 offset, int layerToMake, PlanetSettings settings){
            var octaves = settings.noiseLayers[layerToMake].octaves;
            var lacunarity = settings.noiseLayers[layerToMake].lacunarity;
            var gain = settings.noiseLayers[layerToMake].gain;
            var strength = settings.noiseLayers[layerToMake].strength;
            var noiseBillow = new FractalBillow<Value3D>(octaves, lacunarity, gain, strength);
            var noiseRigid = new FractalRiged<ValueCubic3D>(octaves, lacunarity, gain, strength);
            var map3D = new float[size + 1, size + 1, size + 1];
            float min = -1f * settings.noiseLayers[layerToMake].amplitude;
            float max = 1f * settings.noiseLayers[layerToMake].amplitude;
            for (var x = 0; x <= size; x++) {
                for (var y = 0; y <= size; y++) {
                    for (var z = 0; z <= size; z++) {
                        var sampleX = (x + offset.x) / (45 * settings.noiseLayers[layerToMake].scale) + settings.noiseLayers[layerToMake].offset.x;
                        var sampleY = (y + offset.y) / (45 * settings.noiseLayers[layerToMake].scale) + settings.noiseLayers[layerToMake].offset.y;
                        var sampleZ = (z + offset.z) / (45 * settings.noiseLayers[layerToMake].scale) + settings.noiseLayers[layerToMake].offset.z;
                        var value = 0f;
                        var frequency = settings.noiseLayers[layerToMake].frequency;
                        var amplitude = settings.noiseLayers[layerToMake].amplitude;
                        if (settings.noiseLayers[layerToMake].type == NoiseType.Normal){
                            for (int i = 0; i < octaves; i++) {
                                sampleX *= frequency;
                                sampleY *= frequency;
                                sampleZ *= frequency;
                                value += _noise.GetValue(settings.seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                                frequency *= lacunarity;
                                amplitude *= settings.noiseLayers[layerToMake].persistence;
                            }
                        }else{
                            if (settings.noiseLayers[layerToMake].type == NoiseType.Billow){
                                value +=noiseBillow.GetValue(settings.seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                            }
                            if (settings.noiseLayers[layerToMake].type == NoiseType.Ridged){
                                min = 0;
                                max = 1f * settings.noiseLayers[layerToMake].amplitude;
                                value +=noiseRigid.GetValue(settings.seed, new float3(sampleX, sampleY, sampleZ)) * amplitude;
                            }
                        }

                        var normalizedValue = Mathf.InverseLerp(min, max, value);
                        map3D[x, y, z] = settings.noiseLayers[layerToMake].mapHeightCurve.Evaluate(normalizedValue) * settings.noiseLayers[layerToMake].layerPower;
                    }
                }
            }
            return map3D;
        }
        private static float[] Get3DNoiseMapJob(int size, float3 offset, int layerToMake, PlanetSettings settings) {
            var octaves = settings.noiseLayers[layerToMake].octaves;
            var lacunarity = settings.noiseLayers[layerToMake].lacunarity;
            var gain = settings.noiseLayers[layerToMake].gain;
            var strength = settings.noiseLayers[layerToMake].strength;
            var min = -1f * settings.noiseLayers[layerToMake].amplitude;
            var max = 1f * settings.noiseLayers[layerToMake].amplitude;
            var noiseTypeToUse = 0;
            if (settings.noiseLayers[layerToMake].type == NoiseType.Billow) noiseTypeToUse = 1;
            if (settings.noiseLayers[layerToMake].type == NoiseType.Ridged){
                min = 0;
                max = 1f * settings.noiseLayers[layerToMake].amplitude;
                noiseTypeToUse = 2;
            }
            var returnMap = new float[(size + 1) * (size + 1) * (size + 1)];
            var values = new NativeArray<float>((size + 1) * (size + 1) * (size + 1), Allocator.TempJob);
            var job = new Get3DNoise {
                Size = size,
                Offset = offset,
                NoiseOffset = settings.noiseLayers[layerToMake].offset,
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
                Noise = _noise,
                NoiseBillow = new FractalBillow<Value3D>(octaves, lacunarity, gain, strength),
                NoiseRigid = new FractalRiged<ValueCubic3D>(octaves, lacunarity, gain, strength)
            };
            var handle = job.Schedule(values.Length , 128);
            handle.Complete();
            for (int i = 0; i < values.Length; i++){
                var normalizedValue = Mathf.InverseLerp(min, max, values[i]);
                returnMap[i] = settings.noiseLayers[layerToMake].mapHeightCurve.Evaluate(normalizedValue) * settings.noiseLayers[layerToMake].layerPower;
            }
            values.Dispose();
            return returnMap;
        }
        //Planet Noise Functions
        public static float[,,] GetPlanetNoiseMap(int mapSize, int planetSize, float3 offset, float planetRadius, PlanetSettings settings) {
            var center = Vector3.one * (planetSize * .5f);
            var noiseMaps = new List<float[,,]>(settings.noiseLayers.Length - 1);
            if (settings.useNoiseMap && settings.noiseLayers.Length > 0) {
                for (int i = 0; i < settings.noiseLayers.Length; i++) {
                    noiseMaps.Add(Get3DNoiseMap(mapSize, offset, i, settings));
                }
            }
            float[,,] mapPlanet = new float[mapSize + 1, mapSize + 1, mapSize + 1];
            for (int x = 0; x <= mapSize; x++) {
                for (int y = 0; y <= mapSize; y++) {
                    for (int z = 0; z <= mapSize; z++) {
                        var position = new Vector3(x + offset.x, y + offset.y, z + offset.z);
                        var distanceFromCenter = Vector3.Distance(position, center);
                        var distanceValue = (1 - (distanceFromCenter / planetRadius));
                        mapPlanet[x, y, z] = distanceValue;
                        for (int i = 0; i < noiseMaps.Count; i++) {
                            var maskValue = 0f;
                            var mult = 1f;
                            if (settings.noiseLayers[i].removeLayer) mult = -1f;
                            if (i > 0 && settings.noiseLayers[i].useLastLayerAsMask) maskValue = noiseMaps[i - 1][x, y, z];
                            mapPlanet[x, y, z] += (noiseMaps[i][x, y, z] - maskValue) * mult;
                        }
                    }
                }
            }
            
            return mapPlanet;
        }
        public static float[,,] GetPlanetNoiseMapJob(int mapSize, int planetSize, float3 offset, float planetRadius, PlanetSettings settings) {
            var map3D = new float[mapSize + 1, mapSize + 1, mapSize + 1];
            var noiseMaps = new List<float[]>(settings.noiseLayers.Length - 1);
            if (settings.useNoiseMap && settings.noiseLayers.Length > 0) {
                for (int i = 0; i < settings.noiseLayers.Length; i++) {
                    noiseMaps.Add(Get3DNoiseMapJob(mapSize, offset, i, settings));
                }
            }
            var values = new NativeArray<float>((mapSize + 1) * (mapSize + 1) * (mapSize + 1), Allocator.TempJob);
            var job = new GetPlanetNoise {
                MapSize = mapSize,
                Radius = planetRadius,
                Offset = offset,
                Center = new float3(1, 1, 1) * (planetSize * .5f),
                MapPlanet1D = values
            };
            var handle = job.Schedule(values.Length, mapSize + 1);
            handle.Complete();
            var maskValue = 0f;
            var mult = 1f;
            int index = 0;
            for (int x = 0; x <= mapSize; x++) {
                for (int y = 0; y <= mapSize; y++) {
                    for (int z = 0; z <= mapSize; z++) {
                        map3D[x, y, z] = values[index];
                        for (int i = 0; i < noiseMaps.Count; i++) {
                            if (settings.noiseLayers[i].removeLayer) mult = -1f;
                            if (i > 0 && settings.noiseLayers[i].useLastLayerAsMask) maskValue = noiseMaps[i - 1][index];
                            map3D[x, y, z] += (noiseMaps[i][index] - maskValue) * mult;
                        }
                        index++;
                    }
                }
            }
            values.Dispose();
            return map3D;
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
            var job = new Terrafrom {
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
            [ReadOnly] public float3 NoiseOffset;
            [ReadOnly] public float Scale;
            [ReadOnly] public int Seed;
            [ReadOnly] public float Power;

            [ReadOnly] public float Amplitude;
            [ReadOnly] public float Frequency;
            [ReadOnly] public float Lacunarity;
            [ReadOnly] public float Persistence;
            [ReadOnly] public int Octaves;
            [ReadOnly] public int UseNoise;
            [ReadOnly] public Perlin3D Noise;
            [ReadOnly] public FractalBillow<Value3D> NoiseBillow;
            [ReadOnly] public FractalRiged<ValueCubic3D> NoiseRigid;
            
            public NativeArray<float> MapPlanet1D;

            public void Execute(int index) {
                int z = index % (Size + 1);
                int y = index / (Size + 1) % (Size + 1);
                int x = index / ((Size + 1) * (Size + 1));
                float sampleX = (x + Offset.x) / Scale + NoiseOffset.x;
                float sampleY = (y + Offset.y) / Scale + NoiseOffset.y;
                float sampleZ = (z + Offset.z) / Scale + NoiseOffset.z;
                var value = 0f;
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
                MapPlanet1D[index] = value;
            }
        }
        [BurstCompile]
        private struct GetPlanetNoise : IJobParallelFor {
            [ReadOnly] public int MapSize;
            [ReadOnly] public float Radius;
            [ReadOnly] public float3 Offset;
            [ReadOnly] public float3 Center;
            public NativeArray<float> MapPlanet1D;

            public void Execute(int index) {
                int z = index % (MapSize + 1);
                int y = (index / (MapSize + 1)) % (MapSize + 1);
                int x = index / ((MapSize + 1) * (MapSize + 1));
                var position = new float3(x + Offset.x, y + Offset.y, z + Offset.z);
                var distanceFromCenter = math.distance(position, Center);
                float distanceValue = 1 - (distanceFromCenter / Radius);
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