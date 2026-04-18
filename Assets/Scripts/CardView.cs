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
    }

    public void SetDefinition(CardDefinition definition)
    {
        _definition = definition;
        if (_renderer != null)
            _renderer.sprite = _definition != null ? _definition.Artwork : null;
    }

    public void SetBaseLocalPosition(Vector3 baseLocalPos)
    {
        _baseLocalPos = baseLocalPos;
    }

    private void OnMouseDown()
    {
        if (_hand == null) return;
        _hand.ToggleSelect(this);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        var target = _baseLocalPos + (IsSelected ? new Vector3(0f, selectedYOffset, 0f) : Vector3.zero);
        MoveToLocal(target);
    }

    public void MoveToLocal(Vector3 targetLocalPos)
    {
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = StartCoroutine(MoveRoutine(targetLocalPos));
    }

    private IEnumerator MoveRoutine(Vector3 targetLocalPos)
    {
        var start = transform.localPosition;
        float t = 0f;

        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.01f, moveDuration));
            transform.localPosition = Vector3.Lerp(start, targetLocalPos, a);
            yield return null;
        }

        transform.localPosition = targetLocalPos;
        _moveCo = null;
    }
}