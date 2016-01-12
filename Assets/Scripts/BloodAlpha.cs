using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BloodAlpha : MonoBehaviour {

    [Range(0, 60)]
    public int maxFadeSeconds;

    float fadeAmount = 0;
    Image blood;

    void Start()
    {
        blood = GetComponent<Image>();
    }

	void Update () {
        Color color = blood.color;
        color.a -= fadeAmount * Time.deltaTime;
        blood.color = color;

        if (color.a < 0)
            Destroy(this);
	}

    public void SetFade(float healthMissingPercentage)
    {
        this.fadeAmount = 1 / (healthMissingPercentage * maxFadeSeconds);
    }
}
