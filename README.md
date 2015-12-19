# Voxe
Voxe is an open source voxel framework for Unity3D.

Even though voxels have become rather popular in the last few years there are still only quite a few open-source C# solutions of good quality out there. Voxe is an attempt to bring voxels to everyone. It's meant to bring the community together, be easy to use and yet feature rich.

I am starting this project because I have come to realize (once again) how futile it is trying to create something great alone with only the little spare time one has. Frankly, in its current form it is far from greatness - it is full of bugs, lacks many features and proper documention and its design is not finished still. However, that is not something which can not be changed in the following months given that enough talented people decide to give a hand.

I will be more than glad for any contribution be it just a simple idea or a nasty bug fix.

## Features

##### Terrain generation
Voxe currently sports a few simple terrain generators. One for a simple flat terrain, a perlin noise generator and one generic terrain generator. They all are currently usable mostly for debugging purposes.

##### Terrain streaming
The world is streamed - chunks are loaded and saved as you move. Streaming on a separate thread is supported.

##### Threading
Using a custom threadpool, chunks are generated on multiple threads taking full advantage of you hardware. Voxe uses an event-driven model for chunk generation. Upon creation, each chunk registers to its neighbors and from this moment on everything is automatic. The system is build in a way
that no synchronization is necessary.
There are still some issues with this approach, even when threading is disabled, however, once this is polished, this might become one of Voxe's most powerful assets.

## Development
Voxe is still very early in development. Current focus/plans of the development, ordered by priority, are:
1) fixing the bugs in the event-driven chunk generation
2) fully generic terrain generation
3) fully customizable chunks and blocks
4) LOD
5..N) we'll think of something
