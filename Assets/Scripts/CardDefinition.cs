using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Card Definition", fileName = "CardDefinition")]
public sealed class CardDefinition : ScriptableObject
{
    [SerializeField] private int id;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite artwork;

    public int Id => id;
    public string DisplayName => displayName;
    public Sprite Artwork => artwork;
}