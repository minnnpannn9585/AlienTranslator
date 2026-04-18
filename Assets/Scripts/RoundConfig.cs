using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Round Config", fileName = "RoundConfig")]
public sealed class RoundConfig : ScriptableObject
{
    [Header("Cards dealt to hand (fixed)")]
    [SerializeField] private List<CardDefinition> dealtCards = new List<CardDefinition>();

    [Header("Requirement (what to play)")]
    [Min(1)]
    [SerializeField] private int requiredCount = 1;

    [Tooltip("Correct answer card IDs. Order does not matter.")]
    [SerializeField] private List<int> correctCardIds = new List<int>();

    public IReadOnlyList<CardDefinition> DealtCards => dealtCards;
    public int RequiredCount => requiredCount;
    public IReadOnlyList<int> CorrectCardIds => correctCardIds;

    public bool IsSelectionCountValid(int selectedCount) => selectedCount == requiredCount;

    public bool IsCorrect(IReadOnlyList<int> playedIds)
    {
        if (playedIds == null) return false;
        if (playedIds.Count != requiredCount) return false;

        // Order-independent multiset compare
        return playedIds
            .OrderBy(x => x)
            .SequenceEqual(correctCardIds.OrderBy(x => x));
    }
}