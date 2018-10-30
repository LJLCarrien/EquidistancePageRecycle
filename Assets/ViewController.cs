using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ViewController : MonoBehaviour
{
    private int maxNum = 30;
    public UIScrollView mScrollView;

    private EquidistancePageRecycle mEquidistanceRecycle;


    // Use this for initialization
    void Start()
    {
        mEquidistanceRecycle = new EquidistancePageRecycle(mScrollView, maxNum, 60, 3, LoadCell, UpdateCell);
        cellCtrlerDic = new Dictionary<GameObject, CellController>(mEquidistanceRecycle.PanelMaxShowCount);
        mEquidistanceRecycle.UpdateCell();
    }

    private void UpdateCell(GameObject go, int dataindex)
    {
        CellController ctrler;
        if (!cellCtrlerDic.TryGetValue(go, out ctrler)) return;
        if (dataindex >= maxNum)
        {
            ctrler.UpdateLbl("");

        }
        else
        {
            ctrler.UpdateLbl(dataindex.ToString());

        }

    }


    private Dictionary<GameObject, CellController> cellCtrlerDic;
    private GameObject LoadCell()
    {
        var mCell = (GameObject)Instantiate(Resources.Load("Cell"));
        var ctrler = new CellController(mCell);
        cellCtrlerDic.Add(mCell, ctrler);
        return ctrler.GameObject;
    }
}
