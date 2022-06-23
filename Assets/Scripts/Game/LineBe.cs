using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class LineBe : MonoBehaviour
{
    public float ShowTime = 0.2f;
    public float WaitTime = 0.5f;
    public float HideTime = 0.1f;
    public float TotalTime = 0.0f;
    public Vector3 StartPoint;
    public Vector3 EndPoint;
    
    void Update()
    {
        if (TotalTime >= ShowTime + WaitTime + HideTime) return;
        //RectTransform rt = gameObject.GetComponent<RectTransform>();
        if (TotalTime < ShowTime)
        {
            transform.position = StartPoint;
            transform.localScale = new Vector3((EndPoint - StartPoint).magnitude * TotalTime / ShowTime, 1, 1);
        }
        else if (TotalTime < ShowTime + WaitTime)
        {
            transform.position = StartPoint;
            transform.localScale = new Vector3((EndPoint - StartPoint).magnitude, 1, 1);
        }
        else
        {
            transform.position = StartPoint + (EndPoint - StartPoint) * (TotalTime - ShowTime - WaitTime) / HideTime;
            transform.localScale = new Vector3((EndPoint - StartPoint).magnitude * (ShowTime + WaitTime + HideTime - TotalTime) / HideTime, 1, 1);
        }
        TotalTime += Time.deltaTime;
        if (TotalTime >= ShowTime + WaitTime + HideTime)
        {
            GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
            gb?.Hide(this);
        }
    }

    public void Show(Vector3 _startpoint, Vector3 _endpoint)
    {
        //RectTransform rt = gameObject.GetComponent<RectTransform>();
        StartPoint = _startpoint;
        EndPoint = _endpoint;
        transform.position = StartPoint;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, (float)(Math.Atan2(EndPoint.y - StartPoint.y, EndPoint.x - StartPoint.x) * 180 / Math.PI)));
        transform.localScale = new Vector3((EndPoint - StartPoint).magnitude, 1, 1);
        TotalTime = 0;
    }
}
