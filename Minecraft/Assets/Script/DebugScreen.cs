using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    World world;
    Text text;

    float frameRate;
    float timer;

    int halfwolrdSizeInVoxles;
    int halfwolrdSizeInChunks;

    // Start is called before the first frame update
    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<Text>();

        halfwolrdSizeInVoxles = VoxelData.WorldSizeInVoxels / 2;
        halfwolrdSizeInChunks = VoxelData.WorldSizeInChunks / 2;
    }

    // Update is called once per frame
    void Update()
    {
        string debugText = "psy, Code a Game Like Minecraft in unity";
        debugText += "\n";
        debugText += frameRate + "fps";
        debugText += "\n\n";
        debugText += "xyz: " +
            (Mathf.FloorToInt(world.player.transform.position.x) - halfwolrdSizeInVoxles) + " / " +
            (Mathf.FloorToInt(world.player.transform.position.y)) + " / " +
            (Mathf.FloorToInt(world.player.transform.position.z) - halfwolrdSizeInVoxles);
        debugText += "\n";
        debugText += "Chunk: " + (world.playerChunkCoord.x - halfwolrdSizeInChunks)+ " / " + (world.playerChunkCoord.z - halfwolrdSizeInChunks);

        text.text = debugText;

        if(timer > 1f)
        {
            frameRate = (float)(1f / Time.deltaTime);
            timer = 0;
        }
        else
        {
            timer += Time.deltaTime;
        }
    }
}
