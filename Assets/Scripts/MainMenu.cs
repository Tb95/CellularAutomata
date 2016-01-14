using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour {

    public Slider width;
    public Slider height;
    public Slider enemies;
    public GameObject map;
    public GameObject spawner;

	void Start () {
        DontDestroyOnLoad(this);
	}

    public void Play()
    {
        StartCoroutine(Load());
    }

    public IEnumerator Load()
    {
        Application.LoadLevel("RandomLevel");

        yield return new WaitForSeconds(1);

        MapGenerator mapGen = map.GetComponent<MapGenerator>();
        mapGen.width = (int) width.value;
        mapGen.height = (int) height.value;
        Instantiate(map);

        EnemySpawner spawnerScript = spawner.GetComponent<EnemySpawner>();
        spawnerScript.maxEnemies = (int) enemies.value;
        Instantiate(spawner);
    }
}
