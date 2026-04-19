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

    [Header("Character Animation Prefabs")]
    [Tooltip("Prefabs to instantiate for this round's character animation.")]
    [SerializeField] private List<GameObject> characterPrefabs = new List<GameObject>();

    public IReadOnlyList<CardDefinition> DealtCards => dealtCards;
    public int RequiredCount => requiredCount;
    public IReadOnlyList<int> CorrectCardIds => correctCardIds;
    public IReadOnlyList<GameObject> CharacterPrefabs => characterPrefabs;

    public bool IsSelectionCountValid(int selectedCount) => selectedCount == requiredCount;

    public bool IsCorrect(IReadOnlyList<int> playedIds)
    {
        if (playedIds == null) return false;
        if (playedIds.Count != requiredCount) return false;

        return playedIds
            .OrderBy(x => x)
            .SequenceEqual(correctCardIds.OrderBy(x => x));
    }
}