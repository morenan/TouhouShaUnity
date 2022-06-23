using System;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuitButton : MainMenuButton
{
	void Awake()
	{
		Button button = gameObject.GetComponent<Button>();
		button.onClick.AddListener(OnButtonClick);
	}

	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}

	private void OnButtonClick()
	{
		MainMenu main = gameObject.GetComponentInParent<MainMenu>();
		main.Quit();
	}
}
