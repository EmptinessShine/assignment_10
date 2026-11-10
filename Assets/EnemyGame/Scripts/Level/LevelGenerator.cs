using UnityEngine;
using UnityEngine.UI;

// random level: start room + random rooms + exit room, placed in a row.
// rooms are prefabs made by hand in the scene.
public class LevelGenerator : MonoBehaviour
{
    [Header("Rooms")]
    [SerializeField] GameObject startRoom;
    [SerializeField] GameObject exitRoom;
    [SerializeField] GameObject[] middleRooms;
    [SerializeField] int middleRoomCount = 3;
    [SerializeField] float roomWidth = 16f;

    [Header("Enemies")]
    [SerializeField] GameObject[] enemyPrefabs; // 0 patroller, 1 shooter, 2 charger
    [SerializeField] float spawnChance = 0.7f;

    [SerializeField] Text seedText;

    int enemiesSpawned;

    void Awake()
    {
        // same map until you beat it, so tries can be counted
        int seed = PlayerPrefs.GetInt("RandomSeed", 0);
        if (seed == 0)
        {
            seed = Random.Range(1, 10000);
            PlayerPrefs.SetInt("RandomSeed", seed);
        }
        Random.InitState(seed);
        seedText.text = "Карта #" + seed;

        float x = 0;
        Instantiate(startRoom, new Vector3(x, 0, 0), Quaternion.identity, transform);

        for (int i = 0; i < middleRoomCount; i++)
        {
            x += roomWidth;
            GameObject prefab = middleRooms[Random.Range(0, middleRooms.Length)];
            GameObject room = Instantiate(prefab, new Vector3(x, 0, 0), Quaternion.identity, transform);
            SpawnEnemies(room);
        }

        x += roomWidth;
        Instantiate(exitRoom, new Vector3(x, 0, 0), Quaternion.identity, transform);
    }

    void SpawnEnemies(GameObject room)
    {
        SpawnPoint[] points = room.GetComponentsInChildren<SpawnPoint>();

        foreach (SpawnPoint point in points)
        {
            if (Random.value > spawnChance) continue;

            // first three enemies are one of each type, then random
            int type = enemiesSpawned;
            if (type > 2) type = Random.Range(0, enemyPrefabs.Length);

            GameObject enemy = Instantiate(enemyPrefabs[type], point.transform.position, Quaternion.identity);
            enemy.GetComponent<EnemyBase>().Setup(point.route, point.transform.right);
            enemiesSpawned++;
        }
    }
}
