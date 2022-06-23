using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MessageAskBox : MonoBehaviour
{
    private Text message;
    private Scrollbar timeout;
    private Button yes;
    private Button no;
    private bool iscountingtime;
    private float timemax = 100;
    private float timeremain = 100;

    public string Message
    {
        get
        {
            if (message == null) return "";
            return message.text;
        }
        set
        {
            if (message == null) return;
            message.text = value;
        }
    }

    public Button ButtonYes
    {
        get { return yes; }
    }

    public Button ButtonNo
    {
        get { return no; }
    }

    void Awake()
    {
        for (int i0 = 0; i0 < gameObject.transform.childCount; i0++)
        {
            Transform t0 = gameObject.transform.GetChild(i0);
            GameObject g0 = t0.gameObject;
            switch (g0.name)
            {
                case "Timeout":
                    timeout = g0.GetComponent<Scrollbar>();
                    message = g0.GetComponentInChildren<Text>();
                    break;
                case "Yes":
                    yes = g0.GetComponent<Button>();
                    yes.onClick.AddListener(OnYes);
                    break;
                case "No":
                    no = g0.GetComponent<Button>();
                    no.onClick.AddListener(OnNo);
                    break;
            }
        }
    }

    void Update()
    {
        if (iscountingtime)
        {
            timeremain = Math.Max(0, timeremain - Time.deltaTime);
            timeout.size = timeremain / timemax;
            if (timeremain <= 0)
            {
                StopTimeout();

                GameBoard gb = gameObject.transform.GetComponentInParent<GameBoard>();
                gb?.OnTimeout();
            }
        }
    }

    public void StartTimeout(float _timemax)
    {
        timemax = _timemax;
        timeremain = timemax;
        iscountingtime = true;
    }

    public void StopTimeout()
    {
        iscountingtime = false;
        timemax = 100;
        timeremain = 100;
    }

    private void OnYes()
    {
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        gb?.UI_Ask_Yes();
    }

    private void OnNo()
    {
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        gb?.UI_Ask_No();
    }
}
