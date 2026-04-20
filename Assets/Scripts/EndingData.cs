using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Ending Data", fileName = "EndingData")]
public sealed class EndingData : ScriptableObject
{
    [SerializeField] private Sprite endingImage;
    [SerializeField] private string endingTitle;
    [TextArea(3, 8)]
    [SerializeField] private string endingText;
    [SerializeField] private AudioClip endingBgm;

    public Sprite EndingImage => endingImage;
    public string EndingTitle => endingTitle;
    public string EndingText => endingText;
    public AudioClip EndingBgm => endingBgm;
}