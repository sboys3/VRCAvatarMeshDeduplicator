# VRC Avatar Mesh Deduplicator
This is a small utility to deduplicate the meshes in a VRC Avatar after d4rkAvatarOptimizer has been run. If you have multiple identical copies of a mesh, d4rkAvatarOptimizer will optimize them separately resulting in multiple copies of the same mesh in your avatar. This tool finds and reuses a single instance of the mesh by comparing things like vertices and other properties.

## Usage
Clone this repository into your Unity project's Assets folder.  
Place the Mesh Deduplicator script on the root of your avatar. It will automatically run on upload.


## Modifying d4rkAvatarOptimizer AvatarBuildHook.cs
You must decrease the callbackOrder by one in `Packages/d4rkpl4y3r.d4rkAvatarOptimizer/Editor/AvatarBuildHook.cs` to ensure that this script runs after d4rkAvatarOptimizer's.


## Specifics of the deduplication comparison
The script compares several things as follows if they exist in both meshes:
* mesh name
* vertex count
* triangle count
* 200 vertex positions
* vertex counts of each submesh
* sample 47 values from first uv
* vertex attribute names
* blend shape names
* bone weights for 23 vertices

## Limitations
The optimization process seems to slightly change vertex positions. The vertices have most of their floating point mantissa chopped off to make them more likely to match. This occasionally prevents matches and has the unlikely possibility of deduplicating extremely similar meshes.

