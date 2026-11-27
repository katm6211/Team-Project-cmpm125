using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    #region Public Member Variables 
    public GameObject treePrefab;
    public GameObject tree01Prefab;
    public GameObject rockPrefab;
    public GameObject rock01Prefab;
    public Transform floor;
    [Header("Prefab Orientation")]
    public Vector3 rotationOffset = new Vector3(-90f, 0f, 0f); // fixes Z-up models
    #endregion

    /*#region Private Member Variables
    float rangeX = 100f; // Range for random X position
    float rangeZ = 100f; // Range for random Z position
    #endregion */

    #region Singleton
    public static Spawner Instance { get; internal set; }
    #endregion

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SpawnForest();
    }

    public void SpawnForest()
    {
        Renderer floorRenderer = floor.GetComponent<Renderer>();
        if (floorRenderer == null)
        {
            Debug.LogError("Floor needs a Renderer so we can read its bounds.");
            return;
        }

        Bounds floorBounds = floorRenderer.bounds;
        // Instantiate the player
        //player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        // Instantiate tree prefab at random positions around the spawn point
        for (int i = 0; i < 1000; i++)
        {
            GameObject tree = Instantiate(treePrefab, 
                new Vector3(  
                    Random.Range(floorBounds.min.x, floorBounds.max.x),
                    floorBounds.max.y,
                    Random.Range(floorBounds.min.z, floorBounds.max.z)),
                floor.rotation * Quaternion.Euler(rotationOffset));
            tree.transform.localScale = treePrefab.transform.localScale * Random.Range(0.5f, 0.8f); // Randomly scale tree
        }

        // Instantiate the tree01 prefab
        for (int i = 0; i < 120; i++)
        {
            GameObject tree01 = Instantiate(tree01Prefab, new Vector3(
                Random.Range(floorBounds.min.x, floorBounds.max.x),
                floorBounds.max.y,
                Random.Range(floorBounds.min.z, floorBounds.max.z)),
                floor.rotation * Quaternion.Euler(rotationOffset));
            tree01.transform.localScale = tree01Prefab.transform.localScale * Random.Range(0.5f, 0.8f); // Randomly scale the tree
        }

        //  Adjust the scale and instantiate the rock prefab. 
        for (int i = 0; i < 70; i++)
        {
            GameObject rock = Instantiate(rockPrefab, new Vector3(
                Random.Range(floorBounds.min.x, floorBounds.max.x),
                floorBounds.max.y,
                Random.Range(floorBounds.min.z, floorBounds.max.z)),
                floor.rotation * Quaternion.Euler(rotationOffset));
            rock.transform.localScale = rockPrefab.transform.localScale * Random.Range(0.2f, 0.6f); // Randomly scale the rock
        }

        //  Adjust the scale and instantiate the rock01 prefab. 
        for (int i = 0; i < 70; i++)
        {
            GameObject rock01 = Instantiate(rock01Prefab, new Vector3(
                Random.Range(floorBounds.min.x, floorBounds.max.x),
                floorBounds.max.y,
                Random.Range(floorBounds.min.z, floorBounds.max.z)),
                floor.rotation * Quaternion.Euler(rotationOffset));
            rock01.transform.localScale = rock01Prefab.transform.localScale * Random.Range(0.2f, 0.6f); // Randomly scale the rock
        }
    }
}