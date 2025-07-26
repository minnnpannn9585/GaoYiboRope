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
        }
        //如果设置的鼠标键松开
        if (Input.GetMouseButtonUp(mouseButtonIndex))
        {
            isHolding = false;
        }


        if (isHolding)
        {
            
        }
        else
        {
            // add a force to the sphere
            rb.AddForce(Vector3.forward * force);
        }
    }
}
