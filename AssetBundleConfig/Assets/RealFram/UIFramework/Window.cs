using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Window
{
    //引用Gameobject
    public GameObject GameObject
    {
        get;
        set;
    }
    //引用Transform
    public Transform Transform
    {
        get;
        set;
    }
    //名字
    public string Name
    {
        get;
        set;
    }

    //所有Button
    protected List<Button> m_AllButton = new List<Button>();

    //所有Toggle
    protected List<Toggle> m_AllToggle = new List<Toggle>();

    public virtual bool OnMessage(UIMsgID msgID,params object[] paras)
    {
        return true;
    }


    public virtual void Awake(params object[] paras)
    {

    }

    public virtual void OnShow(params object[] paras)
    {

    }

    public virtual void OnDisable()
    {

    }

    public virtual void OnUpdate()
    {

    }

    public virtual void OnClose()
    {
        RemoveAllButtonListener();
        RemoveAllToggleListener();
        m_AllButton.Clear();
        m_AllToggle.Clear();
    }

    /// <summary>
    /// 同步替换图片
    /// </summary>
    /// <param name="path"></param>
    /// <param name="image"></param>
    /// <param name="setNativeSize"></param>
    /// <returns></returns>
    public bool ChangeImageSprite(string path,Image image,bool setNativeSize = false)
    {
        if (image == null)
        {
            Debug.LogError("Image组件为空！");
            return false;
        }

        Sprite sp = ResourceManager.Instance.LoadResource<Sprite>(path);
        if (sp != null)
        {
            if (image.sprite != null)
            {
                image = null;
            }
            image.sprite = sp;
            if (setNativeSize)
            {
                image.SetNativeSize();
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 异步改变图片
    /// </summary>
    /// <param name="path"></param>
    /// <param name="image"></param>
    /// <param name="setNativeSize"></param>
    public void ChangeImageSpriteAsync(string path, Image image, bool setNativeSize = false)
    {
        if (image == null)
        {
            Debug.LogError("Image组件为空！");
            return;
        }

        ResourceManager.Instance.AsyncLoadResource(path, OnLoadSpriteComplete, LoadResPriority.RES_MIDDLE,0, image, setNativeSize);
    }

    /// <summary>
    /// 图片加载完成
    /// </summary>
    /// <param name="path"></param>
    /// <param name="obj"></param>
    /// <param name="paras"></param>
    void OnLoadSpriteComplete(string path,Object obj,params object[] paras)
    {
        if (obj != null)
        {
            Sprite sp = obj as Sprite;
            Image image = paras[0] as Image;
            bool setNativeSize = (bool)paras[1];
            if (image.sprite != null)
            {
                image = null;
            }
            image.sprite = sp;
            if (setNativeSize)
            {
                image.SetNativeSize();
            }
        }
    }

    /// <summary>
    /// 移除所有的button事件
    /// </summary>
    public void RemoveAllButtonListener()
    {
        foreach (Button btn in m_AllButton)
        {
            btn.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 移除所有toggle事件
    /// </summary>
    public void RemoveAllToggleListener()
    {
        foreach (Toggle toggle in m_AllToggle)
        {
            toggle.onValueChanged.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 添加btn事件监听
    /// </summary>
    /// <param name="btn"></param>
    /// <param name="action"></param>
    public void AddButtonClickListener(Button btn,UnityEngine.Events.UnityAction action)
    {
        if (btn != null)
        {
            if (!m_AllButton.Contains(btn))
            {
                m_AllButton.Add(btn);
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            btn.onClick.AddListener(BtnPlaySound);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="toggle"></param>
    /// <param name="action"></param>
    public void AddToggleClickListener(Toggle toggle,UnityEngine.Events.UnityAction<bool> action)
    {
        if (toggle != null)
        {
            if (!m_AllToggle.Contains(toggle))
            {
                m_AllToggle.Add(toggle);
            }
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(action);
            toggle.onValueChanged.AddListener(TogglePlaySound);
        }
    }

    /// <summary>
    /// 播放button声音
    /// </summary>
    void BtnPlaySound()
    {

    }

    /// <summary>
    /// 播放toggle声音
    /// </summary>
    void TogglePlaySound(bool isOn)
    {

    }

}
