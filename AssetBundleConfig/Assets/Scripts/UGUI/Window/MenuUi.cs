using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUi : Window
{
    private MenuPanel m_MainPanel;

    private AudioClip m_Clip;

    public override void Awake(params object[] paras)
    {
        m_MainPanel = GameObject.GetComponent<MenuPanel>();
        AddButtonClickListener(m_MainPanel.m_StartButton, OnClickStart);
        AddButtonClickListener(m_MainPanel.m_LoadButton, OnClickLoad);
        AddButtonClickListener(m_MainPanel.m_ExitButton, OnClickExit);
        m_Clip = ResourceManager.Instance.LoadResource<AudioClip>(ConstString.MenuSound);
        m_MainPanel.m_Auido.clip = m_Clip;
        m_MainPanel.m_Auido.Play();


    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            ResourceManager.Instance.ReleaseResourece(m_Clip, true);
            m_MainPanel.m_Auido.clip = null;
            m_Clip = null;
        }
    }

    void OnClickStart()
    {
        Debug.Log("start");
    }

    void OnClickLoad()
    {
        Debug.Log("Load");
    }

    void OnClickExit()
    {
        Debug.Log("Exit");
    }
}
