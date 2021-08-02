using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUi : Window
{
    private MenuPanel m_MainPanel;

    public override void Awake(params object[] paras)
    {
        m_MainPanel = GameObject.GetComponent<MenuPanel>();
        AddButtonClickListener(m_MainPanel.m_StartButton, OnClickStart);
        AddButtonClickListener(m_MainPanel.m_LoadButton, OnClickLoad);
        AddButtonClickListener(m_MainPanel.m_ExitButton, OnClickExit);
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
