using System;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class EquidistancePageRecycle
{
    #region 数据们

    /// <summary>
    /// 已包含间隔
    /// </summary>
    private int cellSize;
    private float halfCellSize;
    /// <summary>
    /// 数据总数
    /// </summary>
    private int DataCount;

    /// <summary>
    /// 数据占满一页的排列方式
    /// </summary>
    private UIGrid.Arrangement DataArrangeType;
    #region 分页数据

    private int curPageIndex = 0;
    private int willShowPageIndex;

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
    /// 初始化时，界面生成的cell的最右的cell,在最后一页时的列下标【循环移动使用的列数据】
    /// （注意：是生成的最右,不是可显示在panel的最右）当额外生成列数为0时，生成的最右=可显示的最右
    /// </summary>
    private int lastPageLastShowingColIndex = 0;
    /// <summary>
    /// 界面生成的cell，首列下标【循环移动使用的列数据】
    /// </summary>
    private int curFirstColIndex = 0;
    /// <summary>
    /// 界面生成的cell，末列下标【循环移动使用的列数据】
    /// </summary>
    private int curLastColIndex = 0;

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

    /// <summary>
    /// 生成的所有cell List
    /// </summary>
    private List<GameObject> cellGoList;


    #endregion

    #region 委托

    public delegate GameObject OnLoadItem();
    public delegate void OnUpdateItem(GameObject go, int cellVirtualIndex, int dataIndex);

    private OnLoadItem onLoadItem;
    private OnUpdateItem onUpdateItem;

    #endregion

    #region  cell数据管理

    private class CellInfo
    {
        public int cellMoveIndex;//【移动用下标】
        public int cellVirtualIndex;//【虚拟下标】
        public int dataIndex;//【数据下标】

        public CellInfo(int cMoveIndex, int cVIndex, int dIndex)
        {
            cellMoveIndex = cMoveIndex;
            cellVirtualIndex = cVIndex;
            dataIndex = dIndex;
        }
    }
    Dictionary<int, CellInfo> CellInfoDic = new Dictionary<int, CellInfo>();
    private void AddCellInfo(int cIndex, int cVIndex, int dIndex)
    {
        if (CellInfoDic.ContainsKey(cIndex))
        {
            UpdateCellInfo(cIndex, cVIndex, dIndex);
            return;
        }
        CellInfo info = new CellInfo(cIndex, cVIndex, dIndex);
        CellInfoDic.Add(cIndex, info);
    }

    private void UpdateCellInfo(int cIndex, int cVIndex, int dIndex)
    {
        CellInfo info;
        if (!CellInfoDic.TryGetValue(cIndex, out info)) return;
        //Debug.LogError(string.Format("{0},{1},{2}", info.cellMoveIndex, info.cellVirtualIndex, info.dataIndex));
        info.cellVirtualIndex = cVIndex;
        info.dataIndex = dIndex;
        //Debug.LogError(string.Format("{0},{1},{2}", CellInfoDic[cMoveIndex].cellMoveIndex, CellInfoDic[cMoveIndex].cellVirtualIndex, CellInfoDic[cMoveIndex].dataIndex));
    }
    #endregion

    private UIScrollView mScrollView;
    private UIPanel mPanel;

    private UIScrollView.Movement mMovement;
    private GameObject cellParent;
    //private GameObject cellPool;
    private Bounds mPanelBounds;
    #endregion

    public EquidistancePageRecycle(UIScrollView sv, int dataCount, int size, int pageColum,
        OnLoadItem loadItem, OnUpdateItem updateItem, UIGrid.Arrangement arrangement = UIGrid.Arrangement.Horizontal,
        bool isNeedFirstLastLimit = true, int extraShownum = 1, int minDragCanMoveDistance = 0, bool isNeedFirstLastLimitRecycle = true)
    {
        mScrollView = sv;
        DataCount = dataCount;
        cellSize = size;
        halfCellSize = (float)cellSize / 2;
        pageColumnLimit = pageColum;
        extraShowNum = extraShownum;

        onLoadItem = loadItem;
        onUpdateItem = updateItem;
        DataArrangeType = arrangement;

        minDragMoveDistance = minDragCanMoveDistance;
        IsNeedFirstLastLimit = isNeedFirstLastLimit;
        IsNeedFirstLastLimitRecycle = isNeedFirstLastLimitRecycle;

        InitNeed();
    }
    public void DestoryPageRecycle()
    {
        RemoveEvent();

        cellGoList.Clear();
        CellInfoDic.Clear();

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
        
        InitPanelColRow();


    }
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
            }
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
            }
        }
        if (mMovement == UIScrollView.Movement.Vertical)
        {
            //todo:
        }
        PanelMaxShowCount = mPanelRowLimit * mPanelColumnLimit;
        pageDataTotalCount = mPanelRowLimit * pageColumnLimit;

        cellGoList = new List<GameObject>(PanelMaxShowCount);
        lastPageLastShowingColIndex = GetPageRealCellLineIndexByPageTimes(mPanelInitColumnLimit - 1, pageTotalNum - 1);
        //Debug.LogError("最后页最后列的移动列数据：" + lastPageLastShowingColIndex);

        CellInfoDic = new Dictionary<int, CellInfo>(PanelMaxShowCount);

        DebugLogAllInfo();
    }

    private void DebugLogAllInfo()
    {
        var info = string.Format("界面完全显示行：{0}\n,界面完全显示列：{1}\n,界面完全显示总数：{2}\n\n", mPanelRowLimit, mPanelColumnLimit, PanelMaxShowCount);
        var info1 = string.Format("实际生成行总数：{0}\n,实际生成列总数：{1}\n,实际生成总数：{2}\n\n", mPanelInitRowLimit, mPanelInitColumnLimit, mPanelInitRowLimit * mPanelInitColumnLimit);
        var info2 = string.Format("数据总数：{0}\n,数据每页显示列数：{1}\n,翻页总页数：{2}\n,翻页总列数：{3}\n", DataCount, pageColumnLimit, pageTotalNum, pageTotalColumn);
        Extensions.LogAttentionTip(info + info1 + info2);
    }
    public void InitCell()
    {
        float cellX, cellY;
        GameObject go;
        Transform tf;
        int rowLimit = mMovement == UIScrollView.Movement.Vertical ? mPanelRowLimit + extraShowNum : mPanelRowLimit;
        int lineLimit = mMovement == UIScrollView.Movement.Horizontal ? mPanelColumnLimit + extraShowNum : mPanelColumnLimit;
        int cellMoveIndex, cellVirtualIndex, dataIndex = 0;

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

                ////按cell真实循环列下标显示
                //onUpdateItem(go, curLine,-1);

                cellMoveIndex = curHang * lineLimit + curLine;
                //cell虚拟下标显示
                cellVirtualIndex = GetCellVirtualIndex(curHang, curLine);
                //Debug.LogError(string.Format("{0},{1},{2}", curHang, curLine, cellVirtualIndex));
                dataIndex = DataArrangeType == UIGrid.Arrangement.Horizontal ? GetDataIndexByVirtualIndexInHorWay(cellVirtualIndex) : DataArrangeType == UIGrid.Arrangement.Vertical ? GetDataIndexByVirtualIndexInVerWay(cellVirtualIndex) : 0;
                onUpdateItem(go, cellVirtualIndex, dataIndex);
                AddCellInfo(cellMoveIndex, cellVirtualIndex, dataIndex);
            }
        }

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






    #region 帮助
    /// <summary>
    /// 输入翻页的行列下标，获取【虚拟下标】cellVirtualIndex
    ///  0,1,2,3,4,5,...pageTotalColumn-1
    ///  1*pageTotalColumn....
    ///  2*pageTotalColumn....
    ///  (row-1)*pageTotalColumn....
    /// </summary>
    /// <param name="rowIndex">【0,mPanelInitRowLimit-1】</param>
    /// <param name="cellPageLineIndex">【0，pageTotalColumn-1】</param>
    /// <returns></returns>
    public int GetCellVirtualIndex(int rowIndex, int cellPageLineIndex)
    {
        //括号：index取余保证不出界
        var index = (rowIndex % mPanelInitRowLimit) * pageTotalColumn + (cellPageLineIndex % pageTotalColumn);
        return index;
    }


    /// <summary>
    /// 数据当前页，当前页内的行列数,获取【虚拟下标】cellVirtualIndex
    /// </summary>
    /// <param name="pageIndex">【0,pageDataTotalCount-1】</param>
    /// <param name="rowIndex">【0，mPanelInitRowLimit-1】</param>
    /// <param name="cellEachPagelLineIndex">【0，pageColumnLimit-1】</param>
    /// <returns></returns>
    public int GetCellVirtualIndex(int pageIndex, int rowIndex, int cellEachPagelLineIndex)
    {
        //括号：index取余保证不出界
        if (pageIndex >= pageDataTotalCount)
        {
            Extensions.LogAttentionTip("【页数】下标超过可翻最大页数，取余处理");
        }
        if (rowIndex >= mPanelInitRowLimit)
        {
            Extensions.LogAttentionTip("【行数】下标超过界面生成的最大行数，取余处理");
        }
        if (cellEachPagelLineIndex >= pageColumnLimit)
        {
            Extensions.LogAttentionTip("【列数】下标超过当前页最大列数，取余处理");
        }
        var dIndex = (pageIndex % pageTotalNum) * pageColumnLimit + (rowIndex % mPanelInitRowLimit) * pageTotalColumn + (cellEachPagelLineIndex % pageColumnLimit);
        return dIndex;
    }

    /// <summary>
    /// 获取下pageTimes页对应【移动用】的真实下标
    /// </summary>
    public int GetPageRealCellLineIndexByPageTimes(int curPageRealCellLineIndex, int pageTimes)
    {
        var curLineIndex = curPageRealCellLineIndex;
        for (int i = 0; i < pageTimes; i++)
        {
            curLineIndex = GetNextPageRealCellLineIndex(curLineIndex, pageColumnLimit, mPanelInitColumnLimit);
        }
        //Debug.LogError(string.Format("当前：{0}，下{1}页：{2}", curPageRealCellLineIndex, pageTimes, curLineIndex));
        return curLineIndex;
    }
    /// <summary>
    /// 获取下一页对应【移动用】的真实下标
    /// </summary>
    /// <param name="curPageRealCellLineIndex">当前所在页的cell真实列下标【0，mPanelInitColumnLimit-1】</param>
    /// <param name="eachPageDataLineCount">每页显示数据的列数</param>
    /// <param name="eachPageInitCellCount">每页生成的总列数</param>
    /// <returns></returns>
    private int GetNextPageRealCellLineIndex(int curPageRealCellLineIndex, int eachPageDataLineCount, int eachPageInitCellCount)
    {
        var nextPageRealCellLineIndex = (curPageRealCellLineIndex + eachPageDataLineCount) % eachPageInitCellCount;
        return nextPageRealCellLineIndex;
    }

    /// <summary>
    ///(数据水平排列) 输入【虚拟下标】，映射出【数据下标】
    ///  数据根据每页可显示的总数量，水平排布，先占满一页里的第一行
    /// </summary>
    /// <returns></returns>
    private int GetDataIndexByVirtualIndexInHorWay(int virtualIndex)
    {
        var pageIndex = (virtualIndex / pageColumnLimit) % pageTotalNum;
        //每页的列index（水平：每页页头的列index为0，列尾index为pageColumnLimit-1）
        var eachPageLineIndex = virtualIndex % pageColumnLimit;
        var hangIndex = virtualIndex / pageTotalColumn;

        var dataIndex = pageIndex * pageDataTotalCount + hangIndex * pageColumnLimit + eachPageLineIndex;
        return dataIndex;
    }

    /// <summary>
    /// (数据垂直排列)输入【虚拟下标】，映射出【数据下标】
    /// 数据根据每页可显示的总数量，垂直排布，先占满一页里的第一列
    /// </summary>
    /// <returns></returns>
    private int GetDataIndexByVirtualIndexInVerWay(int virtualIndex)
    {
        var pageIndex = (virtualIndex / pageColumnLimit) % pageTotalNum;
        //每页的列index（水平：每页页头的列index为0，列尾index为pageColumnLimit-1）
        var eachPageLineIndex = virtualIndex % pageColumnLimit;
        var hangIndex = virtualIndex / pageTotalColumn;

        var dataIndex = pageIndex * pageDataTotalCount + eachPageLineIndex * mPanelInitRowLimit + hangIndex;
        return dataIndex;
    }

    #endregion



    private enum DragmoveDir
    {
        None,
        Left,
        Right,
        Top,
        Down
    }


    /// <summary>
    /// 检测并移动
    /// </summary>
    private void CheckMove()
    {
        int rowLimit = mMovement == UIScrollView.Movement.Vertical ? mPanelRowLimit + extraShowNum : mPanelRowLimit;
        int lineLimit = mMovement == UIScrollView.Movement.Horizontal ? mPanelColumnLimit + extraShowNum : mPanelColumnLimit;
        if (cellGoList.Count <= lineLimit) return;
        int cellMoveIndex = 0, moveColIndex, intMoveDir, cellVirtualIndex = 0, dataIndex;

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
                //Debug.LogError("移动方向" + intMoveDir);
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
                willShowPageIndex = curPageIndex + intMoveDir * pageNum;
                //Debug.LogError(willShowPageIndex);
            }
            //Debug.LogError(curPageIndex);
            #region  限制3：首尾翻页预设循环限制
            if (IsNeedFirstLastLimit && IsNeedFirstLastLimitRecycle)
            {

                if (mPanel.clipOffset.x < 0)
                {
                    Extensions.LogAttentionTip("首页右拖翻上一页，预设不循环");
                    return;
                }

                if (mPanel.clipOffset.x > (pageTotalNum - 1) * goToNextPageDistance)
                {
                    Extensions.LogAttentionTip("尾页左拖翻下一页，预设不循环");
                    return;
                }
            }
            #endregion
            if (IsAllOutHoriPanel(cellX))
            {
                for (int hangIndex = 0; hangIndex < rowLimit; hangIndex++)
                {
                    cellMoveIndex = hangIndex * lineLimit + moveColIndex;
                    cellGo = cellGoList[cellMoveIndex];
                    cellX = cellGo.transform.localPosition.x + intMoveDir * lineLimit * cellSize;

                    cellVirtualIndex = CellInfoDic[cellMoveIndex].cellVirtualIndex + intMoveDir * mPanelInitColumnLimit;
                    dataIndex = DataArrangeType == UIGrid.Arrangement.Horizontal ? GetDataIndexByVirtualIndexInHorWay(cellVirtualIndex) : DataArrangeType == UIGrid.Arrangement.Vertical ? GetDataIndexByVirtualIndexInVerWay(cellVirtualIndex) : 0;
                    onUpdateItem(cellGo, cellVirtualIndex, dataIndex);
                    UpdateCellInfo(cellMoveIndex, cellVirtualIndex, dataIndex);

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
                //Debug.LogError(curFirstColIndex);

            }
        }
        if (mMovement == UIScrollView.Movement.Vertical)
        {
            //todo
        }

    }

    #region  事件

    #region 拖动数据变量
    /// <summary>
    /// 开始拖动时，panel的clipOffset（水平x 垂直y）
    /// </summary>
    private float panelStartOffset;


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

    #endregion
    #region 拖动限制
    /// <summary>
    /// 限制1：翻页的最短距离限制
    /// </summary>
    private int minDragMoveDistance = 0;
    /// <summary>
    /// 限制2：首尾翻页限制（水平：首页再往右翻不能翻到末页，末页往左不能翻到左）
    /// </summary>
    private bool IsNeedFirstLastLimit = true;
    /// <summary>
    /// 限制3：首尾翻页预设循环限制（首尾页翻动，是否中断预设循环移动）
    /// 【必须先开启首尾翻页限制 IsNeedFirstLastLimit ,该限制才生效】
    /// </summary>
    private bool IsNeedFirstLastLimitRecycle = true;
    #endregion
    private void RegisterEvent()
    {
        if (mPanel != null) mPanel.onClipMove += OnClipMove;
        if (mScrollView != null)
        {
            mScrollView.onDragStarted += OnDragStarted;
            mScrollView.onDragFinished += OnDragFinished;
            mScrollView.onMomentumMove += onMomentumMove;
            mScrollView.onStoppedMoving += OnStoppedMoving;
            //    mScrollView.onScrollWheel += OnScrollWheel;
        }
    }
    private void RemoveEvent()
    {
        if (mPanel != null) mPanel.onClipMove -= OnClipMove;
        if (mScrollView != null)
        {
            mScrollView.onDragStarted -= OnDragStarted;
            mScrollView.onDragFinished -= OnDragFinished;
            mScrollView.onMomentumMove -= onMomentumMove;
            mScrollView.onStoppedMoving -= OnStoppedMoving;
            //    mScrollView.onScrollWheel -= OnScrollWheel;
        }
    }

    private void OnDragStarted()
    {

        panelStartOffset = mMovement == UIScrollView.Movement.Horizontal ? mPanel.clipOffset.x : mPanel.clipOffset.y;
    }


    private void OnDragFinished()
    {
        var dragOffset = mPanel.clipOffset.x - panelStartOffset;
        //拖动方向
        mFinisedSvDragDir = dragOffset > 0 ? DragmoveDir.Left : dragOffset < 0 ? DragmoveDir.Right : DragmoveDir.None;
        intFinishedSvDragDir = mFinisedSvDragDir == DragmoveDir.Left ? 1 : mFinisedSvDragDir == DragmoveDir.Right ? -1 : 0;
        //Debug.LogError("Drag:" + mFinisedSvDragDir);

        #region 跳转页下标,页位置计算
        var pageNum = 0;
        var goToNextPageDistance = pageColumnLimit * cellSize;
        var offsetDistance = Mathf.Abs((int)(mPanel.clipOffset.x - panelStartOffset));
        if (mMovement == UIScrollView.Movement.Horizontal)
        {
            pageNum = (offsetDistance / goToNextPageDistance) + 1;
            pageNum = offsetDistance >= goToNextPageDistance + goToNextPageDistance / 2 ? pageNum - 1 : pageNum;
        }

        var dragDistance = Mathf.Abs(dragOffset);
        int willGotoPageIndex;
        if (dragDistance >= minDragMoveDistance)// 限制1：翻页的最短距离限制
        {
            if (mFinisedSvDragDir == DragmoveDir.Left)
            {
                willGotoPageIndex = curPageIndex + pageNum;
                if (IsNeedFirstLastLimit && willGotoPageIndex >= pageTotalNum)//限制2：首尾翻页限制
                {
                    Extensions.LogAttentionTip(string.Format("当前第{0}页，不支持左拖翻页到{1}", curPageIndex, willGotoPageIndex));
                    willGotoPageIndex = pageTotalNum - 1;
                }
                curPageIndex = willGotoPageIndex;
            }
            if (mFinisedSvDragDir == DragmoveDir.Right)
            {
                willGotoPageIndex = curPageIndex - pageNum;


                if (IsNeedFirstLastLimit && willGotoPageIndex < 0) //限制2：首尾翻页限制
                {
                    Extensions.LogAttentionTip(string.Format("当前第{0}页，不支持右拖翻页{1}", curPageIndex, willGotoPageIndex));
                    willGotoPageIndex = 0;
                }
                curPageIndex = willGotoPageIndex;
            }
        }
        else
        {
            Extensions.LogAttentionTip(string.Format("拖动距离{0}太短，不足以翻页", dragDistance));
        }
        //Debug.LogError(curPageIndex);


        var finalmoveTo = -1 * curPageIndex * pageColumnLimit * cellSize;
        SpringPanel.Begin(mPanel.gameObject, new Vector3(finalmoveTo, 0, 0), 8f);

        #endregion
        //移动方向
        mFinisedSvMoveDir = mPanel.transform.localPosition.x - finalmoveTo > 0 ? DragmoveDir.Left : mPanel.transform.localPosition.x - finalmoveTo < 0 ? DragmoveDir.Right : DragmoveDir.None;
        intFinishedSvMoveDir = mFinisedSvMoveDir == DragmoveDir.Left ? 1 : mFinisedSvMoveDir == DragmoveDir.Right ? -1 : 0;
        //Debug.LogError("Move:" + mFinisedSvMoveDir);
        //Debug.LogError("Move int:" + intFinishedSvMoveDir);

        //CheckMove();
    }



    private void onMomentumMove()
    {
        //DebugIsDraging();
    }
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
    /// <summary>
    /// 是否显示非bug类提醒
    /// </summary>
    public static bool IsShowLog = false;
    public static void LogAttentionTip(object message)
    {
        if (IsShowLog)
        {
            Debug.LogError(message);
        }
    }

    public static string WrapColor(this string text, string colorCode)
    {
        text = "[" + colorCode + "]" + text + "[-]";
        return text;
    }
}
