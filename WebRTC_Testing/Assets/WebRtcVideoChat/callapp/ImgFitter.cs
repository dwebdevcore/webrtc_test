using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This will resize the video screen for different aspect ratios.
/// </summary>
[ExecuteInEditMode]
public class ImgFitter : MonoBehaviour {
    
    // Update is called once per frame
    void Update ()
    {
        RawImage image = GetComponent<RawImage>();
        RectTransform ltransform = GetComponent<RectTransform>();
        RectTransform ptransform = ltransform.parent.GetComponent<RectTransform>();

        Vector2 parentSize = new Vector2(ptransform.rect.width, ptransform.rect.height);

        Vector2 availableDelta = (ltransform.rotation) * parentSize;
        availableDelta = Abs(availableDelta);

        int width = image.texture.width;
        int height = image.texture.height;
        float ratio = width / (float)height;

        Vector2 res = new Vector2();
        if(availableDelta.x / width < availableDelta.y / height)
        {
            res.x = availableDelta.x;
            res.y = availableDelta.x / ratio;
        }else
        {
            res.x = availableDelta.y * ratio;
            res.y = availableDelta.y;
        }
        ltransform.sizeDelta = res;
        ltransform.anchorMin = new Vector2(0.5f, 0.5f);
        ltransform.anchorMax = new Vector2(0.5f, 0.5f);
        ltransform.pivot = new Vector2(0.5f, 0.5f);

    }

    Vector2 Abs(Vector2 v)
    {
        v.x = Mathf.Abs(v.x);
        v.y = Mathf.Abs(v.y);
        return v;
    }
}
