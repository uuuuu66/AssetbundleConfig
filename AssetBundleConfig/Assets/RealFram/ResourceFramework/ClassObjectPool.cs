using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassObjectPool<T> where T:class,new()
{
    //池
    protected Stack<T> m_Pool = new Stack<T>();
    //最大对象个数，<=0表示个数不限
    protected int m_MaxCount = 0;
    //没有回收的对象个数
    protected int m_NoRecycleCount = 0;

    public ClassObjectPool(int maxcount)
    {
        m_MaxCount = maxcount;
        for (int i = 0; i < maxcount; i++)
        {
            m_Pool.Push(new T()); 
        }
    }

    /// <summary>
    /// 从类取对象
    /// </summary>
    /// <param name="creatIfPoolEmpty"></param>
    /// <returns></returns>
    public T Spawn(bool creatIfPoolEmpty)
    {
        //池子里有东西
        if (m_Pool.Count > 0)
        {
            //取出对象
            T rtn = m_Pool.Pop();
            //没有对象
            if (rtn == null)
            {
                //判断一下没有就new一个
                if (creatIfPoolEmpty)
                {
                    rtn = new T();
                }
            }
            //没有回收的对象+1
            m_NoRecycleCount++;
            //对象返回
            return rtn;
        }
        else
        {
            if (creatIfPoolEmpty)
            {
                T rtn = new T();
                m_NoRecycleCount++;
                return rtn;
            }
        }

        return null;


    }

    /// <summary>
    /// 回收类对象
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool Recycle(T obj)
    {
        //
        if (obj == null)
        {
            return false;
        }

        m_NoRecycleCount--;

        //回收的时候多出来的时候就直接置空
        if (m_Pool.Count >= m_MaxCount && m_MaxCount > 0)
        {
            obj = null;
            return false;
        }

        m_Pool.Push(obj);
        return true;
    }
}
