using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour 
{
	private enum Status
    {
		None,
		MainMenuBegin,
		MainMenuEnd,
		MainMenu,
		GameModeBegin,
		GameModeEnd,
		GameMode,
		GalleryBegin,
		GalleryEnd,
		Gallery,
    }

	private Status status = Status.MainMenuBegin;
	private Status nextstatus = Status.GameModeBegin;
	private int tick;
	private List<MainMenuButton> button0s = new List<MainMenuButton>();
	private List<GameModeButton> button1s = new List<GameModeButton>();
	private List<GalleryMenuButton> button2s = new List<GalleryMenuButton>();
	private BackButton backbutton;
	private ConfigTab configtab;
	
	// Use this for initialization
	void Start() 
	{
		button0s.Clear();
		button1s.Clear();
		button2s.Clear();
		button0s.AddRange(gameObject.GetComponentsInChildren<MainMenuButton>());
		button1s.AddRange(gameObject.GetComponentsInChildren<GameModeButton>());
		button2s.AddRange(gameObject.GetComponentsInChildren<GalleryMenuButton>());
		backbutton = gameObject.GetComponentInChildren<BackButton>();
		configtab = gameObject.GetComponentInChildren<ConfigTab>();
		App.Hide(configtab);
		InvokeRepeating("OnTimer", 0.01f, 0.01f);
		status = Status.MainMenuBegin;
		tick = 0;
	}

	// Update is called once per frame
	void Update () 
	{
		AudioSource audio_bgm = gameObject.GetComponent<AudioSource>();
		audio_bgm.volume = (float)TouhouSha.Core.Config.SoundConfig.Volume_Bgm;
	}
	
	private void OnTimer()
    {
		//print(string.Format("Timer status={0} tick={1}", status, tick));
		switch (status)
		{
			#region 主菜单淡入
			case Status.MainMenuBegin:
				if (++tick <= button0s.Count * 20 + 20)
				{
					int tickstart = 0;
					for (int i = 0; i < button0s.Count; i++)
					{
						MainMenuButton btnc = button0s[i];
						Button btn = btnc.gameObject.GetComponent<Button>();
						Image im = btnc.gameObject.GetComponent<Image>();
						RectTransform rt = btnc.gameObject.GetComponent<RectTransform>();
						ColorBlock cb = btn.colors;
						Color c = cb.normalColor;
						Vector3 p = rt.position;
						float opacity = 1.0f;
						float posx = 32.0f + i * (rt.rect.width + 8);
						float posy = 32.0f;
						float posy_0 = 32.0f;
						float posy_1 = 32.0f;
						if ((i & 1) != 0)
						{
							posy_1 = 64.0f;
							posy_0 = posy_1 + rt.rect.height / 2;
						}
						else
						{
							posy_1 = 32.0f;
							posy_0 = posy_1 - rt.rect.height / 2;
						}
						if (tick >= tickstart + 40)
						{
							opacity = 1.0f;
							posy = posy_1;
						}
						else if (tick < tickstart)
						{
							opacity = 0.0f;
							posy = posy_0;
						}
						else
						{
							float r = (tick - tickstart) / 40.0f;
							opacity = 1.0f * r;
							posy = posy_0 + (posy_1 - posy_0) * r;
						}
						c.a = opacity;
						p.x = posx;
						p.y = posy;
						cb.normalColor = c;
						c = cb.highlightedColor;
						c.a = opacity;
						cb.highlightedColor = c;
						c = cb.pressedColor;
						c.a = opacity;
						cb.pressedColor = c;
						c = cb.selectedColor;
						c.a = opacity;
						cb.selectedColor = c;
						btn.colors = cb;
						rt.position = p;
						btn.interactable = true;
						im.raycastTarget = true;
						tickstart += 20;
					}
				}
				else
				{
					status = Status.MainMenu;
					tick = 0;
				}
				break;
			#endregion
			#region 主菜单淡出
			case Status.MainMenuEnd:
				if (++tick <= button0s.Count * 20 + 20)
				{
					int tickstart = 0;
					for (int i = 0; i < button0s.Count; i++)
					{
						MainMenuButton btnc = button0s[i];
						Button btn = btnc.gameObject.GetComponent<Button>();
						RectTransform rt = btnc.gameObject.GetComponent<RectTransform>();
						ColorBlock cb = btn.colors;
						Color c = cb.normalColor;
						Vector3 p = rt.position;
						float opacity = 1.0f;
						float posy = 32.0f;
						float posy_0 = 32.0f;
						float posy_1 = 32.0f;
						if ((i & 1) != 0)
						{
							posy_0 = 64.0f;
							posy_1 = posy_0 + rt.rect.height / 2;
						}
						else
						{
							posy_0 = 32.0f;
							posy_1 = posy_0 - rt.rect.height / 2;
						}
						if (tick >= tickstart + 40)
						{
							opacity = 0.0f;
							posy = posy_1;
						}
						else if (tick < tickstart)
						{
							opacity = 1.0f;
							posy = posy_0;
						}
						else
						{
							float r = (tick - tickstart) / 40.0f;
							opacity = 1.0f * (1 - r);
							posy = posy_0 + (posy_1 - posy_0) * r;
						}
						c.a = opacity;
						p.y = posy;
						cb.normalColor = c;
						c = cb.highlightedColor;
						c.a = opacity;
						cb.highlightedColor = c;
						c = cb.pressedColor;
						c.a = opacity;
						cb.pressedColor = c;
						c = cb.selectedColor;
						c.a = opacity;
						cb.selectedColor = c;
						btn.colors = cb;
						rt.position = p;
						tickstart += 20;
					}
				}
				else
				{
					foreach (MainMenuButton btnc in button0s)
					{
						Button btn = btnc.gameObject.GetComponent<Button>();
						Image im = btnc.gameObject.GetComponent<Image>();
						btn.interactable = false;
						im.raycastTarget = false;
					}

					status = nextstatus;
					tick = 0;
				}
				break;
			#endregion
			#region 模式菜单淡入
			case Status.GameModeBegin:
				if (++tick <= button1s.Count * 20 + 20)
				{
					int tickstart = 0;
					for (int i = 0; i < button1s.Count; i++)
					{
						GameModeButton btnc = button1s[i];
						Button btn = btnc.gameObject.GetComponent<Button>();
						Image im = btnc.gameObject.GetComponent<Image>();
						RectTransform rt = btnc.gameObject.GetComponent<RectTransform>();
						ColorBlock cb = btn.colors;
						Color c = cb.normalColor;
						Vector3 p = rt.position;
						float opacity = 1.0f;
						float posx = 32.0f + (i & 1) * (rt.rect.width + 16.0f);
						float posy = -32.0f - (i / 2) * (rt.rect.height + 16.0f);
						float posx_0 = posx + rt.rect.width / 2;
						float posx_1 = posx;
						if (tick >= tickstart + 40)
						{
							opacity = 1.0f;
							posx = posx_1;
						}
						else if (tick < tickstart)
						{
							opacity = 0.0f;
							posx = posx_0;
						}
						else
						{
							float r = (tick - tickstart) / 40.0f;
							opacity = 1.0f * r;
							posx = posx_0 + (posx_1 - posx_0) * r;
						}
						c.a = opacity;
						p.x = posx;
						p.y = posy;
						cb.normalColor = c;
						c = cb.highlightedColor;
						c.a = opacity;
						cb.highlightedColor = c;
						c = cb.pressedColor;
						c.a = opacity;
						cb.pressedColor = c;
						c = cb.selectedColor;
						c.a = opacity;
						cb.selectedColor = c;
						btn.colors = cb;
						rt.anchoredPosition = p;
						//print(string.Format("i={0} height={1} posy={2} rt.position={3}", i, rt.rect.height, posy, rt.anchoredPosition));
						btn.interactable = true;
						im.raycastTarget = true;
						tickstart += 20;
					}
					if (backbutton != null)
					{
						Button btn = backbutton.gameObject.GetComponent<Button>();
						btn.interactable = true;
					}
				}
				else
				{
					status = Status.GameMode;
					tick = 0;
				}
				break;
			#endregion
			#region 模式菜单淡出
			case Status.GameModeEnd:
				if (++tick <= button1s.Count * 20 + 20)
				{
					int tickstart = 0;
					for (int i = 0; i < button1s.Count; i++)
					{
						GameModeButton btnc = button1s[i];
						Button btn = btnc.gameObject.GetComponent<Button>();
						Image im = btnc.gameObject.GetComponent<Image>();
						RectTransform rt = btnc.gameObject.GetComponent<RectTransform>();
						ColorBlock cb = btn.colors;
						Color c = cb.normalColor;
						Vector3 p = rt.position;
						float opacity = 1.0f;
						float posx = 32.0f + (i & 1) * (rt.rect.width + 16.0f);
						float posy = -32.0f - (i / 2) * (rt.rect.height + 16.0f);
						float posx_0 = posx;
						float posx_1 = posx + rt.rect.width / 2;
						if (tick >= tickstart + 40)
						{
							opacity = 0.0f;
							posx = posx_1;
						}
						else if (tick < tickstart)
						{
							opacity = 1.0f;
							posx = posx_0;
						}
						else
						{
							float r = (tick - tickstart) / 40.0f;
							opacity = 1.0f * r;
							posx = posx_0 + (posx_1 - posx_0) * r;
						}
						c.a = opacity;
						p.x = posx;
						p.y = posy;
						cb.normalColor = c;
						c = cb.highlightedColor;
						c.a = opacity;
						cb.highlightedColor = c;
						c = cb.pressedColor;
						c.a = opacity;
						cb.pressedColor = c;
						c = cb.selectedColor;
						c.a = opacity;
						cb.selectedColor = c;
						btn.colors = cb;
						rt.anchoredPosition = p;
						//print(string.Format("i={0} height={1} posy={2} rt.position={3}", i, rt.rect.height, posy, rt.anchoredPosition));
						btn.interactable = true;
						im.raycastTarget = true;
						tickstart += 20;
					}
				}
				else
				{
					foreach (GameModeButton btnc in button1s)
					{
						Button btn = btnc.gameObject.GetComponent<Button>();
						Image im = backbutton.gameObject.GetComponent<Image>();
						btn.interactable = false;
						im.raycastTarget = false;
					}
					if (backbutton != null)
					{
						Button btn = backbutton.gameObject.GetComponent<Button>();
						Image im = backbutton.gameObject.GetComponent<Image>();
						btn.interactable = false;
						im.raycastTarget = false;
					}

					status = nextstatus;
					tick = 0;
				}
				break;
			#endregion
			#region 画廊菜单淡入
			case Status.GalleryBegin:
				if (++tick <= button2s.Count * 20 + 20)
				{
					int tickstart = 0;
					for (int i = 0; i < button2s.Count; i++)
					{
						GalleryMenuButton btnc = button2s[i];
						Button btn = btnc.gameObject.GetComponent<Button>();
						Image im = btnc.gameObject.GetComponent<Image>();
						RectTransform rt = btnc.gameObject.GetComponent<RectTransform>();
						ColorBlock cb = btn.colors;
						Color c = cb.normalColor;
						Vector3 p = rt.position;
						float opacity = 1.0f;
						float posx = 32.0f + i * (rt.rect.width + 8);
						float posy = 32.0f;
						float posy_0 = 32.0f;
						float posy_1 = 32.0f;
						if ((i & 1) != 0)
						{
							posy_1 = 64.0f;
							posy_0 = posy_1 + rt.rect.height / 2;
						}
						else
						{
							posy_1 = 32.0f;
							posy_0 = posy_1 - rt.rect.height / 2;
						}
						if (tick >= tickstart + 40)
						{
							opacity = 1.0f;
							posy = posy_1;
						}
						else if (tick < tickstart)
						{
							opacity = 0.0f;
							posy = posy_0;
						}
						else
						{
							float r = (tick - tickstart) / 40.0f;
							opacity = 1.0f * r;
							posy = posy_0 + (posy_1 - posy_0) * r;
						}
						c.a = opacity;
						p.x = posx;
						p.y = posy;
						cb.normalColor = c;
						c = cb.highlightedColor;
						c.a = opacity;
						cb.highlightedColor = c;
						c = cb.pressedColor;
						c.a = opacity;
						cb.pressedColor = c;
						c = cb.selectedColor;
						c.a = opacity;
						cb.selectedColor = c;
						btn.colors = cb;
						rt.position = p;
						btn.interactable = true;
						im.raycastTarget = true;
						tickstart += 20;
					}
				}
				else
				{
					status = Status.Gallery;
					tick = 0;
				}
				break;
			#endregion
			#region 画廊菜单淡出
			case Status.GalleryEnd:
				if (++tick <= button2s.Count * 20 + 20)
				{
					int tickstart = 0;
					for (int i = 0; i < button2s.Count; i++)
					{
						GalleryMenuButton btnc = button2s[i];
						Button btn = btnc.gameObject.GetComponent<Button>();
						Image im = btnc.gameObject.GetComponent<Image>();
						RectTransform rt = btnc.gameObject.GetComponent<RectTransform>();
						ColorBlock cb = btn.colors;
						Color c = cb.normalColor;
						Vector3 p = rt.position;
						float opacity = 1.0f;
						float posy = 32.0f;
						float posy_0 = 32.0f;
						float posy_1 = 32.0f;
						if ((i & 1) != 0)
						{
							posy_0 = 64.0f;
							posy_1 = posy_0 + rt.rect.height / 2;
						}
						else
						{
							posy_0 = 32.0f;
							posy_1 = posy_0 - rt.rect.height / 2;
						}
						if (tick >= tickstart + 40)
						{
							opacity = 0.0f;
							posy = posy_1;
						}
						else if (tick < tickstart)
						{
							opacity = 1.0f;
							posy = posy_0;
						}
						else
						{
							float r = (tick - tickstart) / 40.0f;
							opacity = 1.0f * (1 - r);
							posy = posy_0 + (posy_1 - posy_0) * r;
						}
						c.a = opacity;
						p.y = posy;
						cb.normalColor = c;
						c = cb.highlightedColor;
						c.a = opacity;
						cb.highlightedColor = c;
						c = cb.pressedColor;
						c.a = opacity;
						cb.pressedColor = c;
						c = cb.selectedColor;
						c.a = opacity;
						cb.selectedColor = c;
						btn.colors = cb;
						rt.position = p;
						btn.interactable = true;
						im.raycastTarget = true;
						tickstart += 20;
					}
				}
				else
				{
					foreach (GalleryMenuButton btnc in button2s)
					{
						Button btn = btnc.gameObject.GetComponent<Button>();
						Image im = btnc.gameObject.GetComponent<Image>();
						btn.interactable = false;
						im.raycastTarget = false;
					}

					status = nextstatus;
					tick = 0;
				}
				break;
				#endregion
		}
    }

	public void StartGame(Enum_GameMode gamemode)
    {
		App.World = new World();
		App.World.GameMode = gamemode;
		SceneManager.LoadScene("GameBoard");
    }
	
	public void SingleGame()
    {
		if (status == Status.MainMenu)
        {
			nextstatus = Status.GameModeBegin;
			status = Status.MainMenuEnd;
			tick = 0;
		}
    }

	public void MultiGame()
    {
		SceneManager.LoadScene("Lobby");
    }

	public void Config()
    {
		if (status == Status.MainMenu)
		{
			Button button = backbutton.gameObject.GetComponent<Button>();
			Image image = backbutton.gameObject.GetComponent<Image>();
			App.Show(configtab);
			button.interactable = true;
			image.raycastTarget = true;
		}
	}

	public void Gallery()
    {
		if (status == Status.MainMenu)
		{
			nextstatus = Status.GalleryBegin;
			status = Status.MainMenuEnd;
			tick = 0;
		}
	}

	public void Thanks()
    {

    }

	public void Back()
    {
		if (App.IsVisible(configtab))
		{
			Button button = backbutton.gameObject.GetComponent<Button>();
			Image image = backbutton.gameObject.GetComponent<Image>();
			App.Hide(configtab);
			button.interactable = false;
			image.raycastTarget = true;
			return;
        }
		switch (status)
        {
			case Status.GameMode:
				nextstatus = Status.MainMenuBegin;
				status = Status.GameModeEnd;
				tick = 0;
				break;
			case Status.Gallery:
				nextstatus = Status.MainMenuBegin;
				status = Status.GalleryEnd;
				tick = 0;
				break;
        }
    }

	public void Quit()
    {
		Application.Quit();
    }

	public void CharactorGallery()
    {
		SceneManager.LoadScene("CharactorGallery");
	}

	public void CardGallery()
    {
			
    }

	public void Rule()
    {

    }

}

public class MainMenuButton : MonoBehaviour
{

}

public class GalleryMenuButton : MonoBehaviour
{

}
