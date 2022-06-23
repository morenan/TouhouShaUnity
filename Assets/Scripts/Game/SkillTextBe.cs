using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SkillTextBe : MonoBehaviour
{
    public string Text
    {
        get
        {

            Text text = gameObject.GetComponent<Text>();
            if (text == null) return "";
            return text.text;
        }
        set
        {
            Text text = gameObject.GetComponent<Text>();
            if (text == null) return;
            text.text = value;
        }
    }

    public Vector3 Position
    {
        get
        {
            return transform.position;
        }
        set
        {
            transform.position = value;
        }
    }

    public float Opacity
    {
        get
        {
            Text text = gameObject.GetComponent<Text>();
            if (text == null) return 0.0f;
            return text.color.a;
        }
        set
        {
            Text text = gameObject.GetComponent<Text>();
            if (text == null) return;
            Color c = text.color;
            c.a = value;
            text.color = c;
        }
    }


    public float ShowTime = 0.2f;
    public float WaitTime = 1.8f;
    public float HideTime = 0.05f;
    public float TotalTime = 0.0f;
    public Vector3 StartPoint;
    public Vector3 Move = new Vector3(0, 200);

    void Update()
    {
        if (TotalTime >= ShowTime + WaitTime + HideTime) return;
        transform.position = StartPoint + Move * TotalTime / (ShowTime + WaitTime + HideTime);
        if (TotalTime < ShowTime)
            Opacity = 1.0f * TotalTime / ShowTime;
        else if (TotalTime < ShowTime + WaitTime)
            Opacity = 1.0f;
        else
            Opacity = 1.0f * (ShowTime + WaitTime + HideTime - TotalTime) / HideTime;
        TotalTime += Time.deltaTime;
        if (TotalTime >= ShowTime + WaitTime + HideTime)
        {
            GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
            gb?.Hide(this);
        }
    }

    public void Show(string _text, Vector3 _startpoint)
    {
        Text = _text;
        StartPoint = _startpoint;
        TotalTime = 0;
    }
}
