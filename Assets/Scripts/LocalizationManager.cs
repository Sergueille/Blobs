using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public static class LocalizationManager
{
    public const string LANGUAGE_KEY = "Language";

    public enum Language
    {
        systemLanguage, english, french
    }

    private static TextAsset file;
    private static TextAsset levelFile;
    private static Dictionary<string, string> currentDict;
    private static Dictionary<string, string> levelDict;

    public static Language currentLanguage;

    private const char lineSeparator = '\n';
    private const char surround = '"';
    private static readonly string[] fieldSeparator = { "\",\"" };

    public static void Init()
    {
        LoadCSV();
        currentLanguage = (Language)GameManager.i.GetSettingInt(LANGUAGE_KEY, (int)Language.systemLanguage);
    }

    public static void UpdateLanguage(Language lang)
    {
        currentLanguage = lang;
        PlayerPrefs.SetInt(LANGUAGE_KEY, (int)lang);

        string langString;

        if (lang == Language.systemLanguage)
        {   
            langString = Application.systemLanguage switch
			{
				SystemLanguage.English => "en",
				SystemLanguage.French => "fr",
				_ => "en" 
			};
        }
        else
        {
            langString = lang switch {
                Language.french => "fr",
                Language.english => "en",
                _ => throw new Exception("Uuh?") 
            };
        }

        if (file == null || levelFile == null)
            LoadCSV();

        currentDict = GetDictionaryValues(langString, file);
        levelDict = GetDictionaryValues(langString, levelFile);
    }

    public static string GetValue(string key)
    {
        if (currentDict == null)
            UpdateLanguage(currentLanguage);

        if (currentDict.ContainsKey(key))
            return currentDict[key];

        Debug.LogError($"Localisation key {key} not found!");

        return key;
    }

    public static string GetLevelName(string key)
    {
        if (levelDict == null)
            UpdateLanguage(currentLanguage);

        if (levelDict.ContainsKey(key))
            return levelDict[key];

        Debug.LogError($"Level title {key} not localized!");

        return key;
    }

    public static void LoadCSV()
	{
        file = Resources.Load<TextAsset>("localization");
        levelFile = Resources.Load<TextAsset>("level_localization");
	}

    /// <summary>
    /// Get localization dictionary for one language
    /// </summary>
    /// <param name="attributeId">The id of the language (en, fr)</param>
    private static Dictionary<string, string> GetDictionaryValues(string attributeId, TextAsset file)
	{
        Dictionary<string, string> dictionary = new Dictionary<string, string>();

        string[] lines = file.text.Split(lineSeparator);

        int attributeIndex = -1;

        string[] headers = lines[0].Split(fieldSeparator, System.StringSplitOptions.None);
		for (int i = 0; i < headers.Length; i++)
		{
            if (headers[i].Contains(attributeId))
			{
                attributeIndex = i;
                break;
			}
		}

        Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] fields = CSVParser.Split(line);

			for (int j = 0; j < fields.Length; j++)
			{
                fields[j] = fields[j].TrimStart(' ', surround);
                fields[j] = fields[j].TrimEnd(surround);
            }

            if (fields.Length > attributeIndex)
			{
                string key = fields[0];
                string value = fields[attributeIndex].TrimEnd(surround, '\n', '\r');

                // Escape characters
                string escapedValue = "";
                for (int j = 0; j < value.Length; j++)
                {
                    if (value[j] == '\\')
                    {
                        if (value[j + 1] == 'n')
                            escapedValue += '\n';
                        if (value[j + 1] == 't')
                            escapedValue += '\t';

                        j++;
                    }
                    else escapedValue += value[j];
                }

                if (!dictionary.ContainsKey(key))
				{
                    dictionary.Add(key, escapedValue);
				}
			}
        }

        return dictionary;
    }
}
