using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using System.Collections;
using TouhouSha.Koishi.Cards.Weapons;
using TouhouSha.Koishi.Cards;

public class PlayerBe : MonoBehaviour, IGameBoardArea
{
    #region Number

    private Player core;
    public Player Core
    {
        get
        {
            return this.core;
        }
        set
        {
            if (core != null)
            {
                core.ShaPropertyChanged -= OnPlayerShaPropertyChanged;
                core.Zones.CollectionChanged -= OnPlayerZonesCollectionChanged;
                core.Symbols.CollectionChanged -= OnPlayerSymbolCollectionChanged;
                foreach (Zone zone in core.Zones)
                    zone.ExternZones.CollectionChanged -= OnZoneExternsCollectionChanged;
            }
            this.core = value;
            if (core != null)
            {
                core.ShaPropertyChanged += OnPlayerShaPropertyChanged;
                core.Zones.CollectionChanged += OnPlayerZonesCollectionChanged;
                core.Symbols.CollectionChanged += OnPlayerSymbolCollectionChanged;
                foreach (Zone zone in core.Zones)
                    zone.ExternZones.CollectionChanged += OnZoneExternsCollectionChanged;
            }

            FindAllZones();
            UpdateInfo();
            UpdateHp();
            UpdateEquipItems();
            UpdateJudgeItems();
            UpdateHandNumber();
            UpdateAlive();
            UpdateChained();
            UpdateFacedDown();
            UpdateSymbols();
            UpdateState();
        }
    }

    private EquipZone equipzone;
    public EquipZone EquipZone
    {
        get
        {
            return this.equipzone;
        }
        set
        {
            if (equipzone != null)
                equipzone.EquipsChanged -= EquipZone_EquipsChanged;
            this.equipzone = value;
            if (equipzone != null)
                equipzone.EquipsChanged += EquipZone_EquipsChanged;
        }
    }

    private Zone judgezone;
    public Zone JudgeZone
    {
        get
        {
            return this.judgezone;
        }
        set
        {
            if (judgezone != null)
                judgezone.Cards.CollectionChanged -= JudgeZone_Cards_CollectionChanged;
            this.judgezone = value;
            if (judgezone != null)
                judgezone.Cards.CollectionChanged += JudgeZone_Cards_CollectionChanged;
        }
    }

    private Zone handzone;
    public Zone HandZone
    {
        get
        {
            return this.handzone;
        }
        set
        {
            if (handzone != null)
                handzone.Cards.CollectionChanged -= HandZone_Cards_CollectionChanged;
            this.handzone = value;
            if (handzone != null)
                handzone.Cards.CollectionChanged += HandZone_Cards_CollectionChanged;
        }
    }

    private Vector3 position;
    public Vector3 Position
    {
        get
        {
            return this.position;
        }
        set
        {
            this.position = value;
        }
    }

    public bool IsEnterSelecting = false;

    public bool CanSelect = false;

    public bool IsSelected = false;

    public bool IsBullUp = false;

    private Image playerimage;
    private Button playerbutton;
    private Image blueborder;
    private Image violetborder;
    private Image mask;
    private Text charactorname;
    private Text countryname;
    private Text statename;
    private Dropdown ass;
    private GameObject hpbar;
    private Image hands;
    private Image atkhorse;
    private Image defhorse;
    private Image weapon;
    private Image armor;
    private GameObject judgestack;
    private Image chain;
    private Image die;
    private Image facedown;
    private Scrollbar waitbar;
    private GameObject symbolbox;
    private List<SymbolButton> symbolbuttons = new List<SymbolButton>();

    private Image weapon_icon;
    private Text weapon_color;
    private Text weapon_name;
    private Image armor_icon;
    private Text armor_color;
    private Text armor_name;
    private Text atkhorse_color;
    private Text atkhorse_point;
    private Text defhorse_color;
    private Text defhorse_point;

    private List<Image> hps = new List<Image>();
    private DigitColle digit_hp;
    private DigitColle digit_maxhp;

