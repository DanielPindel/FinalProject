using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public Slider volumeSlider;
    public Slider durationSlider;
    public TextMeshProUGUI volumeText;
    public TextMeshProUGUI durationText;

    void Start()
    {
        volumeSlider.value = PlayerPrefs.GetInt("Volume", 20);
        durationSlider.value = PlayerPrefs.GetInt("GameDuration", 12);

        UpdateVolumeText();
        UpdateDurationText();
    }

    public void OnVolumeChanged()
    {
        PlayerPrefs.SetInt("Volume", (int)volumeSlider.value);
        UpdateVolumeText();
    }

    public void OnDurationChanged()
    {
        PlayerPrefs.SetInt("GameDuration", (int)durationSlider.value);
        UpdateDurationText();
    }    

    private void UpdateVolumeText()
    {
        volumeText.text = volumeSlider.value.ToString();
    }

    private void UpdateDurationText()
    {
        durationText.text = durationSlider.value.ToString();
    }
}
