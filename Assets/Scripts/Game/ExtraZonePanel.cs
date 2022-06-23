using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class ExtraZonePanel : MonoBehaviour
{
    public ExtraZonePanel()
    {
        zones.CollectionChanged += Zones_CollectionChanged;
    }

    #region Number

    private ObservableCollection<Zone> zones = new ObservableCollection<Zone>();
    public ObservableCollection<Zone> Zones { get { return this.zones; } }

    private ScrollRect scrollrect;
    private List<ExtraZoneList> lists = new List<ExtraZoneList>();

    public IEnumerable<ExtraZoneList> Lists
    {
        get
        {
            for (int i = 0; i < zones.Count(); i++)
                yield return lists[i];
        }
    }

    #endregion

    #region MonoBehavior

    void Awake()
    {
        scrollrect = gameObject.GetComponentInChildren<ScrollRect>();
        foreach (ExtraZoneList list in gameObject.GetComponentsInChildren<ExtraZoneList>())
        {
            if (lists.Contains(list)) continue;
            lists.Add(list);
            App.Hide(list);
        }
    }

    void Update()
    {

    }

    #endregion

    #region Event Handler

    private void Zones_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        while (lists.Count() < zones.Count())
        {
            GameObject go0 = lists[0].gameObject;
            GameObject go1 = GameObject.Instantiate(go0, scrollrect.content);
            ExtraZoneList list = go1.GetComponent<ExtraZoneList>();
            lists.Add(list);
        }
        for (int i = 0; i < zones.Count(); i++)
        {
            ExtraZoneList list = lists[i];
            RectTransform list_rt = list.gameObject.GetComponent<RectTransform>();
            App.Show(list);
            list.Zone = zones[i];
        }
        for (int i = zones.Count(); i < lists.Count(); i++)
        {
            ExtraZoneList list = lists[i];
            list.Zone = null;
            App.Hide(list);
        }
    }
    
    #endregion
}