    private DigitColle handnumber;
    private List<Image> delays = new List<Image>();

    private float ShakeTotalTime = 0.4f;
    private float ShakeMaxTime = 0.4f;
    private Vector3 ShakeVelocity = new Vector3(-8, 0);
    private Vector3 ShakeForce = new Vector3(-1, 0);
    private Rect ShakeBox = new Rect(-60, -60, 120, 120);

    private bool fakewaiting;
    private float fakewait_maxtime;
    private float fakewait_remaintime;

    private float PressingTime = 1.5f;

    private bool _toupdate_hp;
    private bool _toupdate_alive;
    private bool _toupdate_faceddown;
    private bool _toupdate_chained;
    private bool _toupdate_equips;
    private bool _toupdate_judges;
    private bool _toupdate_hands;
    private bool _toupdate_symbols;



    #endregion

    #region MonoBehaviour

    void Awake()
    {
        Color c;

        #region 搜集组件
        for (int i0 = 0; i0 < gameObject.transform.childCount; i0++)
        {
            Transform t0 = gameObject.transform.GetChild(i0);
            GameObject g0 = t0.gameObject;
            switch (g0.name)
            {
                case "PlayerIn":
                    playerimage = g0.GetComponent<Image>();
                    playerbutton = g0.GetComponent<Button>();
                    playerbutton.onClick.AddListener(OnClick);
                    break;
                case "Ass":
                    ass = g0.GetComponent<Dropdown>();
                    break;
                case "Mask":
                    mask = g0.GetComponent<Image>();
                    break;
                case "Symbols":
                    symbolbox = g0;
                    foreach (SymbolButton button in symbolbox.GetComponentsInChildren<SymbolButton>())
                    {
                        if (symbolbuttons.Contains(button)) continue;
                        symbolbuttons.Add(button);
                        button.Parent = this;
                        App.Hide(button, false);
                    }
                    break;
                case "WaitBar":
                    waitbar = g0.GetComponent<Scrollbar>();
                    App.Hide(waitbar);
                    break;
            }
            for (int i1 = 0; i1 < t0.childCount; i1++)
            {
                Transform t1 = t0.GetChild(i1);
                GameObject g1 = t1.gameObject;
                switch (g1.name)
                {
                    case "BlueBorder":
                        blueborder = g1.GetComponent<Image>();
                        break;
                    case "ViolerBorder":
                        violetborder = g1.GetComponent<Image>();
                        break;
                    case "HpBar":
                        hpbar = g1;
                        break;
                    case "HandImage":
                        hands = g1.GetComponent<Image>();
                        break;
                    case "+1":
                        defhorse = g1.GetComponent<Image>();
                        App.Hide(defhorse);
                        break;
                    case "-1":
                        atkhorse = g1.GetComponent<Image>();
                        App.Hide(atkhorse);
                        break;
                    case "Weapon":
                        weapon = g1.GetComponent<Image>();
                        App.Hide(weapon);
                        break;
                    case "Armor":
                        armor = g1.GetComponent<Image>();
                        App.Hide(armor);
                        break;
                    case "Judges":
                        judgestack = g1;
                        break;
                    case "Chain":
                        chain = g1.GetComponent<Image>();
                        App.Hide(chain);
                        break;
                    case "Die":
                        die = g1.GetComponent<Image>();
                        App.Hide(die);
                        break;
                    case "FaceDown":
                        facedown = g1.GetComponent<Image>();
                        App.Hide(facedown);
                        break;
                    case "StateName":
                        statename = g1.GetComponent<Text>();
                        break;
                }
                for (int i2 = 0; i2 < t1.childCount; i2++)
                {
                    Transform t2 = t1.GetChild(i2);
                    GameObject g2 = t2.gameObject;
                    switch (g2.name)
                    {
                        case "CharactorName":
                            charactorname = g2.GetComponent<Text>();
                            break;
                        case "CountryName":
                            countryname = g2.GetComponent<Text>();
                            break;
                    }
                }
            }
        }
        #endregion

        #region 装备栏组件
        if (weapon != null)
        {
            for (int i0 = 0; i0 < weapon.gameObject.transform.childCount; i0++)
            {
                Transform t0 = weapon.gameObject.transform.GetChild(i0);
                GameObject g0 = t0.gameObject;
                switch (g0.name)
                {
                    case "Image":
                        weapon_icon = g0.GetComponent<Image>();
                        break;
                    case "ColorText":
                        weapon_color = g0.GetComponent<Text>();
                        break;
                    case "Name":
                        weapon_name = g0.GetComponent<Text>();
                        break;
                }
            }
        }
        if (armor != null)
        {
            for (int i0 = 0; i0 < armor.gameObject.transform.childCount; i0++)
            {
                Transform t0 = armor.gameObject.transform.GetChild(i0);
                GameObject g0 = t0.gameObject;
                switch (g0.name)
                {
                    case "Image":
                        armor_icon = g0.GetComponent<Image>();
                        break;
                    case "ColorText":
                        armor_color = g0.GetComponent<Text>();
                        break;
                    case "Name":
                        armor_name = g0.GetComponent<Text>();
                        break;
                }
            }
        }
        if (atkhorse != null)
        {
            for (int i0 = 0; i0 < atkhorse.gameObject.transform.childCount; i0++)
            {
                Transform t0 = atkhorse.gameObject.transform.GetChild(i0);
                GameObject g0 = t0.gameObject;
                switch (g0.name)
                {
                    case "ColorText":
                        atkhorse_color = g0.GetComponent<Text>();
                        break;
                    case "Name":
                        atkhorse_point = g0.GetComponent<Text>();
                        break;
                }
            }
        }
        if (defhorse != null)
        {
            for (int i0 = 0; i0 < defhorse.gameObject.transform.childCount; i0++)
            {
                Transform t0 = defhorse.gameObject.transform.GetChild(i0);
                GameObject g0 = t0.gameObject;
                switch (g0.name)
                {
                    case "ColorText":
                        defhorse_color = g0.GetComponent<Text>();
                        break;
                    case "Name":
                        defhorse_point = g0.GetComponent<Text>();
                        break;
                }
            }
        }
        #endregion

        #region 手牌数
        if (hands != null)
        {
            for (int i0 = 0; i0 < hands.gameObject.transform.childCount; i0++)
            {
                Transform t0 = hands.gameObject.transform.GetChild(i0);
                GameObject g0 = t0.gameObject;
                switch (g0.name)
                {
                    case "HandNumber":
                        handnumber = g0.GetComponent<DigitColle>();
                        break;
                }
            }
        }
        #endregion

        #region 生命条两种形式
        if (hpbar != null)
        {
            for (int i0 = 0; i0 < hpbar.transform.childCount; i0++)
            {
                Transform t0 = hpbar.transform.GetChild(i0);
                GameObject g0 = t0.gameObject;
                switch (g0.name)
                {
                    case "Digit_Hp":
                        digit_hp = g0.GetComponent<DigitColle>();
                        break;
                    case "Digit_MaxHp":
                        digit_maxhp = g0.GetComponent<DigitColle>();
                        break;
                    default:
                        if (g0.name.StartsWith("Hp"))
                        {
                            Image hp = g0.GetComponent<Image>();
                            hps.Add(hp);
                        }
                        break;
                }
            }
        }
        #endregion

        #region 第一次更新
        FindAllZones();
        UpdateInfo();
        UpdateHp();
        UpdateEquipItems();
        UpdateJudgeItems();
        UpdateHandNumber();
        UpdateAlive();
        UpdateChained();
        UpdateFacedDown();
        UpdateSymbols();
        UpdateState();
        #endregion
    }

