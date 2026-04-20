using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class HandController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handRoot;
    [SerializeField] private UiManager ui;

    [Header("Factory")]
    [SerializeField] private CardView cardPrefab;

    [Header("Current Round")]
    [SerializeField] private RoundConfig currentRound;

    [Header("Radial Layout")]
    [SerializeField] private float radius = 8f;
    [SerializeField] private float maxTotalAngle = 70f;
    [SerializeField] private float anglePerCard = 15f;
    [SerializeField] private Vector3 pivotCenter = new Vector3(0f, -9.5f, 0f);

    [Header("Random Offset")]
    [SerializeField] private float randomPositionOffsetX = 0.15f;
    [SerializeField] private float randomPositionOffsetY = 0.08f;
    [SerializeField] private float randomRotationOffsetZ = 3f;

    private readonly List<CardView> _hand = new();
    private readonly List<CardView> _selected = new();

    public bool CanInteract { get; private set; }

    public int RequiredPlayCount => currentRound != null ? currentRound.RequiredCount : 0;

    /// <summary>
    /// 出牌结果事件：true=correct, false=wrong
    /// </summary>
    public event Action<bool> Played;

    public void SetRound(RoundConfig round)
    {
        currentRound = round;
    }

    private void Awake()
    {
        AutoRegisterCardsUnderRoot();
        LayoutHand();
    }

    public void SetInteractable(bool canInteract)
    {
        CanInteract = canInteract;
        if (!CanInteract)
            ClearSelection();
    }

    /// <summary>
    /// 单选逻辑：最多只允许选中 1 张。
    /// - 点已选中牌：取消选择
    /// - 点未选中牌：取消之前选中 -> 选中新牌
    /// </summary>
    public void ToggleSelect(CardView card)
    {
        if (!CanInteract) return;
        if (card == null) return;

        if (_selected.Contains(card))
        {
            _selected.Remove(card);
            card.SetSelected(false);
            UpdateSelectedCardTextUi();
            return;
        }

        // 选中新牌前，取消所有历史选中（保证单选）
        for (int i = 0; i < _selected.Count; i++)
        {
            var prev = _selected[i];
            if (prev != null) prev.SetSelected(false);
        }
        _selected.Clear();

        _selected.Add(card);
        card.SetSelected(true);

        if (ui != null)
            ui.HidePlayHint();

        UpdateSelectedCardTextUi();
    }

    // 供 UI Button 直接绑定调用
    public void PlaySelected()
    {
        if (!CanInteract) return;

        SfxManager.Instance?.PlayPlayButton();

        if (_selected.Count == 0)
        {
            if (ui != null)
                ui.ShowPlayHint();
            return;
        }

        if (currentRound == null) return;

        // 数量不对：直接判错并通知
        if (!currentRound.IsSelectionCountValid(_selected.Count))
        {
            Played?.Invoke(false);
            return;
        }

        var playedIds = _selected.Select(c => c.CardId).ToArray();
        bool correct = currentRound.IsCorrect(playedIds);

        if (!correct)
        {
            if (ui != null)
                ui.HidePlayButton();

            Played?.Invoke(false);
            return;
        }

        // 正确：移除已出的牌
        foreach (var c in _selected.ToArray())
        {
            _hand.Remove(c);
            Destroy(c.gameObject);
        }

        _selected.Clear();
        LayoutHand();
        UpdateSelectedCardTextUi();

        if (ui != null)
            ui.HidePlayButton();

        Played?.Invoke(true);
    }

    public void ClearHand()
    {
        ClearSelection();

        foreach (var card in _hand.ToArray())
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        _hand.Clear();
        LayoutHand();
        UpdateSelectedCardTextUi();

        if (ui != null)
        {
            ui.HidePlayHint();
            ui.HidePlayButton();
        }
    }

    /// <summary>
    /// 按当前回合配置发固定手牌（RoundConfig.dealtCards）。
    /// </summary>
    public void DealCurrentRound()
    {
        if (currentRound == null)
        {
            Debug.LogError($"{nameof(HandController)}: currentRound is not assigned.");
            return;
        }

        DealCards(currentRound.DealtCards);
    }

    public void DealCards(IReadOnlyList<CardDefinition> definitions)
    {
        ClearHand();

        if (handRoot == null)
        {
            Debug.LogError($"{nameof(HandController)}: handRoot is not assigned.");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogError($"{nameof(HandController)}: cardPrefab is not assigned.");
            return;
        }

        if (definitions == null || definitions.Count == 0)
        {
            Debug.LogWarning($"{nameof(HandController)}: no cards to deal.");
            return;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            var def = definitions[i];
            var card = Instantiate(cardPrefab, handRoot);
            card.Initialize(this);
            card.SetDefinition(def);

            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-randomPositionOffsetX, randomPositionOffsetX),
                UnityEngine.Random.Range(-randomPositionOffsetY, randomPositionOffsetY),
                0f);

            float randomRotZ = UnityEngine.Random.Range(-randomRotationOffsetZ, randomRotationOffsetZ);
            card.SetRandomOffset(randomOffset, randomRotZ);

            _hand.Add(card);
        }

        LayoutHand();
        UpdateSelectedCardTextUi();

        if (ui != null)
        {
            ui.HidePlayHint();
            ui.ShowPlayButton();
        }
    }

    public void LayoutHand()
    {
        if (handRoot == null) return;
        if (_hand.Count == 0) return;

        float totalAngle = Mathf.Min(maxTotalAngle, (_hand.Count - 1) * anglePerCard);
        float startAngle = -totalAngle * 0.5f;
        float angleStep = _hand.Count > 1 ? (totalAngle / (_hand.Count - 1)) : 0f;

        for (int i = 0; i < _hand.Count; i++)
        {
            var card = _hand[i];
            if (card == null) continue;

            float currentAngle = startAngle + i * angleStep;
            float rad = currentAngle * Mathf.Deg2Rad;

            float x = Mathf.Sin(rad) * radius;
            float y = Mathf.Cos(rad) * radius;

            var basePos = pivotCenter + new Vector3(x, y, 0f);
            var baseRot = Quaternion.Euler(0f, 0f, -currentAngle);

            card.SetBaseTransform(basePos, baseRot);
            card.SetSelected(card.IsSelected); // Automatically handles local direction movement

            card.transform.SetSiblingIndex(i);
        }
    }

    private void UpdateSelectedCardTextUi()
    {
        if (ui == null) return;

        if (_selected.Count == 0 || _selected[0] == null || _selected[0].Definition == null)
        {
            ui.HideSelectedCardText();
            return;
        }

        ui.ShowSelectedCardText(_selected[0].Definition.ContentText);
    }

    private void ClearSelection()
    {
        foreach (var c in _selected)
            if (c != null) c.SetSelected(false);

        _selected.Clear();
        UpdateSelectedCardTextUi();
    }

    private void AutoRegisterCardsUnderRoot()
    {
        if (handRoot == null) return;

        _hand.Clear();

        foreach (Transform child in handRoot)
        {
            var card = child.GetComponent<CardView>();
            if (card == null) continue;

            card.Initialize(this);
            _hand.Add(card);
        }
    }
}