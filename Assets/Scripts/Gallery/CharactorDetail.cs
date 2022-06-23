using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class CharactorDetail : MonoBehaviour
{
    #region Number

    public List<Charactor> Charactors;
    public int CurrentIndex;
    public SkillInfo CurrentSkill;
    public SkillInfo CurrentAttachedSkill;

    public Image Face;
    public Text Name;
    public Text Hp;
    public Text Country;
    public Text SkillMessage;
    public ToggleButton[] SkillButtons;
    public ToggleButton[] AttachedSkillButtons;
    public Button Previous;
    public Button Next;
    public Button Return;

    #endregion

    #region MonoBehavior

    void Awake()
    {
        for (int i = 0; i < SkillButtons.Length; i++)
        {
            ToggleButton button = SkillButtons[i];
            button.CheckedOrNot += OnSkillButtonToggleChanged;
        }
        for (int i = 0; i < AttachedSkillButtons.Length; i++)
        {
            ToggleButton button = AttachedSkillButtons[i];
            button.CheckedOrNot += OnSkillButtonToggleChanged;
        }
        Previous.onClick.AddListener(OnPrevious);
        Next.onClick.AddListener(OnNext);
        Return.onClick.AddListener(OnReturn);
    }

    #endregion

    #region Method

    public void UpdateCurrent()
    {
        if (Charactors == null) return;
        if (CurrentIndex < 0) return;
        if (CurrentIndex >= Charactors.Count()) return;
        Charactor char0 = Charactors[CurrentIndex];
        if (char0 == null) return;
        CharactorInfoCore info = char0.GetInfo();
        if (info == null) return;
        StringBuilder sb = new StringBuilder();

        Face.sprite = ImageHelper.CreateSprite(char0, Face.GetComponent<RectTransform>().rect);
        Name.text = info.Name;
        Hp.text = String.Format("HP {0}/{1}", char0.HP, char0.MaxHP);
        sb.Append(char0.Country);
        if (char0.OtherCountries.Count() > 0)
        {
            sb.Append("（可选：");
            for (int i = 0; i < char0.OtherCountries.Count(); i++)
            {
                sb.Append(char0.OtherCountries[i]);
                sb.Append(i + 1 >= char0.OtherCountries.Count() ? "）" : "/");
            }
        }
        Country.text = sb.ToString();
        CurrentSkill = info.Skills.FirstOrDefault();
        CurrentAttachedSkill = null;
        UpdateSkill();

        for (int i = 0; i < info.Skills.Count(); i++)
        {
            ToggleButton button = SkillButtons[i];
            Text text = button.gameObject.GetComponentInChildren<Text>();
            text.text = info.Skills[i].Name;
            if (button.IsOn && CurrentSkill != info.Skills[i])
                button.OnClick();
            else if (!button.IsOn && CurrentSkill == info.Skills[i])
                button.OnClick();
            App.Show(button);
        }
        for (int i = info.Skills.Count(); i < SkillButtons.Length; i++)
        {
            ToggleButton button = SkillButtons[i];
            App.Hide(button);
        }

        Previous.interactable = CurrentIndex > 0;
        Next.interactable = CurrentIndex + 1 < Charactors.Count();
        Return.interactable = true;
    }

    protected void UpdateSkill()
    {
        if (CurrentAttachedSkill != null)
            SkillMessage.text = CurrentAttachedSkill.Description;
        else if (CurrentSkill != null)
            SkillMessage.text = CurrentSkill.Description;
        else
            SkillMessage.text = "";
        if (CurrentSkill != null)
        {
            List<SkillInfo> attachs = CurrentSkill.AttachedSkills;
            for (int i = 0; i < attachs.Count(); i++)
            {
                ToggleButton button = AttachedSkillButtons[i];
                Text text = button.gameObject.GetComponentInChildren<Text>();
                text.text = attachs[i].Name;
                if (button.IsOn && CurrentAttachedSkill != attachs[i])
                    button.OnClick();
                else if (!button.IsOn && CurrentSkill == attachs[i])
                    button.OnClick();
                App.Show(button);
            }
            for (int i = attachs.Count(); i < AttachedSkillButtons.Length; i++)
            {
                ToggleButton button = AttachedSkillButtons[i];
                App.Hide(button);
            }
        }
    }

    #endregion

    #region Event Handler

    private void OnSkillButtonToggleChanged(object sender, EventArgs e)
    {
        if (Charactors == null) return;
        if (CurrentIndex < 0) return;
        if (CurrentIndex >= Charactors.Count()) return;
        Charactor char0 = Charactors[CurrentIndex];
        if (char0 == null) return;
        CharactorInfoCore info = char0.GetInfo();
        if (info == null) return;

        if (!(sender is ToggleButton)) return;
        ToggleButton button = (ToggleButton)sender;

        int buttonindex = Array.IndexOf(SkillButtons, button);
        if (buttonindex >= 0 && buttonindex < info.Skills.Count())
        {
            if (!button.IsOn)
            {
                if (CurrentSkill == info.Skills[buttonindex])
                    button.OnClick();
                return;
            }
            if (CurrentSkill == info.Skills[buttonindex]) return;
            CurrentSkill = info.Skills[buttonindex];
            CurrentAttachedSkill = null;
            for (int i = 0; i < SkillButtons.Length; i++)
            {
                ToggleButton other = SkillButtons[i];
                if (other.IsOn && i != buttonindex)
                    other.OnClick();
            }
            for (int i = 0; i < AttachedSkillButtons.Length; i++)
            {
                ToggleButton other = AttachedSkillButtons[i];
                if (other.IsOn)
                    other.OnClick();
            }
            UpdateSkill();
            return;
        }
        if (CurrentSkill != null)
        {
            List<SkillInfo> attachs = CurrentSkill.AttachedSkills;
            buttonindex = Array.IndexOf(AttachedSkillButtons, button);
            if (buttonindex >= 0 && buttonindex < attachs.Count())
            {
                if (!button.IsOn)
                {
                    if (CurrentAttachedSkill == attachs[buttonindex])
                        button.OnClick();
                    return;
                }
                if (CurrentAttachedSkill == attachs[buttonindex]) return;
                CurrentAttachedSkill = attachs[buttonindex];
                for (int i = 0; i < AttachedSkillButtons.Length; i++)
                {
                    ToggleButton other = AttachedSkillButtons[i];
                    if (other.IsOn && i != buttonindex)
                        other.OnClick();
                }
                UpdateSkill();
                return;
            }
        }
    }

    private void OnPrevious()
    {
        if (Charactors == null) return;
        if (CurrentIndex <= 0) return;
        CurrentIndex--;
        UpdateCurrent();
    }

    private void OnNext()
    {
        if (Charactors == null) return;
        if (CurrentIndex + 1 >= Charactors.Count()) return;
        CurrentIndex++;
        UpdateCurrent();
    }

    private void OnReturn()
    {
        GameObject canvas = gameObject.transform.parent.gameObject;
        CharactorGallery gallery = canvas.GetComponentInChildren<CharactorGallery>();
        App.Show(gallery);
        App.Hide(this);
        Previous.interactable = false;
        Next.interactable = false;
        Return.interactable = false;
    }

    #endregion
}
