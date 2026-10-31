// B112_R3C_Assert.cs — 执行代理 `B112_R3C`（第一百一十一会话第 2 波 · 转岗写手）：
// `B-134` 施工单 A（targeted 战术卡点亮合法目标）+ B（法术准星配色）。
//
// 判据（三源亲读，见 `资料/完全复刻/判据/B112_R3B_SpellFrom与非目标Spell.md` §一）：
//   · `cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:43834-43863`（`StartSpellFrom` 全文 5 步）
//   · `decomp_full/BattleManager__StartSpellFrom.c:13-47`
//   · `decomp_full/CrosshairLineEffect__SetAttackType.c:11-22`（`attackType 3/4` 同走 `+0x30` 曲线）
//   · `decomp_full/TargetReticleController__PrepareMovement.c:15-38`（内含 `SetAttackType(param_3)`）
//
// 🔴 **每一段都带「改哪一处才红」**（`🧨`）—— 没写判别式的断言在本仓算没断。
//
// ⚠️ **宿主契约（接线方负责）**：
//   `B112R3C_Assert.Run(driver)` —— `driver` 必须是一局**已开始、未结束**的对战，并且
//   **本地玩家（`driver.MySeat`）手牌里有一张 `Targeted == 1 && CardType == 20` 的战术卡，
//   且 `driver.PlayTargetPoolForTest(那张卡).Count > 0`**。
//   ⛔ 前提不成立时本文件会**报红并写明缺什么**（不静默跳过 —— 本仓红线条目）。
//   ⛔ 别把它塞进某条已有断言的**行尾 `//` 之后**（本仓踩过 6 次，代码被注释吞掉而 `typecheck` 照样 0/0）。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;
using RuleEngine;

