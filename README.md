# Voxe
Voxe is an open source voxel framework for Unity3D.

Even though voxels have become rather popular in the last few years there are still only quite a few open-source C# solutions of good quality out there. Voxe is an attempt to bring voxels to everyone. It's meant to bring the community together, be easy to use and yet feature rich.

I am starting this project because I have come to realize (once again) how futile it is trying to create something great alone with only the little spare time one has. Frankly, in its current form it is far from greatness - it lacks many features, proper documention and its design is not finished still. However, that is not something which can not be changed in the following months given enough talented people decide to give a hand.

I will be more than glad for any contribution be it just a simple idea or a nasty bug fix.

## Goals
Main focus of Voxe will be put on technology and extensibility. I want this to become a simple yet very powerful framework you can build your voxel games on. For now I will focus on framework's architecture and basic features like world generation, streaming and serialization. Later on I might implement features like fluid simulation, block physics and multiplayer but I am still unsure whether or not these will become a part of Voxe or a part of another closed-source project directly building on Voxe.

## Features

##### Terrain generation
Voxe currently sports a few simple terrain generators. One for a simple flat terrain, a perlin noise generator and one generic terrain generator. They all are currently usable mostly for debugging purposes and will later be replaced with a proper fully configurable terrain generator.

##### Terrain streaming
The world is streamed - chunks are loaded and saved as you move. Streaming on a separate thread is supported. RLE compression is used.

##### Threading
Using a custom threadpool, chunks are generated on multiple threads taking full advantage of you hardware. Voxe uses an event-driven model for chunk generation. Upon creation, each chunk registers to its neighbors and from this moment on everything is automatic. The system is build in a way that no synchronization is necessary.

## Development
Voxe is still very early in development. Current focus/plans ordered by priority are:
######1) Making world generator independent - ability to have multiple different chunk providers, e.g. one for terrain, one for characters
######2) Modularization - I want to make full use of Unity's component model and split different features of the framework into independent modules. For instance, if an user wants to have a blocky world he would attach a "BlockBuilder" component to chunk generator. If he decides to have his world look realistic he would attach a "DualContouringBuilder" component.
######3) optimization of performance and memory consumption - octrees, LOD, OpenCL, etc.
######3) fully generic terrain generation
######4) fully customizable chunks and blocks
######5..N) we'll think of something

## Note
Here and there, there might be parts of the code or even assets present in Voxe which are originaly not mine but taken from one of a million other voxel frameworks I went through in the past few months. Unfortunatelly I can not possibly remember where they all come from. If you happen to identify them and know that there is an issue with using them legally under GPL2.1 license, please, let me know so that I can change the code or ask for approval to use it from the original author or give them credit respectively.
