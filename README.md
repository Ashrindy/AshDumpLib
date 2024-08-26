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

|                      |              **Castle Siege**              |             |
| :------------------: | :----------------------------------------: | :---------: |
| **File format name** |                  **Info**                  | **Credits** |
|    RDA (Archive)     | For easy reading and writing of .rda files |             |
|  RDO (Model/Object)  | For easy reading and writing of .rdo files |             |

### Hedgehog Engine 1/2

|                      |                                                                                    **Sonic Frontiers**                                                                                    |                                                               |
| :------------------: | :---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------: | :-----------------------------------------------------------: |
| **File format name** |                                                                                         **Info**                                                                                          |                          **Credits**                          |
|   CameraAnimation    |                                                                      For easy reading and writing of .cam-anim files                                                                      | Used Kwas's research on .cam-anim's, used ik-01's FOV formula |
|  MaterialAnimation   |                                                                      For easy reading and writing of .mat-anim files                                                                      |             Used ik-01's research on .mat-anim's              |
|     UVAnimation      |                                                                      For easy reading and writing of .uv-anim files                                                                       |              Used Kwas's research on .uv-anim's               |
| VisibilityAnimation  |                                                                      For easy reading and writing of .vis-anim files                                                                      |             Used ik-01's research on .mat-anim's              |
|     AnimationPXD     |                                              For easy reading and writing of .anm.pxd files, though it doesn't decompress the data just yet                                               |            Used Adel's Blender Addon as reference             |
|  DensityPointCloud   |                                               For easy reading and writing of .densitypointcloud files, though there's a few unknown values                                               |                                                               |
|    DensitySetting    |                                                     For easy reading of .densitysetting files, though there's alot of unknown values                                                      |                                                               |
|     ObjectWorld      | For easy reading and unfinished writing of .gedit files, though you need to download a [template](https://github.com/Radfordhound/HedgeLib/tree/HedgeLib%2B%2B/Templates) from HedgeLib++ |            Align information taken from HedgeLib++            |
|      PointCloud      |                                                               For easy reading and writing of .pcmodel, .pcrt, .pccol files                                                               |                                                               |
|        Probe         |                                                                       For easy reading and writing of .probe files                                                                        |                                                               |
|     SkeletonPXD      |                                                                  For easy reading and writing reading of .skl.pxd files                                                                   |                                                               |
|   TerrainMaterial    |                                           For easy reading and writing reading of .terrain-material files, though there's a few unknown values                                            |                                                               |
|         Text         |                                                  For easy reading and writing reading of .cnvrs-text files, though it's not finished yet                                                  |                                                               |
|         PAC          |                                                              For easy reading of .pac files, so far only Frontiers' version                                                               |      A lot of information has been taken from HedgeLib++      |
|     HeightField      |                                                 For easy reading and writing of .heightfield files, though some values are still unknown                                                  |            Used ik-01's research on .heightfield's            |
|    NavMeshConfig     |                                                                        For easy reading and writing of .nmc files                                                                         |                                                               |
|     NavMeshTile      |                                                         For easy reading of .nmt files, though there's really nothing much known                                                          |                                                               |
