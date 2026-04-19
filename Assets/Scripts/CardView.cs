using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CardView : MonoBehaviour
{
    [Header("Selection Visual")]
    [SerializeField] private float selectedYOffset = 0.35f;
    [SerializeField] private float moveDuration = 0.08f;

    private HandController _hand;
    private Vector3 _baseLocalPos;
    private Quaternion _baseLocalRot = Quaternion.identity;
    private Coroutine _moveCo;

    private SpriteRenderer _renderer;
    private CardDefinition _definition;

    public bool IsSelected { get; private set; }
    public CardDefinition Definition => _definition;
    public int CardId => _definition != null ? _definition.Id : -1;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(HandController hand)
    {
        _hand = hand;
        _baseLocalPos = transform.localPosition;
        _baseLocalRot = transform.localRotation;
    }

    public void SetDefinition(CardDefinition definition)
    {
        _definition = definition;
        if (_renderer != null)
            _renderer.sprite = _definition != null ? _definition.Artwork : null;
    }

    public void SetBaseTransform(Vector3 baseLocalPos, Quaternion baseLocalRot)
    {
        _baseLocalPos = baseLocalPos;
        _baseLocalRot = baseLocalRot;
    }

    private void OnMouseDown()
    {
        if (_hand == null) return;
        _hand.ToggleSelect(this);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        Vector3 localUp = _baseLocalRot * Vector3.up;
        var targetPos = _baseLocalPos + (IsSelected ? localUp * selectedYOffset : Vector3.zero);
        
        MoveAndRotateToLocal(targetPos, _baseLocalRot);
    }

    public void MoveAndRotateToLocal(Vector3 targetLocalPos, Quaternion targetLocalRot)
    {
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = StartCoroutine(MoveAndRotateRoutine(targetLocalPos, targetLocalRot));
    }

    private IEnumerator MoveAndRotateRoutine(Vector3 targetLocalPos, Quaternion targetLocalRot)
    {
        var startPos = transform.localPosition;
        var startRot = transform.localRotation;
        float t = 0f;

        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.01f, moveDuration));
            transform.localPosition = Vector3.Lerp(startPos, targetLocalPos, a);
            transform.localRotation = Quaternion.Lerp(startRot, targetLocalRot, a);
            yield return null;
        }

        transform.localPosition = targetLocalPos;
        transform.localRotation = targetLocalRot;
        _moveCo = null;
    }
}