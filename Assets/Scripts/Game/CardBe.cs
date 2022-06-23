using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;

public class CardBe : MonoBehaviour
{
    #region Resources

    /// <summary>
    /// 一张卡在界面上默认的呈现宽度。
    /// </summary>
    static public float DefaultWidth
    {
        get
        {
            GameObject go0 = Resources.Load<GameObject>("Card");
            RectTransform rt = go0.GetComponent<RectTransform>();
            return rt.rect.width;
        }
    }

    /// <summary>
    /// 一张卡在界面上默认的呈现高度。
    /// </summary>
    static public float DefaultHeight
    {
        get
        {
            GameObject go = Resources.Load<GameObject>("Card");
            RectTransform rt = go.GetComponent<RectTransform>();
            return rt.rect.height;
        }
    }

    /// <summary>
    /// 当前桌面阶段id。
    /// </summary>
    /// <remarks>
    /// 以以下三因素划分：
    /// 1. 玩家阶段更变时。
    /// 2. 初动(没有Reason)的卡发出事件。
    /// 3. 初动(没有Reason)的技能发出事件。
    /// 小于当前Phase一格的卡半显示，小于当前Phase两格以上的卡隐藏。
    /// </remarks>
    static public int PhaseNow;

    /// <summary>
    /// 正在拖动的卡片。同时只能有一个卡片拖动。
    /// </summary>
    static public CardBe Dragging;

    /// <summary>
    /// 按下保持不动时计时，超时显示卡牌信息。
    /// </summary>
    private float PressingTime = 1.5f;

    #endregion 

    #region Number

    private Card core;
    public Card Core
    {
        get
        {
            return this.core;
        }
        set
        {
            if (core != null)
                core.PropertyChanged -= OnCorePropertyChanged;
            this.core = value;
            if (core != null)
                core.PropertyChanged += OnCorePropertyChanged;

            UpdateFace();
            UpdateComment();
        }
    }

    private bool isfacedown = false;
    public bool IsFaceDown
    {
        get
        {
            return this.isfacedown;
        }
        set
        {
            this.isfacedown = value;
            UpdateFace();
            UpdateComment();
        }
    }

    public bool CanDrag = false;

    public bool IsEnterSelecting = false;

    public bool CanSelect = false;

    public bool IsSelected = false;

    public bool IsLocalPosition = false;

    public bool IsDesktopMoveOut = false;

    public string DesktopComment = null;

    public Vector3 Position;

    private float opacity = 1.0f;
    public float Opacity
    {
        get
        {
            return this.opacity;
        }
        set
        {
            if (opacity == value) return;
            this.opacity = value;
            foreach (Image image in gameObject.GetComponentsInChildren<Image>())
            {
                Color c = image.color;
                c.a = value;
                image.color = c;
            }
            foreach (Text text in gameObject.GetComponentsInChildren<Text>())
            {
                Color c = text.color;
                c.a = value;
                text.color = c;
            }
        }
    }

    private Color color = new Color(1.0f, 1.0f, 1.0f);
    public Color Color
    {
        get
        {
            return this.color;
        }
        set
        {
            if (color == value) return;
            this.color = value;
            foreach (Image image in gameObject.GetComponentsInChildren<Image>())
            {
                Color c = image.color;
                c.r = color.r;
                c.g = color.g;
                c.b = color.b;
                image.color = c;
            }
        }
    }

    public int PhaseId;

    private bool istouched;
    private bool isdragging;
    private Vector2 dragpos;

    private Button button;
    private Image cardimage;
    private Text colortext;
    private Text pointtext;
    private Text comment;

    private bool _toupdate_comment;

    private List<CardAnim> anims = new List<CardAnim>();

    #endregion

    #region MonoBehavior

    void Awake()
    {
        button = gameObject.GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        cardimage = gameObject.GetComponent<Image>();
        for (int i0 = 0; i0 < gameObject.transform.childCount; i0++)
        {
            Transform t0 = gameObject.transform.GetChild(i0);
            GameObject g0 = t0.gameObject;
            switch (g0.name)
            {
                case "Color":
                    colortext = g0.GetComponent<Text>();
                    break;
                case "Point":
                    pointtext = g0.GetComponent<Text>();
                    break;
                case "Comment":
                    comment = g0.GetComponent<Text>();
                    break;
            }
        }

        UpdateFace();
        UpdateComment();
    }

