using System;
using System.Collections.Generic;
using RetroCat.Modules.Core.UI.Scrolls;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class SnapScrollBase : MonoBehaviour
{
    public abstract UnityEvent<int> OnIndexChanged { get; }
    public abstract int Count { get; }
    public abstract int CurrentIndex { get; protected set; }

    public abstract void Next();
    public abstract void Prev();
}


[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public abstract class SnapScroll<T> : SnapScrollBase, IBeginDragHandler, IEndDragHandler where T : SnapScrollItem
{
    [Header("References")] [SerializeField]
    private ScrollRect _scrollRect;

    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _content;

    [Header("Behaviour")] [SerializeField] private bool _horizontal = true;
    [SerializeField] private bool _vertical = false;
    [SerializeField] private bool _snapOnEndDrag = true;
    [SerializeField, Min(0.01f)] private float _snapSpeed = 12f;
    [SerializeField, Min(0.0001f)] private float _stopThreshold = 0.1f;

    [Header("Manual Layout")] [SerializeField, Min(0)]
    private float _spacing = 100f;

    [SerializeField, Min(0)] private float _edgePadding = -1f;
    [SerializeField] private bool _alignVerticallyToMiddle = true;
    [SerializeField] private bool _usePreferredSize = true;
    [SerializeField] private bool _lockChildAnchors = true;
    [SerializeField] private bool _forceChildCenterPivot = true;

    [Header("Scaling")] [SerializeField] private bool _scaleByCenter = true;
    [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0, 1f, 1f, 0.8f);
    [SerializeField, Min(1f)] private float _scaleFalloff = 350f;
    [SerializeField] private bool _clampZScaleToOne = true;

    [Header("Interaction")] [SerializeField]
    private bool _clickToSnap = true;

    [SerializeField] private bool _keyboardArrows = true;
    
    [Header("Items (auto)")] 
    [SerializeField] private List<T> _items = new List<T>();

    [Header("Events")] [SerializeField] private UnityEvent<int> _onIndexChanged = new UnityEvent<int>();

    public Action<T> ItemChanged;
    public override UnityEvent<int> OnIndexChanged => _onIndexChanged;

    public IReadOnlyList<T> Items => _items;
    public T SelectedItem => _items[CurrentIndex];
    
    public override int Count => _items.Count;
    public override int CurrentIndex { get; protected set; } = -1;
    public bool IsSnapping => _snapping;

    private Vector2 _vel;
    private bool _snapping;
    private Vector2 _targetPos;

    private readonly List<float> _itemCenterAxis = new List<float>();
    private float _computedEdgePadding = 0f;

    private void Reset()
    {
        _scrollRect = GetComponentInChildren<ScrollRect>();
    }

    private void Awake()
    {
        if (!_scrollRect) _scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (!_viewport && _scrollRect) _viewport = _scrollRect.viewport;
        if (!_content && _scrollRect) _content = _scrollRect.content;

        if (_scrollRect)
        {
            _scrollRect.horizontal = _horizontal;
            _scrollRect.vertical = _vertical;
            _scrollRect.inertia = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }
    }

    private void Start()
    {
        CollectChildrenIfEmpty();
        if (_clickToSnap) MakeChildrenClickable();

        ForceLayoutNow();
        Relayout();
        UpdateScaleAndIndex(forceEvent: true);
    }

    private void Update()
    {
        if (_keyboardArrows && Application.isFocused)
        {
            if (_horizontal)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) Next();
                if (Input.GetKeyDown(KeyCode.LeftArrow)) Prev();
            }

            if (_vertical)
            {
                if (Input.GetKeyDown(KeyCode.DownArrow)) Next();
                if (Input.GetKeyDown(KeyCode.UpArrow)) Prev();
            }
        }

        if (_snapping)
        {
            if (_scrollRect) _scrollRect.velocity = Vector2.zero;

            var pos = _content.anchoredPosition;
            pos = Vector2.SmoothDamp(pos, _targetPos, ref _vel, 1f / _snapSpeed);
            _content.anchoredPosition = pos;

            if (Vector2.Distance(pos, _targetPos) <= _stopThreshold)
            {
                _content.anchoredPosition = _targetPos;
                _snapping = false;
            }
        }

        if (_scaleByCenter)
            UpdateScaleAndIndex();
    }

    public override void Next()
    {
        if (Count == 0) return;
        ScrollTo(Mathf.Clamp(CurrentIndex + 1, 0, Count - 1), true);
    }

    public override void Prev()
    {
        if (Count == 0) return;
        ScrollTo(Mathf.Clamp(CurrentIndex - 1, 0, Count - 1), true);
    }

    public void ScrollTo(int index, bool snap = true)
    {
        if (Count == 0) return;
        index = Mathf.Clamp(index, 0, Count - 1);
        Vector2 target = CalcSnapPosition(index);
        if (snap)
        {
            _targetPos = target;
            _snapping = true;
            _vel = Vector2.zero;
        }
        else
        {
            _content.anchoredPosition = target;
        }
    }

    public void RebuildItems()
    {
        CollectChildrenIfEmpty(force: true);
        ForceLayoutNow();
        Relayout();
        UpdateScaleAndIndex(forceEvent: true);
    }

    private void ForceLayoutNow()
    {
        if (!_content) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        if (_viewport) LayoutRebuilder.ForceRebuildLayoutImmediate(_viewport);
        // На всякий случай – прогнать детей:
        for (int i = 0; i < _content.childCount; i++)
        {
            var rt = _content.GetChild(i) as RectTransform;
            if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        Canvas.ForceUpdateCanvases();
    }


    public void ClearItems()
    {
        foreach (T item in _items)
            Destroy(item.gameObject);

        _items.Clear();
    }

    public void Relayout()
    {
        if (_content == null || _viewport == null) return;

        // Зафиксировать якоря/пивоты контейнера и детей (как у тебя)
        if (_lockChildAnchors)
        {
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(0f, 1f);
            _content.pivot = new Vector2(0f, 1f);

            for (int i = 0; i < _items.Count; i++)
            {
                var rt = _items[i];
                if (!rt) continue;
                rt.RectTransform.anchorMin = new Vector2(0f, 1f);
                rt.RectTransform.anchorMax = new Vector2(0f, 1f);
                if (_forceChildCenterPivot)
                    rt.RectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        _itemCenterAxis.Clear();

        if (_horizontal)
        {
            // ----- HORIZONTAL LAYOUT -----
            float vpW = _viewport.rect.width;
            float vpH = _viewport.rect.height;

            // FIX: если высота viewport ещё не посчитана — подстрахуемся
            if (vpH <= 0.1f) vpH = Mathf.Max(vpH, 1f);

            // Авто-паддинг: центр крайних может встать по центру viewport
            _computedEdgePadding = (_edgePadding >= 0f) ? _edgePadding : vpW * 0.5f;

            float cx = _computedEdgePadding; // текущий центр X (в локале контента)

            for (int i = 0; i < _items.Count; i++)
            {
                var rt = _items[i];
                if (!rt) continue;

                float w = GetItemWidth(rt.RectTransform);
                float h = GetItemHeight(rt.RectTransform);

                // Центр по Y: либо от высоты viewport (чтобы визуально по центру),
                // либо от собственного h
                float centerY = _alignVerticallyToMiddle
                    ? Mathf.Max(vpH, h) * 0.5f
                    : h * 0.5f;

                // Размеры
                rt.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                rt.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);

                rt.RectTransform.anchoredPosition = new Vector2(Mathf.Round(cx), Mathf.Round(-centerY));

                _itemCenterAxis.Add(cx);

                if (i + 1 < _items.Count)
                {
                    float nextW = GetItemWidth(_items[i + 1].RectTransform);
                    cx += (w * 0.5f) + _spacing + (nextW * 0.5f);
                }
            }

            // Общая ширина контента
            float totalWidth;
            if (_items.Count > 0)
            {
                float lastW = GetItemWidth(_items[_items.Count - 1].RectTransform);
                totalWidth = _itemCenterAxis[_itemCenterAxis.Count - 1] + lastW * 0.5f + _computedEdgePadding;
            }
            else
            {
                totalWidth = vpW;
            }

            // Устанавливаем размеры контента
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalWidth);
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(vpH, _content.rect.height));

            // FIX: подавить вертикальный «ползучий» сдвиг от ScrollRect
            var pos = _content.anchoredPosition;
            pos.y = 0f; // держим контент без вертикального смещения
            // Округление для исключения полупикселей
            _content.anchoredPosition = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
        }
        else if (_vertical)
        {
            // ----- VERTICAL LAYOUT -----
            float vpW = _viewport.rect.width;
            float vpH = _viewport.rect.height;
            if (vpW <= 0.1f) vpW = Mathf.Max(vpW, 1f);

            float y = 0f;

            float firstH = GetItemHeight(_items.Count > 0 ? _items[0].RectTransform : null);
            _computedEdgePadding = (_edgePadding >= 0f)
                ? _edgePadding
                : Mathf.Max(0f, (vpH - firstH) * 0.5f);

            y += _computedEdgePadding;

            for (int i = 0; i < _items.Count; i++)
            {
                var rt = _items[i];
                if (!rt) continue;

                float w = GetItemWidth(rt.RectTransform);
                float h = GetItemHeight(rt.RectTransform);

                float xLeft = 0f;
                if (_alignVerticallyToMiddle)
                {
                    float contentW = Mathf.Max(vpW, w);
                    xLeft = (contentW - w) * 0.5f;
                }

                rt.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                rt.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);

                rt.RectTransform.anchoredPosition = new Vector2(Mathf.Round(xLeft), Mathf.Round(-y));

                _itemCenterAxis.Add(y + h * 0.5f);
                y += h + _spacing;
            }

            float totalHeight = (_items.Count > 0)
                ? (y - _spacing + _computedEdgePadding)
                : vpH;

            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(vpW, _content.rect.width));

            // Для вертикали оставляем X как есть, но округлим
            var pos = _content.anchoredPosition;
            _content.anchoredPosition = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
        }
    }

    public void OnBeginDrag(PointerEventData eventData) => _snapping = false;

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_snapOnEndDrag || Count == 0) return;
        int idx = FindClosestToCenter();
        ScrollTo(idx, true);
    }

    private void CollectChildrenIfEmpty(bool force = false)
    {
        if (!force && _items.Count > 0)
            return;
        _items.Clear();
        
        if (_content)
        {
            for (int i = 0; i < _content.childCount; i++)
            {
                var rt = _content.GetChild(i).GetComponent<T>();
                
                if (rt && rt.gameObject.activeInHierarchy) 
                    _items.Add(rt);
            }
        }
    }

    private void MakeChildrenClickable()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            var rt = _items[i];
            var btn = rt.GetComponent<Button>() ?? rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            int index = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                ScrollTo(index, true);
            });
        }
    }

    private float GetItemWidth(RectTransform rt)
    {
        if (rt == null) return 0f;
        if (_usePreferredSize)
        {
            var le = rt.GetComponent<LayoutElement>();
            if (le && le.preferredWidth > 0f) return le.preferredWidth;
        }

        return Mathf.Max(1f, rt.rect.width > 0f ? rt.rect.width : 600f);
    }

    private float GetItemHeight(RectTransform rt)
    {
        if (rt == null) return 0f;
        if (_usePreferredSize)
        {
            var le = rt.GetComponent<LayoutElement>();
            if (le && le.preferredHeight > 0f) return le.preferredHeight;
        }

        return Mathf.Max(1f, rt.rect.height > 0f ? rt.rect.height : 600f);
    }

    private int FindClosestToCenter()
    {
        if (Count == 0) return -1;
        var vp = ViewportCenterInContentSpace();
        float vpAxis = _horizontal ? vp.x : vp.y;

        float best = float.MaxValue;
        int bestIdx = 0;

        for (int i = 0; i < _items.Count; i++)
        {
            Vector3 icWorld = WorldCenter(_items[i].RectTransform);
            Vector2 icLocal = _content.InverseTransformPoint(icWorld);
            float axis = _horizontal ? icLocal.x : icLocal.y;
            float d = Mathf.Abs(axis - vpAxis);
            if (d < best)
            {
                best = d;
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    private static Vector3 WorldCenter(RectTransform rectTransform)
    {
        Vector3[] w = new Vector3[4];
        rectTransform.GetWorldCorners(w);
        return 0.5f * (w[0] + w[2]);
    }

    private Vector2 CalcSnapPosition(int index)
    {
        var itemWorld = WorldCenter(_items[index].RectTransform);
        var vpWorld = WorldCenter(_viewport);

        Vector3 deltaWorld = itemWorld - vpWorld;

        var parent = _content.parent as RectTransform;
        Vector2 deltaParent = parent.InverseTransformVector(deltaWorld);

        var pos = _content.anchoredPosition;
        pos -= deltaParent;

        pos.x = Mathf.Round(pos.x);
        pos.y = Mathf.Round(pos.y);
        return pos;
    }

    private void UpdateScaleAndIndex(bool forceEvent = false)
    {
        if (Count == 0 || !_viewport || !_content) return;

        var vp = ViewportCenterInContentSpace();
        float vpAxis = _horizontal ? vp.x : vp.y;

        float best = float.MaxValue;
        int closest = -1;

        for (int i = 0; i < _items.Count; i++)
        {
            Vector3 icWorld = WorldCenter(_items[i].RectTransform);
            Vector2 icLocal = _content.InverseTransformPoint(icWorld);
            float axis = _horizontal ? icLocal.x : icLocal.y;
            float dist = Mathf.Abs(axis - vpAxis);

            if (_scaleByCenter)
            {
                float t = Mathf.Clamp01(dist / Mathf.Max(1f, _scaleFalloff));
                float scale = _scaleCurve.Evaluate(1f - t);
                _items[i].RectTransform.localScale = new Vector3(scale, scale, _clampZScaleToOne ? 1f : scale);
            }

            if (dist < best)
            {
                best = dist;
                closest = i;
            }
        }

        if (closest != -1 && (closest != CurrentIndex || forceEvent))
        {
            CurrentIndex = closest;
            _onIndexChanged.Invoke(CurrentIndex);
            ItemChanged?.Invoke(_items[CurrentIndex]);
        }
    }

    private Vector2 ViewportCenterInContentSpace()
    {
        var vpWorld = WorldCenter(_viewport);
        var local = _content.InverseTransformPoint(vpWorld);
        return new Vector2(local.x, local.y);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        Relayout();
        UpdateScaleAndIndex(forceEvent: true);
    }
}