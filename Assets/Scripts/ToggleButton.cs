using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    public event EventHandler CheckedOrNot;

    public bool IsOn;
    public Color32 CheckedColor;
    public Color32 CheckedForeground;
    private Color32 old_normalcolor;
    private Color32 old_highlightedcolor;
    private Color32 old_selectedcolor;
    private Color32 old_foreground;

    void Awake()
    {
        Button button = gameObject.GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void Update()
    {
        
    }

    public void OnClick()
    {
        Button button = gameObject.GetComponent<Button>();
        ColorBlock cb = button.colors;
        IsOn = !IsOn;
        if (IsOn)
        {
            old_normalcolor = cb.normalColor;
            old_highlightedcolor = cb.highlightedColor;
            old_selectedcolor = cb.selectedColor;
            cb.normalColor = CheckedColor;
            cb.highlightedColor = CheckedColor;
            cb.selectedColor = CheckedColor;
            foreach (Text text in button.GetComponentsInChildren<Text>())
            {
                old_foreground = text.color;
                text.color = CheckedForeground;
            }
        }
        else
        {
            cb.normalColor = old_normalcolor;
            cb.highlightedColor = old_highlightedcolor;
            cb.selectedColor = old_selectedcolor;
            foreach (Text text in button.GetComponentsInChildren<Text>())
            {
                text.color = old_foreground;
            }
        }
        button.colors = cb;
        CheckedOrNot?.Invoke(this, new EventArgs());
    }
    
}