    void Update()
    {
        #region 调整颜色
        #region 选择状态
        if (IsEnterSelecting)
        {
            if (IsSelected)
                mask.color = new Color(0.5f, 1.0f, 0.5f, 0.5f);
            else if (CanSelect)
                mask.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            else
                mask.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
        #endregion
        #region 默认状态
        else
        {
            if (IsBullUp)
                mask.color = new Color(1.0f, 0.5f, 0.5f, 0.5f);
            else
                mask.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        }
        #endregion 
        #endregion
        #region 调整位置
        #region 摇动状态
        if (ShakeTotalTime < ShakeMaxTime)
        {
            Vector3 p = transform.position;
            p += ShakeVelocity;
            Vector3 pd = p - Position;
            if (pd.x < ShakeBox.xMin)
            {
                p.x += (ShakeBox.xMin - pd.x) * 2;
                ShakeVelocity.x = -ShakeVelocity.x;
            }
            if (pd.x > ShakeBox.xMax)
            {
                p.x += (ShakeBox.xMax - pd.x) * 2;
                ShakeVelocity.x = -ShakeVelocity.x;
            }
            if (pd.y < ShakeBox.yMin)
            {
                p.y += (ShakeBox.yMin - pd.y) * 2;
                ShakeVelocity.y = -ShakeVelocity.y;
            }
            if (pd.y > ShakeBox.yMax)
            {
                p.y += (ShakeBox.yMax - pd.y) * 2;
                ShakeVelocity.y = -ShakeVelocity.y;
            }
            transform.position = p;
            Vector3 f0 = ShakeForce * (ShakeMaxTime - ShakeTotalTime) / ShakeMaxTime;
            Vector3 f1 = (Position - p) * 0.6f;
            Vector3 f = f0 + f1;
            ShakeVelocity += f;
            ShakeTotalTime += Time.deltaTime;
        }
        #endregion
        #region 默认状态
        else
        {
            transform.position = Position;
        }
        #endregion
        #endregion
        #region 更新状态
        if (_toupdate_hp) UpdateHp();
        if (_toupdate_alive) UpdateAlive();
        if (_toupdate_faceddown) UpdateFacedDown();
        if (_toupdate_chained) UpdateChained();
        if (_toupdate_equips) UpdateEquipItems();
        if (_toupdate_judges) UpdateJudgeItems();
        if (_toupdate_hands) UpdateHandNumber();
        if (_toupdate_symbols) UpdateSymbols();  
        #endregion
        #region 虚假等待框
        if (waitbar != null && fakewaiting)
        {
            fakewait_remaintime = Math.Max(0, fakewait_remaintime - Time.deltaTime);
            waitbar.size = fakewait_remaintime / fakewait_maxtime;
            if (fakewait_remaintime <= 0) EndFakeWait();
        }
        #endregion
        #region 长按提示
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
            if (gb != null && gb.UI_PlayerTooltip.Player == this)
                gb.UI_PlayerTooltip.Player = null;
        }
        else if ((PressingTime -= Time.deltaTime) <= 0.0f)
        {
            PressingTime = 0.0f;
            if (gb != null && gb.UI_PlayerTooltip.Player == null)
                gb.UI_PlayerTooltip.Player = this;
        }
        #endregion
    }

