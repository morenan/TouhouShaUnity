using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class ListPanel : MonoBehaviour
{
    public class ButtonHandler
    {
        public ListPanel Parent;
        public Button Button;
        public object Data;
       
        public void OnClick()
        {
            GameBoard gb = Parent.gameObject.GetComponentInParent<GameBoard>();
            Parent.SelectedItems.Clear();
            Parent.SelectedItems.Add(Data);
            gb?.UI_ListPanel_SelectedDone();
        }
    }

    #region Number

    private ListBoardCore core;
    public ListBoardCore Core
    {
        get
        {
            return this.core;
        }
        set
        {
            this.core = value;
            if (core == null) return;
            selecteditems.Clear();
            while (buttons.Count() < core.Items.Count())
            {
                GameObject go0 = buttons[0].gameObject;
                GameObject go1 = GameObject.Instantiate(go0, gameObject.transform);
                Button button = go1.GetComponent<Button>();
                ButtonHandler handler = new ButtonHandler();
                handler.Parent = this;
                handler.Button = button;
                button.onClick.AddListener(handler.OnClick);
                button2handlers.Add(button, handler);
                buttons.Add(button);
            }
            for (int i = 0; i < core.Items.Count(); i++)
            {
                Button button = buttons[i];
                button.gameObject.transform.parent = grid.transform;
                App.Show(button);
            }
            for (int i = core.Items.Count(); i < buttons.Count(); i++)
            {
                Button button = buttons[i];
                ButtonHandler handler = button2handlers[button];
                App.Hide(button);
                button.gameObject.transform.parent = null;
                handler.Data = null;
            }
            for (int i = 0; i < core.Items.Count(); i++)
            {
                Button button = buttons[i];
                ButtonHandler handler = button2handlers[button];
                Text text = button.gameObject.GetComponentInChildren<Text>();
                handler.Data = core.Items[i];
                text.text = core.Items[i].ToString();
            }
        }
    }

    private Text message;
    private Scrollbar timeout;
    private GameObject grid;
    private List<Button> buttons = new List<Button>();
    private Dictionary<Button, ButtonHandler> button2handlers = new Dictionary<Button, ButtonHandler>();

    private List<object> selecteditems = new List<object>();
    public List<object> SelectedItems
    {
        get
        {
            return this.selecteditems;
        }
    }

    #endregion

    #region MonoBehavior

    void Awake()
    {
        for (int i0 = 0; i0 < gameObject.transform.childCount; i0++)
        {
            Transform t0 = gameObject.transform.GetChild(i0);
            GameObject g0 = t0.gameObject;
            switch (g0.name)
            {
                case "Grid":
                    grid = g0;
                    break;
            }
        }
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
        foreach (Button button in grid.GetComponentsInChildren<Button>())
        {
            if (buttons.Contains(button)) continue;
            ButtonHandler handler = new ButtonHandler();
            handler.Parent = this;
            handler.Button = button;
            button.onClick.AddListener(handler.OnClick);
            buttons.Add(button);
            button2handlers.Add(button, handler);
            App.Hide(button);
            button.gameObject.transform.parent = null;
        }
    }

    #endregion

}
