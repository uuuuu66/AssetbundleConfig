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

    }
}
