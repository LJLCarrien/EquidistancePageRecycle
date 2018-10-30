using UnityEngine;
using System.Collections;

public class CellController
{
    private GameObject go;
    private UISprite sprite;
    private UILabel label;
    public CellController(GameObject cellPrefab)
    {
        go = cellPrefab;
        if (go==null)return;
        
        sprite = go.GetComponent<UISprite>();
        label = go.GetComponentInChildren<UILabel>();
        GameObject = go;

    }

    public int Size
    {
        get
        {
            if (sprite != null)
            {
                if (sprite.width > sprite.height)
                    return sprite.width;
                return sprite.height;
            }
            return 0;
        }
    }

    public GameObject GameObject
    {
        get;
        set;
    }

    public void UpdateLbl(string text)
    {
        if (label == null) return;

        label.text = text;
    }
}
