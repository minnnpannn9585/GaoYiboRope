
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
    public bool isAllRelease = false;
    public float force; // 向前的力
    public Transform otherSphere;
    [Header("距离限制设置")]
    [Tooltip("最大允许距离")]
    public float maxDistance = 3f;
    [Header("鼠标控制设置")]
    [Tooltip("选择使用鼠标左键还是右键来控制")]
    public MouseButton controlButton = MouseButton.LeftButton;
    [Header("位置控制设置")]
    [Tooltip("鼠标移动的敏感度")]
    public float mouseSensitivity = 0.01f;
    [Tooltip("推向目标位置的力的大小")]
    public float moveForce = 10f;
    // 记录开始holding时的位置
    private Vector3 initialPosition;
    // 记录开始holding时的鼠标位置（用于未锁定状态）
    private Vector3 initialMousePosition;
    // 累计鼠标移动偏移量
    private Vector3 accumulatedMouseDelta = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        //隐藏  锁定鼠标
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    // Update is called once per frame
    void Update()
    {
        // 根据设置的按键类型来检测鼠标输入
        int mouseButtonIndex = (controlButton == MouseButton.LeftButton) ? 0 : 1;
        //如果左右键都抬起
        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
        {
            Debug.Log("左右键都抬起");
            // 当左右键都抬起时，恢复刚体到基础状态,且不施加向前的力
            isHolding = false;
            rb.drag = 0f;
            rb.useGravity = true;
            rb.isKinematic = false;
            isAllRelease = true;
        }
        //如果设置的鼠标键按下
        else if (Input.GetMouseButtonUp(mouseButtonIndex))
        {
            isHolding = true;
            isAllRelease = false;
            // 记录开始holding时的物体位置和鼠标位置，重置累计偏移量
            initialPosition = transform.position;
            initialMousePosition = Input.mousePosition;
            accumulatedMouseDelta = Vector3.zero;
            rb.drag = 22f;
        }
        //如果设置的鼠标键松开
        else if (Input.GetMouseButtonDown(mouseButtonIndex))
        {
            rb.drag = 0f;
            StartCoroutine(TemporaryKinematic());
            isHolding = false;
            isAllRelease = false;
        }



    }

    void FixedUpdate()
    {
        if (isHolding)
        {
            rb.useGravity = false;
            // 获取鼠标增量输入 - 使用更可靠的方法
            Vector3 mouseDelta = Vector3.zero;
            
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                // 当鼠标锁定时，使用GetAxis获取增量（FPS风格）
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                
                // 调试输出鼠标输入值
                if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
                {
                    Debug.Log($"Mouse Input - X: {mouseX}, Y: {mouseY}");
                }
                
                // 需要调整灵敏度以适应锁定模式
                mouseDelta = new Vector3(
                    mouseX * 30f,  // 大幅增加灵敏度
                    mouseY * 30f,  // 大幅增加灵敏度
                    0f
                );
            }
            else
            {
                // 当鼠标未锁定时，使用传统方法
                Vector3 currentMousePosition = Input.mousePosition;
                mouseDelta = currentMousePosition - initialMousePosition;
                initialMousePosition = currentMousePosition;
            }
            
            // 累计鼠标移动偏移量
            accumulatedMouseDelta += mouseDelta;
            // 将鼠标的2D移动转换为3D世界坐标的移动
            Vector3 worldDelta = new Vector3(
                accumulatedMouseDelta.x * mouseSensitivity,
                accumulatedMouseDelta.y * mouseSensitivity,
                0f // Z轴不变
            );
            // 计算目标位置 = 初始位置 + 鼠标移动偏移
            Vector3 targetPosition = initialPosition + worldDelta;
            // 检查与otherSphere的距离限制
            if (otherSphere != null)
            {
                float currentDistance = Vector3.Distance(transform.position, otherSphere.position);
                float targetDistance = Vector3.Distance(targetPosition, otherSphere.position);
                
                // 如果目标位置会导致距离超过最大限制，则限制移动
                if (targetDistance > maxDistance && targetDistance > currentDistance)
                {
                    // 计算允许的最大位置（在最大距离范围内）
                    Vector3 directionToOther = (otherSphere.position - transform.position).normalized;
                    Vector3 maxAllowedPosition = otherSphere.position - directionToOther * maxDistance;
                    
                    // 重新计算目标位置，确保不超过最大距离
                    targetPosition = maxAllowedPosition;
                    
                    // 重置累计偏移量以避免继续累积
                    accumulatedMouseDelta = (targetPosition - initialPosition) / mouseSensitivity;
                }
            }
            // 计算从当前位置到目标位置的方向和距离
            Vector3 direction = targetPosition - transform.position;
            // 施加力推向目标位置
            rb.AddForce(direction * moveForce);
        }
        else
        {
            if (isAllRelease)
            {
                rb.useGravity = true;
                return;
            }
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
