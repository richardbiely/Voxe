# Voxe

[![Join the chat at https://gitter.im/AlwaysGeeky/Vox](https://badges.gitter.im/richardbiely/Voxe.svg)](https://gitter.im/richardbiely/Voxe?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Repo Size](https://reposs.herokuapp.com/?path=richardbiely/Voxe)](https://github.com/richardbiely/Voxe)
[![License](https://img.shields.io/badge/Licence-GNU-blue.svg)](https://github.com/richardbiely/Voxe/LICENSE.md)
[![Stories in progress](https://badge.waffle.io/richardbiely/Voxe.png?label=In Progress&title=In Progress)](https://waffle.io/AlwaysGeeky/Vox)

Voxe is an open source voxel framework for Unity3D.

Even though voxels have become rather popular in the last few years there are still only quite a few open-source C# solutions of good quality out there. Voxe is an attempt to bring voxels to everyone. It's meant to bring the community together, be easy to use and yet feature rich.

I am starting this project because I have come to realize (once again) how futile it is trying to create something great alone with only the little spare time one has. Frankly, in its current form it is far from greatness - it lacks many features, proper documention and its design is not finished still. However, that is not something which can not be changed in the following months given enough talented people decide to give a hand.

I will be more than glad for any contribution be it just a simple idea or a nasty bug fix.

## Goals
Main focus of Voxe will be put on technology and extensibility. I want this to become a simple yet very powerful framework you can build your voxel games on. For now I will focus on framework's architecture and basic features like world generation, streaming and serialization. Later on I might implement features like fluid simulation, block physics and multiplayer but I am still unsure whether or not these will become a part of Voxe or a part of another closed-source project directly building on Voxe.

## Features

### World management

##### Terrain generation
Voxe currently sports a few simple terrain generators. One for a simple flat terrain, a perlin noise generator and one generic terrain generator. They all are currently usable mostly for debugging purposes and will later be replaced with a proper fully configurable terrain generator.

##### Infinite terrain
Real-time generation of terrain in all three axes is supported.

##### Terrain level of detail
In order to save performance Voxe is capable of generating terrain in multiple levels of detail.
NOTE: Currently WIP

##### Terrain streaming
The world is streamed - chunks are loaded and saved as you move. Streaming on a separate thread is supported. RLE compression is used.

### Special features

##### Threading
Using a custom threadpool, chunks are generated on multiple threads taking full advantage of you hardware. Voxe uses an event-driven model for chunk generation. Upon creation, each chunk registers to its neighbors and from this moment on everything is automatic. The system is build in a way that no synchronization is necessary.

##### Memory pooling
Voxe tries to waste as little memory as possible. It sports a memory pool manager that stores and reuses objects as necessary to improve performance.

##### Neighbor face merging
In order to minimize the amount of data passed to GPU and to minimize the amount of memory used by Unity when building geometry (not to mention this being a performance improvement with regards to GC), Voxe supports merging of adjacent faces for blocks of the same type.
NOTE: Shader handling texture wrapping for merged faces is WIP.

### Utilities

##### Occlusion culling
Voxe has support for culling geometry which is covered by another geometry. This helps optimize performance in cases when, for instance, the camera is in a cave or looking at a mountain behind which there is another mountain.
NOTE: This feature is currently disabled by default for performance reasons. More efficient version is WIP.

## Development
Voxe is still very early in development. Current areas of focus are (in no specific order):
######1) Making world generator independent - ability to have multiple different chunk providers, e.g. one for terrain, one for characters
######2) optimization of performance and memory consumption - octrees, LOD, OpenCL, etc.
######3) fully generic terrain generation
######4) fully customizable chunks and blocks
######5) biomes
######6..N) to be determined :)

## Note
Here and there, there might be parts of the code or even assets present in Voxe which are originaly not mine but taken from one of a million other voxel frameworks I went through in the past few months. Unfortunatelly I can not possibly remember where they all come from. If you happen to identify them and know that there is an issue with using them legally under GPL2.1 license, please, let me know so that I can change the code or ask for approval to use it from the original author or give them credit respectively.
