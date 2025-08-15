using UnityEngine;

public interface IPointerInput
{
    bool IsDragging { get; }
    Vector2 StartScreenPos { get; }
    Vector2 CurrentScreenPos { get; }
    float DragDuration { get; }

    // Events-like callbacks (subscribe in Start)
    event System.Action<Vector2> OnDragStart;            // screen pos
    event System.Action<Vector2> OnDrag;                 // screen pos
    event System.Action<Vector2, Vector2, float> OnDragEnd; // start, end, duration (seconds)
}
