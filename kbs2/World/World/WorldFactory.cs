﻿using kbs2.World.Chunk;
using System.Collections.Generic;

namespace kbs2.World.World
{
    public static class WorldFactory
    {
        public static FastNoise.NoiseType Noise;

        /// <summary>
        /// The default function to generate a new world.
        /// </summary>
        /// <returns>Return a world made with terrain generation</returns>
        public static WorldController GetNewWorld(FastNoise.NoiseType noise)
        {
            // Set the generation noise type
            Noise = noise;

            // Initialize World 
            WorldController world = new WorldController();

            // Initialize WorldModel and ChunkGrid in World so you can add chunks to this grid
            world.WorldModel = new WorldModel
            {
                ChunkGrid = new Dictionary<Coords, WorldChunkController>()
            };

            // For loop to add chunks from -2 to +2 in both x and y directions. this makes for a 5x5 chunkmap.
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    // Sets the coords for the new chunk
                    Coords coords = new Coords {x = x, y = y};

                    // Initializes a new chunk with the set coords and a basic terrain type
                    WorldChunkController chunkController = WorldChunkFactory.ChunkOfDefaultTerrain(coords);

                    // Adds the new chunk to the worldgrid
                    world.WorldModel.ChunkGrid.Add(coords, chunkController);
                }
            }

            // Returns the worldController with a 5x5 chunkGrid initialized
            return world;
        }

        /// <summary>
        /// Debugging function for getting a world of empty chunks
        /// </summary>
        /// <returns>Returns a world of 5x5 chuncks that are default terrain </returns>
        public static WorldController GetNewEmptyWorld()
        {
            // Initialize World 
            WorldController world = new WorldController();

            // Initialize WorldModel and ChunkGrid in World so you can add chunks to this grid
            world.WorldModel = new WorldModel
            {
                ChunkGrid = new Dictionary<Coords, WorldChunkController>()
            };

            // Returns the initialized worldController without chunks loaded
            return world;
        }
    }
}