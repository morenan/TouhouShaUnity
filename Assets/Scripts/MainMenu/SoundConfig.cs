using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;

public class SoundConfig : MonoBehaviour
{
    private Slider slider_bgm;
    private Slider slider_se;
    private Slider slider_voice;

    void Awake()
    {
        foreach (Slider slider in gameObject.GetComponentsInChildren<Slider>())
        {
            switch (slider.name)
            {
                case "Slider_BGM": 
                    slider_bgm = slider;
                    slider_bgm.value = (float)Config.SoundConfig.Volume_Bgm;
                    break;
                case "Slider_SE":
                    slider_se = slider;
                    slider_se.value = (float)Config.SoundConfig.Volume_Se;
                    break;
                case "Slider_Voice":
                    slider_voice = slider;
                    slider_voice.value = (float)Config.SoundConfig.Volume_Voice;
                    break;
            }
        }
    }

    void Update()
    {
        Config.SoundConfig.Volume_Bgm = slider_bgm.value;
        Config.SoundConfig.Volume_Se = slider_se.value;
        Config.SoundConfig.Volume_Voice = slider_voice.value;
    }
}
