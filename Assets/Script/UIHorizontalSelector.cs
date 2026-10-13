using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIHorizontalSelector : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Transform boxFrame;

    private List<string> _options = new List<string>();
    private int _currentIndex = 0;

    public event Action<int> OnValueChanged;

    private void Reset() => AutoAssignReferences();
    private void OnValidate() => AutoAssignReferences();

    private void Awake()
    {
        AutoAssignReferences();
        InitButtons();
    }

    public void AutoAssignReferences()
    {
        // 1. Знаходимо рамку SelectorBox
        if (boxFrame == null)
        {
            Transform box = transform.name.Equals("SelectorBox", StringComparison.OrdinalIgnoreCase)
                ? transform
                : transform.Find("SelectorBox");
            boxFrame = box != null ? box : transform;
        }

        if (boxFrame == null) return;

        // 2. Знаходимо кнопки напряму за іменами
        if (leftButton == null)
        {
            Transform btnL = boxFrame.Find("Btn_Left");
            if (btnL != null) leftButton = btnL.GetComponent<Button>();
        }

        if (rightButton == null)
        {
            Transform btnR = boxFrame.Find("Btn_Right");
            if (btnR != null) rightButton = btnR.GetComponent<Button>();
        }

        // 3. ПРИМУСОВО шукаємо об'єкт ValueText напряму за точним ім'ям
        Transform exactValueTextTransform = boxFrame.Find("ValueText");
        if (exactValueTextTransform != null)
        {
            valueText = exactValueTextTransform.GetComponent<TextMeshProUGUI>();
        }

        // 4. Захисна перевірка: якщо в valueText все одно затесався текст з кнопок — очищаємо його
        if (valueText != null)
        {
            bool isInsideLeftBtn = leftButton != null && valueText.transform.IsChildOf(leftButton.transform);
            bool isInsideRightBtn = rightButton != null && valueText.transform.IsChildOf(rightButton.transform);

            if (isInsideLeftBtn || isInsideRightBtn)
            {
                valueText = null;
                if (exactValueTextTransform != null)
                {
                    valueText = exactValueTextTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }
    }

    public void InitButtons()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(SelectPrevious);
            leftButton.onClick.AddListener(SelectPrevious);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(SelectNext);
            rightButton.onClick.AddListener(SelectNext);
        }
    }

    public void SetOptions(List<string> options, int defaultIndex = 0)
    {
        AutoAssignReferences();
        InitButtons();

        _options = options != null ? options : new List<string>();
        _currentIndex = Mathf.Clamp(defaultIndex, 0, Mathf.Max(0, _options.Count - 1));
        UpdateDisplay(false);
    }

    public void SetIndex(int index)
    {
        if (_options == null || _options.Count == 0) return;
        _currentIndex = Mathf.Clamp(index, 0, _options.Count - 1);
        UpdateDisplay(false);
    }

    public int CurrentIndex => _currentIndex;

    public void SelectPrevious()
    {
        if (_options == null || _options.Count == 0) return;
        _currentIndex--;
        if (_currentIndex < 0) _currentIndex = _options.Count - 1;

        AnimateButtonClick(leftButton != null ? leftButton.transform : null);
        UpdateDisplay(true);
        OnValueChanged?.Invoke(_currentIndex);
    }

    public void SelectNext()
    {
        if (_options == null || _options.Count == 0) return;
        _currentIndex++;
        if (_currentIndex >= _options.Count) _currentIndex = 0;

        AnimateButtonClick(rightButton != null ? rightButton.transform : null);
        UpdateDisplay(true);
        OnValueChanged?.Invoke(_currentIndex);
    }

    private void UpdateDisplay(bool animate)
    {
        if (valueText == null || _options == null || _options.Count == 0 || _currentIndex >= _options.Count) return;

        valueText.text = _options[_currentIndex];

        if (animate)
        {
            valueText.DOKill();
            valueText.alpha = 0.3f;
            valueText.DOFade(1f, 0.15f).SetUpdate(true);

            if (boxFrame != null)
            {
                boxFrame.DOKill();
                boxFrame.DOShakePosition(0.15f, new Vector3(2f, 0f, 0f), 10).SetUpdate(true);
            }
        }
    }

    private void AnimateButtonClick(Transform btnTransform)
    {
        if (btnTransform == null) return;
        btnTransform.DOKill();
        btnTransform.DOPunchScale(new Vector3(-0.15f, -0.15f, 0f), 0.15f).SetUpdate(true);
    }
}