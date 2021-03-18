using UnityEngine.Audio;
using System;
using UnityEngine;
//using System.Media;

/**Sources
 * https://www.youtube.com/watch?v=6OT43pvUyfY
 * https://www.youtube.com/watch?v=YOaYQrN1oYQ
 **/
public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;
    public static AudioManager instance;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.outputAudioMixerGroup = s.group;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Play("MenuMusic", true, 1);
    }

    public void Play(string name, bool willPlay, float pitch)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);

        if (s == null)
        {
            Debug.LogWarning("Sound " + name + " not found!");
            return;
        }

        s.source.pitch = pitch;

        if (willPlay)
            s.source.Play();
        else
            s.source.Stop();
    }
}
