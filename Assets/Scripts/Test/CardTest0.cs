using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CardTest0 : MonoBehaviour
{
    public float TotalTime = 0.0f;

    private List<CardBe> items = new List<CardBe>();
    private bool inited;
    
    void Awake()
    {

    }

    void Update()
    {
        TotalTime += Time.deltaTime;
        if (!inited && TotalTime >= 3.0f)
        {
            inited = true;
            for (int i = 0; i < 40; i++)
            {
                GameObject go0 = Resources.Load<GameObject>("Card");
                GameObject go1 = GameObject.Instantiate(go0, gameObject.transform);
                CardBe item = go1.GetComponent<CardBe>();
                items.Add(item);
            }
            for (int i = 0; i < items.Count(); i++)
            {
                double a = Math.PI * 2 * i / items.Count();
                double sin = Math.Sin(a);
                double cos = Math.Cos(a);
                CardBe item = items[i];
                item.Position = new Vector3(1200, 600);
                item.Move(new Vector3(
                    (float)(500 * cos) + 1200,
                    (float)(500 * sin) + 600));
            }
        }
    }
}
