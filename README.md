# `AshDumpLib (C# Library)`

**_C# library for various file formats_**

## 📜 Description 📜

A library with simple reading and writing function for various file formats in various games.

## 🗂️ Projects 🗂️

- AshDumpLib - The actual C# library itself.

- AshDumpLibTest - A testing sandbox for the C# library.

## 🗃 Dependencies 🗃

|                                                     Name                                                      |                            Use                            |
| :-----------------------------------------------------------------------------------------------------------: | :-------------------------------------------------------: |
| [Amiticia.IO](<[https://github.com/tge-was-taken/Amicitia.IO](https://github.com/tge-was-taken/Amicitia.IO)>) | Used for its upgraded and better binary reader and writer |

# 🗂️ Contents 🗂️

### Castle Siege

|                       |              **Castle Siege**              |             |
| :-------------------: | :----------------------------------------: | :---------: |
| **File format name**  |                  **Info**                  | **Credits** |
|     RDA (Archive)     | For easy reading and writing of .rda files |             |
|  RDO (Model/Object)   | For easy reading and writing of .rdo files |             |
|    RDM (Animation)    | For easy reading and writing of .rdm files |             |
| HLM (HeightLevelMap)  | For easy reading and writing of .hlm files |             |
| TLM (TerrainLevelMap) | For easy reading and writing of .tlm files |             |

### Hedgehog Engine 1/2

|                      |                                                                              **Sonic Frontiers**                                                                               |                                                                                  |
| :------------------: | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------: | :------------------------------------------------------------------------------: |
| **File format name** |                                                                                    **Info**                                                                                    |                                   **Credits**                                    |
|   CameraAnimation    |                                                                For easy reading and writing of .cam-anim files                                                                 |          Used Kwas's research on .cam-anim's, used ik-01's FOV formula           |
|  MaterialAnimation   |                                                                For easy reading and writing of .mat-anim files                                                                 |                       Used ik-01's research on .mat-anim's                       |
|     UVAnimation      |                                                                 For easy reading and writing of .uv-anim files                                                                 |                        Used Kwas's research on .uv-anim's                        |
| VisibilityAnimation  |                                                                For easy reading and writing of .vis-anim files                                                                 |                       Used ik-01's research on .mat-anim's                       |
|       Animator       |                                                                   For easy reading and writing of .asm files                                                                   |                       Used ik-01's and angryzor's research                       |
|     AnimationPXD     |                                         For easy reading and writing of .anm.pxd files, though it doesn't decompress the data just yet                                         |                      Used Adel's Blender Addon as reference                      |
|  DensityPointCloud   |                                         For easy reading and writing of .densitypointcloud files, though there's a few unknown values                                          |                                                                                  |
|    DensitySetting    |                                                For easy reading of .densitysetting files, though there's alot of unknown values                                                |                                                                                  |
|     ObjectWorld      | For easy reading and writing of .gedit files, though you need to download a [template](https://github.com/Radfordhound/HedgeLib/tree/HedgeLib%2B%2B/Templates) from HedgeLib++ | Align information taken from HedgeLib++, and info on Forces gedits from angryzor |
|      Reflection      |                                         For easy reading and writing of .rfl files, though there's no way to get a template right now                                          |       Align information taken from HedgeLib++, and templates from angryzor       |
|      PointCloud      |                                                         For easy reading and writing of .pcmodel, .pcrt, .pccol files                                                          |                                                                                  |
|        Probe         |                                                                  For easy reading and writing of .probe files                                                                  |                                                                                  |
|      ShaderList      |                                                           For easy reading and writing reading of .shader-list files                                                           |                                                                                  |
|     SkeletonPXD      |                                                             For easy reading and writing reading of .skl.pxd files                                                             |                                                                                  |
|   TerrainMaterial    |                                      For easy reading and writing reading of .terrain-material files, though there's a few unknown values                                      |                                                                                  |
|         Text         |                                                               For easy reading and writing of .cnvrs-text files                                                                |                                                                                  |
|         PAC          |                                      For easy reading and almost perfect writing of .pac files, so far only Frontiers' and SXSG' version                                       |               A lot of information has been taken from HedgeLib++                |
|     HeightField      |                                            For easy reading and writing of .heightfield files, though some values are still unknown                                            |                     Used ik-01's research on .heightfield's                      |
|    NavMeshConfig     |                                                                   For easy reading and writing of .nmc files                                                                   |                                                                                  |
|     NavMeshTile      |                                                    For easy reading of .nmt files, though there's really nothing much known                                                    |                                                                                  |
|     NeedleShader     |                                         For easy reading of .cso, .pso and .vso files, though the shader data part is still unfinished                                         |                                                                                  |
|    NeedleArchive     |                                                            For easy reading and writing of .model (with lods) files                                                            |                      Used HedgeNeedle's output as reference                      |
|   ParticleLocator    |                                              For easy reading and writing of .effdb files, though a few values are still unknown                                               |                                                                                  |
|    AIStateMachine    |                                               For easy reading and writing of .aism files, though a few values are still unknown                                               |                                                                                  |
|         NTSP         |                                                                        For easy reading of .ntsp files                                                                         |                      Some info taken from Skyth's NtspMaker                      |
