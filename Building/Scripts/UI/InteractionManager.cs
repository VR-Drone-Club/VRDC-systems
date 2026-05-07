
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class InteractionManager : UdonSharpBehaviour
{
    private Vector2[] _buttonStarts;
    private Vector2 _buttonEnds;
    private Button[] _buttons;
    private RectTransform[] _rectTransforms;
    private Rect[] _rects;
    private RectTransform _canvas;
    
    void Start()
    {
        _buttons = GetComponentsInChildren<Button>(true);
        _canvas = GetComponent<RectTransform>();
        _rectTransforms = new RectTransform[_buttons.Length];
        _rects = new Rect[_buttons.Length];
        for (int i = 0; i < _buttons.Length; i++)
        {
            _rectTransforms[i] = _buttons[i].GetComponent<RectTransform>();
            _rects[i] = _rectTransforms[i].rect;
        }
    }


    public bool Hover(Vector2 position)
    {
        return TestPosition(position);
    }

    public bool Click(Vector2 position)
    {
        Button button = TestPosition(position);
        if (!Utilities.IsValid(button)) return false;
        button.GetComponent<UdonBehaviour>().SendCustomEvent("ButtonPressed");
        return true;
    }

    private Button TestPosition(Vector3 position)
    {
        var worldPos = _canvas.TransformPoint(position);
        for (int i = 0; i < _buttons.Length; i++)
        {
            if (!_buttons[i].gameObject.activeInHierarchy) continue;
            Vector2 localPos = _buttons[i].transform.InverseTransformPoint(worldPos);
            if (!_rects[i].Contains(localPos)) continue;
            return _buttons[i];
        }
        return null;
    }
}
