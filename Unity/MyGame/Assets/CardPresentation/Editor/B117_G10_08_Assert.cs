// B117_G10_08_Assert.cs — 执行代理 `B117-G10-08`（战斗域第 8 部分「目标选择」）的断言宿主（2026-11-03）
//
// 覆盖两件事（判据全部现读解包原始资产，逐条写在每组头里）：
//   A. **主动技能钮的数字角标**（原版 `Drag Attack Selector/Select Active Skill Button/ValueText`）：
//      字号 / 颜色 / 位置三样原来**全都没有出处**（字号从来没定过、颜色是暖白、位置低了 6.8 px）。
//      判据 → `bundle_scenes_scenes_battlearena1` 的 `MonoBehaviour_3755.json`（TMP）·
//      `MonoBehaviour_4514.json`（size fitter）· `RectTransform_2617.json`（父 `RT_2626`）。
//   B. **可点区域 = 可射线件的【并集】**（原版钮本体 `Image.m_RaycastTarget = 1`（118.372×118.806）
//      ∪ 子件 `Highlight`（176.23×176.875，**只在「已选打法」那一格 `SetActive(true)`**））——
//      这正是一条反复踩过的坑（⛔ 不是「`m_TargetGraphic` 那一处」、也不是「以钮心为圆心的圆」）。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_G10_08_Assert.Run -logFile -
//
// 判绿红：逐条按**行首** `B117G1008 ✓` / `B117G1008 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主状态要求（给调度台挂进 `Editor/BattleScene.cs` 时看）：
//   **零前提** —— 不读 `BattleDriver` / 场景 / 存档；自带临时根、收尾 `DestroyImmediate`；
//   建的是**世界档**（不登记宿主画布 ⇒ `BuildInWorld` 那条路），断的几何两档同源（都走 `U()`/`W()`）。
//   ⚠️ 本文件是**新文件**，`.meta` 要等下一次 Unity 跑起来才生成。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class B117_G10_08_Assert
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1008 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1008 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(!float.IsNaN(got) && Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
                  + "；实得 " + got.ToString("F4") + "）");
    }

    // ── 原版真值（判据出处见文件头 A） ────────────────────────────────────────────────
    const float ValueFontPx = 89.01f;      // `MB_3755.m_fontSize`（autosize 上限 = 89.01 / base 36）
    const float ValueDx = 0.452f;          // `RT_2617`: (−59.186 + 59.638)
    const float ValueDy = -1.205f;         // `RT_2617`: (+59.403 − 60.608)
    const float ButtonW = 118.372f, ButtonH = 118.806f;   // `RT_3368/2804/2626`
    const float RingW = 176.23f, RingH = 176.875f;        // `RT_3028/2776/3512`

    /// <summary>零前提入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;

        var root = new GameObject("B117G1008_Root");
        try
        {
            var sel = root.AddComponent<AttackSelector>();
            sel.Build();                       // 批处理没有 Awake ⇒ 显式建（同 `Editor/BattleScene.cs` 的做法）

            var opts = new List<AttackSelector.Option>
            {
                new AttackSelector.Option { Kind = AttackKind.Melee,  Enabled = true },
                new AttackSelector.Option { Kind = AttackKind.Ranged, Enabled = true },
                // 角标只有**主动技能**那一格有（原版 `buttonIconValue` 近战/远程是 null）
                new AttackSelector.Option { Kind = AttackKind.Ability, Enabled = true, Badge = "3" },
            };
            sel.Show(opts, null, null, Vector3.zero, 0);

            if (!sel.Visible) { Check(false, "前提：`Show()` 之后选择器应当是可见的（下面的断言全依赖它）"); return; }

            A_ValueText(sel);
            B_HitUnion(sel);
        }
        catch (System.Exception e)
        {
            Check(false, "异常：" + e.GetType().Name + " " + e.Message);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        Debug.Log("B117G1008 === 合计 " + _pass + " / " + _fail + " ===");
    }

    // ==========================================================================================
    //  A · 技能钮的数字角标（原版 `ValueText`）
    //  新口径：`FontPxNow ≈ 89.01`（白、居中）。三条**各自独立**的判别式：
    //    · 字号 —— 🧨 删掉 `SetBadgeSizeOnce` 那一句 ⇒ 落回 `Label` 的 `scale = 4` 默认档（≈30 px）⇒ 红；
    //    · 颜色 —— 🧨 改回原来的暖白 `(1, 0.96, 0.80)` ⇒ 红；
    //    · 位置 —— 🧨 改回原来的 `(0, −8)` ⇒ y 差 6.8 px ⇒ 红（x 差 0.452 也一起抓）。
    // ==========================================================================================
    static void A_ValueText(AttackSelector sel)
    {
        var badge = sel.BadgeForTest(AttackKind.Ability);
        var capM = sel.BadgeForTest(AttackKind.Melee);
        var capR = sel.BadgeForTest(AttackKind.Ranged);
        if (badge == null || capM == null || capR == null)
        { Check(false, "A0 前提：三格的角标标签都应当建出来了（缺件 = TMP 字体资产没加载）"); return; }

        CheckClose(badge.FontPxNow, ValueFontPx, ValueFontPx * 0.20f,
                   "A1 角标字号 = 原版 `ValueText.m_fontSize 89.01`（画布 px 口径，容差 20%）");
        Check(badge.color == Color.white,
              "A2 角标颜色 = 纯白（原版 `m_fontColor32 = 0xFFFFFFFF`；实得 " + badge.color.ToString() + "）");

        var off = sel.BadgeOffsetPxForTest(AttackKind.Ability);
        CheckClose(off.x, ValueDx, 0.5f, "A3a 角标 x 偏移（原版 `RT_2617` 换算到钮心 = +0.452）");
        CheckClose(off.y, ValueDy, 0.5f, "A3b 角标 y 偏移（原版 = −1.205；旧实现是 −8）");
        Check(Mathf.Abs(off.x) <= ButtonW * 0.5f && Mathf.Abs(off.y) <= ButtonH * 0.5f,
              "A4 角标落在钮框内（|dx| ≤ " + (ButtonW * 0.5f).ToString("F1") + " · |dy| ≤ "
            + (ButtonH * 0.5f).ToString("F1") + "；实得 " + off.ToString("F2") + "）");

        // 近战 / 远程**没有**角标（原版 `SetButtonValue` 只对主动技那一格调过）
        Check(!capM.gameObject.activeSelf, "A5a 近战那格**没有**数字角标（原版 `buttonIconValue` = null）");
        Check(!capR.gameObject.activeSelf, "A5b 远程那格**没有**数字角标（同上）");
        Check(badge.gameObject.activeSelf, "A5c 技能那格**有**数字角标（`Badge = \"3\"`）");
    }

    // ==========================================================================================
    //  B · 受体几何：钮矩形（118.372 × 118.806）∪ 圈矩形（176.23 × 176.875）
    //  判据：`RT_3368/2804/2626` 的 `Image.m_RaycastTarget = 1` ∪
    //        `RT_3028/2776/3512` 的 `Highlight`（`CardDisplayAttackTypeButton__HighlightIfNeeded.c`
    //        第一句 `SetActive(highlightImage, param_2 == attackType)` ⇒ 圈只在已选那格亮）。
    //  🔴 2026-11-03（`B124-W29`）**改回原版**：`KindAtPoint` = **受体查询**，按原版 `Highlight` 的
    //      `m_RaycastTarget = 1` + 已选那格 `SetActive(true)` ⇒ **圈亮的那格确实是靶**；「同档不重复下单」那条
    //      闸已挪回点击消费者（`BattleDriver.CommitCommand` 的 `if (kind == _command) return;`）。
    //  判别式：
    //    · 🧨 把 `KindAtPoint` 的命中改成「以钮心为圆心、直径 118.8 的圆」⇒ B2（两钮空隙）/B3（钮下方 100 px）可能红；
    //    · 🧨 删掉 `KindAtPoint` 里 `IsPickedSlot(opt) && _rings[i]…HitPiece(RingW,RingH)` 那第二句 ⇒ B4 变 None 红
    //      （W28 曾把它改钉成「圈亮 ≠ 可点」，本轮按裁定改回「圈亮 ⇒ 命中该格」；「点了能不能生效」由点击口另测）；
    //    · 🧨 把 `SetSlotActive` 的 `&& IsPickedSlot(opt)` 去掉 ⇒ B6 红。
    // ==========================================================================================
    static void B_HitUnion(AttackSelector sel)
    {
        // C 组：两条尺子先钉住（否则 B4 的「钮外 / 圈内」立不住）
        var bs = sel.ButtonNodeSizePx(AttackKind.Melee);
        CheckClose(bs.x, ButtonW, 0.05f, "C1a 钮节点宽 = 原版 `RT_3368.m_SizeDelta.x`");
        CheckClose(bs.y, ButtonH, 0.05f, "C1b 钮节点高 = 原版 `RT_3368.m_SizeDelta.y`");
        var rs = sel.HighlightNodeSizePx(AttackKind.Melee);
        CheckClose(rs.x, RingW, 0.05f, "C2a 圈节点宽 = 原版 `RT_3028.m_SizeDelta.x`");
        CheckClose(rs.y, RingH, 0.05f, "C2b 圈节点高 = 原版 `RT_3028.m_SizeDelta.y`");
        Check(rs.x > bs.x && rs.y > bs.y,
              "C2c 圈比钮大（" + rs.ToString("F1") + " > " + bs.ToString("F1") + "）—— 原版悬停区就是这样外扩的");

        // 先把打法切回「没选过」那一档：三格圈全关，命中区 = 纯钮矩形
        sel.SetSelected(AttackKind.None);

        Vector3 melee, ability, ranged;
        {
            var a = sel.ButtonWorld(AttackKind.Melee);
            var b = sel.ButtonWorld(AttackKind.Ability);
            var c = sel.ButtonWorld(AttackKind.Ranged);
            if (!a.HasValue || !b.HasValue || !c.HasValue)
            { Check(false, "B0 前提：三颗钮都建出来了（`ButtonWorld` 拿不到 = 贴图缺 / 那一格没显示）"); return; }
            melee = a.Value; ability = b.Value; ranged = c.Value;
        }

        Check(sel.KindAtPoint(melee) == AttackKind.Melee, "B1a 钮心 ⇒ 命中近战");
        Check(sel.KindAtPoint(ability) == AttackKind.Ability, "B1b 钮心 ⇒ 命中技能");
        Check(sel.KindAtPoint(ranged) == AttackKind.Ranged, "B1c 钮心 ⇒ 命中远程");

        // 空隙：两钮**边缘之间**的 37 px 带（中心距 155.372 ⇒ 钮外 59.2、另一钮外 59.2）
        Vector3 gap = (melee + ability) * 0.5f;
        Check(sel.KindAtPoint(gap) == AttackKind.None,
              "B2 两钮之间的空隙**不算**受体（原版那里没有可射线件）");

        // 钮下方 100 px：既在钮外（59.4）、也在圈外（88.4）
        float px = sel.WorldOfPxForTest(100f);
        Check(sel.KindAtPoint(melee + Vector3.down * px) == AttackKind.None,
              "B3 钮下方 100 px 不算受体（旧实现那个「直径 118.8 的圆」也在这里，圆外同样不该命中）");

        // ---- 选中近战 ⇒ 那一格的圈亮起来（圈亮 ⇒ 受体跟着变大：原版 `Highlight.m_RaycastTarget = 1`，见下面 B4）----
        sel.SetSelected(AttackKind.Melee);
        // `AttackSelector.Order = { Melee, Ability, Ranged }` ⇒ 下标就是这三格
        const int iMelee = 0, iAbility = 1, iRanged = 2;
        Check(sel.HighlightActiveForTest(iMelee), "B6a 已选那一格的圈亮着");
        Check(!sel.HighlightActiveForTest(iAbility), "B6b 没选中那格（技能）的圈是关的（原版 `SetActive(param_2 == attackType)`）");
        Check(!sel.HighlightActiveForTest(iRanged), "B6c 没选中那格（远程）的圈是关的");

        // 钮外 75 px、圈内（|75| < 88.44）
        // ---- 🔴 2026-11-03（`B124-W29`）**就地改回原版**（铁律 5）----
        // W28 曾把本条改钉成「圈亮 ≠ 可点」（判据 = 当时 `AttackSelector.Selectable()` 里的
        // `if (opt.Kind == _selected) return false;`）。2026-11-03 调度台按铁律 18·0 裁定：
        // 那句在原版是 `UnitOnBoardAttackTypeSelector__AttackButtonClick.c` 第一句 = **点击消费者**的闸，
        // 不在受体查询里；产品侧已把 `Selectable()` 那句删掉（消费口 `BattleDriver.CommitCommand`
        // 早已有 `if (kind == _command) return;`，`B117-R9` 补丁③）。
        // ⇒ 受体查询侧按原版**不筛已选项**：圈亮的那格（`Highlight`，`m_RaycastTarget = 1` + `SetActive(true)`）
        // 确实是靶 ——「圈放大命中区」重新可观测。
        // 「点了能不能生效」由点击口（`CommitCommand` 的同档闸）另测；本文件只测受体几何。
        // 🔑 判别式 = 删掉 `KindAtPoint` 里圈那半句（`IsPickedSlot(opt) && … HitPiece(RingW, RingH)`）⇒ 本行变 None。
        float up75 = sel.WorldOfPxForTest(75f);
        Check(sel.HighlightActiveForTest(iMelee),
              "B4a（前提）已选那格的圈**亮着**（原版那颗 `m_RaycastTarget = 1` 的 `Highlight`；几何见 C2c）");
        var at75 = sel.KindAtPoint(melee + Vector3.up * up75);
        Check(at75 == AttackKind.Melee,
              "★★ B4 已选那格「钮外 75 px、圈内」**算受体**（原版 `Highlight.m_RaycastTarget = 1`；圈亮 ⇒ 命中该格）"
            + "｜🧨 删掉 `KindAtPoint` 里 `IsPickedSlot(opt) && …HitPiece(RingW, RingH)` 那半句 ⇒ 本行变 None（红）"
            + $"（实得 {at75}｜up75 = {up75:F5} 世界单位）");

        // 同一地点在「没选中」那一档必须**不**命中 —— 这一对就是 B4 的判别式
        sel.SetSelected(AttackKind.None);
        Check(sel.KindAtPoint(melee + Vector3.up * up75) == AttackKind.None,
              "B5 同一地点在「没选中」那一档不算受体（圈关着 ⇒ 不吃射线）");
    }
}
