using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;

public class CharactorBe : MonoBehaviour
{
    #region Number

    private Charactor core;
    public Charactor Core
    {
        get
        {
            return this.core;
        }
        set
        {
            this.core = value;
            UpdateInfo();
        }
    }

    private Button button;
    private Image playerimage;
    private Text charactorname;

    #endregion

    #region MonoBehavior

    void Awake()
    {
        button = GetComponent<Button>();
        playerimage = GetComponent<Image>();
        charactorname = GetComponentInChildren<Text>();
        button.onClick.AddListener(OnClick);
        UpdateInfo();
    }

    #endregion

    #region Method

    protected void UpdateInfo()
    {
        if (core == null) return;
        if (playerimage != null)
            playerimage.sprite = ImageHelper.CreateSprite(core, new Rect(0, 0, 187.5f, 240));
        if (charactorname != null)
            charactorname.text = core.GetInfo().Name;
    }

    #endregion

    #region Event Handler
   
    private void OnClick()
    {
        if (core == null) return;
        CharactorSelectPanel csp = gameObject.GetComponentInParent<CharactorSelectPanel>();
        if (csp != null)
        {
            csp.Select(core);
            return;
        }
        KOFPanel kof = gameObject.GetComponentInParent<KOFPanel>();
        if (kof != null)
        {
            kof.Select(core);
            return;
        }
        CharactorGallery gal = gameObject.GetComponentInParent<CharactorGallery>();
        if (gal != null)
        {
            gal.Select(core);
            return;
        }
    }

    #endregion
}