    void Update()
    {
        #region 触碰
        #region 拖动状态
        if (isdragging)
        {
            #region 检查是否离开拖动
            bool leave = false;
            if (istouched)
            {
                leave |= Input.touchCount != 1;
                if (!leave)
                {
                    Touch touch = Input.GetTouch(0);
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                        case TouchPhase.Stationary:
                        case TouchPhase.Moved:
                            break;
                        default:
                            leave = true;
                            break;
                    }
                }
            }
            else
            {
                leave |= Input.GetMouseButtonUp(0);
            }
            #endregion
            #region 离开拖动
            if (leave)
            {
                isdragging = false;
                Dragging = null;
                // 通知桌面面板拖动完毕
                DesktopPanel dp = gameObject.GetComponentInParent<DesktopPanel>();
                if (dp != null) dp.Drop(this);
                // 通知主面板拖动完毕
                else
                {
                    GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
                    if (gb != null) gb.Drop(this);
                }
            }
            #endregion
        }
        #endregion
        #region 空闲状态
        else
        {
            #region 检查是否进入拖动
            if (CanDrag && Dragging == null) 
            { 
                if (Input.touchCount == 1)
                {
                    Touch touch = Input.GetTouch(0);
                    switch (touch.phase)
                    {
                        case TouchPhase.Moved:
                            istouched = true;
                            DragEnter(touch.position);
                            break;
                    }
                }
                else if (Input.GetMouseButtonDown(0))
                {
                    Vector3 mp = Input.mousePosition;
                    istouched = false;
                    DragEnter(mp);
                }
            }
            #endregion
        }
        #endregion 
        #endregion
        #region 位置/颜色/透明度
        #region 拖动状态
        if (isdragging)
        {
            if (istouched)
            {
                if (Input.touchCount == 1)
                {
                    Touch touch = Input.GetTouch(0);
                    Vector3 p = transform.position;
                    p.x = touch.position.x + dragpos.x;
                    p.y = touch.position.y + dragpos.y;
                    transform.position = p;
                }
            }
            else
            {
                Vector3 p = transform.position;
                p.x = Input.mousePosition.x + dragpos.x;
                p.y = Input.mousePosition.y + dragpos.y;
                transform.position = p;
                print(String.Format("Dragging Mouse={0} p={1}", Input.mousePosition, p));
            }
            // 通知桌面面板拖动更变
            DesktopPanel dp = gameObject.GetComponentInParent<DesktopPanel>();
            if (dp != null) dp.DragMove(this);
            // 通知主面板拖动更变
            else
            {
                GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
                if (gb != null) gb.DragMove(this);
            }

        }
        #endregion
        #region 选择状态
        else if (IsEnterSelecting)
        {
            if (IsDesktopMoveOut)
            {
                Opacity = 0.75f;
                Color = new Color(0.5f, 0.5f, 0.5f);
            }
            else if (IsSelected)
            {
                Opacity = 1.0f;
                Color = new Color(0.5f, 1.0f, 0.5f);
            }
            else if (CanSelect)
            {
                Opacity = 1.0f;
                Color = new Color(1.0f, 1.0f, 1.0f);
            }
            else
            {
                Opacity = 0.75f;
                Color = new Color(0.5f, 0.5f, 0.5f);
            }
            if (IsLocalPosition)
                transform.localPosition = Position;
            else 
                transform.position = Position;
        }
        #endregion
        #region 动画状态 
        else if (anims.Count() > 0)
        {
            Color = new Color(1.0f, 1.0f, 1.0f);
            foreach (CardAnim anim in anims.ToArray())
            {
                if (anim is CardMove)
                {
                    CardMove move = (CardMove)anim;
                    Position = move.GetPoint();
                }
                else if (anim is CardShowWait)
                {
                    CardShowWait showwait = (CardShowWait)anim;
                    Opacity = showwait.GetOpacity();
                }
                else if (anim is CardWaitHide)
                {
                    CardWaitHide waithide = (CardWaitHide)anim;
                    Opacity = waithide.GetOpacity();
                }
                else if (anim is CardShowWaitHide)
                {
                    CardShowWaitHide showwaithide = (CardShowWaitHide)anim;
                    Opacity = showwaithide.GetOpacity();
                }
                if (anim.TotalTime >= anim.MaxTime)
                {
                    anims.Remove(anim);
                    if (anim is CardWaitHide
                     || anim is CardShowWaitHide)
                    {
                        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
                        gb?.Hide(this);
                    }
                }
            }
            if (IsLocalPosition)
                transform.localPosition = Position;
            else
                transform.position = Position;
        }
        #endregion
        #region 默认状态
        else
        {
            Opacity = 1.0f;
            Color = new Color(1.0f, 1.0f, 1.0f);
            if (IsLocalPosition)
                transform.localPosition = Position;
            else
                transform.position = Position;
        }
        #endregion
        #endregion
        #region 更新
        if (_toupdate_comment)
            UpdateComment();
        #endregion 
        #region 长按提示
        {
            GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
            RectTransform rt = GetComponent<RectTransform>();
            bool istouched = false;
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    case TouchPhase.Stationary:
                        if (Math.Abs(touch.position.x - transform.position.x) <= rt.rect.width / 2
                         && Math.Abs(touch.position.y - transform.position.y) <= rt.rect.height / 2)
                            istouched = true;
                        break;
                }
            }
            else if (Input.GetMouseButton(0))
            {
                if (Math.Abs(Input.mousePosition.x - transform.position.x) <= rt.rect.width / 2
                 && Math.Abs(Input.mousePosition.y - transform.position.y) <= rt.rect.height / 2)
                    istouched = true;
            }
            if (!istouched)
            {
                PressingTime = 1.5f;
                if (gb != null && gb.UI_CardTooltip.Card == this)
                    gb.UI_CardTooltip.Card = null;
            }
            else if ((PressingTime -= Time.deltaTime) <= 0.0f)
            {
                PressingTime = 0.0f;
                if (gb != null && gb.UI_CardTooltip.Card == null)
                    gb.UI_CardTooltip.Card = this;
            }
        }
        #endregion
    }

    #endregion

    #region Method

    #region Text

    protected void SetColor(Text text, Card card)
    {
        if (card.CardColor?.SeemAs(Enum_CardColor.Red) == true)
            text.color = new Color(1.0f, 0.0f, 0.0f);
        else
            text.color = new Color(0.0f, 0.0f, 0.0f);
    }

    protected void SetEmoji(Text text, Card card)
    {
        switch (card.CardColor?.E)
        {
            case Enum_CardColor.Heart: text.text = "♥"; break;
            case Enum_CardColor.Diamond: text.text = "♦"; break;
            case Enum_CardColor.Spade: text.text = "♠"; break;
            case Enum_CardColor.Club: text.text = "♣"; break;
        }
    }

    protected void SetPoint(Text text, Card card)
    {
        switch (card.CardPoint)
        {
            case 1: text.text = "A"; break;
            case 11: text.text = "J"; break;
            case 12: text.text = "Q"; break;
            case 13: text.text = "K"; break;
            default: text.text = card.CardPoint.ToString(); break;
        }
    }

    #endregion

    #region Update

    protected void UpdateFace()
    {
        if (IsFaceDown)
        {
            if (cardimage != null)
                cardimage.sprite = ImageHelper.CreateSprite("FaceDown");
            if (colortext != null)
                colortext.text = "";
            if (pointtext != null)
                pointtext.text = "";
            return;
        }
        if (core == null) return;
        Card initcard = core.GetInitialCards().FirstOrDefault();
        if (initcard == null) return;
        if (cardimage != null)
            cardimage.sprite = ImageHelper.CreateSprite(initcard);
        if (colortext != null)
        {
            SetColor(colortext, initcard);
            SetEmoji(colortext, initcard);
        }
        if (pointtext != null)
        {
            SetColor(pointtext, initcard);
            SetPoint(pointtext, initcard);
        }
    }

    protected void UpdateComment()
    {
        _toupdate_comment = false;
        if (IsFaceDown)
        {
            if (comment != null)
                comment.text = "";
            return;
        }
        if (core == null) return;
        if (comment != null)
            comment.text = core.Comment;
    }

    protected void UpdateSelect()
    {
        
    }

    #endregion

    #region Animation
   
    public float GetEllapsedTime()
    {
        float time = 0.0f;
        foreach (CardAnim anim in anims)
            time = Math.Max(time, anim.MaxTime - anim.TotalTime);
        return time;
    }

    public void Move(Vector3 p)
    {
        Vector3 p0 = Position;
        foreach (CardAnim anim in anims.ToArray())
            if (anim is CardMove) anims.Remove(anim);
        anims.Add(new CardMove() { From = p0, To = p });
    }

    public void ShowWait()
    {
        foreach (CardAnim anim in anims.ToArray())
        {
            if (anim is CardShowWait) anims.Remove(anim);
            else if (anim is CardWaitHide) anims.Remove(anim);
            else if (anim is CardShowWaitHide) anims.Remove(anim);
        }
        anims.Add(new CardShowWait());
    }

    public void WaitHide()
    {
        foreach (CardAnim anim in anims.ToArray())
        {
            if (anim is CardShowWait) anims.Remove(anim);
            else if (anim is CardWaitHide) anims.Remove(anim);
            else if (anim is CardShowWaitHide) anims.Remove(anim);
        }
        anims.Add(new CardWaitHide());
    }

    public void ShowWaitHide()
    {
        foreach (CardAnim anim in anims.ToArray())
        {
            if (anim is CardShowWait) anims.Remove(anim);
            else if (anim is CardWaitHide) anims.Remove(anim);
            else if (anim is CardShowWaitHide) anims.Remove(anim);
        }
        anims.Add(new CardShowWaitHide());
    }
    public void ImmediatelyHide()
    {
        foreach (CardAnim anim in anims.ToArray())
        {
            if (anim is CardShowWait) anims.Remove(anim);
            else if (anim is CardWaitHide) anims.Remove(anim);
            else if (anim is CardShowWaitHide) anims.Remove(anim);
        }
        anims.Add(new CardWaitHide() { WaitTime = 0, MaxTime = 0.2f });
    }

    #endregion

    #region Drag & Drop
    
    public void DragEnter(Vector3 p)
    {
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        if (Math.Abs(rt.position.x - p.x) > rt.rect.width / 2) return;
        if (Math.Abs(rt.position.y - p.y) > rt.rect.height / 2) return;
        dragpos = new Vector2();
        dragpos.x = transform.position.x - p.x;
        dragpos.y = transform.position.y - p.y;
        isdragging = true;
        Dragging = this;
        // 通知桌面面板进入拖动
        DesktopPanel dp = gameObject.GetComponentInParent<DesktopPanel>();
        if (dp != null)
        {
            dp.DragEnter(this);
            dp.DragMove(this);
        }
        // 通知主面板拖动完毕
        else
        {
            GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
            if (gb != null)
            {
                gb.DragEnter(this);
                gb.DragMove(this);
            }
        }
    }

    #endregion 

    #endregion

    #region Event Handler

    private void OnCorePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "Comment":
                _toupdate_comment = true;
                //UpdateComment();
                break;  
        }
    }

    private void OnClick()
    {
        //if (isdragging) return;
        // 通知桌面面板点击
        DesktopPanel dp = gameObject.GetComponentInParent<DesktopPanel>();
        if (dp != null) dp.Click(this);
        // 通知主面板点击
        else
        {
            GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
            if (gb != null) gb.CardView_MouseDown(this);
        }
    }

    #endregion
}

