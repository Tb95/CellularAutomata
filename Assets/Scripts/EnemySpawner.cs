using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour {

    public GameObject[] enemies;
    [Range(0, 10)]
    public float secondsBetweenSpawn;

    List<Vector3> spawnPoints;
    float time;

	void Start () {
        spawnPoints = GetComponent<MapGenerator>().GetSpawnPoints();
        time = 0;
	}
	
	void Update () {
        time += Time.deltaTime;

        if (time > secondsBetweenSpawn)
        {
            SpawnEnemy();
            time = 0;
        }
	}

    void SpawnEnemy()
    {
        Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject enemy = enemies[Random.Range(0, enemies.Length)];
        Instantiate(enemy, spawnPos, Quaternion.identity);
    }
}
