using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MouseButton
{
    LeftButton,   // 左键
    RightButton   // 右键
}

public class SphereControl : MonoBehaviour
{
    private Rigidbody rb;
    public bool isHolding = false;
    public float force; // 向前的力

    [Header("鼠标控制设置")]
    [Tooltip("选择使用鼠标左键还是右键来控制")]
    public MouseButton controlButton = MouseButton.LeftButton;

    [Header("位置控制设置")]
    [Tooltip("鼠标移动的敏感度")]
    public float mouseSensitivity = 0.01f;
    [Tooltip("推向目标位置的力的大小")]
    public float moveForce = 10f;

    // 记录开始holding时的位置和鼠标位置
    private Vector3 initialPosition;
    private Vector3 initialMousePosition;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // 根据设置的按键类型来检测鼠标输入
        int mouseButtonIndex = (controlButton == MouseButton.LeftButton) ? 0 : 1;

        //如果设置的鼠标键按下
        if (Input.GetMouseButtonDown(mouseButtonIndex))
        {
            isHolding = true;
            // 记录开始holding时的物体位置和鼠标位置
            initialPosition = transform.position;
            initialMousePosition = Input.mousePosition;
            rb.drag = 22f;
        }
        //如果设置的鼠标键松开
        if (Input.GetMouseButtonUp(mouseButtonIndex))
        {
            rb.drag = 0f;
            StartCoroutine(TemporaryKinematic());
            isHolding = false;
            // 启动协程：短暂设置为kinematic

        }


    }


    void FixedUpdate()
    {

        if (isHolding)
        {
            rb.useGravity = false;
            // 计算鼠标移动的偏移量
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 mouseDelta = currentMousePosition - initialMousePosition;

            // 将鼠标的2D移动转换为3D世界坐标的移动
            // 这里假设鼠标的X轴对应世界的X轴，Y轴对应世界的Y轴
            Vector3 worldDelta = new Vector3(
                mouseDelta.x * mouseSensitivity,
                mouseDelta.y * mouseSensitivity,
                0f // Z轴不变
            );

            // 计算目标位置 = 初始位置 + 鼠标移动偏移
            Vector3 targetPosition = initialPosition + worldDelta;

            // 计算从当前位置到目标位置的方向和距离
            Vector3 direction = targetPosition - transform.position;

            // 施加力推向目标位置
            rb.AddForce(direction * moveForce);
        }
        else
        {
            rb.useGravity = true;
            // add a force to the sphere
            rb.AddForce(Vector3.forward * force);
        }
    }

    // 协程：短暂设置刚体为kinematic
    private IEnumerator TemporaryKinematic()
    {
        // 设置为kinematic（静态运动学）
        rb.isKinematic = true;

        // 等待0.1秒
        yield return new WaitForSeconds(0.1f);

        // 恢复为非kinematic
        rb.isKinematic = false;
    }
}
