using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using System.Collections.Specialized;

public class SymbolButton : MonoBehaviour
{
    #region Number

    public PlayerBe Parent;

    private Zone zone;
    public Zone Zone
    {
        get
        {
            return this.zone;
        }
        set
        {
            if (zone == value) return;
            if (zone != null)
                zone.Cards.CollectionChanged -= Zone_Cards_CollectionChanged;
            this.zone = value;
            if (zone != null)
                zone.Cards.CollectionChanged += Zone_Cards_CollectionChanged;
            UpdateText();
        }
    }

    private ExternZone externzone;
    public ExternZone ExternZone
    {
        get
        {
            return this.externzone;
        }
        set
        {
            if (externzone == value) return;
            if (externzone != null)
                externzone.Cards.CollectionChanged -= ExternZone_Cards_CollectionChanged;
            this.externzone = value;
            if (externzone != null)
                externzone.Cards.CollectionChanged += ExternZone_Cards_CollectionChanged;
            UpdateText();
        }
    }

    private Symbol symbol;
    public Symbol Symbol
    {
        get
        {
            return this.symbol;
        }
        set
        {
            if (symbol == value) return;
            if (symbol != null)
                symbol.PropertyChanged -= Symbol_PropertyChanged;
            this.symbol = value;
            if (symbol != null)
                symbol.PropertyChanged += Symbol_PropertyChanged;
            UpdateText();
        }
    }

    [SerializeField]
    private Text text;

    #endregion

    #region Mono

    void Awake()
    {
        Button button = gameObject.GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    #endregion

    #region Method

    public void UpdateText()
    {
        if (text == null) return;
        if (Zone != null)
        {
            if (Zone.Cards.Count() > 0)
            {
                text.text = String.Format("{0}({1})", Zone.KeyName, Zone.Cards.Count());
                //gameObject.transform.parent = _old_parent;
                App.Show(this);
            }
            else
            {
                App.Hide(this);
                //gameObject.transform.parent = null;
                text.text = "";
            }
        }
        else if (ExternZone != null)
        {
            if (ExternZone.Cards.Count() > 0)
            {
                text.text = String.Format("{0}({1})", ExternZone.KeyName, ExternZone.Cards.Count());
                //gameObject.transform.parent = _old_parent;
                App.Show(this);
            }
            else
            {
                App.Hide(this);
                //gameObject.transform.parent = null;
                text.text = "";
            }
        }
        else if (Symbol != null)
        {
            if (Symbol.Count > 0)
            {
                text.text = String.Format("{0}({1})", Symbol.KeyName, Symbol.Count);
                //gameObject.transform.parent = _old_parent;
                App.Show(this);
            }
            else
            {
                App.Hide(this);
                //gameObject.transform.parent = null;
                text.text = "";
            }
        }
        else
        {
            App.Hide(this);
            //gameObject.transform.parent = null;
            text.text = "";
        }
    }

    #endregion 

    #region Event Handler

    private void Zone_Cards_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Parent?.NoticeChanged(this);
    }

    private void ExternZone_Cards_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Parent?.NoticeChanged(this);
    }

    private void Symbol_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Parent?.NoticeChanged(this);
    }

    private void OnClick()
    {
        PlayerBe playerview = gameObject.GetComponentInParent<PlayerBe>();
        if (playerview == null) return;
        GameBoard gb = playerview.gameObject.GetComponentInParent<GameBoard>();
        if (gb == null) return;
        gb.UI_ZoneTooltip.Player = playerview;
        gb.UI_ZoneTooltip.Zone = Zone;
    }

    #endregion 
}
