# Unity-Voxel-Planets
![ezgif com-optimize](https://github.com/B0XEY/Unity-Voxel-Planets/assets/94720404/764d639a-c221-4c43-85b4-63d31b6a2f7c)

## Make Procedural Unity Voxel Planets with smooth terrain and a dynamic texture via a Shader graph
### Unity 2022.3.1f1 - URP
### https://github.com/unbeGames/noise.git (Required)

[Odin Inspector recommended](https://odininspector.com/download) (More Recent Versions, Link to free trial)
or [Naughty Attributes](https://assetstore.unity.com/packages/tools/utilities/naughtyattributes-129996)
     
Features
- 3D noise function uses jobs / brust in runtime for faster performance. Inspector (not playing) uses normal functions
- Noise settings controlled with Scriptable Object
- Using [Noise](https://github.com/unbeGames/noise.git) for 3D noise values
- Example scene
- Customizable planet Material
- Water Shader
- Full Debug of time taken to generate
- Simple Tools to View Data (Rotate, Bounds Viewer/center, Camera Position, Mesh Info)
- Events when Generation Planets (Runtime Only)
                  
TO-DO
- Fix Mesh Seams :(
- Lower Memory usage :|
- Better Shaders :P
- Walk around Planets :(
- Terraforming :(
- Auto Sand / Water Creation for entire procedural terrain :(
- Use GPU to draw Meshes :(
- Atmosphere (very hard for me) :(
         
         
Version 2 Generation (recommended)
- One Script (Place and Generate)
- Chunk System
- Faster Generation (All Values(To a Point))
- Custom Look of the planet (Flat shading, Smoothing)
- Faster Smooth terrain Generation
- Times (256 x 256 Noise Map, No Jobs/Burst, In Editor, Not Playing)
- - Noise Generation - 22.93 Seconds
- - Chunk Generation - 2.3 Seconds (Flat Shading, Total)
- - Chunk Generation - 8.3 Seconds (Smooth Shading, Total)

Version 1 Generation (Obsolete)
- 2 Parts
- Fast Mesh Generation at Small Values
- Custom Look of the planet (Flat shading, Smoothing)
- Custom scale of the planet
- - Times (256 x 256 Noise Map, No Jobs/Burst, In Editor, Not Playing)
- - Noise Generation - 12.07 Seconds
- - Mesh Generation - 2.11 Seconds (Flat Shading, Total)
- - Mesh Generation - 27.91 Minutes (Smooth Shading, Total)