    #endregion

    #region Method

    #region Text

    protected void SetColor(Text text, Card card)
    {
        if (card == null) return;
        if (card.CardColor?.SeemAs(Enum_CardColor.Red) == true)
            text.color = new Color(1.0f, 0.75f, 0.75f);
        else
            text.color = new Color(1.0f, 1.0f, 1.0f);
    }

    protected void SetEmoji(Text text, Card card)
    {
        if (card == null) return;
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
        if (card == null) return;
        switch (card.CardPoint)
        {
            case 1: text.text = "A"; break;
            case 11: text.text = "J"; break;
            case 12: text.text = "Q"; break;
            case 13: text.text = "K"; break;
            default: text.text = card.CardPoint.ToString(); break;
        }
    }

    protected void SetName(Text text, Card card)
    {
        if (card == null) return;
        string point = "";
        switch (card.CardPoint)
        {
            case 1: point = "A"; break;
            case 11: point = "J"; break;
            case 12: point = "Q"; break;
            case 13: point = "K"; break;
            default: point = card.CardPoint.ToString(); break;
        }
        if (card is SelfWeapon)
            text.text = String.Format("{0} {1}{2}", point, card.Name, ((SelfWeapon)card).GetWeaponRange());
        else 
            text.text = String.Format("{0} {1}", point, card.Name);

    }

