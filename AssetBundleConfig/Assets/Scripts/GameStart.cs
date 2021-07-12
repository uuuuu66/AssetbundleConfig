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
        ResourceManager.Instance.Init(this);
    }
    // Start is called before the first frame update
    void Start()
    {
        //同步加载
        //clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
        //m_Audio.clip = clip;
        //m_Audio.Play();
        //异步加载
        ResourceManager.Instance.AsyncLoadResource("Assets/GameData/Sounds/menusound.mp3", OnLoadFinish,LoadResPriority.RES_MIDDLE);
    }

    void OnLoadFinish(string path,Object obj,params object[] param)
    {
        clip = obj as AudioClip;
        m_Audio.clip = clip;

        m_Audio.Play();

    }

    // Update is called once per frame
    void Update()
    {
        //同步卸载
        //if (Input.GetKeyDown(KeyCode.A))
        //{
        //    m_Audio.Stop();
        //    m_Audio.clip = null;
        //    ResourceManager.Instance.ReleaseResourece(clip);
        //    clip = null;
        //}
        //异步卸载
        if (Input.GetKeyDown(KeyCode.A))
        {
            m_Audio.Stop();
            m_Audio.clip = null;
            ResourceManager.Instance.ReleaseResourece(clip,true);
            clip = null;
        }
    }

    private void OnApplicationQuit()
    {
#if UNITY_EDITOR
        Resources.UnloadUnusedAssets();
#endif
    }

    public void Test()
    {
        Debug.Log(111);
    }
}
