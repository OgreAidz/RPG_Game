using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDistance = 450;
    public Transform viewer;

    public static Vector2 viewerPosition;
    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunkVisibleLastUpdate = new List<TerrainChunk>();

    void Start(){
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    void Update(){
       viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
       UpdateVisibleChunks(); 
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
                   if (terrainChunkDictionary [viewedChunkCoordinate].IsVisible ()) {
                       terrainChunkVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoordinate]);
                   }
               }else{
                   terrainChunkDictionary.Add (viewedChunkCoordinate, new TerrainChunk(viewedChunkCoordinate, chunkSize, transform));
               }
           }
       } 
    }

    public class TerrainChunk {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        public TerrainChunk(Vector2 coordinate, int size, Transform parent){
            position = coordinate * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x,0,position.y);

            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            meshObject.transform.localScale = Vector3.one * size/10f;
            meshObject.transform.parent = parent;
            SetVisible(false);
        }

        public void UpdateTerrainChunk(){
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
            SetVisible(visible);
        }

        public void SetVisible(bool visible){
            meshObject.SetActive (visible);
        }

        public bool IsVisible(){
            return meshObject.activeSelf;
        }
    }
}