    #endregion 

    #region Update

    public void UpdateInfo()
    {
        if (core == null) return;
        Charactor char0 = core.Charactors.FirstOrDefault();
        if (char0 != null && playerimage != null)
        {
            RectTransform rt = playerimage.gameObject.GetComponent<RectTransform>();
            //print(String.Format("char0={0} rect={1}", char0, rt.rect));
            playerimage.sprite = ImageHelper.CreateSprite(char0, rt.rect);
        }
        if (charactorname != null)
            charactorname.text = core.Name;
        if (countryname != null)
            countryname.text = core.Country;
    }

    public void UpdateSelectedAss()
    {
        if (core == null) return;
        if (ass == null) return;
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        if (Core?.Ass == null)
            ass.value = 0;
        else if (Core.IsAssVisibled || Core == gb?.CurrentPlayer)
            ass.value = (int)(Core.Ass.E) + 1;
        else
            ass.value = 0;

    }

    protected void UpdateHp()
    {
        if (core == null) return;
        if (hpbar == null) return;
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        if (gb == null) return;
        if (!gb.CharactorDetermined) return;
        _toupdate_hp = false;
        if (core.MaxHP <= hps.Count())
        {
            for (int i = 0; i < core.MaxHP; i++)
            {
                int i0 = 0;
                int i1 = 0;
                if (i > 0) i0 = 1;
                if (i < core.HP)
                {
                    switch (core.HP)
                    {
                        case 0:
                        case 1: i1 = 1; break;
                        case 2: i1 = 2; break;
                        default: i1 = 3; break;
                    }
                }
                App.Show(hps[i]);
                hps[i].sprite = ImageHelper.CreateSprite(String.Format("Borders/Bar{0}{1}", i0, i1));
            }
            for (int i = core.MaxHP; i < hps.Count(); i++)
                App.Hide(hps[i]);
            if (digit_hp != null)
                App.Hide(digit_hp);
            if (digit_maxhp != null)
                App.Hide(digit_maxhp);
        }
        else
        {
            DigitColor dc = DigitColor.Green;
            switch (core.HP)
            {
                case 0:
                case 1: dc = DigitColor.Red; break;
                case 2: dc = DigitColor.Yellow; break;
            }
            for (int i = 0; i < hps.Count(); i++)
                App.Hide(hps[i]);
            if (digit_hp != null)
            {
                App.Show(digit_hp);
                digit_hp.Color = dc;
                digit_hp.Digit = core.HP;
            }
            if (digit_maxhp != null)
            {
                App.Show(digit_maxhp);
                digit_maxhp.Color = dc;
                digit_maxhp.Digit = core.MaxHP;
            }
        }
    }

