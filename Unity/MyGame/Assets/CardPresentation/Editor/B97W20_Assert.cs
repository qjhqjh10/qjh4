// B97W20_Assert.cs — 执行代理 `B97_W20`（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `特效动画:492` = 原版 `BattleManager.ResolveAnimInfoRangeAttack`（`_d__468`，**以及逐句同形的
//   `_d__466` 通用动画路**）里 `AnimInfo.tweenAnim`(`+0x80`) 那一支的**数据侧**：
//     ① `Resources/cardanim_clips.json` 的 `tw`  = 该 CardAnim 的 `tweenAnim` 指向的 **`UnitTweenSO` 名字**
//     ② 同表 `tww` = 那个 SO 的 **`waitAnimation`**（`so+0x18`；1 = 「等它播完再往下」）
//
//   判据（⛔ 别在这里抄第二份）→ `工具/gen_cardanim_clips.py` 的 `_tween_block` docstring
//   （`dump.cs:25243` 字段偏移 · `decomp_full/BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:85-108`
//   · `decomp_full/BattleManager._ResolveAnim_d__466__MoveNext.c:108-137`）。
//
//   🔴 **本文件里没有一处期望值是从我们自己的常量反算的**（那就是自证）：
//     · 行数口径来自**全池现扫**（`assets_full` 的 1,152 份 CardAnim JSON × `数据/索引/anim_address_map.json`）；
//     · `tww` 的期望值来自 **`数据/游戏数据/tween/*.json` 的 `waitAnimation`**（原始导出），
//       并且**逐行与 `Resources/UnitTweens.json` 的 `wait` 交叉核**（两处都是原版数据，不是我们编的）。
//
// ============================ 怎么用（宿主）============================
// `B97W20Assert.Run();`
//   + 宿主里两行：`pass += B97W20Assert.Pass; fail += B97W20Assert.Fail;`
//   · 🔴 **推荐宿主 = `CardPresentation/Editor/BattleScene.cs`（`BattleScene.Run`）的合计行之前**
//     —— 那里已经是一长串 `Xxx.Run(); pass += …; fail += …;` 的**同一物理行**，追加即零行数变化。
//   · 本文件**不建任何资产 / 不碰场景**：只 `Resources.Load` 两张表 + 读 `UnitTweenTable`，无副作用。
//   · 宿主里该插的那一行（**整行复制**，接在 `B97W8Assert.Run(); … fail += B97W8Assert.Fail;` 之后）：
//         B97W20Assert.Run(); pass += B97W20Assert.Pass; fail += B97W20Assert.Fail;
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A4（★ 最硬的一条）：把 `_tween_so_name` 里的**真名**换成 `_asset_meta_index()` 回的那份
//     **文件名**（`Recoil Normal 0_2s Tween`）—— 那些行 `UnitTweenTable.Get()` 会返 null ⇒ A4 红。
//     （这不是假想：AssetRipper 建文件名时**把逗号洗成了 `_`**，本轮第一版就是这么踩的，8 个 SO 全中招。）
//   · A6：把 `_tween_block` 的 `tww` 换成「`UnitTweens.json` 里随便挑一个」或恒 0 ⇒ A6 红
//     （12 行 `waitAnimation==1` 会变成 0 条）。
//   · A7：把 `_tween_block` 里的 `out["tww"] = w` 删掉（只写 `tw`）⇒ A7 红（`tww` 缺键 = 0 ⇒ 12 行掉 0）。
//   · A8：把 `_tween_block` 的写列条件改成「有 `unitsShake` 就不写 `tw`」（把原版的**并列**错读成 if/else）
//     ⇒ A8 红（139 行会掉到 0 行）。⚠️ 这条是**数据层**的灭自证；运行期那一半（`BattleDriver` 里
//     别写成 else-if）由接线的那个代理负责，本文件管不到。
//   · A9：把 `UnitTweenTable.WaitOf` 的 `e == null ? -1 : e.wait` 改成 `e == null ? 0 : e.wait`
//     ⇒ A9 红（「表里没有」和「不需要等」会被混成一个 0，调用方再也分不出错）。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_W20`：`cardanim_clips.json` 的 `tw` / `tww` 两列（`AnimInfo.tweenAnim`）断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W20Assert
{
    static readonly CheckSink _sink = new CheckSink("[W20] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ---- 期望值（全池现扫，出处见文件头；⛔ 不是拿我们的常量反算）----
    /// <summary>`cardanim_clips.json` 的行数（= `build()` 产出的 `count`；2026-10-29 现读）。</summary>
    const int WantRows = 1067;
    /// <summary>`tweenAnim` 非空的行数（现读 244 / 1067）。</summary>
    const int WantTwRows = 244;
    /// <summary>这 244 行覆盖的 distinct `UnitTweenSO` 数（现读 22）。</summary>
    const int WantTwSoCount = 22;
    /// <summary>`tww == 1`（原版会 `WaitForCompletion`）的行数（现读 12）。</summary>
    const int WantTwwOnes = 12;
    /// <summary>同时有 `tw` 与 `unitsShake` 的行数（现读 139 —— 原版这两族是**并列**、不是 if/else）。</summary>
    const int WantTwAndUsh = 139;

    // ---- 运行期那份 JSON 的形状（只读本文件关心的四列；其余列不声明 = JsonUtility 忽略）----
    [System.Serializable] class RtFile { public int schema, count; public RtRow[] rows; }
    [System.Serializable] class RtRow { public string a; public string tw; public int tww; public RtUsh[] ush; }
    [System.Serializable] class RtUsh { public int t; }

    public static void Run()
    {
        // ── 硬前提（先断前提、再断结论；⛔ 不写 `if (x != null) Check(...)`）──
        var ta = Resources.Load<TextAsset>(CardAnimClips.ResourcePath);
        Tr(ta != null, "`Resources/" + CardAnimClips.ResourcePath + ".json` 载不到 ⇒ `tw`/`tww` 两列无从校验");
        if (ta == null) return;                 // 上面那条已经红了；提前返回只为避免 NRE

        RtFile f = null;
        try { f = JsonUtility.FromJson<RtFile>(ta.text); }
        catch (System.Exception e) { Tr(false, "`cardanim_clips.json` 解析失败：" + e.Message); return; }
        Tr(f != null && f.rows != null && f.rows.Length > 0,
           "`cardanim_clips.json` 解析成空表（`rows` = " + (f == null || f.rows == null ? "null" : f.rows.Length.ToString()) + "）");
        if (f == null || f.rows == null || f.rows.Length == 0) return;

        // ── A1：表规模（改成别的列数 / 少写行 ⇒ 红）──
        C(f.rows.Length, WantRows, "A1 `rows` 条数（现读期望 " + WantRows + "）");
        C(f.count, WantRows, "A1' `count` 与 `rows` 对不上");

        // ── 扫一遍 ──
        var soNames = new HashSet<string>();
        int twRows = 0, twwOnes = 0, twAndUsh = 0, twwWithoutTw = 0;
        int unreachable = 0; string firstUnreachable = null;
        int waitMismatch = 0; string firstWaitMismatch = null;
        for (int i = 0; i < f.rows.Length; i++)
        {
            var r = f.rows[i];
            if (r == null) { Tr(false, "A0 `rows[" + i + "]` 是 null"); continue; }
            bool hasTw = !string.IsNullOrEmpty(r.tw);
            bool hasTwwKey = r.tww != 0;                // 缺键与 0 同义；只有 `1` 能在数据上区分出来
            if (!hasTw) { if (hasTwwKey) twwWithoutTw++; continue; }

            twRows++;
            soNames.Add(r.tw);
            if (hasTwwKey) twwOnes++;
            if (r.ush != null && r.ush.Length > 0) twAndUsh++;

            // ② 名字必须能被运行期读到（`UnitTweenTable` 认不出的名字 ⇒ 这一拍静默不播）
            if (UnitTweenTable.Get(r.tw) == null)
            {
                unreachable++;
                if (firstUnreachable == null) firstUnreachable = r.a + " → " + r.tw;
                continue;
            }
            // ③ 两列必须自洽（`tww` = 那个 SO 的 `waitAnimation`；两处不一致 ⇒ 有人解错了 SO）
            int w = UnitTweenTable.WaitOf(r.tw);
            if (w != r.tww)
            {
                waitMismatch++;
                if (firstWaitMismatch == null)
                    firstWaitMismatch = r.a + " → " + r.tw + "：`tww`=" + r.tww + " / `UnitTweens.json`=" + w;
            }
        }

        // ── A2/A3：两列的行数口径 ──
        C(twRows, WantTwRows, "A2 `tw` 非空的行数（= `AnimInfo.tweenAnim` 非空）");
        C(soNames.Count, WantTwSoCount, "A3 `tw` 覆盖的 distinct `UnitTweenSO` 数");

        // ── A4（★）：**两列要被运行期读到** —— 244 行的名字全部能在 `UnitTweenTable` 查到 ──
        C(unreachable, 0, "A4 `tw` 的名字在 `UnitTweens.json` 里查不到的条数（首例：" + (firstUnreachable ?? "无") + "）"
                          + " —— 非 0 说明生成器解错了资产名（⚠️ AssetRipper 会把逗号洗成 `_`）");

        // ── A5/A6：`tww`（`waitAnimation`）那一列 ──
        C(twwOnes, WantTwwOnes, "A6 `tww == 1`（原版 `WaitForCompletion`）的行数");
        C(twwWithoutTw, 0, "A7 有 `tww` 却没有 `tw` 的行数（两列必须同写同不写）");

        // ── A8：`tweenAnim` 与 `unitsShake` 是**并列**（原版 `_d__468:62-84`），⛔ 不是 if/else ──
        C(twAndUsh, WantTwAndUsh, "A8 同时有 `tw` 与 `ush` 的行数（原版两样都播；"
                                  + "把数据侧写成「有 ush 就不写 tw」⇒ 掉到 0）");

        // ── A9：逐行两列自洽（`tww` == 那个 SO 的 `waitAnimation`）──
        C(waitMismatch, 0, "A9 `tww` 与 `UnitTweens.json` 的 `wait` 不一致的行数（首例："
                           + (firstWaitMismatch ?? "无") + "）");

        // ── A10：读数口的三档契约（`-1` = 表里没有 · `0` = 不等 · `1` = 等）──
        C(UnitTweenTable.WaitOf(""), -1, "A10 `WaitOf(\"\")` —— 空名字必须回 -1（⛔ 不是 0）");
        C(UnitTweenTable.WaitOf("__B97W20_不存在的补间__"), -1,
          "A10' `WaitOf(<不存在的名字>)` —— 必须回 -1（⛔ 不是 0：「表里没有」不能被混成「不需要等」）");
        C(UnitTweenTable.WaitOf("Recoil Normal Tween"), 0,
          "A10'' `Recoil Normal Tween` 的 `waitAnimation`（原始导出 = 0；172 行引用它）");
        C(UnitTweenTable.WaitOf("AeldariRecallTween"), 1,
          "A10''' `AeldariRecallTween` 的 `waitAnimation`（原始导出 = 1；3 行引用它）");
    }
}
