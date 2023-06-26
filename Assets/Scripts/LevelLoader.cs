using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class LevelLoader
{
    private static string currentText;
    private static int textPosition;

    public static List<LevelData> ReadMainCollection()
    {
        TextAsset file = Resources.Load<TextAsset>("main");
        currentText = file.text;
        textPosition = 0;
        return ReadCollectionText();
    }

    private static List<LevelData> ReadCollectionText()
    {
        List<LevelData> res = new List<LevelData>();

        while (true)
        {
            if (IsEndOfFile()) break;
            res.Add(ReadLevel());
        }

        return res;
    }

    private static LevelData ReadLevel()
    {
        LevelData data = new LevelData();
        string title = ReadLine(); // Read title

        data.title = title;
        data.size = Vector2Int.zero;
        
        //// Get map lines
        string levelData = "";
        string contentLine;
        while (true) {
            contentLine = ReadLine();
            char firstChar = contentLine[0];

            if (firstChar == '-')
                break;

            if (data.size.x > 0 && contentLine.Length != data.size.x)
                throw new System.Exception($"In level map '{title}': inconsistent line length on line '{contentLine}', expected a length of {data.size.x}");

            data.size.x = contentLine.Length;
            data.size.y++;
            levelData += contentLine;
        }

        LevelObjectData[] objects = new LevelObjectData[26];
        int objectCountOnMap = 0;

        // Convert map to bool array
        data.data = new bool[data.size.x * data.size.y];
        for (int x = 0; x < data.size.x; x++)
        {
            for (int y = 0; y < data.size.y; y++)
            {   
                char c = levelData[x + (data.size.y - y - 1) * data.size.x]; 
                int targetIndex = x + y * data.size.x;

                if (c == '.')
                    data.data[targetIndex] = true;
                else if (c == '#')
                    data.data[targetIndex] = false;
                else if (IsValidLevelObjectCharacter(c))
                {
                    objectCountOnMap++;
                    objects[c - 'a'] = new LevelObjectData{
                        position = new Vector2Int(x, y)
                    };
                    
                    data.data[targetIndex] = true;
                }
                else
                    throw new System.Exception($"In level map '{title}' at position ({x}, {y}): expected a map character, got {c}");
            }
        }

        //// Get object lines
        data.objects = new LevelObjectData[objectCountOnMap];
        int objectCountInObjects = 0;
        while (true)
        {
            if (objectCountInObjects > objectCountOnMap)
                throw new System.Exception($"In level objects '{title}': there are more objects described than there are on the map");

            contentLine = ReadLine();

            char c = contentLine[0];

            if (c == '-') 
                break;

            if (!IsValidLevelObjectCharacter(c))
                throw new System.Exception($"In level objects '{title}': expected an object character, got {c}");

            LevelObjectData obj = objects[c - 'a'];

            if (obj == null)
                throw new System.Exception($"In level objects '{title}': the object {c} is not present in the map");

            string[] tokens = contentLine.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length != 4)
                throw new System.Exception($"In level objects '{title}', object {c}: expected 4 tokens on line, but got {tokens.Length}");

            obj.type = tokens[1] switch {
                "blob" => ObjectType.blob,
                "end" => ObjectType.end,
                _ => throw new System.Exception($"In level objects '{title}', object {c}: unknown object type '{tokens[1]}'")
            };

            obj.color = tokens[2] switch {
                "none" => GameColor.none,
                "red" => GameColor.red,
                "blue" => GameColor.blue,
                "green" => GameColor.green,
                "magenta" => GameColor.magenta,
                "cyan" => GameColor.cyan,
                "yellow" => GameColor.yellow,
                "brown" => GameColor.brown,
                _ => throw new System.Exception($"In level objects '{title}', object {c}: unknown object color '{tokens[2]}'")
            };

            try {
                obj.eyes = int.Parse(tokens[3]);
            }
            catch (System.FormatException) {
                throw new System.Exception($"In level objects '{title}', object {c}: expected an integer, got '{tokens[3]}'");
            }

            data.objects[objectCountInObjects] = obj;
            objects[c - 'a'] = null; // Prevent multiple descriptions on the same object

            objectCountInObjects++;
        }

        if (objectCountInObjects < objectCountOnMap)
            throw new System.Exception($"In level objects '{title}': some objects are present on the map but not described below");

        return data;
    }

    /// <summary>
    /// Read until EOF or \n, ignoring whitespace. Throw if directly EOF without anything in front of it
    /// </summary>
    private static string ReadLine()
    {
        char start = ReadChar();

        int startPosition = textPosition;
        while (textPosition < currentText.Length && currentText[textPosition] != '\n')
        {
            textPosition++;
        }

        textPosition++;
        return start.ToString() + currentText[startPosition..(textPosition - 1)].Replace("\r", "");
    }

    /// <summary>
    /// Read until find a non whitespace
    /// </summary>
    private static char ReadChar()
    {
        while (textPosition < currentText.Length && IsWhitespace(currentText[textPosition]))
        {
            textPosition++;
        }

        if (textPosition == currentText.Length)
            throw new System.Exception("Expected a character, but got to end of file");

        textPosition++;
        return currentText[textPosition - 1];
    }

    private static bool IsValidLevelMapCharacter(char c)
    {
        return c == '#' || c == '.' || IsValidLevelObjectCharacter(c);
    } 

    private static bool IsValidLevelObjectCharacter(char c)
    {
        return 'a' <= c && c <= 'z';
    } 

    public static bool IsWhitespace(char c)
    {
        return " \n\t\r".Contains(c);
    }

    private static bool IsEndOfFile()
    {
        int pos = textPosition;
        while (pos < currentText.Length && IsWhitespace(currentText[pos]))
            pos++;

        return pos == currentText.Length;
    }
}