    protected void UpdateEquipItems()
    {
        Card weapon = null;
        Card armor = null;
        Card attackhorse = null;
        Card defencehorse = null;
        _toupdate_equips = false;
        if (EquipZone != null)
        {
            for (int i = 0; i < EquipZone.Cells.Count(); i++)
            {
                TouhouSha.Core.EquipCell cell = EquipZone.Cells[i];
                if (cell.CardIndex < 0) continue;
                if (cell.CardIndex >= EquipZone.Cards.Count()) continue;
                Card card = EquipZone.Cards[cell.CardIndex];
                switch (cell.E)
                {
                    case Enum_CardSubType.Weapon: weapon = card; break;
                    case Enum_CardSubType.Armor: armor = card; break;
                    case Enum_CardSubType.HorseMinus: attackhorse = card; break;
                    case Enum_CardSubType.HorsePlus: defencehorse = card; break;
                }
            }
        }
        #region 更新武器
        if (weapon != null)
        {
            App.Show(this.weapon);
            if (weapon_color != null)
            {
                SetColor(weapon_color, weapon);
                SetEmoji(weapon_color, weapon);
            }
            if (weapon_name != null)
            {
                SetColor(weapon_name, weapon);
                SetName(weapon_name, weapon);
            }
        }
        else
        {
            App.Hide(this.weapon);
        }
        #endregion
        #region 更新防具
        if (armor != null)
        {
            App.Show(this.armor);
            if (armor_color != null)
            {
                SetColor(armor_color, armor);
                SetEmoji(armor_color, armor);
            }
            if (armor_name != null)
            {
                SetColor(armor_name, armor);
                SetName(armor_name, armor);
            }
        }
        else
        {
            App.Hide(this.armor);
        }
        #endregion
        #region 更新进攻马
        if (attackhorse != null)
        {
            App.Show(this.atkhorse);
            if (atkhorse_color != null)
            {
                SetColor(atkhorse_color, attackhorse);
                SetEmoji(atkhorse_color, attackhorse);
            }
            if (atkhorse_point != null)
            {
                SetColor(atkhorse_point, attackhorse);
                SetPoint(atkhorse_point, attackhorse);
            }
        }
        else
        {
            App.Hide(this.atkhorse);
        }
        #endregion
        #region 更新防御马
        if (defencehorse != null)
        {
            App.Show(this.defhorse);
            if (defhorse_color != null)
            {
                SetColor(defhorse_color, defencehorse);
                SetEmoji(defhorse_color, defencehorse);
            }
            if (defhorse_point != null)
            {
                SetColor(defhorse_point, defencehorse);
                SetPoint(defhorse_point, defencehorse);
            }
        }
        else
        {
            App.Hide(this.defhorse);
        }
        #endregion
    }

    protected void UpdateJudgeItems()
    {
        if (JudgeZone == null) return;
        List<Card> judges = JudgeZone.Cards.ToList();
        List<Card> exists = new List<Card>();
        float y = 0;
        _toupdate_judges = false;
        while (delays.Count() < judges.Count())
        {
            GameObject go0 = Resources.Load<GameObject>("Delay");
            GameObject go1 = GameObject.Instantiate(go0, judgestack.transform);
            Image item = go1.GetComponent<Image>();
            delays.Add(item);
        }
        for (int i = 0; i < judges.Count(); i++)
        {
            Card judge = judges[i];
            Image delay = delays[i];
            App.Show(delay);
            delay.sprite = ImageHelper.CreateDelayIcon(judge);
        }
        for (int i = judges.Count(); i < delays.Count(); i++)
        {
            Image delay = delays[i];
            App.Hide(delay);
        }
    }

    protected void UpdateHandNumber()
    {
        if (HandZone == null) return;
        if (handnumber == null) return;
        _toupdate_hands = false;
        handnumber.Digit = HandZone.Cards.Count;
    }

    protected void FindAllZones()
    {
        EquipZone = core?.Zones?.FirstOrDefault(_zone => _zone is EquipZone) as EquipZone;
        HandZone = core?.Zones?.FirstOrDefault(_zone => _zone.KeyName?.Equals(Zone.Hand) == true);
        JudgeZone = core?.Zones?.FirstOrDefault(_zone => _zone.KeyName?.Equals(Zone.Judge) == true);
    }

    protected void UpdateAlive()
    {
        if (core == null) return;
        if (die == null) return;
        _toupdate_alive = false;
        if (core.IsAlive)
            App.Hide(die);
        else
            App.Show(die);
    }

    protected void UpdateChained()
    {
        if (core == null) return;
        if (chain == null) return;
        _toupdate_chained = false;
        if (core.IsChained)
            App.Show(chain);
        else
            App.Hide(chain);
    }

