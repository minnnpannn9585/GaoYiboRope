using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SteamBlower : MonoBehaviour
{
    [Header("吹力（加速度 m/s?）")]
    public float strength = 30f;

    [Header("本地方向（喷出方向）")]
    public Vector3 localDirection = Vector3.forward; // 与喷口朝向对应

    [Header("只在粒子播放时施力")]
    public bool onlyWhenPlaying = true;

    [Header("只作用这些层（可选，建议只勾Player/Anchor）")]
    public LayerMask affectLayers = ~0;

    ParticleSystem ps;

    void Awake() { ps = GetComponent<ParticleSystem>(); }

    void OnTriggerStay(Collider other)
    {
        if (onlyWhenPlaying && ps && !ps.isPlaying) return;

        // 层过滤
        if (((1 << other.gameObject.layer) & affectLayers) == 0) return;

        var rb = other.attachedRigidbody;
        if (!rb) return;

        Vector3 dir = transform.TransformDirection(localDirection).normalized;
        rb.AddForce(dir * strength, ForceMode.Acceleration);
    }
}