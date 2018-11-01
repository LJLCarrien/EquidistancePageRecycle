using System;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class EquidistancePageRecycle
{
    private int cellSize;
    private float halfCellSize;

    #region 分页数据

    /// <summary>
    /// 每页数据行列数(水平为列，垂直为行)
    /// </summary>
    private int pageColumnLimit;

    /// <summary>
    /// 每页数据总个数
    /// </summary>
    public int pageDataTotalCount { get; private set; }
    /// <summary>
    /// 总页数
    /// </summary>
    public int pageTotalNum
    {
        get
        {
            var count = 0;
            if (mMovement == UIScrollView.Movement.Horizontal)
            {
                var pageIndex = DataCount / pageDataTotalCount;
                count = DataCount % pageDataTotalCount != 0 ? pageIndex + 1 : pageIndex;
            }
            else if (mMovement == UIScrollView.Movement.Vertical)
            {
                //todo:
            }
            return count;
        }
    }
    /// <summary>
    /// 翻页的总列数
    /// </summary>
    public int pageTotalColumn
    {
        get
        {
            var count = 0;
            if (mMovement == UIScrollView.Movement.Horizontal)
            {
                count = pageTotalNum * pageDataTotalCount / mPanelInitRowLimit;
            }
            else if (mMovement == UIScrollView.Movement.Vertical)
            {
                //todo:
            }
            return count;
        }
    }

    #endregion

    #region 界面显示

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
    /// 界面显示时，需要额外增加行数/列数（用于移动）
    /// </summary>
    private int extraShowNum;

    /// <summary>
    /// 界面最后生成的总列数
    /// </summary>
    private int mPanelInitColumnLimit
    {
        get
        {
            var count = mPanelColumnLimit;
            if (mMovement == UIScrollView.Movement.Horizontal)
            {
                count = mPanelColumnLimit + extraShowNum;
            }
            else if (mMovement == UIScrollView.Movement.Vertical)
            {
                //todo:
            }
            return count;
        }
    }

    /// <summary>
    ///  界面最后生成的总行数
    /// </summary>
    private int mPanelInitRowLimit
    {
        get
        {
            var count = mPanelColumnLimit;
            if (mMovement == UIScrollView.Movement.Horizontal)
            {
                count = mPanelRowLimit;
            }
            else if (mMovement == UIScrollView.Movement.Vertical)
            {
                //todo:
            }
            return count;
        }
    }

    #endregion

    #region 数据

    /// <summary>
    /// 数据总数
    /// </summary>
    private int DataCount;

    #endregion


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
        var sizey = mPanel.baseClipRegion.w;
        mPanel.baseClipRegion = new Vector4(0, 0, cellSize * pageColumnLimit, sizey);
        Vector3 center = mScrollView.transform.position;
        Vector3 boundSize = mPanel.GetViewSize();
        mPanelBounds = new Bounds(center, boundSize);
        mMovement = mScrollView.movement;
        mScrollView.DisableSpring();

        RegisterEvent();

        cellParent = NGUITools.AddChild(mScrollView.gameObject);
        cellParent.name = "EquidistancePageRecycle";

        cellPool = NGUITools.AddChild(mScrollView.gameObject);
        cellPool.name = "EquidistancePagePool";
        cellPool.gameObject.SetActive(false);

        InitPanelColRow();


    }
    void OnDestory()
    {
        RemoveEvent();
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
    /// 生成的所有cell
    /// </summary>
    private List<GameObject> cellGoList;


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
        PanelMaxShowCount = mPanelRowLimit * mPanelColumnLimit;
        pageDataTotalCount = mPanelRowLimit * pageColumnLimit;

        cellGoList = new List<GameObject>(PanelMaxShowCount);


        //Debug.LogError("-----------Result-----------");
        //Debug.LogError(mPanelColumnLimit);
        //Debug.LogError(mPanelRowLimit);
        DebugLogAllInfo();
    }

    private void DebugLogAllInfo()
    {
        var info = string.Format("界面完全显示行：{0}\n,界面完全显示列：{1}\n,界面完全显示总数：{2}\n\n", mPanelRowLimit, mPanelColumnLimit, PanelMaxShowCount);
        var info1 = string.Format("实际生成行总数：{0}\n,实际生成列总数：{1}\n,实际生成总数：{2}\n\n", mPanelInitRowLimit, mPanelInitColumnLimit, mPanelInitRowLimit * mPanelInitColumnLimit);
        var info2 = string.Format("数据总数：{0}\n,数据每页显示列数：{1}\n,翻页总页数：{2}\n,翻页总列数：{3}\n", DataCount, pageColumnLimit, pageTotalNum, pageTotalColumn);
        Debug.LogError(info + info1 + info2);
    }
    public void InitCell()
    {
        float cellX, cellY;
        GameObject go;
        Transform tf;
        int rowLimit = mMovement == UIScrollView.Movement.Vertical ? mPanelRowLimit + extraShowNum : mPanelRowLimit;
        int lineLimit = mMovement == UIScrollView.Movement.Horizontal ? mPanelColumnLimit + extraShowNum : mPanelColumnLimit;
        int cellIndex, dataIndex = 0;

        curPageIndex = (int)mPanel.clipOffset.x / pageColumnLimit * cellSize;
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

                //todo:
                onUpdateItem(go, curLine);

            }
        }
    }
    #region 帮助
    /// <summary>
    /// 输入翻页的行列下标，获取cellIndex
    ///  0,1,2,3,4,5,...pageTotalColumn-1
    ///  1*pageTotalColumn....
    ///  2*pageTotalColumn....
    ///  (row-1)*pageTotalColumn....
    /// </summary>
    /// <param name="rowIndex">【0,mPanelInitRowLimit-1】</param>
    /// <param name="cellPageLineIndex">【0，pageTotalColumn-1】</param>
    /// <returns></returns>
    public int GetCellIndex(int rowIndex, int cellPageLineIndex)
    {
        //括号：index取余保证不出界
        var dIndex = (rowIndex % mPanelInitColumnLimit) * pageTotalColumn + (cellPageLineIndex % pageTotalColumn);
        return dIndex;
    }



    /// <summary>
    /// 数据当前页，当前页内的行列数
    /// </summary>
    /// <param name="pageIndex">【0,pageDataTotalCount-1】</param>
    /// <param name="rowIndex">【0，mPanelInitRowLimit-1】</param>
    /// <param name="cellEachPagelLineIndex">【0，pageColumnLimit-1】</param>
    /// <returns></returns>
    public int GetCellIndex(int pageIndex, int rowIndex, int cellEachPagelLineIndex)
    {
        //括号：index取余保证不出界
        if (pageIndex >= pageDataTotalCount)
        {
            Debug.LogError("【页数】下标超过可翻最大页数，取余处理");
        }
        if (rowIndex >= mPanelInitRowLimit)
        {
            Debug.LogError("【行数】下标超过界面生成的最大行数，取余处理");
        }
        if (cellEachPagelLineIndex >= pageColumnLimit)
        {
            Debug.LogError("【列数】下标超过当前页最大列数，取余处理");
        }
        var dIndex = (pageIndex % pageTotalNum) * pageColumnLimit + (rowIndex % mPanelInitRowLimit) * pageTotalColumn + (cellEachPagelLineIndex % pageColumnLimit);
        return dIndex;
    }
    #endregion
    /// <summary>
    /// 界面显示的cell首列下标
    /// </summary>
    private int curFirstColIndex = 0;
    private int curLastColIndex = 0;


    private enum DragmoveDir
    {
        None,
        Left,
        Right,
        Top,
        Down
    }


    private int willShowPage;
    /// <summary>
    /// 检测并移动
    /// </summary>
    private void CheckMove()
    {
        int rowLimit = mMovement == UIScrollView.Movement.Vertical ? mPanelRowLimit + extraShowNum : mPanelRowLimit;
        int lineLimit = mMovement == UIScrollView.Movement.Horizontal ? mPanelColumnLimit + extraShowNum : mPanelColumnLimit;
        if (cellGoList.Count <= lineLimit) return;
        int cellIndex = 0, moveColIndex, dataIndex, intMoveDir;

        GameObject cellGo;
     
        DragmoveDir moveDir;
        if (mMovement == UIScrollView.Movement.Horizontal)
        {
            #region cell移动计算

            curFirstColIndex = curFirstColIndex % lineLimit;
            curLastColIndex = (curFirstColIndex + lineLimit - 1) % lineLimit;
            //Debug.LogError(string.Format("{0},{1}", curFirstColIndex, curLastColIndex));
            bool moveLeft, moveRight;
            if (mScrollView.isDragging)
            {
                //sv移动方向等于手指拖动方向
                moveLeft = mScrollView.currentMomentum.x < 0;
                moveRight = mScrollView.currentMomentum.x > 0;
                //sv移动方向
                intMoveDir = moveLeft ? 1 : moveRight ? -1 : 0;
                //当前拖动方向
                moveDir = moveLeft ? DragmoveDir.Left : moveRight ? DragmoveDir.Right : DragmoveDir.None;
            }
            else
            {
                //移动方向根据spring要之后要移动到的位置与当前位置计算
                intMoveDir = intFinishedSvMoveDir;
                moveDir = mFinisedSvMoveDir;
            }

            if (moveDir == DragmoveDir.None) return;

            //需要移动的列index
            moveColIndex = moveDir == DragmoveDir.Left ? curFirstColIndex : moveDir == DragmoveDir.Right ? curLastColIndex : -1;
            //Debug.LogError(moveDir);

            //左拖动 panel offset渐大，左侧cell往右移 x增加
            var cellX = cellGoList[moveColIndex].transform.localPosition.x;
            cellX = cellX - mPanel.clipOffset.x;

            #endregion



            var offsetDistance = Mathf.Abs((int)(mPanel.clipOffset.x - panelStartOffset));
            var goToNextPageDistance = pageColumnLimit * cellSize;
            var pageNum = (offsetDistance / goToNextPageDistance) + 1;
            if (mScrollView.isDragging)
            {
                willShowPage = curPageIndex + intMoveDir * pageNum;
                //Debug.LogError(willShowPage);
            }
            if (IsAllOutHoriPanel(cellX))
            {
                for (int hangIndex = 0; hangIndex < rowLimit; hangIndex++)
                {
                    cellIndex = hangIndex * lineLimit + moveColIndex;
                    cellGo = cellGoList[cellIndex];
                    cellX = cellGo.transform.localPosition.x + intMoveDir * lineLimit * cellSize;

                    //dataIndex = GetDataIndex(willShowPage, hangIndex, );
                    //onUpdateItem(cellGo, dataIndex);


                    cellGo.transform.SetLocalX(cellX);

                }

                if (moveDir == DragmoveDir.Left)
                {
                    curFirstColIndex++;
                }
                else if (moveDir == DragmoveDir.Right)
                {
                    curFirstColIndex = curLastColIndex;
                }
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
            mScrollView.onMomentumMove += onMomentumMove;
            mScrollView.onDragFinished += OnDragFinished;
            //    mScrollView.onScrollWheel += OnScrollWheel;
            //}
        }
    }
    private void RemoveEvent()
    {
        if (mPanel != null) mPanel.onClipMove -= OnClipMove;
        if (mScrollView != null)
        {
            mScrollView.onStoppedMoving -= OnStoppedMoving;
            mScrollView.onDragStarted -= OnDragStarted;
            mScrollView.onMomentumMove -= onMomentumMove;
            mScrollView.onDragFinished -= OnDragFinished;
            //    mScrollView.onScrollWheel -= OnScrollWheel;
            //}
        }
    }
    private void onMomentumMove()
    {
        //DebugIsDraging();
    }

    private void DebugIsDraging()
    {
        //Debug.LogError(mScrollView.isDragging);
    }

    /// <summary>
    /// 拖动结束时，不管往哪边拖动，最后sv真正移动的方向
    /// 存在两头不允许拖动时，拖动方向和移动方向相反
    /// </summary>
    private DragmoveDir mFinisedSvMoveDir = DragmoveDir.None;
    private int intFinishedSvMoveDir = 0;

    /// <summary>
    ///拖动结束时，对比拖动开始，往哪边拖
    /// </summary>
    private DragmoveDir mFinisedSvDragDir = DragmoveDir.None;
    private int intFinishedSvDragDir = 0;

    private void OnDragFinished()
    {
        //拖动方向
        mFinisedSvDragDir = mPanel.clipOffset.x - panelStartOffset > 0 ? DragmoveDir.Left : mPanel.clipOffset.x - panelStartOffset < 0 ? DragmoveDir.Right : DragmoveDir.None;
        intFinishedSvDragDir = mFinisedSvDragDir == DragmoveDir.Left ? 1 : mFinisedSvDragDir == DragmoveDir.Right ? -1 : 0;
        //Debug.LogError("Drag:" + mFinisedSvDragDir);

        var pageNum = 0;
        var goToNextPageDistance = pageColumnLimit * cellSize;
        var offsetDistance = Mathf.Abs((int)(mPanel.clipOffset.x - panelStartOffset));
        if (mMovement == UIScrollView.Movement.Horizontal)
        {
            pageNum = (offsetDistance / goToNextPageDistance) + 1;
            pageNum = offsetDistance >= goToNextPageDistance + goToNextPageDistance / 2 ? pageNum - 1 : pageNum;
        }
        var isChange = 0;
        if (mFinisedSvDragDir == DragmoveDir.Left)
        {
            curPageIndex += pageNum;
            isChange = 1;
        }
        if (mFinisedSvDragDir == DragmoveDir.Right && curPageIndex > 0)
        {
            curPageIndex -= pageNum;
            isChange = 1;
        }

        var finalmoveTo = curMoveTo - pageNum * isChange * intFinishedSvDragDir * pageColumnLimit * cellSize;
        SpringPanel.Begin(mPanel.gameObject, new Vector3(finalmoveTo, 0, 0), 8f);
        curMoveTo = finalmoveTo;

        //移动方向
        mFinisedSvMoveDir = mPanel.transform.localPosition.x - finalmoveTo > 0 ? DragmoveDir.Left : mPanel.transform.localPosition.x - finalmoveTo < 0 ? DragmoveDir.Right : DragmoveDir.None;
        intFinishedSvMoveDir = mFinisedSvMoveDir == DragmoveDir.Left ? 1 : mFinisedSvDragDir == DragmoveDir.Right ? -1 : 0;
        //Debug.LogError("Move:" + mFinisedSvMoveDir);

        CheckMove();
    }

    private void OnDragStarted()
    {

        panelStartOffset = mMovement == UIScrollView.Movement.Horizontal ? mPanel.clipOffset.x : mPanel.clipOffset.y;
    }

    private int curPageIndex = 0;
    /// <summary>
    /// 当前panel移动到的位置（水平:x）
    /// </summary>
    private float curMoveTo = 0;
    private int minDragMoveDistance = 0;
    private void OnStoppedMoving()
    {



    }

    private void OnClipMove(UIPanel panel)
    {


        CheckMove();
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
