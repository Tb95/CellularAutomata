using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LinkTextToSlider : MonoBehaviour {

    public Slider slider;

    Text text;
    void Start()
    {
        text = GetComponent<Text>();

        ChangeValue();
    }

	public void ChangeValue () {
        text.text = slider.value.ToString();
	}
}
