using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;

public class YujiPanel : MonoBehaviour
{
    public class ButtonHandler
    {
        public YujiPanel Parent;
        public Button Button;
        public Card Card;
        
        public void OnClick()
        {
            GameBoard gb = Parent.gameObject.GetComponentInParent<GameBoard>();
            gb?.UI_Yuji_Click(Card);
        }
    }

    public Skill Skill;
    public Card SkillFromCard;

    private List<Button> buttons = new List<Button>();
    private Dictionary<Button, ButtonHandler> button2handlers = new Dictionary<Button, ButtonHandler>();

    private List<Card> cardlist = new List<Card>();
    public List<Card> CardList
    {
        get
        {
            return this.cardlist;
        }
        set
        {
            this.cardlist = value;
            if (cardlist == null) return;
            RectTransform rt = gameObject.GetComponent<RectTransform>();
            GridLayoutGroup glg = gameObject.GetComponent<GridLayoutGroup>();
            int columns = Math.Min(cardlist.Count(), 6);
            while (buttons.Count() < cardlist.Count())
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
            for (int i = 0; i < cardlist.Count(); i++)
            {
                Button button = buttons[i];
                ButtonHandler handler = button2handlers[button];
                Text text = button.gameObject.GetComponentInChildren<Text>();
                handler.Card = cardlist[i];
                text.text = cardlist[i].Name;
                App.Show(button);
            }
            for (int i = cardlist.Count(); i < buttons.Count(); i++)
            {
                Button button = buttons[i];
                ButtonHandler handler = button2handlers[button];
                App.Hide(button);
                handler.Card = null;
            }
            rt.sizeDelta = new Vector2(
                columns * glg.cellSize.x,
                ((cardlist.Count() - 1) / columns + 1) * glg.cellSize.y);
        }
    }
    
    void Awake()
    {
        foreach (Button button in gameObject.GetComponentsInChildren<Button>())
        {
            if (buttons.Contains(button)) continue;
            ButtonHandler handler = new ButtonHandler();
            handler.Parent = this;
            handler.Button = button;
            button.onClick.AddListener(handler.OnClick);
            buttons.Add(button);
            button2handlers.Add(button, handler);
            App.Hide(button);
        }
    }
}

