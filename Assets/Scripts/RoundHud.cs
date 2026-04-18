using TMPro;
using UnityEngine;

public sealed class RoundHud : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text requiredCountText;

    public void SetRoundInfo(int roundNumber, int requiredCount)
    {
        if (roundText != null)
            roundText.text = $"Round: {roundNumber}";

        if (requiredCountText != null)
            requiredCountText.text = $"Need: {requiredCount}";
    }
}