    protected void UpdateFacedDown()
    {
        if (core == null) return;
        if (facedown == null) return;
        _toupdate_faceddown = false;
        if (core.IsFacedDown)
            App.Show(facedown);
        else
            App.Hide(facedown);
    }

    protected void UpdateSymbols()
    {
        if (core == null) return;
        if (symbolbox == null) return;
        _toupdate_symbols = false;
        int index = 0;
        foreach (Zone zone in core.Zones)
        {
            if ((zone.Flag & Enum_ZoneFlag.LabelOnPlayer) != Enum_ZoneFlag.None)
                index++;
            foreach (ExternZone ext in zone.ExternZones)
                index++;
        }
        foreach (Symbol symbol in core.Symbols)
            index++;
        while (index > symbolbuttons.Count())
        {
            GameObject go0 = symbolbuttons[0].gameObject;
            GameObject go1 = GameObject.Instantiate(go0, symbolbox.transform);
            SymbolButton button = go1.GetComponent<SymbolButton>();
            button.Parent = this;
            symbolbuttons.Add(button);
        }
        index = 0;
        foreach (Zone zone in core.Zones)
        {
            if ((zone.Flag & Enum_ZoneFlag.LabelOnPlayer) != Enum_ZoneFlag.None)
            {
                SymbolButton button = symbolbuttons[index++];
                button.Zone = zone;
                button.ExternZone = null;
                button.Symbol = null;
                button.UpdateText();
            }
            foreach (ExternZone ext in zone.ExternZones)
            {
                SymbolButton button = symbolbuttons[index++];
                button.Zone = null;
                button.ExternZone = ext;
                button.Symbol = null;
                button.UpdateText();
            }
        }
        foreach (Symbol symbol in core.Symbols)
        {
            SymbolButton button = symbolbuttons[index++];
            button.Zone = null;
            button.ExternZone = null;
            button.Symbol = symbol;
            button.UpdateText();
        }
        while (index < symbolbuttons.Count())
        {
            SymbolButton button = symbolbuttons[index++];
            button.Zone = null;
            button.ExternZone = null;
            button.Symbol = null;
            button.UpdateText();
        }
    }

    public void UpdateState()
    {
        if (core == null) return;
        if (statename == null) return;
        if (App.World == null) 
        { 
            statename.text = "";
            CancelAllBorders();
            ShowOrHideWaitBar();
            return; 
        }
        State state = App.World.GetCurrentState();
        State mainstate = App.World.GetPlayerState();
        if (state?.Owner == core)
        {
            switch (state.KeyName)
            {
                case State.Dying:
                    statename.text = "濒死阶段";
                    SetVioletBorder();
                    ShowOrHideWaitBar();
                    return;
                case State.Handle:
                    statename.text = "响应阶段";
                    SetVioletBorder();
                    ShowOrHideWaitBar();
                    return;
            }
        }
        if (mainstate?.Owner == core)
        {
            switch (state.KeyName)
            {
                case State.Begin:
                    statename.text = "回合开始";
                    SetBlueBorder();
                    ShowOrHideWaitBar();
                    return;
                case State.Judge:
                    statename.text = "判定阶段";
                    SetBlueBorder();
                    ShowOrHideWaitBar();
                    return;
                case State.Draw:
                    statename.text = "摸牌阶段";
                    SetBlueBorder();
                    ShowOrHideWaitBar();
                    return;
                case State.UseCard:
                    statename.text = "出牌阶段";
                    SetBlueBorder();
                    ShowOrHideWaitBar();
                    return;
                case State.Discard:
                    statename.text = "弃牌阶段";
                    SetBlueBorder();
                    ShowOrHideWaitBar();
                    return;
                case State.End:
                    statename.text = "回合结束";
                    SetBlueBorder();
                    ShowOrHideWaitBar();
                    return;
            }
        }
        statename.text = "";
        CancelAllBorders();
        ShowOrHideWaitBar();
    }

