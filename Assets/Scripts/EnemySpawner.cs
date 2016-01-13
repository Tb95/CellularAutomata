using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{

    #region variables
    public GameObject[] enemies;
    [Range(0, 10)]
    public float secondsBetweenSpawn;
    [Range(0, 100)]
    public int maxEnemies;
    public bool spawnEnemies;

    List<Vector3> spawnPoints;
    List<EnemyController>[] reusableEnemies;
    float time;
    public int currentEnemies;
    #endregion

    void Start () {
        spawnPoints = GameObject.FindGameObjectWithTag("Map").GetComponent<MapGenerator>().GetSpawnPoints();

        reusableEnemies = new List<EnemyController>[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            reusableEnemies[i] = new List<EnemyController>();
        }

        foreach (var enemy in enemies)
        {
            enemy.GetComponent<EnemyController>().SetSpawner(this);
        }

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
            int index = Random.Range(0, enemies.Length);

            GameObject instantiatedEnemy;
            if (reusableEnemies[index].Count > 0)
            {
                EnemyController controller = reusableEnemies[index][0];
                reusableEnemies[index].RemoveAt(0);

                controller.gameObject.SetActive(true);
                controller.Initialize();
                controller.transform.position = spawnPos;

                instantiatedEnemy = controller.gameObject;
            }
            else
                instantiatedEnemy = Instantiate(enemies[index], spawnPos, Quaternion.identity) as GameObject;

            instantiatedEnemy.transform.parent = transform;
            instantiatedEnemy.tag = "Enemy";
            instantiatedEnemy.GetComponent<EnemyController>().SetSpawner(this);
            currentEnemies++;
        }
    }

    public void DiedEnemy(GameObject enemy)
    {
        currentEnemies--;

        enemy.transform.position = new Vector3(Random.Range(-1000, 1000), 1000, Random.Range(-1000, 1000));
        EnemyController controller = enemy.GetComponent<EnemyController>();
        enemy.SetActive(false);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (controller.type == enemies[i].GetComponent<EnemyController>().type)
                reusableEnemies[i].Add(controller);
        }
    }
}
