using UnityEngine;
using System.Collections;
using System.Threading;
/// <summary>
/// 单例基类，提供两个抽象函数Init 和 DisInit 初始化和逆初始化过程。
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class MonoSingleton<T> : MonoBehaviour
where T : MonoSingleton<T>
{

    private static T m_Instance = null;
    private static string name;
    private static Mutex mutex;
    public static T instance
    {
        get
        {
            if (m_Instance == null)
            {
                if (IsSingle())
                {
                    m_Instance = new GameObject(name, typeof(T)).GetComponent<T>();
                    m_Instance.Init();
                }
            }
            return m_Instance;
        }
    }

    private static bool IsSingle()
    {
        bool createdNew;
        name = "Singleton of " + typeof(T).ToString();
        mutex = new Mutex(false, name, out createdNew);
        return createdNew;
    }

    private void Awake()
    {
        if (m_Instance == null)
        {
            if (IsSingle())
            {
                m_Instance = this as T;
                m_Instance.Init();
            }
        }
        else
        {
            Destroy(this);
        }
    }

    protected abstract void Init();
    protected abstract void DisInit();
    private void OnDestory()
    {
        if (m_Instance != null)
        {
            mutex.ReleaseMutex();
            DisInit();
            m_Instance = null;
        }
    }
    private void OnApplicationQuit()
    {
        mutex.ReleaseMutex();
    }
}

