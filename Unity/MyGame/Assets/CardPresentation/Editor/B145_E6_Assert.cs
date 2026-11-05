// B145_E6_Assert.cs — 执行写手 `B145-E6`（2026-11-05）的断言宿主
//
// ============================ 盯的是什么 ============================
//  ① A-14 近战反击的**起播时刻**（判据 = `_ResolveAttack_d__438__MoveNext.c:988-1001,1114-1152`）：
//     原版受方 `ReceiveDamage` **不 yield**、反击在同一趟协程里紧接着做 ⇒ 反击起点 = **主攻击动画结束那一刻**
//     = `Attack + 0.4`（与受方伤害计数器同拍）。我们时间线里 `Hit` 占满 1.0 s，改前那条 `case Retaliation: return 0f`
//     把反击排到 `Hit` **之后** ⇒ 实测 `Attack + 1.4`（晚 1.0 s）。
//     落点 = `CardPresentation/Core/EventTiming.cs` 的 `DelayBetween`（✅ 已复刻 2026-11-05 · B145-E6）。
//  ② A-15 选牌面板**标题字号**（判据 = B129 二·★3）：原版 `ChooseText` TMP `m_fontSize 55`、字体
//     `capLine 36 / pointSize 50` ⇒ 大写 **cap = 39.6 px**；改前 `SetGlyphHeight(55)` 把 55 当汉字墨高 ⇒ cap ≈ 45.2（大 14%）。
//     落点 = `CardPresentation/Battle/CardChoicePanel.cs` 的 `TitleCapPerFontSize` / `TitleCapPxFor`。
//  ③ A-16 「确认」字的**右对齐**（判据 = B129 二·★4）：原版 `ContinueText` 框 `372.857×43.622`、心 (1519,980.26)、
//     `m_HorizontalAlignment = 4 (Right)` ⇒ **字右沿 = 框右沿 1705.43**；改前 pivot 居中 ⇒ 字右沿 ≈1574（左 120–130 px）。
//     落点 = `CardChoicePanel.ConfirmBoxW` / `ConfirmRightEdgePx` / `ConfirmTextAnchor`。
//  ④ A-17 多卡展示窗**标题命中框**（判据 = B129 二·#32）：原版 `Header Text` 节点 rect **1192.37×63.204**
//     （TMP `m_RaycastTarget = 1`、零处理器）⇒ 整条吃射线；我们原来只有 TMP 的文字 bbox。
//     落点 = `CardPresentation/Battle/MultiCardDisplay.cs` 的 `HeaderHitSizePx` / `HeaderHitCenterPx` / `BuildHeaderHitRect`。
//
// ============================ 怎么接 ============================
//  `B145_E6_Assert.Run();`（静态类 + 无参 `Run()`；全局命名空间、类名 == 文件名）
//  ⚠️ 宿主接线**不在本写手白名单**（`Editor/*Scene.cs` 与 `RuleEngineTest.cs` 都不可改）⇒ 待调度台接线。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A-1d2：把 `DelayBetween` 的 `case Retaliation` 改回 `return 0f;` ⇒ A-1d2/A-1d3 红。
//   · A-2c：把 `TitleCapPxFor` 改回 `lo.TitlePx`（即 `SetGlyphHeight` 那条口径）⇒ A-2b 红（39.6 → 55）。
//   · A-3c：把 anchor 改回 `(0.5, 0.5)` ⇒ A-3c 红；把右沿算式改回 `ConfirmCx` ⇒ A-3b 红（1705.43 → 1519）。
//   · A-4b：把 `HeaderH` 改回「文字 bbox 高度」/把命中框建成文字尺寸 ⇒ A-4a 红。
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static class B145_E6_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "[B145E6]";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol, msg + "（实得 " + got.ToString("F4") + " / 期望 " + want.ToString("F4") + "）");
    }

    public static void Run()
    {
        _pass = _fail = 0;

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  ① A-14 近战反击的起播时刻（判据 = `_ResolveAttack_d__438__MoveNext.c:988-1001,1114-1152`）
        //     用**与 `BattleDriver.PlaySignals` 逐句相同**的推进式复算一遍：
        //       t += DelayBetween(prev, e);  事件 = t;  t += DurationOf(e);
        //     ⛔ 这不是「拿被测代码算期望」—— 被测的只有 `DelayBetween` 那一格，
        //        期望值 = 「反击起点与 `Hit` 起点相同」这条**原版语义**（两处比较，不是同义反复）。
        // ═══════════════════════════════════════════════════════════════════════════════════════
        {
            var skipWas = EventTiming.ReplaySkipAnims;
            EventTiming.ReplaySkipAnims = null;      // 回放「跳过动画」档：本组要的是**常速**那套值
            try
            {
                var atk = new BattleEvent { Kind = EvtKind.Attack, Ranged = false };
                var hit = new BattleEvent { Kind = EvtKind.Hit, Ranged = false };
                var ret = new BattleEvent { Kind = EvtKind.Retaliation, Ranged = false };
                var hit2 = new BattleEvent { Kind = EvtKind.Hit, Ranged = false };

                float t = 0f;
                t += EventTiming.DelayBetween(null, atk); float atkAt = t; t += EventTiming.DurationOf(atk);
                t += EventTiming.DelayBetween(atk, hit); float hitAt = t; t += EventTiming.DurationOf(hit);
                t += EventTiming.DelayBetween(hit, ret); float retAt = t; t += EventTiming.DurationOf(ret);
                t += EventTiming.DelayBetween(ret, hit2); float hit2At = t;

                // A-1（前提，硬检查）：命中拍 = Attack + 0.4（原版整条近战动画 0.4 s 跑完才落伤害）
                float hitLag = hitAt - atkAt;
                Check(Mathf.Abs(hitLag - 0.4f) < 1e-3f,
                      "A-1（前提）命中拍 = **Attack + 0.4 s**（实 " + hitLag.ToString("F3") + "）—— 原版 `_AttackMeleeAnim` 整条 0.4");
                // A-1b（前提）：反击那一条**不是零时长**的近战档（原版 `:1132-1150` 守方什么都不播 ⇒ `DurationOf` = 0）
                Near(EventTiming.DurationOf(ret), 0f, 1e-6f, "A-1b（前提）近战反击**占 0 s**（原版守方不播任何东西）");

                // A-1c ★ 核心：反击的**起点**与上一条 `Hit` 的**起点**同一刻（原版受方 `ReceiveDamage` 不 yield）
                Near(retAt, hitAt, 1e-6f,
                     "A-1c ★★ 近战反击**与受方伤害同拍**（`Retaliation.at == Hit.at`）"
                   + " —— 原版 `_ResolveAttack…:988-1001` 的 `ReceiveDamage` **不 yield**、`:1114-1152` 紧接着反击"
                   + "｜🧨 把 `DelayBetween` 的 `case Retaliation` 改回 `return 0f;` ⇒ 反击被排到 1.0 s 之后（本组三条一起红）");

                // A-1d ★ 原版那句「Attack+0.4」：反击起点 - 攻击起点 = 0.4（= 命中拍）
                Near(retAt - atkAt, 0.4f, 1e-3f,
                     "A-1d ★ 反击起点 = **Attack + 0.4 s**（B129 三·★15 的实测口径；实 "
                   + (retAt - atkAt).ToString("F3") + "）｜🧨 同上 ⇒ 实测会变 +1.4（晚 1.0 s）");

                // A-1e ★ 反击那一击自己的 `Hit` 也落在**同一拍**（原版两边伤害计数器同一刻弹）
                Near(hit2At, hitAt, 1e-6f,
                     "A-1e ★ 反击那一击的 `Hit` 也落在同一拍（原版两边的伤害计数器同帧）"
                   + "｜🧨 只把 `DurationOf(Retaliation)` 调小而不改 `DelayBetween` ⇒ 本条与 A-1c 红");

                // A-1f 反号检查：这一格必须是**负**偏移（把上一拍的 1.0 s 收回去），不是「多等一会儿」
                Check(EventTiming.DelayBetween(hit, ret) < 0f,
                      "A-1f ★ `DelayBetween(Hit→Retaliation)` 是**负**值（= `-DurationOf(prev)`，把 `Hit` 那 1.0 s 收回去）"
                    + "（实 " + EventTiming.DelayBetween(hit, ret).ToString("F3") + "）"
                    + "｜🧨 写成 `+0.4` 之类 ⇒ 反击反而更晚（A-1c/A-1d 红、本条也红）");
            }
            finally { EventTiming.ReplaySkipAnims = skipWas; }
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  ② A-15 标题字号（判据 = B129 二·★3：原版 `m_fontSize 55` × `capLine 36/pointSize 50`）
        // ═══════════════════════════════════════════════════════════════════════════════════════
        Near(CardChoicePanel.TitleCapPerFontSize, 0.72f, 1e-6f,
             "A-2a 原版字体的**大写高 ÷ fontSize** = `capLine 36 / pointSize 50` = **0.72**"
           + "（⛔ 不是我们自己那个含 SDF padding 的 `CapOverGlyph` 0.8217）");
        Near(CardChoicePanel.TitleCapPxFor(55f), 39.6f, 0.01f,
             "A-2b ★★ 标题的大写高目标 = `m_fontSize 55 × 0.72` = **39.6 px**（B129 二·★3 的实测值）"
           + "｜🧨 把 `TitleCapPxFor` 改回「直接用 55」（= 旧 `SetGlyphHeight(55)` 那条口径）⇒ 本条红（实得 55）");

        // 真渲那一档：同一个 Label 先按旧口径、再按新口径定档 ⇒ 新档的**实际字号必须更小**
        // （旧 55 当汉字墨高 ⇒ cap 45.2；新按 cap 39.6 ⇒ 字号降 ~12%）。
        {
            var root = new GameObject("B145E6 label probe");
            try
            {
                var lb = Label.CreateUgui(root.transform, "选择一张牌", Vector3.zero, 1, Color.white,
                                          new Vector2(0.5f, 0.5f), "probe");
                Check(lb != null, "A-2c（前提）夹具 Label 建出来了（`Label.CreateUgui`；拿不到字体资产时会为 null）");
                if (lb != null)
                {
                    lb.SetGlyphHeight(55f);                 // ← 改前那一条（`SetGlyphHeight(lo.TitlePx)`）
                    float oldPx = lb.FontPxNow;
                    lb.SetCapHeight(CardChoicePanel.TitleCapPxFor(55f));   // ← 改后：按大写高定档
                    float newPx = lb.FontPxNow;
                    Near(lb.CapHeightWorld, 39.6f, 0.01f,
                         "A-2c ★ `SetCapHeight(39.6)` 之后这个 Label 的大写高**就是 39.6 px**（uGUI 单位 = 设计 px）");
                    Check(newPx < oldPx - 0.5f,
                          "A-2d ★ 改后**真的变小了**：旧的 `SetGlyphHeight(55)` 折出字号 " + oldPx.ToString("F2")
                        + " px，新的 `SetCapHeight(39.6)` 折出 " + newPx.ToString("F2") + " px"
                        + "｜🧨 两条口径等价（例如有人把这条改回 `SetScriptHeight(text, 55, 1)`）⇒ 两个数相等、本条红");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  ③ A-16 「确认」字右对齐（判据 = B129 二·★4：原版框 `372.857×43.622`、`m_HorizontalAlignment = 4(Right)`）
        // ═══════════════════════════════════════════════════════════════════════════════════════
        Near(CardChoicePanel.ConfirmBoxW, 372.857f, 1e-3f,
             "A-3a 原版「确认」字那颗 TMP 的**框宽** = `RectTransform_2741` 的 `m_SizeDelta.x` = 372.857");
        Near(CardChoicePanel.ConfirmRightEdgePx(1519f), 1705.4285f, 0.01f,
             "A-3b ★★ 字右沿 = 框心 1519 + 框宽/2 = **1705.43**（B129 二·★4 的实测右沿）"
           + "｜🧨 改回把节点钉在框心（`ConfirmCx`）⇒ 本条红（实得 1519）");
        Near(CardChoicePanel.ConfirmTextAnchor.x, 1f, 1e-6f,
             "A-3c ★ 「确认」字的锚点 x = **1（右对齐）** —— 原版 `m_HorizontalAlignment = 4 (Right)`"
           + "｜🧨 改回 `(0.5, 0.5)`（居中）⇒ 字右沿回到 ≈1574、本条红");
        Check(CardChoicePanel.ConfirmRightEdgePx(1519f) > 1519f + 180f,
              "A-3d ★ 右沿在框心**右侧** 186.4 px（= 半个框宽）—— 改前那个居中锚正是把这 120–130 px 丢了");

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  ④ A-17 多卡展示窗标题命中框（判据 = B129 二·#32：原版 `Header Text` 节点 rect 1192.37×63.204）
        // ═══════════════════════════════════════════════════════════════════════════════════════
        {
            var sz = MultiCardDisplay.HeaderHitSizePx;
            var ctr = MultiCardDisplay.HeaderHitCenterPx;
            Near(sz.x, 1192.37f, 1e-3f,
                 "A-4a ★★ 标题命中框宽 = 原版节点 rect **1192.37 px**（⛔ 不是 TMP 的文字 bbox）"
               + "｜🧨 把命中框建成「文字尺寸」/ 删掉 `BuildHeaderHitRect` ⇒ 本条红");
            Near(sz.y, 63.204f, 1e-3f,
                 "A-4b ★ 标题命中框高 = 原版节点 rect **63.204 px**"
               + "｜🧨 同上 ⇒ 红");
            Near(ctr.x, 960f, 0.01f, "A-4c 命中框横向居中（x = 960；原版 `anchor(0.5,1)`）");
            Near(ctr.y, MultiCardDisplay.BandTopPx + 63.204f * 0.5f, 1e-3f,
                 "A-4d 命中框中心 = 窗口带顶边 131 + 框高/2 = 162.602（原版 `pivot(0.5,1)` 顶边锚）");
            Check(sz.x > 900f && sz.x < 1400f,
                  "A-4e ★ 命中框是**一整条**（> 900 px），远宽于任何标题文字 bbox ⇒ 原版「点标题不关窗」那一整片都挡住"
                + "｜🧨 改回只让 TMP 自己吃射线 ⇒ 本条与 A-4a 红");
        }

        Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }
}
