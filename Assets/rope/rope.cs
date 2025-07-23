using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoEndControl : MonoBehaviour
{
    public Transform leftEnd;   // 左端控制点
    public Transform rightEnd;  // 右端控制点
    public float moveSpeed = 5f; // 移动速度
    public float zAxis;

    void Update()
    {
        // 获取鼠标在世界空间的位置
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zAxis));
        //mousePos.z = -1.862f; // 如果是2D场景，Z轴固定

        if(Input.GetMouseButton(0) && Input.GetMouseButton(1))
        {
            return;
        }

        else if (Input.GetMouseButton(0))
        {

            rightEnd.position = Vector3.Lerp(rightEnd.position, mousePos, Time.deltaTime * moveSpeed);

            float dis = Vector3.Distance(rightEnd.position, leftEnd.position);

            if (dis < 0.5f)
            {
                //constrain right hand not too close
                Vector3 dir = rightEnd.position - leftEnd.position;
                rightEnd.position = leftEnd.position + dir.normalized * 0.5f;
                //print(dir);
            }
            else if (dis > 4f)
            {
                Vector3 dir = rightEnd.position - leftEnd.position;
                rightEnd.position = leftEnd.position + dir.normalized * 4f;
            }
        }

        else if (Input.GetMouseButton(1))
        {

            leftEnd.position = Vector3.Lerp(leftEnd.position, mousePos, Time.deltaTime * moveSpeed);

            float dis = Vector3.Distance(rightEnd.position, leftEnd.position);

            if (dis < 0.5f)
            {
                //constrain right hand not too close
                Vector3 dir = leftEnd.position - rightEnd.position;
                leftEnd.position = rightEnd.position + dir.normalized * 0.5f;
                //print(dir);
            }
            else if (dis > 4f)
            {
                Vector3 dir = leftEnd.position - rightEnd.position;
                leftEnd.position = rightEnd.position + dir.normalized * 4f;
            }
        }

    }
}