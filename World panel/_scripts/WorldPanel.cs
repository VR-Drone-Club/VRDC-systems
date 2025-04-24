
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class WorldPanel : UdonSharpBehaviour
{
    public GameObject[] categoryObjects;
    public GameObject categoryButtonTemplate;
    private Toggle[] _categoryButtons;
    private Transform[] _categoryBorders;
    private int _selectedCategory;
    void Start()
    {
        _categoryButtons = new Toggle[categoryObjects.Length];
        _categoryBorders = new Transform[categoryObjects.Length];
        for (int i = 0; i < categoryObjects.Length; i++)
        {
            if (!Utilities.IsValid(categoryObjects[i])) continue;
            var newObj = Instantiate(categoryButtonTemplate, categoryButtonTemplate.transform.parent);
            newObj.SetActive(true);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;
            newObj.transform.localScale = Vector3.one;
            Toggle newToggle = newObj.GetComponentInChildren<Toggle>();
            _categoryButtons[i] = newToggle;
            TextMeshProUGUI text = newToggle.GetComponentInChildren<TextMeshProUGUI>();
            _categoryBorders[i] = newToggle.transform.Find("Border");
            if (Utilities.IsValid(text)) text.text = categoryObjects[i].name;
        }
        CategoryButtonClicked(0);
    }

    public void CategoryButtonClicked()
    {
        if (!Utilities.IsValid(_categoryButtons)) return;
        for (int i = 0; i < _categoryButtons.Length; i++)
        {
            if (_categoryButtons[i].isOn)
            {
                _categoryButtons[i].SetIsOnWithoutNotify(false);
                CategoryButtonClicked(i);
                return;
            }
        }
    }

    private void CategoryButtonClicked(int index)
    {
        _selectedCategory = index;
        for (int i = 0; i < categoryObjects.Length; i++)
        {
            if (Utilities.IsValid(categoryObjects[i])) categoryObjects[i].SetActive(_selectedCategory == i);
            if (Utilities.IsValid(_categoryBorders[i])) _categoryBorders[i].gameObject.SetActive(_selectedCategory == i);
        }
    }
}
