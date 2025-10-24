using UnityEngine;

public class RandomSpawnSteam : MonoBehaviour
{
    [Header("要随机开关的喷口（粒子对象数组）")]
    public ParticleSystem[] steams;

    [Header("开关时间范围（秒）")]
    public Vector2 onTime = new Vector2(0.6f, 1.2f);   // 开启时长随机范围
    public Vector2 offTime = new Vector2(1.5f, 4.5f);   // 关闭时长随机范围

    float[] timers;
    bool[] states;

    void Awake()
    {
        int n = steams != null ? steams.Length : 0;
        timers = new float[n];
        states = new bool[n];

        for (int i = 0; i < n; i++)
        {
            SetState(i, false);                                  // 开局关闭并清空
            timers[i] = Random.Range(offTime.x, offTime.y);      // 先等一段再开启
        }
    }

    void Update()
    {
        for (int i = 0; i < steams.Length; i++)
        {
            timers[i] -= Time.deltaTime;
            if (timers[i] > 0f) continue;

            // 切换状态
            bool next = !states[i];
            SetState(i, next);

            // 重置计时
            timers[i] = next
                ? Random.Range(onTime.x, onTime.y)    // 开着 → 等多久再关
                : Random.Range(offTime.x, offTime.y); // 关着 → 等多久再开
        }
    }

    void SetState(int i, bool on)
    {
        states[i] = on;

        var ps = steams[i];
        if (ps == null) return;

        // 确保不开机就喷
        var main = ps.main;
        main.playOnAwake = false;

        if (on)
        {
            if (!ps.isPlaying) ps.Play(true);
        }
        else
        {
            if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // 若喷口物体上直接挂了触发器，让它跟随启停
        var trig = ps.GetComponent<Collider>();
        if (trig) trig.enabled = on;
    }
}