using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class MainMenu : MonoBehaviour
{

    #region variables
    public Slider width;
    public Slider height;
    public Slider enemies;
    public Slider spawnTime;
    public Text resolutionSelectorLabel;
    public Toggle fullscreen;
    public Dropdown vSyncSelector;
    public GameObject map;
    public GameObject spawner;
    #endregion

    void Start () {
        DontDestroyOnLoad(this);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        fullscreen.isOn = Screen.fullScreen;
	}

    public void ChangeResolution()
    {
        int width;
        width = resolutionSelectorLabel.text.TakeWhile(c => c >= '0' && c <= '9').Select(c => c - '0').Aggregate(0, (acc, n) => acc * 10 + n);
        int height;
        height = resolutionSelectorLabel.text.SkipWhile(c => c >= '0' && c <= '9').Skip(3).Select(c => c - '0').Aggregate(0, (acc, n) => acc * 10 + n);
        Screen.SetResolution(width, height, fullscreen.isOn);

        QualitySettings.vSyncCount = vSyncSelector.value;
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
        spawnerScript.secondsBetweenSpawn = spawnTime.value;
        Instantiate(spawner);

        Destroy(this, 2);
    }
}
