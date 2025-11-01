using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntRoute : MonoBehaviour
{
    Transform ant;
	Transform[] points;
	public float speed;
	private int index = 0;

	private float antRunTime = 0;

	[Header("Rotation")]
	[Tooltip("Degrees to offset the sprite/model so it faces movement direction correctly")]
	public float rotationOffset = 0f;
	[Tooltip("Smoothly rotate towards movement direction")]
	public bool smoothRotation = true;
	[Tooltip("Degrees per second when smoothing")]
	public float rotationSpeed = 720f;

    // Start is called before the first frame update
    void Start()
    {
	    points = new Transform[11];
        ant = transform.GetChild(0);
		for(int i = 0; i < transform.childCount - 1 ; i++)
		{
			points[i] = transform.GetChild(i+1);
		}
    }

    // Update is called once per frame
    void Update()
    {
	    if (index >= transform.childCount - 1)
	    {
		    return;
	    }

	    antRunTime += Time.deltaTime;
	    
	    Vector3 dir = points[index].position - ant.position;

        // move in world space (use position change rather than local Translate)
        if (dir.sqrMagnitude > 0.000001f)
        {
            ant.position += dir.normalized * speed * Time.deltaTime;
        }
		
        // rotation: face movement direction around world Z axis
        if (dir.sqrMagnitude > 0.000001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffset;
            Quaternion target = Quaternion.AngleAxis(angle, Vector3.forward);
            if (smoothRotation)
            {
                ant.rotation = Quaternion.RotateTowards(ant.rotation, target, rotationSpeed * Time.deltaTime);
            }
            else
            {
                ant.rotation = target;
            }
        }

        if (Vector3.Magnitude(dir) < 0.1f)
        {
	        index++;
        }
    }
}
