using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    public AudioSource m_Audio;
    private AudioClip clip;
    void Awake()
    {
        AssetBundleManager.Instance.LoadAssetBundleConfig();
    }
    // Start is called before the first frame update
    void Start()
    {
        clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
        m_Audio.clip = clip;
        m_Audio.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            m_Audio.Stop();
            m_Audio.clip = null;
            ResourceManager.Instance.ReleaseResourece(clip);
            clip = null;
        }
    }

    

    public void Test()
    {
        Debug.Log(111);
    }
}
