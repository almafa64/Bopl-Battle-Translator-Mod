using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Translator
{
	public class Translator : MelonMod
	{
		public static new HarmonyLib.Harmony harmonyInstance;
		public static DirectoryInfo translationsDir;
		public static MelonPreferences_Category category;
		public static MelonPreferences_Entry<string> lastCustomLanguageCode;

		public override void OnInitializeMelon()
		{
			harmonyInstance = HarmonyInstance;
			translationsDir = new DirectoryInfo(Path.Combine(MelonEnvironment.ModsDirectory, "BoplTranslator"));
			translationsDir.Create();
			category = MelonPreferences.CreateCategory("BoplTranslator");
			lastCustomLanguageCode = category.CreateEntry("last_custom_language_code", "");

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
			if (sceneName == "MainMenu")
			{
				if (LanguagePatch.languages.Count == 0)
				{
					if ((int)Settings.Get().Language > Utils.MaxOfEnum<Language>()) Settings.Get().Language = 0;
					return;
				}
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
				// add readed languages
				foreach (string[] words in LanguagePatch.languages)
				{
					selector.languageNames.Add(words[0]);
				}
				if (lastCustomLanguageCode.Value == "")
				{
					lastCustomLanguageCode.Value = selector.languageNames[0];
					category.SaveToFile();
				}
				selector.langMenu = langMenu;
				selector.Init();

				MainMenu menu = langMenu.GetComponent<MainMenu>();
				menu.ConfiguredMenuItems.Add(lang);
				Traverse menuTraverse = Traverse.Create(menu);
				menuTraverse.Field("Indices").GetValue<List<int>>().Add(0);
				menuTraverse.Field("MenuItemTransforms").GetValue<List<RectTransform>>().Add(lang.GetComponent<RectTransform>());
				menuTraverse.Field("MenuItems").GetValue<List<IMenuItem>>().Add(lang.GetComponent<OptionsButton>()); // bs but works

				List<Vector2> poses = menuTraverse.Field("originalMenuItemPositions").GetValue<List<Vector2>>();
				float diff = poses[0].y - poses[2].y;
				poses.Add(new Vector2(0, poses[lastOGLanguage].y - diff));

				lang.name = "Custom Languages";
				CallOnHover call = lang.GetComponent<CallOnHover>();
				UnityEvent hover = call.onHover;
				hover.RemoveAllListeners();
				hover.AddListener(() =>
				{
					menu.Select(14);
					selector.InputActive = true;
				});
				call.onExitHover.AddListener(() =>
				{
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
		public List<string> languageNames = new List<string>();
		public GameObject langMenu;

		private void TryFindLastLanguage()
		{
			int index = languageNames.IndexOf(Translator.lastCustomLanguageCode.Value);
			if (index != -1)
			{
				optionIndex = index;
				langMenu.GetComponent<LanguageMenu>().SetLanguage(index + LanguagePatch.MaxOGLanguage + 1);
				MelonLogger.Msg($"Found last used language \"{Translator.lastCustomLanguageCode.Value}\"");
			}
			else
			{
				langMenu.GetComponent<LanguageMenu>().SetLanguage((int)Language.EN);
				optionIndex = 0;
				Translator.lastCustomLanguageCode.Value = languageNames[0];
				Translator.category.SaveToFile();
				MelonLogger.Error($"Couldn't find last used language \"{Translator.lastCustomLanguageCode.Value}\"");
			}
		}

		public void Init()
		{
			optionIndex = (int)Settings.Get().Language;
			if (optionIndex <= LanguagePatch.MaxOGLanguage) optionIndex = 0;
			else
			{
				optionIndex = optionIndex - LanguagePatch.MaxOGLanguage - 1;
				if (optionIndex >= languageNames.Count)
				{
					MelonLogger.Warning($"Language number {optionIndex} was selected, but no language with that number exists");
					TryFindLastLanguage();
				}
				else if (languageNames[optionIndex] != Translator.lastCustomLanguageCode.Value)
				{
					MelonLogger.Warning($"last language wasn't \"{languageNames[optionIndex]}\", it was \"{Translator.lastCustomLanguageCode.Value}\"");
					TryFindLastLanguage();
				}

			}
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
			textMesh.text = languageNames[optionIndex];
			for (int i = 0; i < InputArrows.Length; i++)
			{
				InputArrows[i].enabled = InputActive;
			}
		}

		public void Next()
		{
			optionIndex = (optionIndex + 1) % languageNames.Count;
			Update();
		}

		public void Previous()
		{
			optionIndex = (optionIndex - 1 + languageNames.Count) % languageNames.Count;
			Update();
		}

		public void Click()
		{
			if (InputActive && isActiveAndEnabled)
			{
				langMenu.GetComponent<LanguageMenu>().SetLanguage(LanguagePatch.MaxOGLanguage + 1 + optionIndex);
				GameObject.Find("mainMenu_leaveACTIVE").GetComponent<MainMenu>().EnableAll();
				langMenu.GetComponent<MainMenu>().DisableAll();
				AudioManager.Get().Play("return3");
				Translator.lastCustomLanguageCode.Value = languageNames[optionIndex];
				Translator.category.SaveToFile();
			}
		}
	}
}