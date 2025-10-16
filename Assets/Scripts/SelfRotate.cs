using UnityEngine;

public class SelfRotate : MonoBehaviour
{
    [Header("旋转速度（度/秒）")]
    public Vector3 rotationSpeed = new Vector3(0f, 50f, 0f); // 默认绕Y轴旋转

    void Update()
    {
        // 每帧绕自身圆心旋转
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }
}