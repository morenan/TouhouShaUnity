using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class KOFPanel : MonoBehaviour
{
    #region Number

    private KOFSelectCore core;
    public KOFSelectCore Core
    {
        get
        {
            return this.core;
        }
        set
        {
            if (core != null) core.Selected -= OnCoreSelected;
            this.core = value;
            if (core == null) return;
            core.Selected += OnCoreSelected;
            GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
            Player currentplayer = gb?.CurrentPlayer;
            int itemindex = 0;
            int columns = core.Charactors.Count() / 2;
            foreach (KeyValuePair<Player, List<Charactor>> kvp in core.PlayerSelecteds)
            {
                int row = 0;
                if (kvp.Key != currentplayer) row = 3;
                for (int column = 0; column < kvp.Value.Count; column++)
                {
                    CharactorBe item = GetItem(ref itemindex);
                    RectTransform item_rt = item.gameObject.GetComponent<RectTransform>();
                    item.Core = kvp.Value[column];
                    App.Show(item);
                    item_rt.localPosition = new Vector3(
                        16 + column * item_rt.rect.width,
                        -16 - row * item_rt.rect.height);
                }
            }
            for (int i = 0; i < core.Charactors.Count(); i++)
            {
                if (core.Charactors[i] == null) continue;
                CharactorBe item = GetItem(ref itemindex);
                RectTransform item_rt = item.gameObject.GetComponent<RectTransform>();
                int column = i % columns;
                int row = i / columns + 1;
                item.Core = core.Charactors[i];
                App.Show(item);
                item_rt.localPosition = new Vector3(
                    16 + column * item_rt.rect.width,
                    -16 - row * item_rt.rect.height);
            }
            while (itemindex < items.Count())
            {
                CharactorBe item = items[itemindex++];
                item.Core = null;
                App.Hide(item);
            }
            {
                CharactorBe item = items[0];
                RectTransform item_rt = item.gameObject.GetComponent<RectTransform>();
                scrollrect.content.sizeDelta = new Vector2(
                    32 + item_rt.rect.width * columns,
                    32 + item_rt.rect.height * 4);
            }
            CenterResize resize = gameObject.GetComponent<CenterResize>();
            if (resize != null)
            {
                RectTransform item_rt = items[0].gameObject.GetComponent<RectTransform>();
                resize.Width = columns * item_rt.rect.width + 32;
                resize.Height = 4 * item_rt.rect.height + 80;
            }
        }
    }

    private Text message;
    private Scrollbar timeout;
    private ScrollRect scrollrect;
    private List<CharactorBe> items = new List<CharactorBe>();
    private bool iscountingtime;
    private float timemax = 100;
    private float timeremain = 100;

    private float maxtime = 0.5f;
    private float totaltime = 0.0f;
    private KOFPanelAnimStatus status;
    private CharactorBe moveditem;
    private Vector3 movefrom;
    private Vector3 moveto;
    private KOFPanelActionStatus action;

    private Futex dispatcher_futex = new Futex();
    private Queue<KOFSelectEventArgs> dispatcher_queue = new Queue<KOFSelectEventArgs>();

    #endregion

    #region MonoBehavior

    void Awake()
    {
        scrollrect = gameObject.GetComponentInChildren<ScrollRect>();
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
        foreach (CharactorBe item in scrollrect.content.GetComponentsInChildren<CharactorBe>())
        {
            if (items.Contains(item)) continue;
            items.Add(item);
            App.Hide(item);
        }
    }

    void Update()
    {
        #region 动画
        switch (status)
        {
            #region 移动卡牌
            case KOFPanelAnimStatus.MoveCard:
                {
                    if (totaltime <= maxtime)
                    {
                        Vector3 p = movefrom + (moveto - movefrom) * totaltime / maxtime;
                        if (moveditem != null)
                            moveditem.gameObject.transform.position = p;
                        totaltime += Time.deltaTime;
                    }
                    else
                    {
                        status = KOFPanelAnimStatus.None;
                    }
                    break;
                }
            #endregion
            #region 空闲加载
            case KOFPanelAnimStatus.None:
                {
                    KOFSelectEventArgs ev = null;
                    dispatcher_futex.Invoke(() =>
                    {
                        if (dispatcher_queue.Count() > 0)
                            ev = dispatcher_queue.Dequeue();
                    });
                    if (ev != null)
                    {
                        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
                        moveditem = items.FirstOrDefault(_item => _item.Core == ev.Charactor);
                        if (moveditem != null)
                        {
                            RectTransform rt = moveditem.gameObject.GetComponent<RectTransform>();
                            movefrom = rt.localPosition;
                            moveto = new Vector3(
                                16 + rt.rect.width * ev.IndexTo,
                                -16 - rt.rect.height * (ev.Player == gb.CurrentPlayer ? 0 : 3));
                            totaltime = 0;
                            status = KOFPanelAnimStatus.MoveCard;
                        }
                    }
                    break;
                }
            #endregion
        }
        #endregion
        #region 计时
        if (iscountingtime)
        {
            timeremain = Math.Max(0, timeremain - Time.deltaTime);
            timeout.size = timeremain / timemax;
            if (timeremain <= 0)
            {
                GameBoard gb = gameObject.transform.GetComponentInParent<GameBoard>();
                Charactor remain = null;
                LeaveTimeout();
                for (int i = 0; i < Core.Charactors.Count; i++)
                {
                    if (Core.Charactors[i] != null) remain = Core.Charactors[i];
                    if (remain != null) break;
                }
                if (remain == null)
                {
                    action = KOFPanelActionStatus.WatingSelecting;
                    gb?.Console?.WorldContiune();
                }
                else
                {
                    Select(remain);
                }
            }
        }
        #endregion
    }

    #endregion

    #region Method

    protected CharactorBe GetItem(ref int itemindex)
    {
        while (itemindex >= items.Count())
        {
            GameObject go0 = items[0].gameObject;
            GameObject go1 = GameObject.Instantiate(go0, scrollrect.content);
            CharactorBe item = go1.GetComponent<CharactorBe>();
            items.Add(item);
        }
        return items[itemindex++];
    }

    public void BeginSelect()
    {
        if (Core == null) return;

        List<List<Charactor>> charlists = Core.PlayerSelecteds.Values.ToList();

        status = KOFPanelAnimStatus.None;
        action = charlists[0].Count() == charlists[1].Count()
            ? KOFPanelActionStatus.SelectingTwo
            : KOFPanelActionStatus.SelectingOne;
        message.text = "请选择一名角色。";
        EnterTimeout();
    }
    
    public void Select(Charactor char0)
    {
        if (Core == null) return;
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        if (gb == null) return;
        LeaveTimeout();
        Core.Select(gb.CurrentPlayer, char0);

        switch (action)
        {
            case KOFPanelActionStatus.SelectingOne:
                Charactor remain = null;
                for (int i = 0; i < Core.Charactors.Count; i++)
                {
                    if (Core.Charactors[i] != null) remain = Core.Charactors[i];
                    if (remain != null) break;
                }
                if (remain == null)
                {
                    action = KOFPanelActionStatus.WatingSelecting;
                    gb?.Console?.WorldContiune();
                    break;
                }
                action = KOFPanelActionStatus.SelectingTwo;
                message.text = "请再选择一名角色。";
                EnterTimeout();
                break;
            case KOFPanelActionStatus.SelectingTwo:
                action = KOFPanelActionStatus.WatingSelecting;
                message.text = "请等待对方选择完毕。";
                gb?.Console?.WorldContiune();
                break;
        }
    }

    public void EnterTimeout()
    {
        if (core == null) return;
        timemax = core.Timeout;
        timeremain = core.Timeout;
        iscountingtime = true;
    }

    public void LeaveTimeout()
    {
        if (core == null) return;
        iscountingtime = false;
        timemax = core.Timeout;
        timeremain = core.Timeout;
    }

    #endregion

    #region Event Handler

    private void OnCoreSelected(object sender, KOFSelectEventArgs e)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(e); });
    }

    #endregion
}

public enum KOFPanelAnimStatus
{
    None,
    MoveCard,
}

public enum KOFPanelActionStatus
{
    SelectingOne,
    SelectingTwo,
    WatingSelecting,
}
