using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class World : MonoBehaviour
{
    public int seed;
    public BiomeAttributes biome;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;
    public BlockType[] blockTypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunk = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    List<Chunk> chunksToUpdate = new List<Chunk>();
    private bool isCreatingChunks;

    Queue<VoxelMod> modification = new Queue<VoxelMod>();

    public GameObject debugScreen;

    private void Start()
    {
        Random.InitState(seed);

        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight - 10, (VoxelData.WorldSizeInChunks * VoxelData.chunkWidth) / 2f);
        Debug.Log("spawnpsoition " + spawnPosition);
        GenrateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if (chunksToCreate.Count > 0 && !isCreatingChunks)
            StartCoroutine("CreateChunks");

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);

    }

    void GenrateWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; z++)
            {
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this,true);
                activeChunk.Add(new ChunkCoord(x, z));
            }
        }

        while (modification.Count > 0)
        {
            VoxelMod v = modification.Dequeue();

            ChunkCoord c = GetChunkCoordFromVector3(v.position);

            if (chunks[c.x, c.z] == null)
            {
                chunks[c.x, c.z] = new Chunk(c, this, true);
                activeChunk.Add(c);
            }
            chunks[c.x, c.z].modification.Enqueue(v);

            if (!chunksToUpdate.Contains(chunks[c.x, c.z]))
                chunksToUpdate.Add(chunks[c.x, c.z]);
        }

        for (int i = 0; i < chunksToUpdate.Count; i++)
        {
            chunksToUpdate[0].UpdateChunk();
            chunksToUpdate.RemoveAt(0);
        }

        player.position = spawnPosition;
    }

    IEnumerator CreateChunks()
    {
        isCreatingChunks = true;

        while(chunksToCreate.Count > 0)
        {
            chunks[chunksToCreate[0].x, chunksToCreate[0].z].Init();
            chunksToCreate.RemoveAt(0);
            yield return null;
        }

        isCreatingChunks = false;
    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3 (Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);
        return chunks[x, z];
    }

    void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;
        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunk);
        activeChunk.Clear();

        for (int x = coord.x - VoxelData.ViewDistanceInChunks; x < coord.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.ViewDistanceInChunks; z < coord.z + VoxelData.ViewDistanceInChunks; z++)
            {
                if (IsChunkInWorld(new ChunkCoord(x, z)))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                        chunksToCreate.Add(new ChunkCoord(x, z));
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    activeChunk.Add(new ChunkCoord(x, z));
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        foreach (ChunkCoord c in previouslyActiveChunks)
        {
            chunks[c.x, c.z].isActive = false;
        }
    }

    public bool CheckForVoxel(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsVoxelInWorld(pos) || pos.y < 0 || pos.y > VoxelData.chunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelfromGlobalVector3(pos)].isSolid;

        return blockTypes[GetVoxel(pos)].isSolid;
    }

    public bool CheckForVoxelTransparent(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsVoxelInWorld(pos) || pos.y < 0 || pos.y > VoxelData.chunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelfromGlobalVector3(pos)].isTransparent;

        return blockTypes[GetVoxel(pos)].isTransparent;
    }
    
    public byte GetVoxel (Vector3 pos)
    {
        int yPos = Mathf.FloorToInt(pos.y);

        /* IMMUTABLE PASS */

        // If outside world, retain air
        if (!IsVoxelInWorld(pos))
            return 0;

        //IF bottom block of chunk, return bedrock
        if (yPos == 0)
            return 1;


        /* BAIC TERAIN PASS */
        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.TerrainScale )) + biome.solidGroundHeight;
        byte voxelValue = 0;

        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = 5;
        else if (yPos > terrainHeight)
            return 0;
        else
            voxelValue = 2;

        /* SECOND PASS*/
        if (voxelValue == 2)
        {
            foreach (Lode lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
            }
        }

        /* TREE PASS*/
        if (yPos == terrainHeight)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treeZoneScale) > biome.treeZoneThreshold)
            {
                voxelValue = 1;
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treePlacementScale) > biome.treePlacementThreshold)
                {
                    voxelValue = 8;
                    modification.Enqueue(new VoxelMod(new Vector3(pos.x, pos.y + 1, pos.z), 6));
                }
            }
        }

        return voxelValue;
    }


    bool IsChunkInWorld(ChunkCoord coord)
        => (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1);

    bool IsVoxelInWorld(Vector3 pos) 
        => (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels );
}



[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;
    public bool isTransparent;
    public Sprite icon;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    //Back Front Top Bottom Left Right

    public int GetTexutureID (int faceTexture)
    {
        switch (faceTexture)
        {
            case 0: return backFaceTexture;
            case 1: return frontFaceTexture;
            case 2: return topFaceTexture;
            case 3: return bottomFaceTexture;
            case 4: return leftFaceTexture;
            case 5: return rightFaceTexture;

            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;
        }
    }
}

public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }

    public VoxelMod(Vector3 position, byte id)
    {
        this.position = position;
        this.id = id;
    }

}

