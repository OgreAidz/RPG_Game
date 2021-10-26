using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float scale = 5f;

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;
    public static float maxViewDistance;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunkVisibleLastUpdate = new List<TerrainChunk>();

    void Start(){
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);

        UpdateVisibleChunks();
    }

    void Update(){
       viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks(){

        for (int i = 0; i < terrainChunkVisibleLastUpdate.Count; i++){
            terrainChunkVisibleLastUpdate [i].SetVisible (false);
        }
        terrainChunkVisibleLastUpdate.Clear ();

       int currentChinkCoordX = Mathf.RoundToInt(viewerPosition.x/chunkSize);
       int currentChinkCoordY = Mathf.RoundToInt(viewerPosition.y/chunkSize);

       for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
           for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
               Vector2 viewedChunkCoordinate = new Vector2(currentChinkCoordX + xOffset, currentChinkCoordY + yOffset);

               if (terrainChunkDictionary.ContainsKey (viewedChunkCoordinate)) {
                   terrainChunkDictionary[viewedChunkCoordinate].UpdateTerrainChunk();
               } else{
                   terrainChunkDictionary.Add (viewedChunkCoordinate, new TerrainChunk(viewedChunkCoordinate, chunkSize, detailLevels, transform, mapMaterial));
               }
           }
       } 
    }

    public class TerrainChunk {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coordinate, int size, LODInfo[] detailLevels, Transform parent, Material material){
            this.detailLevels = detailLevels;

            position = coordinate * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x,0,position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk(){
            if (mapDataReceived)
            {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    terrainChunkVisibleLastUpdate.Add(this);
                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible){
            meshObject.SetActive (visible);
        }

        public bool IsVisible(){
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();

        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceieved);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreshold;
    }
}
