// B121_COLLFIX_Assert.cs — 执行代理 `E121-COLLFIX` 的断言宿主（收藏 chip 三条后续）
//
// 覆盖（= 施工单验收 ①…⑤）：
//   ① 同一筛选只出现一颗 chip：**抽屉行内 0 颗 + 页头 1 颗**（`B121-COLL-21`）
//   ② 上限 5 仍生效（跨 filter 累计；生产落点已从 `AppendChips` 换成 `BuildHeaderChips`）
//   ③ 卡组编辑窗页头 chip 的数量 / 顺序（`B121-COLL-22`；**模型层**，节点层见下方「未验」）
//   ④ 卡背抽屉点阵营 ⇒ 选中态亮（`B121-COLL-23`：`BuildCosmetics` 的 `On` 统一到 `FactionSelected`）
//   ⑤ 反证：无筛选 ⇒ 0 颗（页头两档都 0）
//
// 判据全文：`资料/完全复刻/报告/B121_COLL16_页头chip.md` §一 / §五（原版 `NotifyFilterChange` 的 `SetParent` 时序
// = `…__NotifyFilterChange__15f01a0.c:154`；上限 = `FillToggleList(toggleOptions, 5)`）。
//
// 接线由**调度台**做（⛔ 本文件不改 `Editor/*Scene.cs`）：宿主里调 `B121_COLLFIX_Assert.Run()` 即可。
// 跑法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B121_COLLFIX_Assert.Run -logFile -
//
// 判绿红：按**行首** `B121CF ✓` / `B121CF ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）；「未验」单独计数、不算通过。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static class B121_COLLFIX_Assert
{
    static int _pass, _fail, _unverified;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int Unverified { get { return _unverified; } }

    const string Tag = "B121CF";

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

        GroupRowVsHeader();
        GroupCapAndOrder();
        GroupDeckEditHeader();
        GroupCardbackSelection();
        GroupCounterProof();

        MarkUnverified("卡组编辑窗页头 chip 的**真节点**：要 `DeckRuntime` 实例（`Build()` 之后读 `UiHeaderChipCount` /"
                     + " `UiHeaderChipRect(i)`、点一下再读）—— 本宿主不建窗（与 `B121_COLL16_Assert` 同一条口径）。");
        MarkUnverified("`Clear filters` 在卡组编辑窗的**真位移**：要 `DeckRuntime`（选 2 个阵营后 `UiBtnRect(\"hdr_clear\")`"
                     + " 的 x 应 = `FilterPanelModel.HdrClearX(2)`）—— 同上，节点层留给宿主。");

        Debug.Log(Tag + " ==== 合计：通过 " + _pass + " · 失败 " + _fail + " · 未验 " + _unverified + " ====");
    }

    // ---------------- 夹具 ----------------
    static List<string> Facs() { return new List<string> { "Ultramarines", "Goff", "SaimHann" }; }

    static DeckEditorState State()
    {
        var pool = new List<CardDef>
        {
            new CardDef("cf1", "cf1", "unit", "", "common", "Ultramarines", 1, 1, 1, 0),
            new CardDef("cf2", "cf2", "unit", "", "common", "Ultramarines", 2, 1, 1, 0),
            new CardDef("cf3", "cf3", "unit", "", "rare", "Goff", 3, 1, 1, 0),
            new CardDef("cf4", "cf4", "tactic", "", "epic", "SaimHann", 4, 0, 0, 0),
        };
        return new DeckEditorState(pool);
    }

    static int CountChips(List<FilterPanelModel.Cell> cells)
    {
        int n = 0;
        for (int i = 0; i < cells.Count; i++)
            if (cells[i].Key != null && cells[i].Key.StartsWith("$chip:")) n++;
        return n;
    }

    // ---------------- ① 行内 0 颗 + 页头 1 颗 ----------------
    static void GroupRowVsHeader()
    {
        var st = State();
        var f = DeckFilter.None;
        f.Factions = new HashSet<string> { "Ultramarines" };
        st.SetFilter(f);

        var rows = new List<FilterPanelModel.Cell>();
        FilterPanelModel.Build(st, 335.31f, rows);
        Check(CountChips(rows) == 0, "① 选 1 个阵营：**抽屉行内 0 颗** `$chip:`（原版 `SetParent(filterIconHolder)` 搬走 —— "
                                    + "`…__NotifyFilterChange__15f01a0.c:154`；实得 " + CountChips(rows) + "）");

        var hdr = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildHeaderChips(st.Factions(), st.Filter, hdr);
        Check(hdr.Count == 1 && hdr[0].Key == "$chip:$fac:Ultramarines",
              "① 同一个筛选在**页头恰好 1 颗**（key = `$chip:$fac:Ultramarines`；实得 " + hdr.Count + " 颗）");

        // 行高：Army 那一行只由**阵营格数**决定（旧写法把 chip 也算进格数 ⇒ 多选就往下挪）
        var L = FilterPanelModel.ComputeLayout(st);
        var LNo = FilterPanelModel.ComputeLayout(st.Factions().Count);
        CheckNear(L.RarityTop, LNo.RarityTop, 0.001f, "① 选中阵营**不改变** Rarity 行顶（chip 已不占抽屉行格位）");
    }

    // ---------------- ② 上限 5 + 顺序 ----------------
    static void GroupCapAndOrder()
    {
        var facs = new List<string>();
        for (int i = 0; i < 8; i++) facs.Add("F" + i);
        var f = DeckFilter.None;
        f.Factions = new HashSet<string>(facs);
        var cells = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildHeaderChips(facs, f, cells);
        Check(cells.Count == 5, "② 页头 chip 上限 = 5（8 项全选也只画 5 颗；实得 " + cells.Count + "）");
        Check(cells.Count == 5 && cells[0].Key == "$chip:$fac:F0" && cells[4].Key == "$chip:$fac:F4",
              "② 顺序照选项表走、被截掉的是**排最后**的那几项（首 F0 / 第 5 颗 F4）");

        // 跨 filter 累计：3 阵营 + 2 稀有度 = 5 ⇒ 费用/卡类那两颗被截
        var m = DeckFilter.None;
        m.Factions = new HashSet<string>(Facs());
        m.Rarities = new HashSet<string> { FilterPanelModel.RarityKeys[0], FilterPanelModel.RarityKeys[1] };
        m.Costs = new HashSet<int> { 1 };
        m.Types = new HashSet<string> { FilterPanelModel.TypeKeys[0] };
        var mix = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildHeaderChips(Facs(), m, mix);
        Check(mix.Count == 5 && !mix[4].Key.StartsWith("$chip:$cost:"),
              "② 上限是**跨 filter 累计**（3 阵营 + 2 稀有度 = 5 ⇒ 费用那颗被截；实得 " + mix.Count + "）");
    }

    // ---------------- ③ 卡组编辑窗页头：数量 / 顺序（模型层） ----------------
    static void GroupDeckEditHeader()
    {
        // 卡组编辑窗那份 = `DeckRuntime.RefreshHeaderChips` 走的**同一条**调用：
        //   卡牌页 `BuildHeaderChips(State.Factions(), State.Filter, cells)`；
        //   卡背页 `…, _cosmoFilter, cells, armyOnly: true`（原版那棵树 `filters[]` 里只有 `CardArmyFilter`）。
        var st = State();
        var f = DeckFilter.None;
        f.Factions = new HashSet<string> { "SaimHann", "Ultramarines" };   // 故意与选项表**反序**选
        st.SetFilter(f);
        var cells = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildHeaderChips(st.Factions(), st.Filter, cells);
        Check(cells.Count == 2, "③ 卡组编辑窗（卡牌页）：选 2 个阵营 ⇒ 页头 2 颗（实得 " + cells.Count + "）");
        Check(cells.Count == 2 && cells[0].Key == "$chip:$fac:Ultramarines" && cells[1].Key == "$chip:$fac:SaimHann",
              "③ 顺序 = `State.Factions()` 的 `ArmyOrder` 序（不是点击序）");

        // 卡背页：同一个筛选集合，`armyOnly` ⇒ 只剩阵营那几颗
        var cosmo = DeckFilter.None;
        cosmo.Factions = new HashSet<string> { "Goff" };
        var only = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildHeaderChips(st.Factions(), cosmo, only, armyOnly: true);
        Check(only.Count == 1 && only[0].Key == "$chip:$fac:Goff",
              "③ 卡组编辑窗（卡背页 / `armyOnly`）：只有阵营 chip（实得 " + only.Count + " 颗）");
    }

    // ---------------- ④ 卡背抽屉的选中态（集合 vs 单值的分叉） ----------------
    static void GroupCardbackSelection()
    {
        var facs = Facs();

        // 🔴 判别式：卡背页点阵营走 `FilterPanelModel.Click("$fac:")` ⇒ 写的是**集合** `f.Factions`，
        //   而旧写法 `On = f.Faction == facs[i]` 读**单值** ⇒ 点了不亮。修后必须亮。
        var f = DeckFilter.None;
        f.Factions = new HashSet<string> { "Goff" };
        var cells = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildCosmetics(facs, f, 335.31f, cells, ownedFirst: true);
        bool goffOn = false, ultrasOn = false, sawGoff = false, sawUltras = false;
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].Key == "$fac:Goff") { sawGoff = true; goffOn = cells[i].On; }
            if (cells[i].Key == "$fac:Ultramarines") { sawUltras = true; ultrasOn = cells[i].On; }
        }
        Check(sawGoff && goffOn, "④ 卡背抽屉：集合 `Factions = {Goff}` ⇒ `Goff` 那格**亮**（旧写法读单值 ⇒ 恒不亮）");
        Check(sawUltras && !ultrasOn, "④ 灭自证：没选中的 `Ultramarines` 那格**不亮**（挡「恒真」）");

        // 兼容支：单值 `f.Faction` 那条老路**零变化**（卡组编辑窗自己用它）
        var g = DeckFilter.None;
        g.Faction = "Goff";
        var cells2 = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildCosmetics(facs, g, 335.31f, cells2, ownedFirst: true);
        bool goffOn2 = false;
        for (int i = 0; i < cells2.Count; i++) if (cells2[i].Key == "$fac:Goff") goffOn2 = cells2[i].On;
        Check(goffOn2, "④ 兼容支：单值 `Faction = Goff`（卡组编辑窗那条）**照旧亮**（`FactionSelected` 两条都吃）");
    }

    // ---------------- ⑤ 反证：无筛选 ⇒ 0 颗 ----------------
    static void GroupCounterProof()
    {
        var st = State();
        var none = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildHeaderChips(st.Factions(), DeckFilter.None, none);
        Check(none.Count == 0, "⑤ 反证：`DeckFilter.None` ⇒ 页头 0 颗（实得 " + none.Count + "）");
        var none2 = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildHeaderChips(st.Factions(), DeckFilter.None, none2, armyOnly: true);
        Check(none2.Count == 0, "⑤ 反证：卡背页（`armyOnly`）同样 0 颗");

        var rows = new List<FilterPanelModel.Cell>();
        FilterPanelModel.Build(st, 335.31f, rows);
        Check(CountChips(rows) == 0, "⑤ 反证：无筛选时抽屉行内也是 0 颗 `$chip:`");

        // `Owned` / `Upgradable` 不是 `filters[]` 里的 toggle 集合 ⇒ 不出 chip（原版 `Clear filters` 也不清它们）
        var f = DeckFilter.None; f.Owned = true; f.Upgradable = true;
        var own = new List<FilterPanelModel.Cell>();
        FilterPanelModel.BuildHeaderChips(st.Factions(), f, own);
        Check(own.Count == 0, "⑤ 反证：只开 `Owned` / `Upgradable` 不出 chip");
    }
}
