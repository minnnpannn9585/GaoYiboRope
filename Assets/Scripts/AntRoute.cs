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
	    //print(antRunTime);
	    //calculate total time : 16s
	    
	    Vector3 dir = points[index].position - ant.position;
	    //print(points[index].position);
	    //print(ant.position);
	    
        ant.Translate(dir.normalized * speed * Time.deltaTime);
		
        // rotation look at destination
        
        if (Vector3.Magnitude(dir) < 0.1f)
        {
	        index++;
	        
        }
    }
}
