using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour {

    public GameObject[] enemies;
    [Range(0, 10)]
    public float secondsBetweenSpawn;
    [Range(0, 100)]
    public int maxEnemies;
    public bool spawnEnemies;

    List<Vector3> spawnPoints;
    float time;
    public int currentEnemies;

	void Start () {
        spawnPoints = GameObject.FindGameObjectWithTag("Map").GetComponent<MapGenerator>().GetSpawnPoints();
        time = 0;
        currentEnemies = 0;
	}
	
	void Update () {
        time += Time.deltaTime;

        if (time > secondsBetweenSpawn)
        {
            if(spawnEnemies)
                SpawnEnemy();
            time = 0;
        }
	}

    void SpawnEnemy()
    {
        if (currentEnemies < maxEnemies)
        {
            Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Count)];
            GameObject enemy = enemies[Random.Range(0, enemies.Length)];
            GameObject instantiatedEnemy = Instantiate(enemy, spawnPos, Quaternion.identity) as GameObject;
            instantiatedEnemy.transform.parent = transform;
            instantiatedEnemy.tag = "Enemy";
            currentEnemies++;
        }
        //else
            //currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
    }
}
