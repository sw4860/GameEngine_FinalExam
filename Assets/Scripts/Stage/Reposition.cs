using System.Collections.Generic;
using UnityEngine;

public class Reposition : MonoBehaviour
{
    private Transform playerTransform;
    public float tileSize = 20f; 
    public int gridCount = 2;

    [Header("Prop Pooling")]
    public GameObject[] propPrefabs; // Assign in Inspector
    public int propCountPerTile = 3;
    private List<GameObject> myProps = new List<GameObject>();

    void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    void Start()
    {
        // Pre-create props for this tile
        for (int i = 0; i < propCountPerTile; i++)
        {
            GameObject prop = Instantiate(propPrefabs[Random.Range(0, propPrefabs.Length)], transform);
            prop.SetActive(false);
            myProps.Add(prop);
        }
        
        RandomizeProps();
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;
        Vector3 myPos = transform.position;

        float diffX = playerPos.x - myPos.x;
        float diffY = playerPos.y - myPos.y;

        float absDiffX = Mathf.Abs(diffX);
        float absDiffY = Mathf.Abs(diffY);

        if (transform.CompareTag("Ground"))
        {
            if (absDiffX > tileSize)
            {
                float dirX = diffX > 0 ? 1 : -1;
                transform.Translate(Vector3.right * dirX * tileSize * gridCount);
                RandomizeProps();
            }
            
            if (absDiffY > tileSize)
            {
                float dirY = diffY > 0 ? 1 : -1;
                transform.Translate(Vector3.up * dirY * tileSize * gridCount);
                RandomizeProps();
            }
        }
    }

    private void RandomizeProps()
    {
        float halfSize = tileSize / 2f;
        
        foreach (GameObject prop in myProps)
        {
            // Randomly decide to show or hide
            bool show = Random.value > 0.5f;
            prop.SetActive(show);

            if (show)
            {
                prop.transform.position = transform.position + new Vector3(
                    Random.Range(-halfSize + 1f, halfSize - 1f),
                    Random.Range(-halfSize + 1f, halfSize - 1f),
                    0
                );
            }
        }
    }
}
