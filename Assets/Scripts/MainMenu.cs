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
        UnityEngine.SceneManagement.SceneManager.LoadScene("RandomLevel");

        yield return new WaitForSeconds(1);

        MapGenerator mapGen = map.GetComponent<MapGenerator>();
        mapGen.width = (int) width.value;
        mapGen.height = (int) height.value;
        mapGen.smallestRoomRegion = Mathf.Max(Mathf.Min(mapGen.width, mapGen.height) / 6, 20);
        mapGen.smallestWallRegion = Mathf.Max(Mathf.Min(mapGen.width, mapGen.height) / 10, 10);
        Instantiate(map);

        EnemySpawner spawnerScript = spawner.GetComponent<EnemySpawner>();
        spawnerScript.maxEnemies = (int) enemies.value;
        Instantiate(spawner);

        Destroy(this, 2);
    }
}
