
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 20f;
    public float panBorderThickness = 10f;
    public Vector2 panLimit;

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = transform.position;
        if (Input.GetKey("d") || Input.GetKey(KeyCode.RightArrow))
        {
            //Input.mousePosition.x >=Screen.width - panBorderThickness
            //Input.mousePosition.x >= Screen.width - panBorderThickness
            pos.x -= panSpeed * Time.deltaTime;
        }

        if (Input.GetKey("a") || Input.GetKey(KeyCode.LeftArrow))
        {
            pos.x += panSpeed * Time.deltaTime;
        }

        pos.x = Mathf.Clamp(pos.x, -panLimit.x, panLimit.x);

        transform.position = pos;
    }
}