public class CardAnim
{
    public float MaxTime = 0.5f;
    public float TotalTime = 0.0f;
    public CardRate Rate = new CardLinearRate();
}

public class CardMove : CardAnim
{
    public Vector3 From;
    public Vector3 To;

    public CardMove()
    {
        Rate = new CardCubeFasterRate();
    }

    public Vector3 GetPoint()
    {
        Vector3 p = From + (To - From) * (Rate?.GetRate(TotalTime, MaxTime) ?? 1.0f);
        p.z = To.z;
        TotalTime = Math.Min(MaxTime, TotalTime + Time.deltaTime);
        return p;
    }
}

public class CardShowWait : CardAnim
{
    public float WaitTime = 0.4f;
    public float GetOpacity()
    {
        float opacity = 1.0f;
        if (TotalTime >= TotalTime - WaitTime) opacity = 1.0f;
        else opacity = (Rate?.GetRate(TotalTime, MaxTime - WaitTime) ?? 1.0f);
        TotalTime = Math.Min(MaxTime, TotalTime + Time.deltaTime);
        return opacity;
    }
}

public class CardWaitHide : CardAnim
{
    public float WaitTime = 0.4f;
    public float GetOpacity()
    {
        float opacity = 1.0f;
        if (TotalTime < WaitTime) opacity = 1.0f;
        else opacity = 1.0f - (Rate?.GetRate(TotalTime - WaitTime, MaxTime - WaitTime) ?? 1.0f);
        TotalTime = Math.Min(MaxTime, TotalTime + Time.deltaTime);
        return opacity;
    }
}

