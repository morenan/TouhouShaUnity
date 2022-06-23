using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;

public class WinnerPanel : MonoBehaviour
{
    #region Number

    private PlayerScore score;
    public PlayerScore Score
    {
        get
        {
            return this.score;
        }
        set
        {
            this.score = value;
            if (score == null) return;
            if (score.Owner != null)
            {
                Charactor char0 = score.Owner.Charactors.FirstOrDefault();
                if (char0 != null)
                {
                    RectTransform face_rt = Face.gameObject.GetComponent<RectTransform>();
                    Face.sprite = ImageHelper.CreateSprite(char0, face_rt.rect);
                }
                Name.text = score.Owner.Name;
            }
        }
    }
    
    public Image Face;
    public Text Name;
    public Text[] Scores;
    public GridLayoutGroup OtherWinners;
    public GridLayoutGroup OtherLosers;
    public Button SaveVideo;
    public Button Return;
    public float TotalTime;
    public float MaxTime = 5.0f;

    private Image canvas_bg;
    private List<Image> winnerimages = new List<Image>();
    private List<Image> loserimages = new List<Image>();

    public float Opacity
    {
        get
        {
            if (canvas_bg == null) return 0.0f;
            return canvas_bg.color.a;
        }
        set
        {
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

    #endregion

    #region MonoBehavior

    void Awake()
    {
        canvas_bg = gameObject.GetComponent<Image>();
        SaveVideo.onClick.AddListener(OnSaveVideo);
        Return.onClick.AddListener(OnReturn);
        TotalTime = 0;
        Opacity = 0;
        foreach (Text score in Scores)
            score.text = "0";
        
        foreach (Image image in OtherWinners.GetComponentsInChildren<Image>())
        {
            if (winnerimages.Contains(image)) continue;
            winnerimages.Add(image);
            App.Hide(image);
        }
        if (loserimages.Count() == 0)
        {
            GameObject go0 = winnerimages[0].gameObject;
            GameObject go1 = GameObject.Instantiate(go0, OtherLosers.gameObject.transform);
            Image image = go1.GetComponent<Image>();
            App.Hide(image);
            loserimages.Add(image);
        }
    }


    void Update()
    {
        if (Score == null) return;
        if (TotalTime < 0.5f)
        {
            Opacity = TotalTime * 2;
        }
        else if (TotalTime < 1.0f)
        {
            Opacity = 1;
        }
        else if (TotalTime < 2.0f)
        {
            Scores[0].text = ((int)(Score.AttackScore * (TotalTime - 1.0f))).ToString();
        }
        else if (TotalTime < 3.0f)
        {
            Scores[0].text = Score.AttackScore.ToString();
            Scores[1].text = ((int)(Score.DefenceScore * (TotalTime - 1.0f))).ToString();
        }
        else if (TotalTime < 4.0f)
        {
            Scores[1].text = Score.DefenceScore.ToString();
            Scores[2].text = ((int)(Score.AssistScore * (TotalTime - 1.0f))).ToString();
        }
        else if (TotalTime < 5.0f)
        {
            Scores[2].text = Score.AssistScore.ToString();
            Scores[3].text = ((int)(Score.ControlScore * (TotalTime - 1.0f))).ToString();
        }
        else
        {
            Scores[3].text = Score.ControlScore.ToString();
        }
        TotalTime = Math.Min(TotalTime + Time.deltaTime, MaxTime);
    }

    #endregion

    #region Method



    #endregion

    #region Event Handler

    private void OnSaveVideo()
    {

    }

    private void OnReturn()
    {

    }

    #endregion
}
