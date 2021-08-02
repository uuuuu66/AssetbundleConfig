using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum UIMsgID
{
    None = 0,

}

public class UIManager : Singleton<UIManager>
{
    //UI节点
    public RectTransform m_UIRoot;
    //窗口节点
    private RectTransform m_WindowRoot;
    //UI摄像机
    private Camera m_UICamera;
    //EventSystem节点
    private EventSystem m_EventSystem;
    //屏幕的宽高比
    private float m_CanvasRate = 0;

    private const string UIPrefabPath = "Assets/GameData/Prefabs/UGUI/Panel/";
    //注册的字典
    private Dictionary<string, System.Type> m_RegisterDic = new Dictionary<string, System.Type>();
    //所有打开窗口
    private Dictionary<string, Window> m_WindowDic = new Dictionary<string, Window>();
    //打开的窗口列表
    private List<Window> m_WindowList = new List<Window>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="uiRoot">UI父节点</param>
    /// <param name="windowRoot">窗口父节点</param>
    /// <param name="uiCamera">UI摄像机</param>
    public void Init(RectTransform uiRoot,RectTransform windowRoot,Camera uiCamera,EventSystem eventSystem)
    {
        this.m_UIRoot = uiRoot;
        this.m_WindowRoot = windowRoot;
        this.m_UICamera = uiCamera;
        this.m_EventSystem = eventSystem;
        this.m_CanvasRate = Screen.height / (m_UICamera.orthographicSize * 2);
    }

    /// <summary>
    /// 显示或者隐藏所有UI
    /// </summary>
    public void ShowOrHideUI(bool show)
    {
        if (m_UIRoot != null)
        {
            m_UIRoot.gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// 设置默认选择对象
    /// </summary>
    /// <param name="obj"></param>
    public void SetNormalSelectObj(GameObject obj)
    {
        if (m_EventSystem == null)
        {
            m_EventSystem = EventSystem.current;
        }
        m_EventSystem.firstSelectedGameObject = obj;
    }

    public void OnUpdate()
    {
        for (int i = 0; i < m_WindowList.Count; i++)
        {
            if (m_WindowList[i] != null)
            {
                m_WindowList[i].OnUpdate();
            }
        }
    }

    /// <summary>
    /// 窗口注册方法
    /// </summary>
    /// <typeparam name="T">窗口泛型类</typeparam>
    /// <param name="name">窗口名</param>
    public void Register<T>(string name) where T : Window
    {
        m_RegisterDic[name] = typeof(T);
    }

    /// <summary>
    /// 发送消息给窗口
    /// </summary>
    /// <param name="name">窗口名</param>
    /// <param name="msgID">消息ID</param>
    /// <param name="paras">参数数组</param>
    /// <returns></returns>
    public bool SendMessageToWindow(string name,UIMsgID msgID = 0,params object[] paras)
    {
        Window window = FindWindowByName<Window>(name);
        if (window != null)
        {
            return window.OnMessage(msgID, paras);
        }
        return false;
    }

    /// <summary>
    /// 根据窗口名字查找窗口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public T FindWindowByName<T>(string name)where T:Window
    {
        Window window = null;
        if (m_WindowDic.TryGetValue(name, out window))
        {
            return (T)window;
        }
        return null;
    }


    public Window PopUpWindow(string windowName,bool beTop = true,params object[] paras)
    {
        Window window = FindWindowByName<Window>(windowName);
        if (window == null)
        {
            System.Type tp = null;
            if (m_RegisterDic.TryGetValue(windowName, out tp))
            {
                window = System.Activator.CreateInstance(tp) as Window;
            }
            else
            {
                Debug.LogError("找不到窗口对应的脚本,窗口名是：" + windowName);
                return null;
            }
            GameObject windowObj = ObjectManager.Instance.InstantiateObject(UIPrefabPath + windowName, false, false);
            if (windowObj == null)
            {
                Debug.LogError("创建窗口prefab失败：" + windowName);
                return null;
            }
            if (!m_WindowDic.ContainsKey(windowName))
            {
                m_WindowDic.Add(windowName, window);
                m_WindowList.Add(window);
            }

            window.GameObject = windowObj;
            window.Transform = windowObj.transform;
            window.Name = windowName;
            window.Awake(paras);
            windowObj.transform.SetParent(m_WindowRoot, false);

            if (beTop)
            {
                window.Transform.SetAsLastSibling();
            }

            window.OnShow(paras);
        }
        else
        {
            ShowWindow(windowName,beTop,paras);
        }

        return window;
    }
    /// <summary>
    /// 根据窗口名字关闭窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="destory"></param>
    public void CloseWindow(string name,bool destory = false)
    {
        Window window = FindWindowByName<Window>(name);
        CloseWindow(window, destory);
    }

    /// <summary>
    /// 根据窗口对象关闭窗口
    /// </summary>
    /// <param name="window"></param>
    /// <param name="destory"></param>
    public void CloseWindow(Window window, bool destory = false)
    {
        if (window != null)
        {
            window.OnDisable();
            window.OnClose();
            if (m_WindowDic.ContainsKey(window.Name))
            {
                m_WindowDic.Remove(window.Name);
                m_WindowList.Remove(window);
            }

            if (destory)
            {
                ObjectManager.Instance.ReleaseObject(window.GameObject, 0, true);
            }
            else
            {
                ObjectManager.Instance.ReleaseObject(window.GameObject, recycleParent: false);
            }
            window.GameObject = null;
            window = null;
        }
    }

    /// <summary>
    /// 关闭所有窗口
    /// </summary>
    public void CloseAllWindow()
    {
        for (int i = m_WindowList.Count; i >=0; i--)
        {
            CloseWindow(m_WindowList[i]);
        }
    }

    /// <summary>
    /// 切换到唯一窗口
    /// </summary>
    public void SwitchStateByName(string name,bool beTop=true,params object[] paras)
    {
        CloseAllWindow();
        PopUpWindow(name, beTop, paras);
    }

    /// <summary>
    /// 根据名字隐藏窗口
    /// </summary>
    /// <param name="name"></param>
    public void HideWindow(string name)
    {
        Window window = FindWindowByName<Window>(name);
        HideWindow(window);
    }

    /// <summary>
    /// 根据窗口对象隐藏窗口
    /// </summary>
    /// <param name="window"></param>
    public void HideWindow(Window window)
    {
        if (window != null)
        {
            window.GameObject.SetActive(false);
            window.OnDisable();
        }
    }

    /// <summary>
    /// 根据窗口名字显示窗口
    /// </summary>
    /// <param name="name">名字</param>
    /// <param name="beTop">是否在最上层</param>
    /// <param name="paras">参数</param>
    public void ShowWindow(string name,bool beTop=true,params object[] paras)
    {
        Window window = FindWindowByName<Window>(name);
        ShowWindow(window,beTop, paras);
    }

    public void ShowWindow(Window window, bool beTop = true, params object[] paras)
    {
        if (window != null)
        {
            if (window.GameObject != null && !window.GameObject.activeSelf)
            {
                window.GameObject.SetActive(true);
            }
            if (beTop)
            {
                window.Transform.SetAsLastSibling();
            }
            window.OnShow(paras);
        }
    }
}
