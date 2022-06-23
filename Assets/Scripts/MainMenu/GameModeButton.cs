using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;

public class GameModeButton : MonoBehaviour
{
    public Enum_GameMode GameMode;

	void Awake()
	{
		Button button = gameObject.GetComponent<Button>();
		button.onClick.AddListener(OnButtonClick);
	}

	// Use this for initialization
	void Start()
	{
		Image image = gameObject.GetComponent<Image>();
		RectTransform rt = gameObject.GetComponent<RectTransform>();
		string path = "Buttons/Standard5";
		switch (GameMode)
        {
			case Enum_GameMode.StandardPlayers5: path = "Buttons/Standard5"; break;
			case Enum_GameMode.StandardPlayers8: path = "Buttons/Standard8"; break;
			case Enum_GameMode.KOF: path = "Buttons/KOF"; break;
			case Enum_GameMode.FightLandlord: path = "Buttons/FightLandlord"; break;
		}
		image.sprite = ImageHelper.CreateSprite(path, rt.rect);
	}

	// Update is called once per frame
	void Update()
	{
		Button btn = gameObject.GetComponent<Button>();
		Text txt = btn.GetComponentInChildren<Text>();
		Image border = transform.Find("Border").gameObject.GetComponent<Image>();

		Color c = txt.color;
		if (!btn.interactable)
			c.a = 0.0f;
		else
			c.a = btn.colors.normalColor.a;
		txt.color = c;

		c = border.color;
		if (!btn.interactable)
			c.a = 0.0f;
		else
			c.a = btn.colors.normalColor.a;
		border.color = c;

	}

	private void OnButtonClick()
	{
		MainMenu main = gameObject.GetComponentInParent<MainMenu>();
		main.StartGame(GameMode);
	}
}
