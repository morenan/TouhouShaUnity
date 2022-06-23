using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class PlayerTooltip : MonoBehaviour
{
    #region Member

    private PlayerBe player;
    public PlayerBe Player
    {
        get
        {
            return this.player;
        }
        set
        {
            this.player = value;
            if (player != null) App.Show(this); else App.Hide(this);
            UpdateText();
        }
    }

    public Text Text;

    #endregion

    #region Mono

    void Update()
    {
        if (Player == null) return;
        RectTransform rt_player = Player.transform as RectTransform;
        RectTransform rt_canvas = gameObject.transform.parent as RectTransform;
        RectTransform rt_this = gameObject.GetComponent<RectTransform>();
        float prefer_y = rt_player.position.y - rt_player.rect.height / 2;
        prefer_y = Math.Min(prefer_y, rt_canvas.rect.height - rt_this.rect.height);
        if (rt_player.position.x - rt_player.rect.width / 2 >= rt_canvas.rect.width / 2)
        {
            rt_this.position = new Vector3(
                rt_player.position.x - rt_player.rect.width / 2 - rt_this.rect.width,
                prefer_y);
        }   
        else
        {
            rt_this.position = new Vector3(
                rt_player.position.x + rt_player.rect.width / 2,
                prefer_y);
        }
    }

    #endregion

    #region Method

    public void UpdateText()
    {
        if (this.player == null) return;
        Player player = this.player.Core;
        if (player == null) return;
        StringBuilder sb = new StringBuilder();
        foreach (Skill skill in player.Skills)
        {
            SkillInfo si = skill.GetInfo();
            sb.Append("<color=#008000>");
            sb.Append("[");
            sb.Append(si.Name);
            sb.Append("]");
            sb.Append("</color>");
            sb.Append(si.Description);
            sb.Append("\n");
        }
        Text.text = sb.ToString();
    }

    #endregion

}
