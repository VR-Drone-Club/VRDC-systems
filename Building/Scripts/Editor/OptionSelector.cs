#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class OptionSelector : ScriptableWizard
{
    public static OptionSelector instance;
    public static string selectedControlID;
    public static int chosenIndex;
    public static string chosenText;
    public static bool itemChosen;
    
    public static int SelectWithInt(int index, string[] options, string name, string uniqueIdentifier)
    {
        index = Mathf.Clamp(index, 0, options.Length - 1);
        if (GUILayout.Button(options[index], GUILayout.MaxWidth(150)))
        {
            if (instance != null)
            {
                instance.Close();
            }

            instance = DisplayWizard<OptionSelector>(name);
            selectedControlID = uniqueIdentifier;
            instance.options = options;
            instance.previousIndex = index;
        }
        if (itemChosen && selectedControlID == uniqueIdentifier)
        {
            itemChosen = false;
            GUI.changed = true;
            return chosenIndex;
        }
        return index;
    }
    public static string SelectWithString(string value, string[] options, string name, string uniqueIdentifier, bool allowArbitraryText)
    {
        string label = string.Empty;
        
        int indexOfValue = Array.IndexOf(options, value);
        if (!options.Contains(value) && !allowArbitraryText)
        {
            indexOfValue = -1;
            if (string.IsNullOrEmpty(value))
            {
                label = "EMPTY";
            }
            else
            {
                label = $"MISSING: {value}";
            }
        }
        else
        {
            label = value;
        }
        if (GUILayout.Button(label, GUILayout.MaxWidth(150)))
        {
            if (instance != null)
            {
                instance.Close();
            }
            instance = DisplayWizard<OptionSelector>(name);
            selectedControlID = uniqueIdentifier;
            instance.options = options;
            instance.previousIndex = indexOfValue;
            instance.allowArbitraryText = allowArbitraryText;
            if (allowArbitraryText && !options.Contains(value))
            {
                instance.previousArbitraryText = value;
            }
        }
        if (itemChosen && selectedControlID == uniqueIdentifier)
        {
            itemChosen = false;
            GUI.changed = true;
            if (allowArbitraryText)
            {
                return chosenText;
            }
            else
            {
                return options[chosenIndex];
            }
        }
        return value;
    }

    public string[] options;
    public string previousArbitraryText;
    public string searchInput;
    public int previousIndex = -1;

    private int currentIndex = -1;
    private bool started;
    private bool hasSearched;
    private bool allowArbitraryText;
    private Vector2 scrollPosition;
    private List<string> visibleOptions;
    private List<int> optionLinks;
    private void OnGUI()
    {
        if (!started)
        {
            started = true;
            Vector2 pos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            Rect rect = position;
            rect.x = pos.x;
            rect.y = pos.y;
            position = rect;
        }
        //GUILayout.Label(staticUniqueIdentifier);
        string oldSearch = searchInput;
        GUI.SetNextControlName("OptionSelectorTextField");
        searchInput = GUILayout.TextField(searchInput);
        GUI.FocusControl("OptionSelectorTextField");
        if (visibleOptions == null)
        {
            UpdateVisibleOptions();
        }
        Event e = Event.current;
        switch (e.type)
        {
            case EventType.Used:
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.DownArrow:
                    {
                        currentIndex++;
                        currentIndex = Mathf.Clamp(currentIndex, 0, visibleOptions.Count - 1);
                        break;
                    }
                    case KeyCode.UpArrow:
                    {
                        currentIndex--;
                        currentIndex = Mathf.Clamp(currentIndex, 0, visibleOptions.Count - 1);
                        break;
                    }
                    case KeyCode.Return:
                    {
                        if (currentIndex == -1) 
                            Close();
                        if (allowArbitraryText)
                            chosenText = visibleOptions[currentIndex];
                        else
                            chosenIndex = optionLinks[currentIndex];
                        
                        itemChosen = true;
                        Close();
                        break;
                    }
                    case KeyCode.Escape:
                    {
                        Close();
                        break;
                    }
                }

                break;
            }
        }
        if (searchInput != oldSearch && (!string.IsNullOrEmpty(searchInput) || allowArbitraryText))
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, visibleOptions.Count - 1);
            UpdateVisibleOptions();
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < visibleOptions.Count; i++)
        {
            if (currentIndex == i)
            {
                GUI.backgroundColor = new Color(0, 0.5f, 1);
            }
            else if (previousIndex == optionLinks[i])
            {
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            }
            else
            {
                GUI.backgroundColor = Color.white;
            }
            if (GUILayout.Button(visibleOptions[i]))
            {
                if (allowArbitraryText)
                    chosenText = visibleOptions[i];
                else
                    chosenIndex = optionLinks[i];
                itemChosen = true;
                Close();
            }
        }
        GUILayout.EndScrollView();
    }

    private void UpdateVisibleOptions()
    {
        visibleOptions = new List<string>();
        optionLinks = new List<int>();
        if (allowArbitraryText)
        {
            if (!string.IsNullOrEmpty(searchInput))
            {
                visibleOptions.Add(searchInput);
                optionLinks.Add(-1);
                hasSearched = true;
            }
            else if (!hasSearched && !string.IsNullOrEmpty(previousArbitraryText))
            {
                visibleOptions.Add(previousArbitraryText);
                optionLinks.Add(-1);
            }
            else
            {
                visibleOptions.Add(searchInput);
                optionLinks.Add(-1);
            }
        }
        for (int i = 0; i < options.Length; i++)
        {
            if (searchInput == null || options[i].ToLower().Contains(searchInput.ToLower()))
            {
                visibleOptions.Add(options[i]);
                optionLinks.Add(i);
            }
        }
    }

    private void OnLostFocus()
    {
        Close();
    }
}
#endif