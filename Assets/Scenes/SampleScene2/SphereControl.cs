
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum MouseButton
{
    LeftButton,   
    RightButton   
}
public class SphereControl : MonoBehaviour
{
    private Rigidbody rb;
    public bool isHolding = false;
    public bool isAllRelease = false;
    public float force; 
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
        // 条件：当且仅当指定的控制键被按下，且另一个键没有被按下时，才进入 holding 状态
        bool isOnlyControlKeyDown = Input.GetMouseButton(mouseButtonIndex) && !Input.GetMouseButton(1 - mouseButtonIndex);

        if (isOnlyControlKeyDown)
        {
            GetComponent<AudioSource>().Play();
            // 检查是否是从“非按住”状态刚刚切换过来
            if (!isHolding)
            {
                
                Debug.Log("设置键按下 (Only)");
                // 记录开始holding时的物体位置和鼠标位置，重置累计偏移量
                initialPosition = transform.position;
                initialMousePosition = Input.mousePosition;
                accumulatedMouseDelta = Vector3.zero;
            }

            isHolding = true;
            isAllRelease = false;
            rb.drag = 22f;
        }
        // 其他所有情况（两个都松开、两个都按下、只按下了非控制键）
        else
        {
            // 检查是否是从“按住”状态刚刚切换过来
            if (isHolding)
            {
                Debug.Log("设置键抬起 或 其他按键冲突");
                rb.drag = 0f;
                StartCoroutine(TemporaryKinematic());
            }

            isHolding = false;
            
            // 检查是否两个键都松开了
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            {
                Debug.Log("左右键都抬起");
                isAllRelease = true; // 进入完全自由落体状态
            }
            else // 两个都按下 或 只按了另一个键
            {
                
                if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
                    Debug.Log("左右键都按下");
                
                isAllRelease = false; // 进入施加向前力的状态
            }
        }



    }

    void FixedUpdate()
    {
        if (isHolding)
        {
            rb.useGravity = false;

            // 获取鼠标增量输入
            Vector3 mouseDelta = Vector3.zero;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                mouseDelta = new Vector3(mouseX * 30f, mouseY * 30f, 0f);
            }
            else
            {
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
                0f
            );

            // 计算理想的目标位置
            Vector3 targetPosition = initialPosition + worldDelta;

            // 限制目标位置，确保它不超过与 otherSphere 的最大距离
            if (otherSphere != null)
            {
                Vector3 directionFromOther = targetPosition - otherSphere.position;
                float distance = directionFromOther.magnitude;

                if (distance > maxDistance)
                {
                    // 将目标位置拉回到最大距离的边界上
                    targetPosition = otherSphere.position + directionFromOther.normalized * maxDistance;
                    
                    // 更新累计鼠标偏移量，以反映被限制后的位置
                    // 这样，下一次计算就会从正确的位置开始，避免了“跳跃”
                    accumulatedMouseDelta = (targetPosition - initialPosition) / mouseSensitivity;
                }
            }

            // 计算从当前位置到（可能被限制过的）目标位置的方向
            Vector3 direction = targetPosition - transform.position;
            
            // 施加力让小球追向目标位置
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
        yield return new WaitForSeconds(0.01f);
        // 恢复为非kinematic
        rb.isKinematic = false;
    }
}
