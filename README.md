[![Codacy Badge](https://app.codacy.com/project/badge/Grade/36f9fdfefa0a48efbef1d50f9d63fac0)](https://app.codacy.com/gh/B0XEY/Unity-Voxel-Planets/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)


# Unity-Voxel-Planets - Unity 2022.3.1f1 - URP
![ezgif com-optimize](https://github.com/B0XEY/Unity-Voxel-Planets/assets/94720404/764d639a-c221-4c43-85b4-63d31b6a2f7c)

## Make Procedural Unity Voxel Planets with smooth terrain and a dynamic texture via a Shader graph

## Features
- 3D noise function uses jobs / brust in runtime for faster performance. Inspector (not playing) uses normal functions
- Noise settings controlled with Scriptable Object
- Using [Noise](https://github.com/unbeGames/noise.git) for 3D noise values
- Example scene
- Customizable planet Material
- Water Shader
- Full Debug of time taken to generate
- Simple Tools to View Data (Rotate, Bounds Viewer/center, Camera Position, Mesh Info)
- Events when Generation Planets (Runtime Only)

                  
## TO-DO
- [ ] Walk around Planets
- [ ] Use GPU to draw Meshes
- [ ] Atmosphere (very hard for me)
         

## Version 2 Generation
- One Script (Place and Generate)
- Chunk System
- Faster Generation (All Values(To a Point))
- Custom Look of the planet (Flat shading, Smoothing)
- Faster Smooth terrain Generation
- Times (256 x 256 Noise Map, No Jobs/Burst, In Editor, Not Playing)
- - Noise Generation - 22.93 Seconds
- - Chunk Generation - 2.3 Seconds (Flat Shading, Total)
- - Chunk Generation - 8.3 Seconds (Smooth Shading, Total)

