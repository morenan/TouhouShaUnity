using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class CharactorSelectPanel : MonoBehaviour
{
    #region Number

    private SelectCharactorBoardCore core;
    public SelectCharactorBoardCore Core
    {
        get
        {
            return this.core;
        }
        set
        {
            this.core = value;
            if (core == null)
            {
                EnterOrLeaveTimeout();
                return;
            }

            GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
            while (items.Count() < core.Charactors.Count())
            {
                GameObject go0 = items[0].gameObject;
                GameObject go1 = GameObject.Instantiate(go0, Grid);
                CharactorBe item = go1.GetComponent<CharactorBe>();
                items.Add(item);
            }
            if (gb != null)
            {
                RectTransform gb_rt = gb.gameObject.GetComponent<RectTransform>();
                float maxwidth = gb_rt.rect.width - 64;
                float maxheight = gb_rt.rect.height - 160;
                float totalwidth = 0;
                float totalheight = 0;
                int columns = 0;
                for (int i = 0; i < core.Charactors.Count(); i++)
                {
                    CharactorBe item = items[i];
                    RectTransform item_rt = item.gameObject.GetComponent<RectTransform>();
                    if (totalwidth + item_rt.rect.width > maxwidth) break;
                    totalwidth += item_rt.rect.width;
                    totalheight = item_rt.rect.height;
                    columns++;
                }
                if (columns > 0)
                    totalheight *= ((core.Charactors.Count() - 1) / columns) + 1;
                Grid.sizeDelta = new Vector2(totalwidth, totalheight);
                scrollrect.horizontalNormalizedPosition = 0;
                scrollrect.verticalNormalizedPosition = 1;
                for (int i = 0; i < core.Charactors.Count(); i++)
                {
                    CharactorBe item = items[i];
                    RectTransform item_rt = item.gameObject.GetComponent<RectTransform>();
                    int xi = i % columns;
                    int yi = i / columns;
                    Vector3 p = item_rt.localPosition;
                    item.Core = core.Charactors[i];
                    p.x = xi * item_rt.rect.width;
                    p.y = -yi * item_rt.rect.height;
                    item_rt.localPosition = p;
                    App.Show(item);
                }
                for (int i = 0; i < core.Charactors.Count(); i++)
                {
                    CharactorBe item = items[i];
                    RectTransform item_rt = item.gameObject.GetComponent<RectTransform>();
                }
                for (int i = core.Charactors.Count(); i < items.Count(); i++)
                {
                    CharactorBe item = items[i];
                    App.Hide(item);
                }
                CenterResize cr = gameObject.GetComponent<CenterResize>();
                if (cr != null)
                {
                    cr.Width = Math.Min(maxwidth, totalwidth) + 80;
                    cr.Height = Math.Min(maxheight, totalheight) + 200;
                }
            }

            message.text = core.Message;
            EnterOrLeaveTimeout();
        }
    }

    public RectTransform Grid;

    private List<CharactorBe> items = new List<CharactorBe>();
    private Scrollbar timeout;
    private Text message;
    private ScrollRect scrollrect;
    private bool iscountingtime;
    private float timemax = 100;
    private float timeremain = 100;

    #endregion

    #region MonoBehavior

    void Awake()
    {
        scrollrect = GetComponentInChildren<ScrollRect>();
        foreach (Scrollbar scrollbar in gameObject.GetComponentsInChildren<Scrollbar>())
        {
            switch (scrollbar.gameObject.name)
            {
                case "Timeout":
                    timeout = scrollbar;
                    message = timeout.gameObject.GetComponentInChildren<Text>();
                    break;
            }
        }
        foreach (CharactorBe item in Grid.gameObject.GetComponentsInChildren<CharactorBe>())
        {
            if (items.Contains(item)) continue;
            items.Add(item);
            App.Hide(item);
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
                GameBoard gb = gameObject.transform.GetComponentInParent<GameBoard>();
                iscountingtime = false;
                // 随机选择一个角色。
                System.Random random = new System.Random();
                Charactor char0 = Core.Charactors[random.Next() % Core.Charactors.Count];
                Select(char0);
            }
        }
    }

    #endregion

    #region Method

    /// <summary>
    /// 进入或者退出计时。
    /// </summary>
    protected void EnterOrLeaveTimeout()
    {
        if (Core != null)
        {
            if (!iscountingtime)
            {
                timemax = Core.Timeout;
                timeremain = timemax;
                iscountingtime = true;
            }
        }
        else
        {
            if (iscountingtime)
            {
                iscountingtime = false;
                timemax = 100;
                timeremain = 100;
            }
        }
    }

    public void Select(Charactor char0)
    {
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        gb?.SelectCharactor(char0);
    }

    #endregion 

}
