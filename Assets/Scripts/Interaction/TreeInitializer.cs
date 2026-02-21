using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreeInitializer : MonoBehaviour
{
    [Header("Trees")]
    [SerializeField] private GameObject treeObject1;
    [SerializeField] private GameObject treeObject2;
    [SerializeField] private GameObject treeObject3;
    [SerializeField] private GameObject treeObject4;

    [Header("Bushes")]
    [SerializeField] private GameObject bushObject1;
    [SerializeField] private GameObject bushObject2;

    [Header("Terrain")]
    [SerializeField] Terrain terrain;
    private TerrainData terrainData;

    void Start()
    {
        terrainData = terrain.GetComponent<Terrain>().terrainData;

        foreach (TreeInstance tree in terrainData.treeInstances)
        {
            Vector3 treePosition = Vector3.Scale(tree.position, terrainData.size) + Terrain.activeTerrain.transform.position;

            
            Instantiate(treeObject1, treePosition, Quaternion.identity);
        }

        List<TreeInstance> treeList = new List<TreeInstance>(0);
        terrainData.treeInstances = treeList.ToArray();
    }
}
