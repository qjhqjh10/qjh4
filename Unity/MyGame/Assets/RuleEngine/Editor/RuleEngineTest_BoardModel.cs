// RuleEngineTest_BoardModel.cs — **棋盘「连续无洞」模型**的自检（2026-10-01 立）
//
// 为什么单开一个文件：这一段是**棋盘语义的正面尺子** —— 老断言是「某个效果对不对」，
// 这一条量的是**模型本身**（插入 / 补位 / 落点 / 不变量）。
// 判据全文 → `RuleEngine/Core/BoardSlots.cs` 的文件头（含原版 `MinionManager` 的逐条出处）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>
    /// **棋盘的「连续无洞」模型**：原版 `MinionManager` 的棋盘是**每侧一条列表**
    /// （`leftMinions` / `rightMinions`，`dump.cs:39326-39329`），所以：
    ///   · **出牌 = 插入**（`List.Insert`）—— 插进中间会把**后面的单位整体外移一格**；
    ///   · **离场 = 移除**（`List.Remove`）—— 后面的单位**整体内移一格补位**；
    ///   · **免费部署 / 抢人 = `GetNextSlotWithoutDisplacing`** —— **人少的那一侧的最外一格**（平手走右）；
    ///   · ⇒ **每侧永远从贴督军那格起连续无洞**（`BoardSlots.IsContiguous` 就是这句话）。
    ///
    /// 格号约定：`0 1 2 3 [4=督军] 5 6 7 8`，**左侧第 0 格 = 3 号格**（贴督军），
    /// 右侧第 0 格 = 5 号格。
    /// </summary>
    static void TestBoardContiguousModel()
    {
        // ---- ① 出牌 = 插入：插在中间会把外侧整体推出去一格 ----
        {
            var ctx = Battle(new[] { Unit("A", 0, 1, 5), Unit("B", 0, 1, 5), Unit("C", 0, 1, 5) },
                             new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);

            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "A"), 3), RuleCodes.OK,
                      "① 打在 3 号格（左侧最里那一格）");
            Check(Board(ctx, 0, 3) != null ? Board(ctx, 0, 3).Name : null, "A", "…A 就在 3 号格");

            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B"), 3), RuleCodes.OK,
                      "② 再打在**同一格**（原版：插在它前面，不是「被占格拒绝」）");
            Check(Board(ctx, 0, 3) != null ? Board(ctx, 0, 3).Name : null, "B", "…新来的占了 3 号格");
            Check(Board(ctx, 0, 2) != null ? Board(ctx, 0, 2).Name : null, "A", "★ 原来那张被整体推出去一格（3 → 2）");

            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "C"), 2), RuleCodes.OK,
                      "③ 打在 2 号格（该侧第 2 格，插在 A 内侧）");
            Check(Board(ctx, 0, 3) != null ? Board(ctx, 0, 3).Name : null, "B", "…3 号格那张（在插入点以内）不动");
            Check(Board(ctx, 0, 2) != null ? Board(ctx, 0, 2).Name : null, "C", "…C 落在 2 号格");
            Check(Board(ctx, 0, 1) != null ? Board(ctx, 0, 1).Name : null, "A", "★ A 又被推出去一格（2 → 1）");
            Check(Board(ctx, 0, 0), null, "…最外那一格还空着（4 张才满）");

            // ---- ② 离场 = 补位：外面的人整体内移一格 ----
            // ⚠️ 走**真实的伤害入口 → 离场那一段**（`ApplyDamage` 只扣血，不处理离场）
            RuleCore.HurtForTest(ctx, Board(ctx, 0, 2), 99, "夹具");
            Check(Board(ctx, 0, 3) != null ? Board(ctx, 0, 3).Name : null, "B", "★ 被杀的是 2 号格的 C ⇒ 3 号格不动");
            Check(Board(ctx, 0, 2) != null ? Board(ctx, 0, 2).Name : null, "A",
                  "★ **外侧那张补位**（A 从 1 号格挪回 2 号格）—— 这是「让位」的数据侧");
            Check(Board(ctx, 0, 1), null, "…原来最外那一格空出来了");

            string why;
            CheckTrue(BoardSlots.IsContiguous(ctx.Players[0], out why),
                      "★ 一连串增删之后**每侧仍然连续无洞**（不变量；" + (why ?? "无洞") + "）");

            // ---- ③ 免费部署 = 人少那一侧的最外一格（平手走右）----
            int slot;
            bool ok = RuleCore.DeployFree(ctx, 0, ctx.NewInstance(Unit("Free", 0, 1, 1)), out slot);
            CheckTrue(ok && slot == 5,
                      $"★ `DeployFree` 落在**右侧最里那一格（5）**（左 2 / 右 0 ⇒ 挑右边）—— 实得 {slot}");

            // ---- ④ 请求的那一侧满了 ⇒ **换到对侧最外那一格**（不是拒绝）----
            //    判据 = 原版 `MinionManager__AdjustedSlot.c:64-72`（右满 ⇒ 返回 `~leftMinions.Count`）
            //    与 `:104-107`（左满 ⇒ 返回 `rightOccupation.Count + 1`）。
            //    ⚠️ **2026-10-01 晚改**：这一条原来写的是 `ErrSlot`（= 我们当时的实现），**那是错的** ——
            //       原版是「换一侧」而不是「拒绝」；照它改了 `BoardSlots.Resolve`。
            //       真正打不出去的只剩**两侧都满**（见本块末尾）。
            // 左侧现在有 2 个（3 号格 B、2 号格 A）⇒ 再往最外补两格就满了
            Place(ctx, 0, 1, Unit("F1", 0, 1, 1));
            Place(ctx, 0, 0, Unit("F0", 0, 1, 1));
            CheckTrue(BoardSlots.IsContiguous(ctx.Players[0], out why),
                      "夹具摆完之后左侧仍是连续的（" + (why ?? "无洞") + "）");
            var extra = Unit("Extra", 0, 1, 1);
            ctx.Players[0].Hand.Add(ctx.NewInstance(extra));
            int extraIdx = ctx.Players[0].Hand.Count - 1;
            CheckCode(RuleCore.CanPlayCard(ctx, 0, extraIdx, 3), RuleCodes.OK,
                      "★ 左侧满 4 个 ⇒ 往**左侧**打仍然合法（原版换到对侧，不是拒绝）");
            CheckCode(RuleCore.PlayCard(ctx, 0, extraIdx, 3), RuleCodes.OK, "…真打出去");
            Check(Board(ctx, 0, 6) != null ? Board(ctx, 0, 6).Name : null, "Extra",
                  "★ 它落在**右侧最外那一格（6）** —— 右侧此刻只有 `DeployFree` 那个（5 号格）⇒ 追加到 6");

            // ---- ④b 两侧都满 ⇒ 这才是唯一真的拒绝 ----
            Place(ctx, 0, 7, Unit("G2", 0, 1, 1), exhausted: true);
            Place(ctx, 0, 8, Unit("G3", 0, 1, 1), exhausted: true);
            CheckTrue(BoardSlots.IsContiguous(ctx.Players[0], out why),
                      "两侧各 4 个、都连续（" + (why ?? "无洞") + "）");
            var extra2 = Unit("Extra2", 0, 1, 1);
            ctx.Players[0].Hand.Add(ctx.NewInstance(extra2));
            int extra2Idx = ctx.Players[0].Hand.Count - 1;
            CheckCode(RuleCore.CanPlayCard(ctx, 0, extra2Idx, 3), RuleCodes.ErrSlot,
                      "★ **两侧都满** ⇒ 打不出去（原版这一步靠调用方事先筛，我们返回 `ErrSlot`）");
            CheckCode(RuleCore.CanPlayCard(ctx, 0, extra2Idx, 5), RuleCodes.ErrSlot,
                      "…请求右侧同理");
        }

        // ---- ⑤ 同一批里死两个：**两条都要真的离场** ----
        //    🔴 这一条钉的是「延后处理」那条链上的一个**真 bug**（2026-10-01 改模型时发现）：
        //       `_pendingDeaths` 原来记的是**格号**，而补位会让后面那个人的格号变掉 ⇒
        //       处理第二个时按记下的格号去找**会指空**，它就一直留在场上（静默）。
        //    挑**右侧**当夹具不是随便挑的：出队顺序按格号升序，而右侧「贴督军那格 = 5」在**前**
        //    ⇒ 先摘掉 5 号格的那个，6 号格那个就会补到 5 号格上 —— 正好踩中。
        {
            var ctx = Battle(new[] { Unit("A", 0, 9, 9) }, new[] { Unit("X", 0, 0, 1) });
            ToP1Turn(ctx, 2);
            var x = Place(ctx, 1, 5, Unit("X1", 0, 0, 1), exhausted: true);
            var y = Place(ctx, 1, 6, Unit("X2", 0, 0, 1), exhausted: true);
            using (RuleCore.SimultaneousDamage(ctx))
            {
                RuleCore.HurtForTest(ctx, x, 5, "夹具");
                RuleCore.HurtForTest(ctx, y, 5, "夹具");
            }
            CheckTrue(x.Health <= 0 && y.Health <= 0, "前提：两个都被打到 0 血（同一批）");
            Check(Board(ctx, 1, 5), null, "★ 5 号格空了");
            Check(Board(ctx, 1, 6), null,
                  "★ 6 号格也空了 —— **同一批里死两个，两个都要处理**"
                  + "（按格号定位的旧写法会漏掉第二个：它补位之后那个格号已经指空）");
        }
    }
}
