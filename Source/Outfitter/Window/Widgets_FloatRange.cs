using Outfitter.Textures;
using UnityEngine;
using Verse;

namespace Outfitter.Window;

public static class Widgets_FloatRange
{
    private static Handle _draggingHandle;

    private static int _draggingId;

    static Widgets_FloatRange()
    {
        _draggingHandle = Handle.None;
        _draggingId = 0;
    }

    public static void FloatRange(
        Rect canvas,
        int id,
        ref FloatRange range,
        FloatRange sliderRange,
        ToStringStyle valueStyle = ToStringStyle.FloatTwo,
        string labelKey = null)
    {
        // margin
        canvas.xMin += 8f;
        canvas.xMax -= 8f;

        // label
        var mainColor = GUI.color;
        GUI.color = new Color(0.4f, 0.4f, 0.4f);
        var text = $"{range.min.ToStringByStyle(valueStyle)} - {range.max.ToStringByStyle(valueStyle)}";
        if (labelKey != null)
        {
            text = labelKey.Translate(text);
        }

        Text.Font = GameFont.Tiny;
        var labelRect = new Rect(canvas.x, canvas.y, canvas.width, 19f);
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(labelRect, text);
        Text.Anchor = TextAnchor.UpperLeft;

        // background line
        var sliderRect = new Rect(canvas.x, labelRect.yMax, canvas.width, 2f);
        GUI.DrawTexture(sliderRect, BaseContent.WhiteTex);
        GUI.color = mainColor;

        // slider handle positions
        var pxPerUnit = sliderRect.width / sliderRange.Span;
        var minHandlePos = sliderRect.xMin + ((range.min - sliderRange.min) * pxPerUnit);
        var maxHandlePos = sliderRect.xMin + ((range.max - sliderRange.min) * pxPerUnit);

        // draw handles
        var minHandleRect = new Rect(minHandlePos - 16f, sliderRect.center.y - 8f, 16f, 16f);
        GUI.DrawTexture(minHandleRect, OutfitterTextures.FloatRangeSliderTex);
        var maxHandleRect = new Rect(maxHandlePos + 16f, sliderRect.center.y - 8f, -16f, 16f);
        GUI.DrawTexture(maxHandleRect, OutfitterTextures.FloatRangeSliderTex);

        // interactions
        var interactionRect = canvas;
        interactionRect.xMin -= 8f;
        interactionRect.xMax += 8f;
        var dragging = false;
        if (Mouse.IsOver(interactionRect) || _draggingId == id)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                _draggingId = id;
                var x = Event.current.mousePosition.x;
                if (x < minHandleRect.xMax)
                {
                    _draggingHandle = Handle.Min;
                }
                else if (x > maxHandleRect.xMin)
                {
                    _draggingHandle = Handle.Max;
                }
                else
                {
                    var distToMin = Mathf.Abs(x - minHandleRect.xMax);
                    var distToMax = Mathf.Abs(x - (maxHandleRect.x - 16f));
                    _draggingHandle = distToMin >= distToMax ? Handle.Max : Handle.Min;
                }

                dragging = true;
                Event.current.Use();
            }

            if (dragging || _draggingHandle != Handle.None && Event.current.type == EventType.MouseDrag)
            {
                // NOTE: this deviates from vanilla, vanilla seemed to assume that max == span?
                var curPosValue = ((Event.current.mousePosition.x - canvas.x) / canvas.width * sliderRange.Span)
                                  + sliderRange.min;
                curPosValue = Mathf.Clamp(curPosValue, sliderRange.min, sliderRange.max);
                switch (_draggingHandle)
                {
                    case Handle.Min:
                    {
                        range.min = curPosValue;
                        if (range.max < range.min)
                        {
                            range.max = range.min;
                        }

                        break;
                    }
                    case Handle.Max:
                    {
                        range.max = curPosValue;
                        if (range.min > range.max)
                        {
                            range.min = range.max;
                        }

                        break;
                    }
                }

                Event.current.Use();
            }
        }

        if (_draggingHandle == Handle.None || Event.current.type != EventType.MouseUp)
        {
            return;
        }

        _draggingId = 0;
        _draggingHandle = Handle.None;
        Event.current.Use();
    }

    private enum Handle
    {
        None,

        Min,

        Max
    }
}