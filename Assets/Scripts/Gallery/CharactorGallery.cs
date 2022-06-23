using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using UnityEngine.SceneManagement;

public class CharactorGallery : MonoBehaviour
{
    #region Number

    private List<Charactor> charactors = new List<Charactor>();
    public List<Charactor> Charactors
    {
        get
        {
            return this.charactors;
        }
    }

    private List<string> countries = new List<string>();
    public List<string> Countries
    {
        get
        {
            return this.countries;
        }
    }

    public Dropdown[] Filters;
    public GameObject Grid;
    public Button Home;
    
    private List<CharactorBe> items = new List<CharactorBe>();
    private List<Charactor> chars = new List<Charactor>();
    
    #endregion

    #region MonoBehavior

    void Awake()
    {
        List<IPackage> packages = new List<IPackage>();
        HashSet<string> countries_hs = new HashSet<string>();
        packages.Add(new TouhouSha.Koishi.Package());
        packages.Add(new TouhouSha.Koishi.Package2());
        packages.Add(new TouhouSha.Reimu.Package());
        foreach (IPackage package in packages)
            charactors.AddRange(package.GetCharactors());
        foreach (Charactor char0 in charactors)
        {
            if (!String.IsNullOrEmpty(char0.Country)
             && !countries_hs.Contains(char0.Country))
                countries_hs.Add(char0.Country);
            foreach (string country in char0.OtherCountries)
                if (!String.IsNullOrEmpty(country)
                 && !countries_hs.Contains(country))
                    countries_hs.Add(country);
        }
        countries.Clear();
        countries.AddRange(countries_hs);

        Filters[0].onValueChanged.AddListener(OnFilterCountryChanged);
        Home.onClick.AddListener(OnHome);
        foreach (CharactorBe item in Grid.GetComponentsInChildren<CharactorBe>())
        {
            if (items.Contains(item)) continue;
            items.Add(item);
            App.Hide(item);
        }

        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        AudioSource audio_bgm = gameObject.GetComponent<AudioSource>();
        audio_bgm.volume = (float)TouhouSha.Core.Config.SoundConfig.Volume_Bgm;
    }

    #endregion

    #region Method

    public void Initialize()
    {
        List<string> options = new List<string>();
        options.Add("全部");
        options.AddRange(countries);
        Filters[0].ClearOptions();
        Filters[0].AddOptions(options);
        Filters[0].value = 0;
        UpdateGrid();
    }

    public void UpdateGrid()
    {
        string country = Filters[0].captionText.text;
        chars.Clear();
        foreach (Charactor char0 in Charactors)
        {
            if (country?.Equals("全部") != true)
            {
                if (char0.Country?.Equals(country) == true) { }
                else if (char0.OtherCountries.Contains(country)) { }
                else continue;
            }
            chars.Add(char0);
        }
        while (items.Count() < chars.Count())
        {
            GameObject go0 = items[0].gameObject;
            GameObject go1 = GameObject.Instantiate(go0, Grid.gameObject.transform);
            CharactorBe item = go1.GetComponent<CharactorBe>();
            App.Hide(item);
            items.Add(item);
        }
        for (int i = 0; i < chars.Count(); i++)
        {
            CharactorBe item = items[i];
            item.Core = chars[i];
            App.Show(item);
        }
        for (int i = chars.Count(); i < items.Count(); i++)
        {
            CharactorBe item = items[i];
            App.Hide(item);
        }
    }

    public void Select(Charactor char0)
    {
        GameObject canvas = gameObject.transform.parent.gameObject;
        CharactorDetail detail = canvas.GetComponentInChildren<CharactorDetail>();
        detail.Charactors = chars.ToList();
        detail.CurrentIndex = chars.IndexOf(char0);
        detail.UpdateCurrent();
        App.Show(detail);
        App.Hide(this);
    }

    #endregion

    #region Event Handler 

    private void OnFilterCountryChanged(int arg0)
    {
        UpdateGrid();
    }

    private void OnHome()
    {
        SceneManager.LoadScene("MainMenu");
    }
    
    #endregion
}
