using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameMapManager : Singleton<GameMapManager>
{
    //加载场景完成回调
    public Action LoadSceneCompleteCallBack;

    //加载场景开始回调
    public Action LoadSceneEnterCallBack;


    /// <summary>
    /// 当前场景名
    /// </summary>
    public string CurrentMapName
    {
        get;
        set;
    }

    //场景加载是否完成
    public bool AlreadyLoadScene
    {
        get;
        set;
    }

    //切换场景进度条
    public static int LoadingProgress = 0;

    private MonoBehaviour m_Mono;

    /// <summary>
    /// 场景管理初始化
    /// </summary>
    /// <param name="mono"></param>
    public void Init(MonoBehaviour mono)
    {
        m_Mono = mono;
    }

    /// <summary>
    /// 加载场景
    /// </summary>
    /// <param name="name">场景名</param>
    public void LoadScene(string name)
    {
        LoadingProgress = 0;
        m_Mono.StartCoroutine(LoadingSceneAsync(name));
    }

    /// <summary>
    /// 设置场景环境
    /// </summary>
    /// <param name="name"></param>
    void SetSceneSetting(string name)
    {
        //设置各种场景环境，可以根据配表来操作（比如场景的雾，摄像机 TODO:
    }

    IEnumerator LoadingSceneAsync(string name)
    {
       
        LoadSceneEnterCallBack?.Invoke();
        
        ClearCache();
        AlreadyLoadScene = false;
        AsyncOperation unLoadScene = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Single);

        while (unLoadScene != null && !unLoadScene.isDone)
        {
            yield return new WaitForEndOfFrame();
        }

        LoadingProgress = 0;
        int targetProgress = 0;
        AsyncOperation asyncScene = SceneManager.LoadSceneAsync(name);
        if (asyncScene != null && !asyncScene.isDone)
        {
            asyncScene.allowSceneActivation = false;
            while (asyncScene.progress<0.9f)
            {
                targetProgress = (int)asyncScene.progress * 100;
                yield return new WaitForEndOfFrame();
                //平滑过渡
                while (LoadingProgress < targetProgress)
                {
                    ++LoadingProgress;
                    yield return new WaitForEndOfFrame();
                }
            }

            CurrentMapName = name;
            SetSceneSetting(name);
            //自行加载剩余10%
            targetProgress = 100;
            while (LoadingProgress < targetProgress - 2)
            {
                ++LoadingProgress;
                yield return new WaitForEndOfFrame();
            }

            LoadingProgress = 100;
            asyncScene.allowSceneActivation = true;
            AlreadyLoadScene = true;
            LoadSceneCompleteCallBack?.Invoke();
        }

        yield return null;
    }

    /// <summary>
    /// 跳场景需要清除的东西
    /// </summary>
    private void ClearCache()
    {
        ObjectManager.Instance.ClearCache();
        ResourceManager.Instance.ClearCache();
    }
}
