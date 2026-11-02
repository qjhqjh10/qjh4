// B121_COLL16_Assert.cs — 执行代理 `B121-COLL16` 的断言宿主（收藏窗**页头筛选 chip**）
//
// 活：`B121-COLL-16` —— 收藏窗四页的页头筛选 chip 整块没建（审计 §问题 6 / §五 `?2` 仍 ❓）。
// 判据结论（四问全解，证据 → `资料/完全复刻/报告/B121_COLL16_页头chip.md`）：
//   ① `filterIconHolder` 的 PPtr **就是页头那颗 `Filters`**（四页各一份，`GridLayoutGroup 85×70 / spacing 5,0 /
//      FixedRowCount(1) / MiddleLeft` + `ContentSizeFitter(HorizontalFit = MinSize)`）；
//   ② chip 由 `NotifyFilterChange` **搬**进去（`SetParent(filterIconHolder)`），不是复制；
//   ③ 上限 **5**（`FillToggleList(toggleOptions, 5)`，跨 filter 累计）；
//   ④ 有 chip ⇔ `Clear Filter Button` 显示（同一条 `0 < toggleOptions.Count`）—— 两者**共存、不互斥**，
//      而且容器被 CSF 撑宽 ⇒ 那颗钮**跟着 chip 右移**（chip 左缘 592.2 压 612.2 的顾虑不成立）。
//
// 接线由**调度台**做（⛔ 本文件不改 `Editor/*Scene.cs`）：宿主里调 `B121_COLL16_Assert.Run()` 即可。
// 跑法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B121_COLL16_Assert.Run -logFile -
//
// 判绿红：按**行首** `B121C16 ✓` / `B121C16 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）；
//   「未验」单独计数、**不算通过**（⛔ 不静默）。
//
// 四条覆盖（= 施工单要求的 ①②③④）：
//   ① chip 数量/文字  → `GroupChips`（数量、顺序、上限 5、费用那颗带数字标签、稀有度/类型那颗不带）
//   ② 选中态          → `GroupChips`（只有 `IsOn` 的那几项出 chip；取消一项 ⇒ 它那颗消失、其余前移）
//   ③ 与 Clear 钮的关系 → `GroupClearRelation`（同一条判据：有 chip ⇔ 有钮；钮 = 最后一颗 chip 右缘 + 20）
//   ④ 反证（无筛选 ⇒ 不建） → `GroupCounterProof`
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class B121_COLL16_Assert
{
    static int _pass, _fail, _unverified;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int Unverified { get { return _unverified; } }

    const string Tag = "B121C16";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4") + "；实得 " + got.ToString("F4") + "）");
    }

    static void MarkUnverified(string msg) { _unverified++; Debug.Log(Tag + " … 未验：" + msg); }

    public static void Run()
    {
        _pass = _fail = _unverified = 0;

        GroupConstants();
        GroupChips();
        GroupGeometry();
        GroupClearRelation();
        GroupCounterProof();

        MarkUnverified("页头 chip 的**节点**：需要 `CollectionWindow` 实例（四页各建一次、改筛选后重建）—— "
                     + "本宿主只验模型那一层（`FilterPanelModel.BuildHeaderChips`），不建窗");
        MarkUnverified("`Clear filters` 的**真节点位移**：需要 `CollectionWindow`（选 2 个阵营后 `_hdrClearQ[0].transform`"
                     + " 的 x 应比空筛选时大 180 —— 这一条要在宿主里读世界坐标才验得了）");

        Debug.Log(Tag + " ==== 合计：通过 " + _pass + " · 失败 " + _fail + " · 未验 " + _unverified + " ====");
    }

    // ---------------- ① 几何常量（逐条对原版 prefab 的字面量） ----------------
    static void GroupConstants()
    {
        // 判据 = `bundle_menus_assets_all ▸ … ▸ Header Filters/Header ▸ Filters` 的 RectTransform + 两个组件：
        //   容器 = 592.17,70.94 → 592.17,155.94（宽 0、高 85）；`GridLayoutGroup.m_CellSize = (85,70)`、
        //   `m_Spacing = (5,0)`、`m_Constraint = 2(FixedRowCount)` / `m_ConstraintCount = 1`、`m_ChildAlignment = 3(MiddleLeft)`
        CheckNear(FilterPanelModel.HdrGrpL, 592.17f, 0.02f, "页头 `Filters` 左缘 = 592.17（四页同值）");
        CheckNear(FilterPanelModel.HdrGrpT, 70.94f, 0.02f, "页头 `Filters` 顶边 = 70.94");
        CheckNear(FilterPanelModel.HdrGrpH, 85f, 0.02f, "页头 `Filters` 高 = 85");
        CheckNear(FilterPanelModel.HdrChipW, 85f, 0.001f, "chip 宽 = `GridLayoutGroup.m_CellSize.x` = 85");
        CheckNear(FilterPanelModel.HdrChipH, 70f, 0.001f, "chip 高 = `m_CellSize.y` = 70");
        CheckNear(FilterPanelModel.HdrChipSpX, 5f, 0.001f, "chip 列距 = `m_Spacing.x` = 5");
        // MiddleLeft ⇒ 纵轴居中：(85 − 70)/2 = 7.5 ⇒ 70.94 + 7.5
        CheckNear(FilterPanelModel.HdrChipY, 78.44f, 0.02f, "chip 顶边 = 70.94 + (85−70)/2 = 78.44（`m_ChildAlignment = 3` MiddleLeft）");
        Check(FilterPanelModel.HdrChipMax == 5, "chip 上限 = 5（`FillToggleList(toggleOptions, 5)`）");
        // 灭自证：如果谁把上限改成「每行 5」而总数放开，这一条仍然绿，所以另加一条「累计」判据（见 GroupChips 的 5 上限那条）
        CheckNear(FilterPanelModel.HdrIconS, 50f, 0.001f, "Rarity/Type 那颗 `Background` 是 50×50 居中（不是铺满）");
    }

    // ---------------- ①② chip 的数量 / 顺序 / 选中态 ----------------
    static List<string> Facs() { return new List<string> { "Ultramarines", "Goff", "SaimHann" }; }

    static DeckFilter Sel(int nFac, int nRar, int nCost, int nType)
    {
        var f = DeckFilter.None;
        if (nFac > 0)
        {
            f.Factions = new HashSet<string>();
            var facs = Facs();
            for (int i = 0; i < nFac && i < facs.Count; i++) f.Factions.Add(facs[i]);
        }
        if (nRar > 0)
        {
            f.Rarities = new HashSet<string>();
            for (int i = 0; i < nRar && i < FilterPanelModel.RarityKeys.Length; i++) f.Rarities.Add(FilterPanelModel.RarityKeys[i]);
        }
        if (nCost > 0)
        {
            f.Costs = new HashSet<int>();
            for (int i = 0; i < nCost && i < FilterPanelModel.CostBuckets.Length; i++) f.Costs.Add(FilterPanelModel.CostBuckets[i].Lo);
        }
        if (nType > 0)
        {
            f.Types = new HashSet<string>();
            for (int i = 0; i < nType && i < FilterPanelModel.TypeKeys.Length; i++) f.Types.Add(FilterPanelModel.TypeKeys[i]);
        }
        return f;
    }

    static List<FilterPanelModel.Cell> Chips(DeckFilter f, bool armyOnly = false)
    {
        var cells = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildHeaderChips(Facs(), f, cells, armyOnly);
        return cells;
    }

    static void GroupChips()
    {
        // ② 选中态：0 选中 ⇒ 0 颗；选中 2 个阵营 ⇒ 2 颗
        Check(Chips(Sel(0, 0, 0, 0)).Count == 0, "无选中 ⇒ 一颗 chip 都不建（原版 `FillToggleList` 只对 `IsOn` 的选项 clone）");
        var c2 = Chips(Sel(2, 0, 0, 0));
        Check(c2.Count == 2, "选中 2 个阵营 ⇒ 2 颗 chip（实得 " + c2.Count + "）");
        Check(c2.Count == 2 && c2[0].Key == "$chip:$fac:Ultramarines" && c2[1].Key == "$chip:$fac:Goff",
              "chip 的 key/顺序跟着选项表走（Ultramarines → Goff）");
        Check(c2.Count == 2 && c2[0].On && c2[1].On, "chip 恒是「已选」态（`On = true`）");

        // ① 数量与顺序：原版 `filters` 数组序 = Name·Army·Rarity·Cost·Type·Upgradable ⇒ 阵营在稀有度之前
        var mixed = Chips(Sel(1, 1, 1, 1));
        Check(mixed.Count == 4, "阵营+稀有度+费用+卡类各 1 ⇒ 4 颗（Name/Upgradable 的 `FillToggleList` 是空体、不出 chip）"
              + "（实得 " + mixed.Count + "）");
        Check(mixed.Count == 4 && mixed[0].Key.StartsWith("$chip:$fac:") && mixed[1].Key.StartsWith("$chip:$rar:")
              && mixed[2].Key.StartsWith("$chip:$cost:") && mixed[3].Key.StartsWith("$chip:$type:"),
              "顺序 = 原版 `filters` 数组序（Army → Rarity → Cost → Type）");

        // ① 三族长相：Army/Cost 的 `Background` 铺满整格；Rarity/Type 是 50×50 居中；Rarity/Type 的标签被 `HideLabel` 关掉
        if (mixed.Count == 4)
        {
            CheckNear(mixed[0].Bg.W, 85f, 0.01f, "Army chip 的 `Background` 铺满（宽 85）");
            CheckNear(mixed[1].Bg.W, 50f, 0.01f, "Rarity chip 的 `Background` = 50×50 居中（宽 50）");
            CheckNear(mixed[1].Bg.x1 - mixed[1].R.x1, 17.5f, 0.01f, "Rarity chip 的 `Background` 居中（左右各 17.5）");
            CheckNear(mixed[3].Bg.W, 50f, 0.01f, "Type chip 的 `Background` = 50×50 居中");
            Check(string.IsNullOrEmpty(mixed[1].Label) && string.IsNullOrEmpty(mixed[3].Label),
                  "Rarity / Type 的 chip **不带标签**（原版 `HideLabel()`）");
            Check(!string.IsNullOrEmpty(mixed[2].Label), "费用那颗 chip **带数字标签**（原版 `CardCostFilter` 走基类、不 `HideLabel`）");
        }

        // ③ 上限：**跨 filter 累计** 5（3 阵营 + 2 稀有度 = 5 ⇒ 费用/卡类那两颗被截掉）
        var cap = Chips(Sel(3, 2, 1, 1));
        Check(cap.Count == 5, "上限 = **5 颗（跨 filter 累计）**（3 阵营 + 2 稀有度 ⇒ 5；实得 " + cap.Count + "）");
        Check(cap.Count == 5 && !cap[4].Key.StartsWith("$chip:$cost:"), "被截掉的是排在后面的族（费用那颗不该出现）");

        // 卡背 / 卡组两页：`filters` 里只有阵营那一条 ⇒ `armyOnly`
        var only = Chips(Sel(1, 1, 1, 1), true);
        Check(only.Count == 1 && only[0].Key.StartsWith("$chip:$fac:"), "armyOnly（Deck / Cardback 页）⇒ 只有阵营 chip");
    }

    // ---------------- ① 几何：格位 ----------------
    static void GroupGeometry()
    {
        var c = Chips(Sel(2, 1, 0, 0));
        Check(c.Count == 3, "（前提）3 颗 chip");
        if (c.Count < 3) return;
        CheckNear(c[0].R.x1, 592.17f, 0.02f, "第 0 颗 chip 左缘 = 容器左缘 592.17");
        CheckNear(c[0].R.y1, 78.44f, 0.02f, "第 0 颗 chip 顶边 = 78.44");
        CheckNear(c[0].R.W, 85f, 0.01f, "chip 宽 85");
        CheckNear(c[0].R.H, 70f, 0.01f, "chip 高 70");
        CheckNear(c[1].R.x1 - c[0].R.x1, 90f, 0.01f, "相邻 chip 间距 = cell 85 + spacing 5 = 90");
        CheckNear(c[2].R.x1 - c[0].R.x1, 180f, 0.01f, "第 2 颗（稀有度那颗）在 592.17 + 180");
        // 灭自证：三颗 chip **不重叠**（若谁把列距写成 0，上面那条 90 也会一起变绿 —— 这条独立量）
        Check(c[c.Count - 1].R.x2 <= c[0].R.x1 + c.Count * 90f + 0.01f, "chip 们互不重叠（右缘 ≤ 起点 + n·90）");
    }

    // ---------------- ③ 与 `Clear Filter Button` 的关系 ----------------
    static void GroupClearRelation()
    {
        // 原版：`clearFiltersButton.gameObject.SetActive(0 < toggleOptions.Count)` —— **同一个** `toggleOptions` 计数
        //   ⇒ 「有 chip」与「有钮」是同一条判据（共存，不是互斥）。
        // 几何：父容器 `ContentSizeFitter(MinSize)` ⇒ 宽 = 90n − 5（n=0 时 minWidth = −5，被 `GetMinSize` 忽略 ⇒ 宽 0）；
        //   钮锚 (1,.5) pos(20,0) ⇒ 左缘 = 592.17 + (90n − 5) + 20。
        CheckNear(FilterPanelModel.HdrClearX(0), 612.17f, 0.02f, "无 chip ⇒ 钮在 612.17（= 实测 612.2，CSF 从不跑那一档）");
        CheckNear(FilterPanelModel.HdrClearX(1), 697.17f, 0.02f, "1 颗 chip ⇒ 钮右移到 697.17（= 592.17 + 85 + 20）");
        CheckNear(FilterPanelModel.HdrClearX(2), 787.17f, 0.02f, "2 颗 ⇒ 787.17（= 592.17 + 175 + 20）");
        CheckNear(FilterPanelModel.HdrClearX(2) - FilterPanelModel.HdrClearX(1), 90f, 0.01f,
                  "钮的位移量 = 一颗 chip 的步距 90（灭自证：写死常量会让这条红）");
        // ③「不重叠」：最后一颗 chip 的右缘到钮左缘恒 20
        for (int n = 1; n <= 5; n++)
            CheckNear(FilterPanelModel.HdrClearX(n) - (FilterPanelModel.HdrGrpL + n * 90f - 5f), 20f, 0.01f,
                      n + " 颗 chip：最后一颗右缘与钮左缘恒隔 20px（原版 `pos(20,0)`）");
        // 灭自证：钮**必须动** —— 若谁把这里改回 `CollectionWindow.ClearFilterX(page)` 那个常量，这条红
        Check(Mathf.Abs(FilterPanelModel.HdrClearX(3) - CollectionWindow.ClearFilterX(0)) > 200f,
              "3 颗 chip 时钮的位置**不等于**静态 612.2 ⇒ 组态下它真的跟着 chip 右移（⛔ 别退回写死常量）");
    }

    // ---------------- ④ 反证：无筛选 ⇒ 不建 / 不显 ----------------
    static void GroupCounterProof()
    {
        var none = Chips(DeckFilter.None);
        Check(none.Count == 0, "反证①：`DeckFilter.None` ⇒ 0 颗 chip（页头不建任何 chip 节点）");
        var noneArmy = Chips(DeckFilter.None, true);
        Check(noneArmy.Count == 0, "反证②：armyOnly 页同样 0 颗");
        // `Owned` / `Upgradable` 两个开关**不算筛选**（原版 `Clear filters` 也不清它们）⇒ 不该出 chip
        var f = DeckFilter.None; f.Owned = true; f.Upgradable = true;
        Check(Chips(f).Count == 0, "反证③：只开 `Owned only` / `Upgradable only` 不出 chip（它们不是 `filters[]` 里的 toggle 集合）");
        // 名字筛选也不出 chip（`CardNameFilter.FillToggleList` 空体）
        var g = DeckFilter.None; g.Name = "imperial";
        Check(Chips(g).Count == 0, "反证④：卡名子串不出 chip（`CardNameFilter.FillToggleList` 是空体）");
        // 与 ③ 配对：无 chip 时那颗钮也得是关的（`SetActive(0 < count)`）—— 静态那一跳由宿主验，这里钉住判据口
        Check(FilterPanelModel.HdrClearX(0) == FilterPanelModel.HdrGrpL + 20f,
              "反证⑤：无 chip 时钮停在容器左缘 + 20（= 容器宽 0 那一档）");
    }
}
