 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectOfflineData : OfflineData
{
    public ParticleSystem[] m_Particle;
    public TrailRenderer[] m_TrailRe;

    public override void ResetProp()
    {
        base.ResetProp();
        int partcleCount = m_Particle.Length;
        for (int i = 0; i < partcleCount; i++)
        {
            m_Particle[i].Clear(true);
            m_Particle[i].Play();
        }

        foreach (TrailRenderer trail in m_TrailRe)
        {
            trail.Clear();
        }
    }

    public override void BindData()
    {
        base.BindData();
        m_Particle = gameObject.GetComponentsInChildren<ParticleSystem>(true);
        m_TrailRe = gameObject.GetComponentsInChildren<TrailRenderer>(true);

    }
}
