using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{

    #region variables
    public Slider width;
    public Slider height;
    public Slider enemies;
    public GameObject map;
    public GameObject spawner;
    #endregion

    void Start () {
        DontDestroyOnLoad(this);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

        Destroy(this, 2);
    }
}