public class CardShowWaitHide : CardAnim
{
    public float ShowTime = 0.1f;
    public float WaitTime = 0.3f;

    public float GetOpacity()
    {
        float opacity = 1.0f;
        if (TotalTime < ShowTime) opacity = Rate?.GetRate(TotalTime, ShowTime) ?? 1.0f;
        else if (TotalTime < ShowTime + WaitTime) opacity = 1.0f;
        else opacity = 1.0f - (Rate?.GetRate(TotalTime - ShowTime - WaitTime, MaxTime - ShowTime - WaitTime) ?? 1.0f);
        TotalTime = Math.Min(MaxTime, TotalTime + Time.deltaTime);
        return opacity;
    }
}


public abstract class CardRate
{
    abstract public float GetRate(float time, float timemax);
}

public class CardLinearRate : CardRate
{
    public override float GetRate(float time, float timemax)
    {
        return 1.0f * time / timemax;
    }
}

public class CardSquireFasterRate : CardRate
{
    public override float GetRate(float time, float timemax)
    {
        return (float)(1 - Math.Pow(timemax - time, 2) / Math.Pow(timemax, 2));
    }
}

public class CardCubeFasterRate : CardRate
{
    public override float GetRate(float time, float timemax)
    {
        return (float)(1 - Math.Pow(timemax - time, 3) / Math.Pow(timemax, 3));
    }
}

public class CardSquireRate : CardRate
{
    public override float GetRate(float time, float timemax)
    {
        return 1.0f * time * time / timemax / timemax;
    }
}

