using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    private GameObject obj;

    public AudioSource m_Audio;
    private AudioClip clip;
    void Awake()
    {
        GameObject.DontDestroyOnLoad(gameObject);
        AssetBundleManager.Instance.LoadAssetBundleConfig();
        ResourceManager.Instance.Init(this);
        ObjectManager.Instance.Init(transform.Find("RecyclePoolTrs"), transform.Find("SceneTrs"));
    }
    // Start is called before the first frame update
    void Start()
    {
        //预加载
        //ResourceManager.Instance.PreloadRes("Assets/GameData/Sounds/senlin.mp3");
        //同步加载
        //clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
        //m_Audio.clip = clip;
        //m_Audio.Play();
        //异步加载
        //ResourceManager.Instance.AsyncLoadResource("Assets/GameData/Sounds/menusound.mp3", OnLoadFinish,LoadResPriority.RES_MIDDLE);
        obj = ObjectManager.Instance.InstantiateObject("Assets/GameData/Prefabs/Attack.prefab");

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

        //预加载使用
        if (Input.GetKeyDown(KeyCode.S))
        {
            long Time = System.DateTime.Now.Ticks;
            clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
            Debug.Log("预加载时间：" + (System.DateTime.Now.Ticks - Time));
            m_Audio.clip = clip;
            m_Audio.Play();
        }
        //预加载卸载
        if (Input.GetKeyDown(KeyCode.D))
        {
            ResourceManager.Instance.ReleaseResourece(clip,true);
            m_Audio.clip = null;
            clip = null;
        }
        //对象同步加载
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ObjectManager.Instance.ReleaseObject(obj);
            obj = null;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            ObjectManager.Instance.ReleaseObject(obj,0,true);
            obj = null;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            obj = ObjectManager.Instance.InstantiateObject("Assets/GameData/Prefabs/Attack.prefab", true);
        }
    }

    private void OnApplicationQuit()
    {
#if UNITY_EDITOR
        ResourceManager.Instance.ClearCache();
        Resources.UnloadUnusedAssets();
#endif
    }

    public void Test()
    {
        Debug.Log(111);
    }
}
