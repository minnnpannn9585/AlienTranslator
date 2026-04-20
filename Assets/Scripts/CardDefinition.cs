using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Card Definition", fileName = "CardDefinition")]
public sealed class CardDefinition : ScriptableObject
{
    [SerializeField] private int id;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite artwork;
    [TextArea(2, 5)]
    [SerializeField] private string contentText;

    public int Id => id;
    public string DisplayName => displayName;
    public Sprite Artwork => artwork;
    public string ContentText => contentText;
}