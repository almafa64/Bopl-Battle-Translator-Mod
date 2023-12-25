using Harmony;
using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Translator
{
	public class Translator : MelonMod
	{
		public static new HarmonyLib.Harmony harmonyInstance;

		public override void OnInitializeMelon()
		{
			harmonyInstance = HarmonyInstance;

			try
			{
				LanguagePatch.Init();
			}
			catch (Exception e)
			{
				MelonLogger.Warning("Error at Language: " + Environment.NewLine + e);
			}
		}

		public override void OnSceneWasInitialized(int buildIndex, string sceneName)
		{
			if(sceneName == "MainMenu")
			{
				GameObject langMenu = GameObject.Find("LanguageMenu_leaveACTIVE");
				int lastOGLanguage = langMenu.transform.childCount - 1;
				GameObject langArrowsParent = UnityEngine.Object.Instantiate(GameObject.Find("Resolution"));
				GameObject lang = UnityEngine.Object.Instantiate(GameObject.Find("en"), langMenu.transform);

				Button[] buttons = langArrowsParent.GetComponentsInChildren<Button>();
				lang.GetComponentInChildren<SelectionBorder>().ButtonsWithTextColors = buttons;
				foreach (Button button in buttons)
				{
					button.transform.SetParent(lang.transform);
					button.transform.localScale = new Vector3(1, 1, 1);
				}
				buttons[0].transform.localPosition = new Vector3(370, 35, 0);
				buttons[1].transform.localPosition = new Vector3(-300, 35, 0);
				UnityEngine.Object.Destroy(langArrowsParent);

				UnityEngine.Object.Destroy(lang.GetComponent<OptionsButton>());
				LanguageSelector selector = lang.AddComponent<LanguageSelector>();
				selector.Init();
				selector.langMenu = langMenu;

				// add readed languages
				selector.languages.Add("HU");
				selector.languages.Add("Convex");
				selector.languages.Add("Nig");

				MainMenu menu = langMenu.GetComponent<MainMenu>();
				menu.ConfiguredMenuItems.Add(lang);
				Traverse menuTraverse = Traverse.Create(menu);
				menuTraverse.Field("Indices").GetValue<List<int>>().Add(0);
				menuTraverse.Field("MenuItemTransforms").GetValue<List<RectTransform>>().Add(lang.GetComponent<RectTransform>());
				menuTraverse.Field("MenuItems").GetValue<List<IMenuItem>>().Add(lang.GetComponent<LanguageSelector>());

				List<Vector2> poses = menuTraverse.Field("originalMenuItemPositions").GetValue<List<Vector2>>();
				float diff = poses[0].y - poses[2].y;
				poses.Add(new Vector2(0, poses[lastOGLanguage].y - diff));

				lang.name = "Custom Languages";
				CallOnHover call = lang.GetComponent<CallOnHover>();
				UnityEvent hover = call.onHover;
				hover.RemoveAllListeners();
				hover.AddListener(() => {
					menu.Select(14);
					selector.InputActive = true;
				});
				call.onExitHover.AddListener(() => {
					selector.InputActive = false;
				});
				UnityEvent click = call.onClick;
				click.RemoveAllListeners();
				click.AddListener(() => selector.Click());
			}
		}
	}

	public class LanguageSelector : MonoBehaviour, IMenuItem
	{
		public bool InputActive { get; set; }
		private BindInputAxisToButton[] InputArrows;
		public int optionIndex { get; private set; } = 0;
		public TextMeshProUGUI textMesh;
		public List<string> languages = new List<string>();
		public GameObject langMenu;

		public void Init()
		{
			optionIndex = (int)Settings.Get().Language;
			int max = Utils.MaxOfEnum<Language>();
			if (optionIndex > max) optionIndex = optionIndex - max - 1;
			else optionIndex = 0;

			InputArrows = GetComponentsInChildren<BindInputAxisToButton>();
			textMesh = GetComponentInChildren<TextMeshProUGUI>();
			textMesh.font = LocalizedText.localizationTable.enFontAsset;

			Button left = InputArrows[1].GetComponent<Button>();
			left.onClick.RemoveAllListeners();
			left.onClick.AddListener(Previous);

			Button right = InputArrows[0].GetComponent<Button>();
			right.onClick.RemoveAllListeners();
			right.onClick.AddListener(Next);
		}

		private void Update()
		{
			textMesh.text = languages[optionIndex];
			for (int i = 0; i < InputArrows.Length; i++)
			{
				InputArrows[i].enabled = InputActive;
			}
		}

		public void Next()
		{
			optionIndex = (optionIndex + 1) % languages.Count;
			Update();
		}

		public void Previous()
		{
			optionIndex = (optionIndex - 1 + languages.Count) % languages.Count;
			Update();
		}

		public void Click()
		{
			if (InputActive && isActiveAndEnabled)
			{
				langMenu.GetComponent<LanguageMenu>().SetLanguage(Utils.MaxOfEnum<Language>() + 1 + optionIndex);
				GameObject.Find("mainMenu_leaveACTIVE").GetComponent<MainMenu>().EnableAll();
				langMenu.GetComponent<MainMenu>().DisableAll();
				AudioManager.Get().Play("return3");
			}
		}
	}
}