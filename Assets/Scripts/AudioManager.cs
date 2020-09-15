using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[ExecuteInEditMode]
public class AudioManager : MonoBehaviour
{
    //public Sound[] sounds;
    public AudioSource[] sources;

   
    private void OnEnable()
    {
        sources = GetComponents<AudioSource>();
    }

    //public static AudioManager instance;
    void Awake()
    {
        /*
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        */

        /*
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
        */
    }
    public void Play_gen()
    {
        sources[1].Play();
    }

    public void Stop_gen()
    {
        sources[1].Pause();
    }

    public void Play_place()
    {
        sources[2].Play();
    }

    private void Start()
    {
        
        //Play("MusiqueAmbiance");
    }
    /*
    public void Play (string name)
    {

        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogError("Sound : " + name + "not found ");
            return;
        }

        s.source.Play();
        
    }
    */
    
}
