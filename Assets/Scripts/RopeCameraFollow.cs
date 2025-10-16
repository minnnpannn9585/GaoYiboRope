using UnityEngine;

public class RopeCameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform LeftEnd;   // 拖左端小球
    public Transform RightEnd;  // 拖右端小球

    [Header("Camera Offsets")]
    public Vector3 offset = new Vector3(0f, 3.5f, -8f); // 相机相对中点的偏移
    public Vector3 lookOffset = new Vector3(0f, 1.0f, 0f); // 视线相对中点的抬头

    [Header("Smoothing")]
    public float posSmooth = 10f;  // 位置平滑
    public float rotSmooth = 10f;  // 朝向平滑

    Vector3 vel; // SmoothDamp 缓存

    void LateUpdate()
    {
        if (!LeftEnd || !RightEnd) return;

        // 1) 计算绳子“关注点”：两端中点
        Vector3 mid = (LeftEnd.position + RightEnd.position) * 0.5f;

        // 2) 目标相机位置：中点 + 固定偏移
        Vector3 targetPos = mid + offset;

        // 3) 平滑移动
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref vel, 1f / Mathf.Max(0.01f, posSmooth));

        // 4) 平滑看向中点（带一点抬头）
        Vector3 lookTarget = mid + lookOffset;
        Quaternion targetRot = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotSmooth);
    }
}
