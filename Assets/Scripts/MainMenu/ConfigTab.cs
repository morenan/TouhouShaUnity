using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ConfigTab : MonoBehaviour
{
    public class TabButtonHandler
    {
        public TabButtonHandler(ConfigTab _parent, string _name)
        {
            this.parent = _parent;
            this.name = _name;
        }

        private ConfigTab parent;
        private string name;

        public void OnClick()
        {
            parent.Select(name);
        }
    }

    private Dictionary<string, Button> tab_buttons = new Dictionary<string, Button>();
    private Dictionary<string, GameObject> tab_contents = new Dictionary<string, GameObject>();
    private Dictionary<string, TabButtonHandler> tab_handlers = new Dictionary<string, TabButtonHandler>();
    private string selectedtab;

    void Awake()
    {
        List<GameObject> children = new List<GameObject>();
        for (int i = 0; i < gameObject.transform.childCount; i++)
            children.Add(gameObject.transform.GetChild(i).gameObject);
        foreach (GameObject child in children)
        {
            if (child.name.EndsWith("_Tab"))
            {
                string name = child.name.Substring(0, child.name.Length - 4);
                Button btn = child.GetComponent<Button>();
                TabButtonHandler handler = new TabButtonHandler(this, name);
                tab_buttons.Add(name, btn);
                tab_handlers.Add(name, handler);
                btn.onClick.AddListener(handler.OnClick);
            }
            else if (child.name.EndsWith("_Panel"))
            {
                string name = child.name.Substring(0, child.name.Length - 6);
                tab_contents.Add(name, child);
                App.Hide(child);
            }
        }
        Select(tab_buttons.Keys.FirstOrDefault());
    }

    void Start()
    {

    }


    void Update()
    {
        
    }

    internal void Select(string name)
    {
        Button button = null;
        GameObject content = null;
        if (!String.IsNullOrEmpty(selectedtab))
        {
            if (tab_buttons.TryGetValue(selectedtab, out button))
                button.interactable = true;
            if (tab_contents.TryGetValue(selectedtab, out content))
                App.Hide(content);
        }
        selectedtab = name;
        if (!String.IsNullOrEmpty(selectedtab))
        {
            if (tab_buttons.TryGetValue(selectedtab, out button))
                button.interactable = false;
            if (tab_contents.TryGetValue(selectedtab, out content))
                App.Show(content);
        }
    }
    
}
