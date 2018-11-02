using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ViewController : MonoBehaviour
{
    private int maxNum = 25;
    public UIScrollView mScrollView;

    private EquidistancePageRecycle mEquidistanceRecycle;


    // Use this for initialization
    void Start()
    {
        mEquidistanceRecycle = new EquidistancePageRecycle(mScrollView, maxNum, 60, 3, LoadCell, UpdateCell);
        cellCtrlerDic = new Dictionary<GameObject, CellController>(mEquidistanceRecycle.PanelMaxShowCount);
        mEquidistanceRecycle.InitCell();
    }

    private void UpdateCell(GameObject go, int cellVirtualIndex, int dataindex)
    {
        CellController ctrler;
        if (!cellCtrlerDic.TryGetValue(go, out ctrler)) return;
        //if (dataindex >= maxNum)
        //{
        //    ctrler.UpdateLbl("");

        //}
        //else
        //{
        var text = cellVirtualIndex.ToString().WrapColor("000000FF") + "\n" + dataindex.ToString().WrapColor("B54646FF");
        ctrler.UpdateLbl(text);

        //}
        //ctrler.UpdateColor((dataindex / mEquidistanceRecycle.pageDataTotalCount)%2 == 0 ? Color.black : Color.red);
    }



    private Dictionary<GameObject, CellController> cellCtrlerDic;
    private GameObject LoadCell()
    {
        var mCell = (GameObject)Instantiate(Resources.Load("Cell"));
        var ctrler = new CellController(mCell);
        cellCtrlerDic.Add(mCell, ctrler);
        return ctrler.GameObject;
    }

    public int page;
    public int row;
    public int line;
    [ContextMenu("GetCellVirtualIndex1")]
    private void GetCellIndex1()
    {
        var cellIndex = mEquidistanceRecycle.GetCellVirtualIndex(row, line);
        Debug.LogError(cellIndex);
    }

    [ContextMenu("GetCellVirtualIndex2")]
    private void GetCellIndex2()
    {
        var cellIndex = mEquidistanceRecycle.GetCellVirtualIndex(page, row, line);
        Debug.LogError(cellIndex);
    }

    public int realLineIndex;
    public int pageTimes;
    [ContextMenu("GetMoveLineIndex")]
    private void GetCellIndex3()
    {
        var cellIndex = mEquidistanceRecycle.GetPageRealCellLineIndexByPageTimes(realLineIndex, pageTimes);
        Debug.LogError(cellIndex);
    }

}
