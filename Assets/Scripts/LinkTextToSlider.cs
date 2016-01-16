using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LinkTextToSlider : MonoBehaviour
{

    #region variables
    public Slider slider;

    Text text;
    #endregion

    void Start()
    {
        text = GetComponent<Text>();

        ChangeValue();
    }

	public void ChangeValue () {
        text.text = slider.value.ToString();
	}
}
