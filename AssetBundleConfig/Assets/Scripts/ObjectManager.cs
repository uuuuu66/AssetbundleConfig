using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ObjectManager : Singleton<ObjectManager>
{
    #region 类对象池的使用
    protected Dictionary<Type, object> m_ClassPoolDic = new Dictionary<Type, object>();
    /// <summary>
    /// 创建雷队想吃，创建完之后外面可以保存ClassObjectPool<T>,然后调用spawn和recycle来创建回收类对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="maxcount"></param>
    /// <returns></returns>
    public ClassObjectPool<T> GetOrCreatClassPool<T>(int maxcount) where T : class, new()
    {
        //存下类型
        Type type = typeof(T);
        object outObj = null;
        //如果不包含这个类型，或者取出来之后没有东西
        if (!m_ClassPoolDic.TryGetValue(type, out outObj) || outObj == null)
        {
            ClassObjectPool<T> newPool = new ClassObjectPool<T>(maxcount);
            //加入
            m_ClassPoolDic.Add(type, newPool);
            return newPool;
        }

        //如果在里面直接返回
        return outObj as ClassObjectPool<T>;
    }

    /// <summary>
    /// 从对象池中取T对象，可以直接调用 ClassObjectPool<T> XXX = ObjectManager.Instance.GetOrCreatClassPool<T>(number);来取
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="maxcount"></param>
    /// <returns></returns>
    public T NewClassObjectFromPool<T>(int maxcount) where T : class, new()
    {
        ClassObjectPool<T> pool = GetOrCreatClassPool<T>(maxcount);
        if (pool == null)
        {
            return null;
        }

        return pool.Spawn(true);
    }
    #endregion
}
