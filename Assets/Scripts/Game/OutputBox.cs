using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;
using TouhouSha.Core.UIs.Texts;
using Com.Morenan.TouhouSha;
using Photon.Pun;

public class OutputBox : MonoBehaviour
{
    #region Number

    private ScrollRect scrollrect;
    private InputField inputfield;
    private Button enter;
    private List<Text> items = new List<Text>();
    private int itemindex = 0;
    private bool pagedown = false;

    #endregion

    #region MonoBehavior

    void Awake()
    {
        scrollrect = gameObject.GetComponentInChildren<ScrollRect>();
        scrollrect.verticalScrollbar.onValueChanged.AddListener(OnVerticalOffsetChanged);
        inputfield = gameObject.GetComponentInChildren<InputField>();
        enter = gameObject.GetComponentInChildren<Button>();
        enter.onClick.AddListener(OnEnter);
        foreach (Text item in scrollrect.content.GetComponentsInChildren<Text>())
        {
            if (items.Contains(item)) continue;
            items.Add(item);
            App.Hide(item);
        }
    }

    void Update()
    {
        if (pagedown && Input.touchCount == 0)
            scrollrect.verticalNormalizedPosition = 0;
    }

    #endregion

    #region Method

    public void AppendLine(string text)
    {
        while (itemindex >= items.Count())
        {
            GameObject go0 = items[0].gameObject;
            GameObject go1 = GameObject.Instantiate(go0, scrollrect.content);
            Text item = go1.GetComponent<Text>();
            items.Add(item);
        }
        {
            Text item = items[itemindex++];
            item.text = text;
            App.Show(item);
            pagedown = true;
        }
    }

    public void AppendLine(ShaText shatext)
    {
        StringBuilder sb = new StringBuilder();
        foreach (ShaShowText showtext in shatext.GetShowTexts())
        {
            ShaBrush32 fg = showtext.Foreground as ShaBrush32 ?? new ShaBrush32() { A = 0xFF, R = 0xFF, G = 0xFF, B = 0xFF };
            if (fg.R == 0xFF && fg.B == 0xFF && fg.G == 0xFF)
            {
                sb.Append(showtext.Text);
                continue;
            }
            sb.Append(String.Format("<color=#{0:X2}{1:X2}{2:X2}>", fg.R, fg.G, fg.B));
            sb.Append(showtext.Text);
            sb.Append("</color>");
        }
        AppendLine(sb.ToString());
    }


    public void AppendLine(Player player, string text)
    {
        AppendLine(text);   
    }

    public void AppendLine(Player player, ShaText shatext)
    {
        AppendLine(shatext);
    }

    public void AppendComment(Player player, string text)
    {
        AppendLine(String.Format("{0}: {1}", player.Name, text));
    }

    public void NextPhase()
    {
        AppendLine("----------------------");
    }

    public string GetEnterText()
    {
        if (inputfield == null) return String.Empty;
        return inputfield.text;
    }

    public void ClearEnterText()
    {
        if (inputfield == null) return;
        inputfield.text = "";
    }

    #endregion

    #region Event Handler

    private void OnVerticalOffsetChanged(float arg0)
    {
        pagedown = false;
    }

    private void OnEnter()
    {
        if (PhotonNetwork.LocalPlayer != null)
            AppendLine(String.Format("{0}：{1}", PhotonNetwork.LocalPlayer.NickName, GetEnterText()));
        LobbyManager lm = gameObject.GetComponentInParent<LobbyManager>();
        if (lm != null)
        {
            lm.Comment(GetEnterText());
            ClearEnterText();
            return;
        }
    }

    #endregion

}
