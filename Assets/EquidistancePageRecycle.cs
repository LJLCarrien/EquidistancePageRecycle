using System;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class EquidistancePageRecycle
{
    private int cellSize;
    private float halfCellSize;

    /// <summary>
    /// 每页行列数(水平为列，垂直为行)
    /// </summary>
    private int pageColumnLimit;
    /// <summary>
    /// 每页数据总个数
    /// </summary>
    private int pageDataTotalCount;
    /// <summary>
    /// 数据总数
    /// </summary>
    private int DataCount;
    /// <summary>
    /// 数据可填充的最大列数
    /// </summary>
    private int dataColumnLImit;

    private UIScrollView mScrollView;
    private UIPanel mPanel;

    private UIScrollView.Movement mMovement;
    private GameObject cellParent;
    private GameObject cellPool;
    private Bounds mPanelBounds;

    public EquidistancePageRecycle(UIScrollView sv, int dataCount, int size, int pageColum,
        OnLoadItem loadItem, OnUpdateItem updateItem, int extraShownum = 1)
    {
        mScrollView = sv;
        DataCount = dataCount;
        cellSize = size;
        halfCellSize = (float)cellSize / 2;

        pageColumnLimit = pageColum;

        onLoadItem = loadItem;
        onUpdateItem = updateItem;

        extraShowNum = extraShownum;
        InitNeed();
    }

    private void InitNeed()
    {
        if (IsSvNull()) return;
        mPanel = mScrollView.panel;
        Vector3 center = mScrollView.transform.position;
        Vector3 boundSize = mPanel.GetViewSize();
        mPanelBounds = new Bounds(center, boundSize);
        mMovement = mScrollView.movement;
        mScrollView.DisableSpring();

        RegisterEvent();

        cellParent = NGUITools.AddChild(mScrollView.gameObject);
        cellParent.name = "EquidistancePageRecycle";
        InitPanelColRow();

       
}
    


    #region  辅助

    private bool IsSvNull()
    {
        return mScrollView == null;
    }

    private bool IsPanelNull()
    {
        return mPanel == null;
    }

    //panel左边界
    private float mPanelLeftPos
    {
        get
        {
            if (IsPanelNull()) return 0;
            return -mPanel.baseClipRegion.z / 2;
        }
    }
    //panel右边界
    private float mPanelRightPos
    {
        get
        {
            if (IsPanelNull()) return 0;
            return mPanel.baseClipRegion.z / 2;
        }
    }
    //panel上边界
    private float mPanelTopPos
    {
        get
        {
            if (IsPanelNull()) return 0;
            return mPanel.baseClipRegion.w / 2;
        }
    }
    //panel下边界
    private float mPanelDownPos
    {
        get
        {
            if (IsPanelNull()) return 0;
            return -mPanel.baseClipRegion.w / 2;
        }
    }

    //当前行/列
    private int mCurCol;

    private bool ReFreshData;

    //最左
    private float mPanelLeft;
    private float PanelCellLeft
    {
        get
        {
            if (ReFreshData || mPanelLeft == 0)
                mPanelLeft = -mPanelBounds.extents.x + halfCellSize;
            return mPanelLeft;
        }
    }
    //最上
    private float mPanelTop;
    private float PanelCellTop
    {
        get
        {
            if (ReFreshData || mPanelTop == 0)
                mPanelTop = mPanelBounds.extents.y - halfCellSize;
            return mPanelTop;
        }
    }
    //最右
    private float mPanelRight;

    private float PanelCellRight
    {
        get
        {
            if (ReFreshData || mPanelRight == 0)
                mPanelRight = mPanelBounds.extents.x - halfCellSize;
            return mPanelRight;
        }
    }
    //最下
    private float mPanelBottom;

    private float PanelCellBottom
    {
        get
        {
            if (ReFreshData || mPanelBottom == 0)
                mPanelBottom = -mPanelBounds.extents.y + halfCellSize;
            return mPanelBottom;
        }
    }
    //整体cell是否在panel里
    private bool IsAllInHoriPanel(float x)
    {
        var cellXLeft = x - halfCellSize;
        var cellXRight = x + halfCellSize;
        var horizontialIn = cellXLeft >= mPanelLeftPos && cellXRight <= mPanelRightPos;
        return horizontialIn;
    }
    private bool IsAllInVerPanel(float y)
    {
        var cellYTop = y + halfCellSize;
        var cellYDown = y - halfCellSize;
        var verticalIn = cellYTop <= mPanelTopPos && cellYDown >= mPanelDownPos;
        return verticalIn;
    }
    //整体cell是否不在panel里
    private bool IsAllOutHoriPanel(float x)
    {
        var cellXLeft = x - halfCellSize;
        var cellXRight = x + halfCellSize;
        var horizontialOut = cellXRight <= mPanelLeftPos || cellXLeft >= mPanelRightPos;
        return horizontialOut;
    }
    private bool IsAllOutVerPanel(float y)
    {
        var cellYTop = y + halfCellSize;
        var cellYDown = y - halfCellSize;
        var verticalOut = cellYDown >= mPanelTopPos || cellYTop <= mPanelDownPos;
        return verticalOut;
    }
    #endregion

    #region 委托

    public delegate GameObject OnLoadItem();
    public delegate void OnUpdateItem(GameObject go, int dataIndex);

    private OnLoadItem onLoadItem;
    private OnUpdateItem onUpdateItem;

    #endregion

    /// <summary>
    //界面容许完全显示的最大列
    /// </summary>
    private int mPanelColumnLimit;
    /// <summary>
    //界面容许完全显示的最大行
    /// </summary>
    private int mPanelRowLimit;

    /// <summary>
    //界面容许显示的最大数量
    /// </summary>
    public int PanelMaxShowCount { get; private set; }

    /// <summary>
    /// 界面显示时，需要额外增加的行数/列数
    /// </summary>
    private int extraShowNum;

    private List<GameObject> cellGoList;
    private Dictionary<GameObject, CellDataInfo> cellDataDic;

    /// <summary>
    /// 初始化显示行列数
    /// </summary>
    private void InitPanelColRow()
    {
        if (IsPanelNull()) return;

        float cellX, cellY;
        int curHang = 0, curLie = 0;
        if (pageColumnLimit == 0) return;

        if (mMovement == UIScrollView.Movement.Horizontal)
        {
            //Debug.LogError("-----------X-----------");
            cellX = PanelCellLeft;
            while (IsAllInHoriPanel(cellX))
            {
                //cell的左边
                if (cellX - halfCellSize <= mPanelRightPos)
                {
                    curLie++;
                    mPanelColumnLimit = curLie;
                }
                cellX = PanelCellLeft + curLie * cellSize;
                //Debug.LogError(cellX);
            }
            //Debug.LogError("-----------Y-----------");

            cellY = PanelCellTop;
            while (IsAllInVerPanel(cellY))
            {
                //cell的上边
                if (cellY - halfCellSize >= mPanelDownPos)
                {
                    curHang++;
                    mPanelRowLimit = curHang;
                }
                cellY = PanelCellTop - curHang * cellSize;
                //Debug.LogError(cellY);
            }
        }
        if (mMovement == UIScrollView.Movement.Vertical)
        {
            //todo:
        }
        PanelMaxShowCount = mPanelColumnLimit * mPanelRowLimit;
        pageDataTotalCount = mPanelRowLimit * pageColumnLimit;

        cellGoList = new List<GameObject>(PanelMaxShowCount);
        cellDataDic = new Dictionary<GameObject, CellDataInfo>(PanelMaxShowCount);

        dataColumnLImit = DataCount / mPanelRowLimit + DataCount % mPanelRowLimit;
        //Debug.LogError("-----------Result-----------");
        //Debug.LogError(mPanelColumnLimit);
        //Debug.LogError(mPanelRowLimit);
    }

    public void UpdateCell()
    {
        float cellX, cellY;
        GameObject go;
        Transform tf;
        int rowLimit = mMovement == UIScrollView.Movement.Vertical ? mPanelRowLimit + extraShowNum : mPanelRowLimit;
        int lineLimit = mMovement == UIScrollView.Movement.Horizontal ? mPanelColumnLimit + extraShowNum : mPanelColumnLimit;
        int cellIndex, dataIndex = 0;
        for (int curHang = 0; curHang < rowLimit; curHang++)
        {
            for (int curLine = 0; curLine < lineLimit; curLine++)
            {

                cellX = PanelCellLeft + curLine * cellSize;
                cellY = PanelCellTop - curHang * cellSize;

                go = onLoadItem();
                cellGoList.Add(go);

                tf = go.transform;
                tf.SetParent(cellParent.transform);
                tf.localScale = Vector3.one;
                tf.localPosition = new Vector3(cellX, cellY, 0);
                //Debug.LogError(string.Format("{0},{1}", cellX, cellY));
                cellIndex = curHang * lineLimit + curLine;

                if (curLine < mPanelColumnLimit)
                {
                    //dataIndex = curHang * pageColumnLimit + (curLine / pageColumnLimit) * pageDataTotalCount + curLine % pageColumnLimit;
                    dataIndex = GetDataIndex(curPageIndex, curHang, curLine);
                }
                else
                {
                    dataIndex = -1;
                }
                //CellDataInfo cellInfo = new CellDataInfo(cellIndex, dataIndex);
                //cellDataDic.Add(go, cellInfo);
                onUpdateItem(go, dataIndex);
            }
        }
    }

    private int GetDataIndex(int page, int row, int line)
    {
        var dIndex = page * mPanelRowLimit * pageColumnLimit + row * pageColumnLimit + line;
        return dIndex;
    }
    /// <summary>
    /// 界面显示的cell首列下标
    /// </summary>
    private int curFirstColIndex = 0;
    private int curLastColIndex = 0;

    /// <summary>
    /// 数据cell 首列下标
    /// </summary>
    private int curDataFirstColIndex = 0;
    private int curDataLastColIndex = 0;
    private enum DragmoveDir
    {
        None,
        Left,
        Right,
        Top,
        Down
    }

    private class CellDataInfo
    {
        public int cellIndex;
        public int dataIndex;

        public CellDataInfo(int cindex, int dIndex)
        {
            cellIndex = cindex;
            dataIndex = dIndex;
        }
    }


    private DragmoveDir GetDragmoveDir()
    {
        var dragDir = mPanel.clipOffset.x - panelStartOffset > 0 ? DragmoveDir.Left :
            mPanel.clipOffset.x - panelStartOffset < 0 ? DragmoveDir.Right : DragmoveDir.None;
        return dragDir;
    }
    /// <summary>
    /// 检测并移动
    /// </summary>
    private void CheckCellMove()
    {
        int rowLimit = mMovement == UIScrollView.Movement.Vertical ? mPanelRowLimit + extraShowNum : mPanelRowLimit;
        int lineLimit = mMovement == UIScrollView.Movement.Horizontal ? mPanelColumnLimit + extraShowNum : mPanelColumnLimit;
        if (cellGoList.Count <= lineLimit) return;
        int cellIndex = 0, moveColIndex, dataIndex;
        float cellX = 0;
        GameObject cellGo;
        if (mMovement == UIScrollView.Movement.Horizontal)
        {

            #region cell移动计算

            curFirstColIndex = curFirstColIndex % lineLimit;
            curLastColIndex = (curFirstColIndex + lineLimit - 1) % lineLimit;

            var dragDir = GetDragmoveDir();
            var moveDir = dragDir == DragmoveDir.Left ? 1 : dragDir == DragmoveDir.Right ? -1 : 0;

            if (dragDir == DragmoveDir.None) return;
            //需要移动的列index
            moveColIndex = dragDir == DragmoveDir.Left ? curFirstColIndex : dragDir == DragmoveDir.Right ? curLastColIndex : -1;

            //左拖动 offset变大 cell右移 x增加
            cellX = cellGoList[moveColIndex].transform.localPosition.x;
            cellX = cellX - mPanel.clipOffset.x;

            #endregion

            #region cell数据index计算

            curDataFirstColIndex = curDataFirstColIndex % dataColumnLImit;
            curDataLastColIndex = (curDataFirstColIndex + dataColumnLImit - 1) % dataColumnLImit;
            //Debug.LogError(string.Format("{0},{1}", curDataFirstColIndex, curDataLastColIndex));


            #endregion
            if (IsAllOutHoriPanel(cellX))
            {
                for (int hangIndex = 0; hangIndex < rowLimit; hangIndex++)
                {
                    cellIndex = hangIndex * lineLimit + moveColIndex;
                    cellGo = cellGoList[cellIndex];
                    cellX = cellGo.transform.localPosition.x + moveDir * lineLimit * cellSize;

                    //dataIndex = cellDataDic[cellGo].dataIndex + (moveDir * rowLimit * mPanelColumnLimit) + 1;
                    //Debug.LogError(string.Format("{0}，{1}", cellDataDic[cellGo].dataIndex, dataIndex));
                    //if (dragDir == DragmoveDir.Left && curDataFirstColIndex + moveDir * mPanelColumnLimit >= dataColumnLImit - 1) return;//滑动限制todo
                    //if (dragDir == DragmoveDir.Right && curDataFirstColIndex - 1 < 0) return;//滑动限制todo
                    //onUpdateItem(cellGo, dataIndex);
                    //cellDataDic[cellGo].dataIndex = dataIndex;

                    cellGo.transform.SetLocalX(cellX);

                }

                if (dragDir == DragmoveDir.Left)
                {
                    curDataFirstColIndex++;
                    curFirstColIndex++;
                }
                else if (dragDir == DragmoveDir.Right)
                {
                    curFirstColIndex = curLastColIndex;
                    curDataFirstColIndex = curDataLastColIndex;
                    //curDataFirstColIndex = curDataLastColIndex;
                }
                //Debug.LogError(string.Format("{0},{1}", curDataFirstColIndex, curDataLastColIndex));
            }
        }
        if (mMovement == UIScrollView.Movement.Vertical)
        {
            //todo
        }

    }

    #region  事件

    private float panelStartOffset;
    private void RegisterEvent()
    {
        if (mPanel != null) mPanel.onClipMove += OnClipMove;
        if (mScrollView != null)
        {
            mScrollView.onStoppedMoving += OnStoppedMoving;
            mScrollView.onDragStarted += OnDragStarted;
            mScrollView.onDragFinished += OnDragFinished;
            //    mScrollView.onScrollWheel += OnScrollWheel;
            //}
        }
    }
    
    private void OnDragFinished()
    {
        //panelStartOffset = mMovement == UIScrollView.Movement.Horizontal ? mPanel.clipOffset.x : mPanel.clipOffset.y;

    }
    
    private void OnDragStarted()
    {

        panelStartOffset = mMovement == UIScrollView.Movement.Horizontal ? mPanel.clipOffset.x : mPanel.clipOffset.y;
    }

    private int curPageIndex = 0;
    private float curMoveTo = 0;
    private void OnStoppedMoving()
    {
        var isChange = 0;
        //Debug.LogError(curPageIndex * pageColumnLimit + (pageColumnLimit - 1));
        var dragDir = GetDragmoveDir();
        var moveDir = dragDir == DragmoveDir.Left ? 1 : dragDir == DragmoveDir.Right ? -1 : 0;

        if (dragDir == DragmoveDir.Left/* && curPageIndex * pageColumnLimit + (pageColumnLimit - 1) < dataColumnLImit - 1*/)
        {
            curPageIndex++;
            isChange = 1;
        }
        if (dragDir == DragmoveDir.Right /*&& curPageIndex > 1*/)
        {
            curPageIndex--;
            isChange = 1;
        }
        float finalmoveTo;
        finalmoveTo = curMoveTo - isChange * moveDir * pageColumnLimit * cellSize;
        curMoveTo = finalmoveTo;
        SpringPanel.Begin(mPanel.gameObject, new Vector3(finalmoveTo, 0, 0), 8f);

        CheckCellMove();


    }

    private void OnClipMove(UIPanel panel)
    {
        CheckCellMove();
    }

    #endregion


}
public static class Extensions
{
    public static void SetLocalX(this Transform cell, float x)
    {
        float y = cell.transform.localPosition.y;
        cell.transform.localPosition = new Vector3(x, y, 0);
    }
}
