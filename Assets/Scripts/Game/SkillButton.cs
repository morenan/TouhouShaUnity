using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;

public class SkillButton : MonoBehaviour
{
    private Skill skill;
    public Skill Skill
    {
        get
        {
            return this.skill;
        }
        set
        {
            this.skill = value;
            UpdateText();
        }
    }

    public bool IsEnabled
    {
        get
        {
            Button button = gameObject.GetComponent<Button>();
            return button?.interactable ?? false;
        }
        set
        {
            Button button = gameObject.GetComponent<Button>();
            if (button == null) return;
            button.interactable = value;
        }
    }

    public bool IsChecked
    {
        get
        {
            ToggleButton toggle = gameObject.GetComponent<ToggleButton>();
            return toggle?.IsOn ?? false;
        }
        set
        {
            ToggleButton toggle = gameObject.GetComponent<ToggleButton>();
            if (toggle == null) return;
            if (toggle.IsOn != value) toggle.OnClick();
        }
    }

    void Awake()
    {
        ToggleButton toggle = gameObject.GetComponent<ToggleButton>();
        toggle.CheckedOrNot += Toggle_CheckedOrNot;

        UpdateText();
    }

    public void UpdateText()
    {
        if (Skill == null) return;
        Text text = gameObject?.GetComponentInChildren<Text>();
        if (text != null) text.text = Skill.GetInfo().Name;
    }

    private void Toggle_CheckedOrNot(object sender, EventArgs e)
    {
        SkillList list = gameObject.GetComponentInParent<SkillList>();
        if (IsChecked)
            list?.SkillButton_Checked(this);
        else
            list?.SkillButton_Unchecked(this);
    }

}
