using System;
using UnityEngine;
using UnityEngine.EventSystems;

//30092026 Nutzerwunsch: Hover-Infos fuer Buttons und Eingabefelder (Tooltip neben dem Cursor) und Hover-Vorschau
//  fuer Reiter bzw. Panels. Die Komponente meldet nur Ein- und Austritt des Cursors; Anzeige, Verzoegerung und
//  Animation steuert ExperimentImageGallery (UpdateHoverUi). Enter-Ereignisse erreichen auch Eltern-Objekte, daher
//  genuegt die Komponente am Wurzelobjekt eines Bedienelements (z.B. Schieberegler-Panel).
public class HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string text;      // Erklaerung fuer den Tooltip (leer = kein Tooltip)
    public Action onEnter;   // z.B. Reiter-Vorschau oeffnen
    public Action onExit;

    public static HoverTip current;  // Element unter dem Cursor
    public static float enterTime;   // Zeitpunkt des Eintritts (Tooltip-Verzoegerung)

    public void OnPointerEnter(PointerEventData eventData)
    {
        current = this;
        enterTime = Time.unscaledTime;
        onEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (current == this)
            current = null;
        onExit?.Invoke();
    }

    private void OnDisable()
    {
        if (current == this)
            current = null;
    }
}