/// <summary>`B-134`：targeted 战术卡的合法目标点亮 + 法术准星金色。</summary>
public static class B112R3C_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B112R3C] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    /// <summary>原版预设表里 `attackType == 3`（Spell）那一条的准星色（`TargetReticle.cs` 的 `Preset Gold`，
    /// 原件 `MonoBehaviour_5098.json` 实读：3 与 4 逐值相同）。</summary>
    static readonly Color GoldCross = new Color(0.9137256f, 0.7764707f, 0.2039216f, 1f);

    // ------------------------------------------------------------------
    //  入口
    // ------------------------------------------------------------------

    /// <summary>入口。<paramref name="driver"/> 的夹具契约见文件头。</summary>
    public static void Run(BattleDriver driver)
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        S("B112_R3C：`B-134` · targeted 战术卡点亮 + 法术准星金色");

        SectionReticlePreset(driver);
        SectionNonUnitTargetHighlight(driver);
        SectionReticleSpace(driver);
    }

    // ------------------------------------------------------------------
    //  B · 准星配色（原版 `StartSpellFrom` → `PrepareMovement(pos, AttackTypes.Spell)` ⇒ `SetAttackType(3)`）
    // ------------------------------------------------------------------

    static void SectionReticlePreset(BattleDriver driver)
    {
        S("B · 法术准星 = 金色（`SetAttackType(3)`）");

        T((int)AttackKind.Spell == 3 && (int)AttackKind.Spell == (int)AttackTypes.Spell,
          $"B1 `AttackKind.Spell`(`{(int)AttackKind.Spell}`) == `AttackTypes.Spell`(`{(int)AttackTypes.Spell}`) == 3"
          + " —— 🧨 改枚举值 ⇒ 红（原版 `dump.cs` `AttackTypes { None0, Melee1, Ranged2, Spell3, Active4 }`）");

        T(driver != null && driver.reticle != null,
          "B2 （前提）宿主给了一个带准星的 driver —— ⛔ 前提不成立=红（不静默跳过）");
        if (driver == null || driver.reticle == null) return;

        // 走**产品同一条公开 API**（`BattleDriver.UpdateSpellDragReticle` 现在就是 `Show(..., AttackKind.Spell, ...)`）。
        bool wasVisible = driver.reticle.Visible;
        driver.reticle.Show(Vector3.zero, new Vector3(0f, 2f, 0f),
                            AttackKind.Spell, new Vector3(0f, 2f, 0f));

        var got = driver.reticle.CrossColor;
        bool near = Mathf.Abs(got.r - GoldCross.r) < 2e-3f
                 && Mathf.Abs(got.g - GoldCross.g) < 2e-3f
                 && Mathf.Abs(got.b - GoldCross.b) < 2e-3f;
        T(near,
          $"B3 `Show(..., AttackKind.Spell, ...)` ⇒ 准星色 = 金 ({GoldCross.r:0.###},{GoldCross.g:0.###},{GoldCross.b:0.###})，"
          + $"实得 ({got.r:0.###},{got.g:0.###},{got.b:0.###})"
          + " —— 🧨 把 `TargetReticle.TryPresetOf` 里 `case AttackKind.Spell` 删掉 ⇒ 走 `default` 不写颜色（保持白）⇒ 红");

        // 反向：`None` 不写颜色（原版 `attackType 0` 那条预置在表里**不存在**）⇒ 准星**保持上一拍的颜色**。
        // 🔴 判别式只能是「与调用前逐分量相同」：上一段刚把它染成金，断「不是金」的话「default 不写颜色」与
        //    「default 返回 Gold」两种实现都会金 ⇒ 结构上分不出来。
        var beforeNone = driver.reticle.CrossColor;
        driver.reticle.Show(Vector3.zero, new Vector3(0f, 2f, 0f),
                            AttackKind.None, new Vector3(0f, 2f, 0f));
        var gotNone = driver.reticle.CrossColor;
        T(Mathf.Abs(gotNone.r - beforeNone.r) < 2e-3f && Mathf.Abs(gotNone.g - beforeNone.g) < 2e-3f
          && Mathf.Abs(gotNone.b - beforeNone.b) < 2e-3f,
          $"B4 判别式：`AttackKind.None` **不写颜色**（与调用前逐分量相同）：调用前 ({beforeNone.r:0.###},{beforeNone.g:0.###},{beforeNone.b:0.###})，实得 ({gotNone.r:0.###},{gotNone.g:0.###},{gotNone.b:0.###})"
          + " —— 🧨 把 `default` 分支改成也返回 Gold（或给 `None` 补一条预置）⇒ 红"
          + "（原版 `SetAttackType` 的预置表里没有 `attackType 0`；`CrosshairLineEffect.SetAttackType` 对 0 还会 LogWarning）");

        if (!wasVisible) driver.reticle.Hide(true);   // 被本段弄亮的准星要还原，别污染后面的夹具
    }

    // ------------------------------------------------------------------
    //  A · 非单位卡（targeted 战术卡）点亮合法目标
    //     判据 = `BattleManager__StartSpellFrom.c:30-45`（= `ShowPotentialTargets(mode=1)` 的内联副本）
    // ------------------------------------------------------------------

    static void SectionNonUnitTargetHighlight(BattleDriver driver)
    {
        S("A · targeted 战术卡 ⇒ 每个合法目标都点亮（原版 `StartSpellFrom` 的内联那一份）");

        T(driver != null && driver.Ctx != null && !driver.Ctx.IsOver,
          "A0 （前提）宿主给了一局**已开始、未结束**的对战 —— ⛔ 前提不成立=红（不静默跳过）");
        if (driver == null || driver.Ctx == null || driver.Ctx.IsOver) return;

        int seat = driver.MySeat;
        var hand = driver.Ctx.Players[seat].Hand;

        // ---- 夹具：手牌里找一张「targeted 战术卡 + 目标池非空」 ----
        int handIdx = -1; CardDef def = null; List<UnitState> pool = null;
        for (int i = 0; i < hand.Count; i++)
        {
            var d = hand[i] != null ? hand[i].Card : null;
            if (d == null || d.IsUnit || d.Targeted != 1 || d.CardType != 20) continue;
            var p = driver.PlayTargetPoolForTest(d);
            if (p == null || p.Count == 0) continue;
            handIdx = i; def = d; pool = p; break;
        }
        T(handIdx >= 0,
          "A1 （前提）本地玩家手牌里有一张 `Targeted==1 && CardType==20` 且目标池非空的战术卡"
          + " —— ⛔ 前提不成立=红（宿主夹具要么补一张，要么这条按红报给调度台，⛔ 不许静默跳过）");
        if (handIdx < 0) return;

        // ---- 落点：空格（避开「落点上就有合法目标 ⇒ 松手即结算」那条快捷路） ----
        int dropSlot = -1;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            bool empty = true;
            for (int o = 0; o < 2; o++) if (driver.Ctx.Players[o].Board[s] != null) empty = false;
            if (empty) { dropSlot = s; break; }
        }
        T(dropSlot >= 0, "A2 （前提）棋盘上至少有一个空格可作落点 —— ⛔ 前提不成立=红");
        if (dropSlot < 0) return;

        bool dropped = driver.SimulateCardDroppedForTest(handIdx, dropSlot);
        T(dropped, "A3 `SimulateCardDroppedForTest` 走到了产品那条落位链路（`OnCardDeployed`）");
        T(driver.AwaitingPlayTarget,
          "A4 打出一张 targeted 战术卡 ⇒ 进「等点目标」态（原版 `playingTargetedSpell(4)`）"
          + " —— 🧨 把 `TryBeginPlayTargetPick` 里 `def.Targeted != 1` 那条闸改成「只单位卡」⇒ 红");

        // ---- 点亮核对：池里每一张（棋盘上有视图的）都必须是 `ValidTarget`，且不许有多余的点亮 ----
        int expected = 0, lit = 0, wrong = 0;
        for (int side = 0; side < 2; side++)
        {
            // 与产品同一映射（`HighlightPlayTargets` 里就是 `side == _me ? _myUnits : _foeUnits`）——
            // ⚠️ 别写成 `side==0 ⇒ MyUnits`：联机客机 `MySeat == 1` 时会整表错位。
            var views = side == seat ? driver.MyUnits : driver.FoeUnits;
            int owner = side;
            for (int t = 0; t < BoardSpec.Size; t++)
            {
                CardView v;
                if (!views.TryGetValue(t, out v) || v == null) continue;
                var u = driver.Ctx.Players[owner].Board[t];
                bool should = u != null && pool.Contains(u);
                bool isLit = v.State == CardHighlightState.ValidTarget;
                if (should) expected++;
                if (should && !isLit) wrong++;
                if (!should && isLit) wrong++;
                if (isLit) lit++;
            }
        }
        T(expected > 0, $"A5 （前提）目标池里至少 {1} 张在棋盘上有视图（实得 {expected}）—— ⛔ 前提不成立=红");
        T(wrong == 0 && lit == expected,
          $"A6 **每个合法目标都点亮、且没有多点亮**（期望 {expected} / 实得 {lit} / 错配 {wrong}）"
          + " —— 🧨 把 `HighlightPlayTargets` 里 `if (!def.IsUnit) return 0;` **加回去** ⇒ 期望>0 而实得 0 ⇒ 红"
          + "（原版 `BattleManager__StartSpellFrom.c:30-45`：`card.isPlayer && card.targeted && IsValidSpellTarget` ⇒ `potentialTargetInBoard`）");

        // ---- 取消 ⇒ 退出等待态 + 绿环全收 ----
        bool cancelled = driver.SimulatePlayTargetPickForTest(-1, -1);
        T(cancelled, "A7 点空白 = 走产品那条取消支（`ResolvePlayTargetByUnit(null,-1)`）");
        int stillLit = 0;
        for (int s2 = 0; s2 < BoardSpec.Size; s2++)
        {
            CardView mv, fv;
            if (driver.MyUnits.TryGetValue(s2, out mv) && mv != null
                && mv.State == CardHighlightState.ValidTarget) stillLit++;
            if (driver.FoeUnits.TryGetValue(s2, out fv) && fv != null
                && fv.State == CardHighlightState.ValidTarget) stillLit++;
        }
        T(!driver.AwaitingPlayTarget && stillLit == 0,
          $"A8 取消之后：退出等待态 **且** 绿环全收（仍亮 {stillLit} 个）"
          + " —— 🧨 把取消支里的 `ClearPlayTargetHighlight()` 删掉 ⇒ 本仓会留一整屏绿环 ⇒ 红");
    }

    // ------------------------------------------------------------------
    //  C · 准星弧线起点 = HUD 平面点（账 `B-153` · 判据 `B113_G4_准星空间与目标判定.md` §五·A）
    //     原版 `StartSpellFrom` 的起点是 `GetHeroPos` = `Get2DPosOfCard(heroCard, useCenter:1)`
    //     （`BattleManager__GetHeroPos.c:10-20`）⇒ **HUD 画布平面**上那个点；
    //     我们原来喂裸 `transform.position`（3D 战场帧）⇒ 弧线从屏幕正中起（偏 ≈ 97 px）。
    // ------------------------------------------------------------------

    static void SectionReticleSpace(BattleDriver driver)
    {
        S("C · 准星弧线起点 = HUD 帧（`Get2DPosOfCard(me, true)`）");

        T(driver != null, "C0 （前提）宿主给了 driver —— ⛔ 前提不成立=红（不静默跳过）");
        if (driver == null) return;

        // 🔴 这两条前提**必须单独断**（`B113_G4` §五·A 明写）：2D 档下「HUD 点」与「裸 transform 点」本来就近似相等
        //    ⇒ 夹具跑错档会让 C6/C7 假绿。
        T(driver.use3DBoard,
          "C1 （前提）`use3DBoard == true`（`Battle.unity:849` 现读 = 1；它是 `Get2DPosOfCard` 会不会走投影那一跳的闸）");
        T(driver.boardCam != null && driver.cam != null,
          "C2 （前提）`boardCam` + `cam` 两台相机都在（缺一个 `Get2DPosOfCard` 就退化成「原样返回」）");
        if (!driver.use3DBoard || driver.boardCam == null || driver.cam == null) return;
        T(driver.reticle != null, "C3 （前提）`reticle` 存在");
        if (driver.reticle == null) return;

        int slot = -1; CardView me = null;
        foreach (var kv in driver.MyUnits)
            if (kv.Value != null) { slot = kv.Key; me = kv.Value; break; }
        T(slot >= 0 && me != null, "C4 （前提）我方棋盘上至少有一张视图可选中（督军那一格也算）");
        if (slot < 0 || me == null) return;

        // 期望值：**独立重算**一遍 `Get2DPosOfCard(me, useCenter:true)` 的那一跳
        //   （`boardCam.WorldToScreenPoint` → `cam.ScreenToWorldPoint(z = HudCanvas.PlaneDistance)`）。
        //   ⚠️ 这是同一条公式的第二份，只为拿期望值：它挡不住「两边一起改」，挡住的是「产品喂了裸坐标」——
        //      后者由 C7（灭自证）钉死。
        var anchor = me.EffectAnchor(true);
        Vector3 w3 = anchor != null ? anchor.position : me.transform.position;   // `useCenter: true`
        Vector3 raw = me.transform.position;
        Vector3 sp = driver.boardCam.WorldToScreenPoint(w3);
        Vector3 hud = driver.cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, HudCanvas.PlaneDistance));

        driver.SetCommandForTest(AttackKind.Melee, slot);   // 只写 `_command` / `_selectedSlot`
        driver.SimulatePointerAt(hud);                      // → `UpdateReticle` → `reticle.Show(Get2DPosOfCard(me, true), …)`
        Vector3 o, e;
        bool has = driver.reticle.ArcEnds(out o, out e);
        driver.SetCommandForTest(AttackKind.None, -1);      // 收干净（`ClearSelection` 是 private，这里只回写这两个字段）
        driver.reticle.Hide(true);
        T(has, "C5 弧线建出来了（`ArcEnds` 有 ≥ 2 个点）");
        if (!has) return;

        Vector3 expHud = driver.reticle.transform.TransformPoint(new Vector3(hud.x, hud.y, TargetReticle.Z));
        Vector3 expRaw = driver.reticle.transform.TransformPoint(new Vector3(raw.x, raw.y, TargetReticle.Z));
        float dHud = Vector3.Distance(o, expHud);
        float dRaw = Vector3.Distance(o, expRaw);
        T(dHud < 0.05f,
          $"C6 弧线起点 ≈ `Get2DPosOfCard(me,true)` 的 **HUD 点**（差 {dHud:0.###} 世界单位）"
          + " —— 🧨 把 `UpdateReticle` 的起点改回 `me.transform.position` ⇒ 红");
        T(dRaw > 0.3f,
          $"C7 **灭自证**：起点**不是**裸 `transform.position`（离裸点 {dRaw:0.###} 世界单位）"
          + " —— 🧨 改回裸坐标 ⇒ 本条与 C6 同时红（本工程 3D 战场帧→HUD 帧差 ≈ 0.9 单位 ≈ 97 px，见 `B113_G4` §1·4）");
    }
}