    protected void SetVioletBorder()
    {
        if (violetborder != null)
            violetborder.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        if (blueborder != null)
            blueborder.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    }
    
    protected void SetBlueBorder()
    {
        if (violetborder != null)
            violetborder.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        if (blueborder != null)
            blueborder.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    }

    protected void CancelAllBorders()
    {
        if (violetborder != null)
            violetborder.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        if (blueborder != null)
            blueborder.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    }

    public void NoticeChanged(SymbolButton button)
    {
        _toupdate_symbols = true;
    }

    #endregion

    #region Animation
   
    public void DamageShake()
    {
        ShakeVelocity = new Vector3(-15, 0);
        ShakeForce = new Vector3(-1.8f, 0);
        ShakeTotalTime = 0;
    }

    public void DamageBlink()
    {
        
    }

    #endregion

    #region Fake Wait

    public void BeginFakeWait(int timeout)
    {
        fakewait_maxtime = timeout;
        fakewait_remaintime = timeout;
        fakewaiting = true;
        ShowOrHideWaitBar();
    }

    public void EndFakeWait()
    {
        fakewaiting = false;
        fakewait_remaintime = fakewait_maxtime;
        ShowOrHideWaitBar();
    }

    public void ShowOrHideWaitBar()
    {
        if (waitbar == null) return;
        if (statename != null && !String.IsNullOrEmpty(statename.text))
            App.Show(waitbar);
        else if (fakewaiting)
            App.Show(waitbar);
        else
            App.Hide(waitbar);
        
    }

    #endregion

    #endregion

    #region IGameBoardArea

    bool IGameBoardArea.KeptCards
    {
        get
        {
            return false;
        }
    }

    IList<Card> IGameBoardArea.Cards
    {
        get
        {
            return new List<Card>();
        }
    }

    Vector3? IGameBoardArea.GetExpectedPosition(Card card)
    {
        return gameObject.transform.position;
    }

    #endregion

    #region Event Handler

    private void OnClick()
    {
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        gb?.PlayerCard_MouseDown(this);
    }

    private void OnPlayerShaPropertyChanged(object sender, ShaPropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "HP":
            case "MaxHP":
                _toupdate_hp = true;
                //UpdateHp();
                break;
            case "IsAlive":
                _toupdate_alive = true;
                //UpdateAlive();
                break;
            case "IsFacedDown":
                _toupdate_faceddown = true;
                //UpdateFacedDown();
                break;
            case "IsChained":
                _toupdate_chained = true;
                //UpdateChained();
                break;
            case LiqureCard.BullUp:
                IsBullUp = Core.GetValue(LiqureCard.BullUp) > 0;
                break;

        }
    }

    private void OnPlayerZonesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (Zone zone in e.OldItems)
                zone.ExternZones.CollectionChanged -= OnZoneExternsCollectionChanged;
        if (e.NewItems != null)
            foreach (Zone zone in e.NewItems)
                zone.ExternZones.CollectionChanged += OnZoneExternsCollectionChanged;
        FindAllZones();
        _toupdate_symbols = true;
        //UpdateSymbols();
    }

    private void OnZoneExternsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _toupdate_symbols = true;
            //UpdateSymbols();
        }
        else if (sender is IList
            && ((((IList)(sender)).Count - (e?.NewItems?.Count ?? 0) + (e?.OldItems?.Count ?? 0) == 0)
              ^ (((IList)(sender)).Count == 0)))
        {
            _toupdate_symbols = true;
            //UpdateSymbols();
        }
    }

    private void OnPlayerSymbolCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _toupdate_symbols = true;
        //UpdateSymbols();
    }

    private void EquipZone_EquipsChanged(object sender, EventArgs e)
    {
        _toupdate_equips = true;
        //UpdateEquipItems();
    }

    private void JudgeZone_Cards_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _toupdate_judges = true;
        //UpdateJudgeItems();
    }

    private void HandZone_Cards_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _toupdate_hands = true;
        //UpdateHandNumber();
    }

    #endregion 

}
