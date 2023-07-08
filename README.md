# Unity-Voxel-Planets / MIT
![ezgif com-optimize](https://github.com/B0XEY/Unity-Voxel-Planets/assets/94720404/764d639a-c221-4c43-85b4-63d31b6a2f7c)

Make Procedural Unity Voxel Planets with smooth terrain and a dynamic texture via a Shader graph.

!Odin Inspector recommended! (Only Version RN)
https://odininspector.com/download
or https://assetstore.unity.com/packages/tools/utilities/naughtyattributes-129996
     
Features
- 3D noise function uses jobs / brust in runtime for faster performance. Inspector (not playing) uses normal functions
- Noise settings controlled with Scriptable Object
- Using FastNoiseLite, and Noise for 3D noise values
- Example scene
- Customizable planet Material
- Water Shader
- Full Debug of time taken to generate
- Simple Tools to View Data (Rotate, Bounds Viewer/center, Camera Position, Mesh Info)
- Events when Generation Planets (Runtime Only)
                  
To-DO (First to Last)
- Mesh Colliders Support
- Fix Mesh Seams      
- Lower Memory usage
- No Odin Support
- Better Shaders
- Walk around Planets
- Terraforming
- Auto Sand / Water Creation for entire procedural terrain
- Use GPU to draw Meshes
- Atmosphere (very hard for me)
         
         
Version 2 generation (recommended)
- One Script (Place and Generate)
- Chunk System
- Faster Generation (All Values(To a Point))
- Custom Look of the planet (Flat shading, Smoothing)
- Faster Smooth terrain Generation
- Times (256 x 256 Noise Map, No Jobs/Burst, In Editor, Not Playing)
- - Noise Generation - 3.18 Seconds
- - Chunk Generation - 1.83 Seconds (Smooth Shading)
- - Chunk Generation - .69 Seconds (Flat Shading)
        
- Version 1 generation
- 2 Parts
- Fast Mesh Generation at Small Values
- Custom Look of the planet (Flat shading, Smoothing)
- Custom scale of the planet
- - Times (256 x 256 Noise Map, No Jobs/Burst, In Editor, Not Playing)
- - Noise Generation - 12.07 Seconds
- - Mesh Generation - 2.11 Seconds (Flat Shading)
- - Mesh Generation - 27.91 Minutes (Smooth Shading) XXXXXXXXXX
