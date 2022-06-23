using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DigitColle : MonoBehaviour
{
    private DigitColor color = DigitColor.Black;
    public DigitColor Color
    {
        get { return this.color; }
        set { this.color = value; UpdateItems(); }
    }

    private int digit;
    public int Digit
    {
        get { return this.digit; }
        set { this.digit = value; UpdateItems(); }
    }

    private List<DigitItem> items = new List<DigitItem>();

    void Awake()
    {

    }

    public void UpdateItems()
    {
        string s = digit.ToString();
        float x = 0;
        while (items.Count() < s.Length)
        {
            GameObject go0 = Resources.Load<GameObject>("Digit");
            GameObject go1 = GameObject.Instantiate(go0, gameObject.transform);
            DigitItem item = go1.GetComponent<DigitItem>();
            items.Add(item);
        }
        for (int i = 0; i < s.Length; i++)
        {
            DigitItem item = items[i];
            RectTransform rt = item.gameObject.GetComponent<RectTransform>();
            App.Show(item);
            rt.localPosition = new Vector3(x, 0);
            item.Color = Color;
            item.Digit = (int)(s[i] - '0');
            x += rt.rect.width;
        }
        for (int i = s.Length; i < items.Count(); i++)
        {
            DigitItem item = items[i];
            App.Hide(item);
        }
    }
}
