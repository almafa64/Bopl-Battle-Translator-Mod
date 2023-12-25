using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Translator
{
	internal static class LanguagePatch
	{
		//public static string[] en = { "en", "play", "start!", "online", "settings", "exit", "sfxvol", "musicvol", "abilities", "screen shake", "rumble", "resolution", "save", "on", "off", "high", "fullscreen", "windowed", "borderless", "screen", "click to join!", "ready!", "color", "team", "rebind keys", "click jump", "click ability_left", "click ability_right", "click ability_top", "click move_left", "click move_down", "click move_right", "click move_up", "vsync", "nothing", "hide", "names", "names and avatars", "mouse only", "local game", "click to start!", "next level", "ability select", "winner!!", "winners!!", "draw!", "wishlist bopl battle!", "choosing...", "leave game?", "invite friend", "practice", "hold down", "to aim", "to throw grenade", "to dash", "click", "credits", "back", "tutorial", "your lobby is empty", "invite a friend to play online", "not available in demo", "bow", "tesla coil", "engine", "smoke", "invisibility", "platform", "meteor", "random", "missile", "black hole", "rock", "push", "dash", "grenade", "roll", "time stop", "blink gun", "gust", "mine", "revival", "spike", "shrink ray", "growth ray", "chain", "time lock", "throw", "teleport", "grappling hook", "drill" };
		public static string[] hu = { "hu", "játsz", "kezdés!", "online", "beállítások", "kilépés", "sfxvol", "musicvol", "abilities", "screen shake", "rumble", "resolution", "save", "on", "off", "high", "fullscreen", "windowed", "borderless", "screen", "click to join!", "ready!", "color", "team", "rebind keys", "click jump", "click ability_left", "click ability_right", "click ability_top", "click move_left", "click move_down", "click move_right", "click move_up", "vsync", "nothing", "hide", "names", "names and avatars", "mouse only", "local game", "click to start!", "next level", "ability select", "winner!!", "winners!!", "draw!", "wishlist bopl battle!", "choosing...", "leave game?", "invite friend", "practice", "hold down", "to aim", "to throw grenade", "to dash", "click", "credits", "back", "tutorial", "your lobby is empty", "invite a friend to play online", "not available in demo", "bow", "tesla coil", "engine", "smoke", "invisibility", "platform", "meteor", "random", "missile", "black hole", "rock", "push", "dash", "grenade", "roll", "time stop", "blink gun", "gust", "mine", "revival", "spike", "shrink ray", "growth ray", "chain", "time lock", "throw", "teleport", "grappling hook", "drill" };

		public enum MyLanguage
		{
			HU = 14
		}

		public static Language GetLanguage(this MyLanguage l) => (Language)l;

		static MethodInfo _updateText;
		static MethodInfo _localTable;

		public static unsafe void Init()
		{
			_updateText = typeof(LocalizedText).GetMethod(nameof(LocalizedText.UpdateText));
			Translator.harmonyInstance.Patch(_updateText, prefix: new HarmonyMethod(Utils.GetMethod(nameof(Patch))));
			
			_localTable = typeof(LocalizationTable).GetMethod(nameof(LocalizationTable.GetText));
			Translator.harmonyInstance.Patch(_localTable, prefix: new HarmonyMethod(Utils.GetMethod(nameof(Patch2))));

			// read languages
		}

		internal static bool Patch(LocalizedText __instance)
		{
			Language currentLanguage = Settings.Get().Language;
			if ((int)currentLanguage <= Utils.MaxOfEnum<Language>()) return true;

			// own language
			TMP_FontAsset font = LocalizedText.localizationTable.GetFont(Language.EN, __instance.useFontWithStroke);

			Traverse traverse = Traverse.Create(__instance);
			traverse.Field("currentLanguage").SetValue(currentLanguage);
			TextMeshProUGUI textToLocalize = (TextMeshProUGUI)traverse.Field("textToLocalize").GetValue();
			string enText = (string)traverse.Field("enText").GetValue();
			TextMesh textToLocalize2 = (TextMesh)traverse.Field("textToLocalize2").GetValue();

			if (textToLocalize == null)
			{
				textToLocalize2.text = LocalizedText.localizationTable.GetText(enText, currentLanguage);
				return false;
			}

			if (!__instance.useFontWithStroke)
			{
				textToLocalize.fontStyle = FontStyles.Normal;
			}
			else
			{
				textToLocalize.fontStyle = FontStyles.Bold;
			}

			textToLocalize.text = LocalizedText.localizationTable.GetText(enText, currentLanguage);

			if (!__instance.ignoreFontChange && textToLocalize.font != font)
			{
				textToLocalize.font = font;
			}

			return false;
		}

		internal static bool Patch2(LocalizationTable __instance, ref string __result, string __0, Language __1)
		{
			if ((int)__1 <= Utils.MaxOfEnum<Language>()) return true;

			if (__1 == MyLanguage.HU.GetLanguage())
			{
				__result = (string)Traverse.Create(__instance).Method("getText", __0, hu).GetValue();
			}
			else __result = __0;

			return false;
		}
	}
}
