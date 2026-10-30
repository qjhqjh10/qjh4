// B101R4_Assert.cs — 第一百零一会话 · 执行代理 `B101_R4`（手牌卡画出徽标层 —— 形态闸）
//
// ============================ 它盯的是什么 ============================
//  **缺陷**：`CardView.SetBadges` 没有形态闸 ⇒ 一张按 `CardFace.Hand` 出生的**单位卡**，
//  只要 `CardData.badges` 非空就会把 7 个徽标位点亮（层挂在 `2DCard`/`FaceRoot` 上）。
//  而手牌的 `Data.badges` **非空是常态**：`BattleDriver.cs:27381` 用手牌的唯一创建入口
//  （`CardFace.Hand`），`:28059` 的 `ToCardData` 用 `Badges.For(u.Keywords, …)` 走默认筛选
//  （47 条 in-play 图标键：`Armour` / `Flying` / `Blast` 都在内）。
//
//  **原版判据（三条，缺一不可）**：
//   ① 组件：`BattleCardUI.boardTraitIcons[7]`（`Warpforge_code/…/BattleCardUI.cs:226`）—— 战斗卡专属。
//   ② 几何：7 个位**物理上挂在 `3DBody` 下**
//      （`Core/Badges.cs:29` 的父链 `TraitIconContainer ← TraitIcons ← 3DBody ← Board Elements ← CardPrefab`）。
//   ③ 可见性：手牌档 `BattleCardUI.SetObjectVisibility` 会 `Card2DController.SetCardVisibility(card2D, 0)`
//      + **`ToggleBody3D(0)`** 把 `3DBody` 关掉
//      （`d:/2/tools/decomp_full/BattleCardUI__SetObjectVisibility.c`，手牌·我方档 `bVar7 = 0`）。
//      🔴 **同一档里 `UpdateTraitIcons` 照样会跑**（同文件尾部 `if (bVar6) UpdateTraitIcons(...)`）
//      ⇒ **原版是「父节点被关」，不是「不刷新」**。这正是 `Badges.cs:73` 找遍
//      `ToggleTraitIcons` / `FadeAllTraitsIcons` 都找不到调用者的原因 —— 根本没有那个调用者。
//
//  我们的层挂在 `2DCard`（`FaceRoot`；坐标是 3DBody 空间，见 `CardView.cs:3904-3911`）
//  ⇒ **不继承那个关闭状态** ⇒ 在 `SetBadges` 里补同一效果（结构等价、语义一致，⛔ 不是自设计）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · A1 / A1b：删掉 `SetBadges` 里的 `&& badgeFaceVisible`（`CardView.cs` 的
//        `_badgePlates[i].enabled = on && badgeFaceVisible;` / `_badgeIcons[i].enabled = on && badgeFaceVisible;`）
//        ⇒ 手牌档又点亮 ⇒ A1 / A1b 红。
//  · A2 / A2b：把闸写成**永假**（`bool badgeFaceVisible = false;`）或忘了在 `SetFace` 那条路上生效
//        ⇒ `SetFace(CardFace.Board)` 之后仍是 0 ⇒ A2 / A2b 红（**与 A1 结构上不可能同时被「闸写反」解释**）。
//  · A3：把闸塞进 `UpdateFigures()` 而不是 `SetBadges` ⇒ 出生入口那一条（`Build` → `UpdateFigures`）
//        仍绿，但 A1 里那句 `SetBadges(...) == 0`（直接调口）会红 —— 两条一起钉住「闸在哪个函数里」。
//  · A4：把闸写成 `_faceMode != CardFace.Hand` ⇒ `Full` 档变 2 ⇒ A4 红
//        （原版 `CardUI`（菜单/HUD 卡）**没有** `boardTraitIcons` 这一族组件 ⇒ 一位都不该有）。
//  · P3（层照建）：把实现改成「非场上就不 `BuildBadgeSlot`」⇒ P3 红（`BadgeTexture(0)` 取不到）
//        —— 这条挡的是「为了过 A1 而把层整族删掉」那种假修法（会连带打红 `Editor/CardBaseDemo.cs:2431`）。
//  · A5：把闸写成「一律 `SetBadges(null)`」⇒ 场上档也清空 ⇒ A2 / A3 / A5 全红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/BattleScene.cs` 的聚合行里，`B100F9_Assert.Run();` 之后**同一物理行**追加：
//      B101R4_Assert.Run(); pass += B101R4_Assert.Pass; fail += B101R4_Assert.Fail;
//  （⚠️ 徽标三件的既有探针 `BattleScene.cs:3426` / `CardBaseDemo.cs:6250` 是否需要改 `CardFace.Board`，
//    见 `报告/B101_R4_演出与预览判据.md` §六 的「宿主改动清单」—— 那两份归调度台。）
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B101_R4_演出与预览判据.md` §二 / §六
//  · 原版：`BattleCardUI__SetObjectVisibility.c` · `BattleCardUI__UpdateTraitIcons.c` · `Card2DController__Toggle.c`
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B101_R4`：徽标层的**形态闸** —— 只有 `CardFace.Board` 那一档画。</summary>
public static class B101R4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B101R4] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>两枚徽标夹具（`armour` / `blast` 在 `CardArt.Trait` 里都有图 —— 前提断言先硬查）。</summary>
    static List<Badge> TwoBadges()
    {
        return new List<Badge>
        {
            new Badge { sprite = "armour", counter = 2, active = true },
            new Badge { sprite = "blast",  counter = 3, active = true },
        };
    }

    /// <summary>带两枚徽标的探针数据（⛔ 不设 `faction`：走的是「无卡框」那条兜底，与徽标无关）。</summary>
    static CardData BadgeProbeData()
    {
        var d = CardData.Simple("BadgeFaceProbe", 3, 3, 4);
        d.badges = TwoBadges();
        return d;
    }

    public static void Run()
    {
        // ---- 前提（硬 Check，⛔ 不写成 `if (x != null) Check(...)`：那就成了静默空转绿）----
        var tArmour = CardArt.Trait("armour");
        var tBlast = CardArt.Trait("blast");
        Tr(tArmour != null && tBlast != null,
           "★ 前提：两枚徽标图取得到（`CardArt.Trait(\"armour\")` / `(\"blast\")`）——"
         + " 取不到时 `BuildBadgeSlot` 整位不建，下面所有「0 == 0」都会变成空转");
        Tr(CardArt.Ui("Base3d_Trait_Background") != null,
           "★ 前提：徽标底板图取得到（`CardArt.Ui(\"Base3d_Trait_Background\")`）—— 缺它 `BuildBadgeSlot` 同样整位不建");

        var root = new GameObject("B101R4_badge_face_probe");
        try
        {
            // ============================================================
            //  A. 手牌档（`CardFace.Hand`）—— **一位都不许画**
            // ============================================================
            var hv = CardView.Create(root.transform, BadgeProbeData(), "hand_badge_probe", CardFace.Hand);
            Tr(hv != null, "★ 前提：手牌档的探针卡建出来了（`CardFace.Hand`）");
            if (hv != null)
            {
                // P3：闸必须是「关可见」而不是「不建层」—— 层建出来但 `enabled == false`
                //     （这条同时挡住「为了过 A1 而把 `BuildBadgeSlot` 整段跳过」那种假修法）
                Tr(hv.BadgeTexture(0) != null,
                   "★ 前提：手牌档的徽标位**确实建出来了**（`BadgeTexture(0)` 非空）——"
                 + " 它只是被形态闸关着；⛔ 删 `BuildBadgeSlot` 那种「假修法」会让本条红");

                C(hv.BadgesShown, 0,
                  "A1 手牌档（`CardFace.Hand`）**一位徽标都不画**"
                + "｜🧨 删掉 `SetBadges` 里两处 `&& badgeFaceVisible` ⇒ 本条红"
                + "（原版：7 个位挂在 `3DBody` 下，手牌档 `ToggleBody3D(0)` 把它关掉）");

                C(hv.SetBadges(TwoBadges()), 0,
                  "A1b 同一个口直接调 `SetBadges(list)` 也回 **0**（返回值语义 =「真正画出来的个数」）"
                + "｜🧨 只改 `enabled` 不改 `:3719` 的返回值 ⇒ 本条红");

                // ============================================================
                //  B. 灭自证：同一张卡换到场上形态 ⇒ 两枚都得出来
                //     （与 A1 结构上不可能被「闸写反 / 闸永假」同时满足）
                // ============================================================
                hv.SetFace(CardFace.Board);
                C(hv.BadgesShown, 2,
                  "A2 同一张卡 `SetFace(CardFace.Board)` 之后**两枚都出来**"
                + "（灭自证：与 A1 不可能被同一个「闸写反」解释）"
                + "｜🧨 `bool badgeFaceVisible = false;` 或闸没在换形态那条路上生效 ⇒ 本条红");
                C(hv.SetBadges(TwoBadges()), 2, "A2b 场上档 `SetBadges(list)` 回 **2**");
                C(hv.BadgesShown, 2, "A2c 换形态之后 `BadgesShown` 与 `SetBadges` 的返回值同源（都 2）");

                // 数据清空 ⇒ 场上档也一位不剩（既有语义不许被这道闸破坏）
                var empty = hv.Data;
                empty.badges = null;
                hv.SetData(empty);
                C(hv.BadgesShown, 0,
                  "A5 场上档把数据清空 ⇒ 一位都不剩（形态闸**不改变**原有的「按数据派生」语义）");
            }

            // ============================================================
            //  C. 出生入口：**直接按 `CardFace.Board` 建**的卡也要画
            //     （闸若只挂在 `SetFace` 那条路上，这里会红）
            // ============================================================
            var bv = CardView.Create(root.transform, BadgeProbeData(), "board_badge_probe", CardFace.Board);
            Tr(bv != null, "★ 前提：场上档的探针卡建出来了（`CardFace.Board`）");
            if (bv != null)
                C(bv.BadgesShown, 2,
                  "A3 出生入口（`Build` → `UpdateFigures`）在场上档照画两枚"
                + "｜🧨 把形态闸只写进 `SetFace` 而漏了 `UpdateFigures`/`SetBadges` ⇒ 本条红");

            // ============================================================
            //  D. 反方向：`CardFace.Full`（菜单 / HUD 的 `CardUI`）**一位都不该有**
            //     原版那一族 prefab 上根本没有 `BattleCardUI.boardTraitIcons`（组件是战斗卡专属）。
            // ============================================================
            var fv = CardView.Create(root.transform, BadgeProbeData(), "full_badge_probe", CardFace.Full);
            Tr(fv != null, "★ 前提：菜单档的探针卡建出来了（`CardFace.Full`）");
            if (fv != null)
                C(fv.BadgesShown, 0,
                  "A4 菜单/HUD 档（`CardFace.Full`）**一位都不画**"
                + "（原版 `CardUI` 没有 `boardTraitIcons` 这一族组件）"
                + "｜🧨 把闸写成 `_faceMode != CardFace.Hand` ⇒ 本条红");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
