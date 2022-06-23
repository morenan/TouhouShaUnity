using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class LineTest0 : MonoBehaviour
{
    public LineBe Line;
    public GameObject StartPoint;
    public GameObject EndPoint;

    private void Awake()
    {
        Line.Show(StartPoint.transform.position, EndPoint.transform.position);
    }
}
