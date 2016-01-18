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
        if (slider.wholeNumbers)
            text.text = slider.value.ToString();
        else
            text.text = string.Format("{0:0.##}", slider.value);
	}
}
