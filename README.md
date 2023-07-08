# Unity-Voxel-Planets
[Screenshot 2023-07-07 215833](https://github.com/B0XEY/Unity-Voxel-Planets/assets/94720404/45c72532-93f0-4ceb-bfa1-bebe1452afe6)

Make Procedural Unity Voxel Planets with smooth terrain and a dynamic texture via a Shader graph.

!Odin Inspector recommended! (Only Version RN)
https://odininspector.com/download

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
- Fix Mesh Seams!

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
- Faster Generation
- Faster Smooth terrain Generation

- Version 1 generation
- 2 Parts
- Fast Mesh Generation at Small Values
- Custom Look of the planet (Flat shading, Smoothing)
- Custom scale of the planet
