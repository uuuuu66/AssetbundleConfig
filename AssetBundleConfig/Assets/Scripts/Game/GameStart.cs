using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameStart : MonoBehaviour
{
    private GameObject m_obj;

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
        //同步obj加载
        //obj = ObjectManager.Instance.InstantiateObject("Assets/GameData/Prefabs/Attack.prefab");
        //异步Obj加载
        //ObjectManager.Instance.InstantiateObjectAsync("Assets/GameData/Prefabs/Attack.prefab", OnAsyncLoadFinish,LoadResPriority.RES_HIGHT,true);
        //obj预加载
        //ObjectManager.Instance.PreloadGameObject("Assets/GameData/Prefabs/Attack.prefab", 20);

        #region 跳场景增加UI
        UIManager.Instance.Init(transform.Find("UIRoot") as RectTransform, transform.Find("UIRoot/WindowRoot") as RectTransform,transform.Find("UIRoot/UICamera").GetComponent<Camera>(),transform.Find("UIRoot/EventSystem").GetComponent<EventSystem>());
        RegisterUI();

        GameMapManager.Instance.Init(this);
        //obj预加载
        ObjectManager.Instance.PreloadGameObject(ConstString.AttackPrefab, 5);
        //资源预加载
        //ResourceManager.Instance.PreloadRes(ConstString.MenuSound);
        AudioClip clip = ResourceManager.Instance.LoadResource<AudioClip>(ConstString.MenuSound);
        ResourceManager.Instance.ReleaseResourece(clip);
        //obj加载跳场景
        //GameObject obj = ObjectManager.Instance.InstantiateObject(ConstString.AttackPrefab, true,bClear:false);
        //ObjectManager.Instance.ReleaseObject(obj);
        //obj = null;


        GameMapManager.Instance.LoadScene(ConstString.MenuScene);
        #endregion
    }

    /// <summary>
    /// 注册UI
    /// </summary>
    void RegisterUI()
    {
        UIManager.Instance.Register<MenuUi>(ConstString.MenuPanel);
        UIManager.Instance.Register<LoadingUI>(ConstString.LoadingPanel);
    }

    void OnAsyncLoadFinish(string path, Object obj, params object[] param)
    {
        m_obj = obj as GameObject;
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
            clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/menusound.mp3");
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
       
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ObjectManager.Instance.ReleaseObject(m_obj);
            m_obj = null;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            ObjectManager.Instance.ReleaseObject(m_obj,0,true);
            m_obj = null;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            m_obj = ObjectManager.Instance.InstantiateObject("Assets/GameData/Prefabs/Attack.prefab", true);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ObjectManager.Instance.InstantiateObjectAsync("Assets/GameData/Prefabs/Attack.prefab", OnAsyncLoadFinish, LoadResPriority.RES_HIGHT,true);
        }

        UIManager.Instance.OnUpdate();
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
