using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/**Sources
 * https://www.youtube.com/watch?v=YOaYQrN1oYQ&ab_channel=Brackeys
 * https://gamedevbeginner.com/the-right-way-to-make-a-volume-slider-in-unity-using-logarithmic-conversion/
 **/
public class Settings : MonoBehaviour
{
    public Slider masterSlider, musicSlider, sfxSlider;
    public AudioMixer audioMixer;

    void Start()
    {
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    public void setMasterVolume (float masterVolume)
    {
        audioMixer.SetFloat("master", Mathf.Log10(masterVolume)*20);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
    }

    public void setMusicVolume(float musicVolume)
    {
        audioMixer.SetFloat("music", Mathf.Log10(musicVolume)*20);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void setSFXVolume(float sfxVolume)
    {
        audioMixer.SetFloat("sfx", Mathf.Log10(sfxVolume)*20);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }
}
