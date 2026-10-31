// C4_Assert.cs — 第一百零九会话 · 执行代理 `C4`（2026-10-31）
//
// ============================ 它盯的三件 ============================
//  A（item ②）`Random Card Drawer ▸ Content/Random Image/Icon` 的 **`m_RaycastTarget = 1`** 等价物：
//     在同一格那颗 `Hit` 节点的子树里建一颗**透明** `ImageQuad`（`Shell/ItemDrawerPartial_C4.cs`），
//     **可点区并集不变**（它整个落在整格框里 ⇒ `PointerLayer.UnionOtherQuads` 只扩不缩 ⇒ 不变）。
//     判据 = `报告/B109_E18.md` §一（prefab 实读：`Icon` 那颗 `Image` 的 `m_RaycastTarget 1`）+ 该报告 §六-3。
//  B（item ③）`40k_UI_bt_back_pressed` 进了 `Resources/`（88×87 · md5 `488515657a513ffa5432c9de0455c311`）。
//     判据 = `报告/B109_E24.md` §四-5（原先只在 `Art/原版/0_mainmenu/`）。
//  C（item ①）`AvatarDisplay`：**不是抽屉档**（不在 `ItemDrawerConfig`，工厂永远选不到；`AvatarDisplay__Draw`
//     零调用点）⇒ 不登记 `DrawerImpls`；其语义落在 `AvatarTab` 自绘口径。本轮把其中**唯一没做实的半跳**
//     —— `ChangeBorder` 的 **sprite 重设** —— 补上（`AvatarTab.ChangeBorder`），并钉住「大图边框跟着已装备那条走」。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A6：把 `AttachC4IconRaycast` 里的矩形换成 `keepAspect` 之后那块画心（宽 171.69）⇒ A5 的 x1/x2 红。
//  · A8：把射线靶**挪到整格框外**（例如矩形改成 (2000,0)-(2100,100)）⇒ A8 红（并集变大）。
//  · A9：把 tint 改成不透明（`new Color(0,0,0,1)`）⇒ A9 红（并且真机上会看到一块黑）。
//  · A10（并集机制自身的灭自证）：临时钮子树里加一块**框外** quad ⇒ 并集**必须**变大；这条红了说明
//     A8「不变」是假绿（机制根本没在工作）。
//  · A11：把 `RandomCard` 的 `army == 0` 判断去掉 ⇒ 非中立档也会建问号（原版不会）⇒ A11 红。
//  · C2：把 `AvatarTab.Refresh` 里的 `ChangeBorder()` 删掉 ⇒ 大图边框仍贴着下标 0 那张 ⇒ C2/C3 红。
//  · C4：`AvatarTab.ChangeAvatar` 不按 `sprite != null` 关层 ⇒ C4 红。
//  · D1：有人把 `"AvatarDisplay:"` 加进 `ItemDrawer.DrawerImpls` ⇒ D1 红（那会造出零调用死档）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  唯一入口 = `C4_Assert.Run()`；建议宿主 `Editor/MainMenuScene.cs`（C 段要一个**建好的** `AvatarTab`，
//  它挂在 `Player Profile Window` 上，只有 `MainMenuScene` 一族会建），在该宿主那条聚合行**同一物理行、
//  第一个 `//` 之前**追加：
//      C4_Assert.Run(); pass += C4_Assert.Pass; fail += C4_Assert.Fail;
//  ⚠️ 若接进别的宿主：A/B/D 段照常跑；C 段会在「找不到建好的 `AvatarTab`」时报一条前提红（⛔ 不静默跳过）。
//
// ============================ 边界（如实记）============================
//  · `AvatarDisplay.ChangeAvatar` 的 `name = "Avatar Display " + LocalizedName`（改 GameObject 名）**没做** ——
//    本页节点名是自检/账的结构词汇，改名零玩家可见效果却会打断锚点（详见 `Shell/AvatarTab.cs` 类尾那段）。
//  · `IsAvatarLocked` / `ToggleLock` 仍是**全解锁口径下的 D**（判据 + 裁定 → 账 `个人资料_成就/排行:71/80`），
//    本轮**没有**造那两条死分支；C 段只把「大图高亮恒关」这一格断出来。
//  · item ③ 的**接线**在 `Shell/DraftModeWindow.cs`（`pressedArt: null` 那一声）—— 该文件**不在本代理白名单**
//    ⇒ 只交补丁（见报告），本件只验图真的取得到。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `C4`：`AvatarDisplay` 口径 / 随机卡问号 raycast / `40k_UI_bt_back_pressed`。</summary>
public static class C4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[C4] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>随机卡抽屉根的设计尺寸（`ItemDrawer.DrRootW/H`；E18 那件也用这一组）。</summary>
    const float BoxW = 712.8f, BoxH = 1080f;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 C4：AvatarDisplay 口径 / 随机卡问号 raycast / back_pressed 图");

        SectionA_IconRaycast();
        SectionB_BackPressed();
        SectionC_AvatarDisplay();
        SectionD_NotADrawer();

        Debug.Log(_sink.P + "C4 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 随机卡问号的 raycast 位
    static void SectionA_IconRaycast()
    {
        MenuCheck.Section(_sink, "A `Content/Random Image/Icon` 的 `m_RaycastTarget = 1`（等价物 = 钮子树里的透明 quad）");

        var box = new PxRect(0f, 0f, BoxW, BoxH);
        var root = new GameObject("C4_RANDOMCARD_RAY_ROOT");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);
            st.NamePx = 0f; st.QuantityPx = 0f;          // 文字层不参与本项
            var spec = new ItemSpec
            {
                Id = "C4_RandomCardRay",
                Type = "DropTableItem",                  // ⇒ `PickDrawer` 落到 `RandomCardDrawer`
                Kind = ItemKind.Generic,
                Rarity = 0,
                CardArmy = 0,                            // Neutral ⇒ 画 `Random Image` + 子件 `Icon`
                Label = "C4",
            };
            res = ItemDrawer.Draw(root.transform, box, spec, 1, DrawerOverride.Default, st);

            Tr(res.Node != null, "A0 **前提**：随机卡抽屉的根建出来了");
            C(res.Drawer, ItemDrawer.DrawerRandomCard, "A0b **前提**：路由到 `RandomCardDrawer`");
            if (res.Node == null || res.View == null) return;

            // 前提：`RandomCardDrawer` 是混合档（`ShowInfoOnClickOf` 给 0）⇒ 默认**没有**命中层，
            //       「raycast 位」也就没有可挂靠的钮（如实记：本仓「不可点 = 连命中层都没有」）。
            Tr(!res.View.ShowInfoOnClick && res.View.HitNode == null,
               "A1 **前提**：默认不可点（混合档 0）⇒ 没有 `Hit` 节点（原版由调用方逐实例给 1）；本项先置 1 再验");
            res.View.SetShowInfoOnClick(true);

            var hit = res.View.HitNode;
            Tr(hit != null, "A2 ★ 置 `ShowInfoOnClick(true)` ⇒ 整格命中区 `Hit` 建出来了");
            if (hit == null) return;
            var wb = hit.GetComponent<WindowButton>();
            Tr(wb != null, "A2b **前提**：`Hit` 节点上有 `WindowButton`（`PointerLayer` 只认它）");
            if (wb == null) return;

            var ray = hit.Find(ItemDrawerView.NodeIconRaycast);   // `MenuDraw.Rect(HitNode, …)` 建的是**直接子件**
            Tr(ray != null, "A3 ★ 问号的射线靶 `" + ItemDrawerView.NodeIconRaycast + "` 建出来了（挂在整格 `Hit` 节点下）"
                          + "｜🧨 删掉 `AttachC4IconRaycast` 的调用 ⇒ 红");
            if (ray == null) return;
            var rq = ray.GetComponent<ImageQuad>();
            Tr(rq != null, "A3b **前提**：它是一颗 `ImageQuad`（`UnionOtherQuads` 只认这个类型）");
            if (rq == null) return;
            Tr(ray.gameObject.activeInHierarchy, "A3c ★ 它在激活链上（原版 `m_RaycastTarget 1` 的等价前提：停用件不进并集）");

            // A4/A5 · 矩形 = `Icon` 的**布局框**（原版射线读 `rectTransform`，与 `preserveAspect` 无关）
            var want = ItemDrawer.RandomCardIconRayRect(box);
            var got = MenuDraw.QuadRectPx(rq);
            N(got.x1, want.x1, 0.5f, "A4a ★ 射线靶 x1 = " + want.x1.ToString("F2") + "（`Icon` 布局框左沿）"
              + "｜🧨 改用 `keepAspect` 画心 ⇒ 142.8 → 270.55 ⇒ 红");
            N(got.y1, want.y1, 0.5f, "A4b ★ 射线靶 y1 = " + want.y1.ToString("F2"));
            N(got.x2, want.x2, 0.5f, "A4c ★ 射线靶 x2 = " + want.x2.ToString("F2")
              + "｜🧨 改用画心 ⇒ 570.0 → 442.25 ⇒ 红");
            N(got.y2, want.y2, 0.5f, "A4d ★ 射线靶 y2 = " + want.y2.ToString("F2"));
            N(want.W, 427.2f, 0.5f, "A5 布局框宽 = 427.2（父件 712×540 的 60%；⛔ 不是画心 171.69）");
            N(want.H, 324f, 0.5f, "A5b 布局框高 = 324（父件 540 的 60%）");

            // A6 · 结构不变式：它整个落在整格框里（这就是「并集不变」的来源）
            Tr(want.x1 >= box.x1 && want.y1 >= box.y1 && want.x2 <= box.x2 && want.y2 <= box.y2,
               "A6 ★ `Icon` 布局框 ⊆ 整格框（" + box.W.ToString("F1") + "×" + box.H.ToString("F1") + "）"
             + " ⇒ 把它并进本钮的命中区**不会**扩边");

            // A7 · 「一颗钮」：它是这件事那颗钮的子件（不是另建一颗能点的钮）
            Tr(ray.IsChildOf(wb.transform), "A7 ★ 射线靶在**整格那颗钮**的子树里（⛔ 不是另建一颗钮）"
              + "｜🧨 改成 `MenuDraw.Hit(...)` 单独建钮 ⇒ 点问号什么都不发生（原版会冒泡到抽屉根）⇒ 红");

            // A8 · 可点区并集不变（口径 = `PointerLayer.HitBoxForTest`，生产命中用的同一份）
            Vector2 c, half; ImageQuad primary;
            bool ok = PointerLayer.HitBoxForTest(wb, out c, out half, out primary);
            Tr(ok && primary != null, "A8a **前提**：这颗钮可命中（`HitBoxForTest` 给出命中区与命中用 quad）");
            if (ok && primary != null)
            {
                var pr = MenuDraw.QuadRectPx(primary);
                N(c.x - half.x, pr.x1, 1.0f, "A8b ★ 命中区并集 x1 = 整格框 x1（= " + pr.x1.ToString("F2") + "，**没被问号扩出去**）");
                N(c.x + half.x, pr.x2, 1.0f, "A8c ★ 命中区并集 x2 = 整格框 x2（= " + pr.x2.ToString("F2") + "）");
                N(c.y - half.y, pr.y1, 1.0f, "A8d ★ 命中区并集 y1 = 整格框 y1（= " + pr.y1.ToString("F2") + "）");
                N(c.y + half.y, pr.y2, 1.0f, "A8e ★ 命中区并集 y2 = 整格框 y2（= " + pr.y2.ToString("F2") + "）");
            }

            // A9 · 它是**透明**的（射线靶不改变画面）
            N(rq.Tint.a, 0f, 1e-4f, "A9 ★ 射线靶 tint 的 alpha = 0（不可见；⛔ 不拿一块黑盖住问号）");

            // A11 · 非中立档：原版 `Random Image` 整件关 ⇒ 连子件带射线靶都不该建
            var spec2 = new ItemSpec
            {
                Id = "C4_RandomCardRayArmy", Type = "DropTableItem", Kind = ItemKind.Generic,
                Rarity = 0, CardArmy = 10, Label = "C4",
            };
            var res2 = ItemDrawer.Draw(root.transform, new PxRect(0f, 0f, BoxW, BoxH), spec2, 1, DrawerOverride.Default, st);
            try
            {
                if (res2.View != null) res2.View.SetShowInfoOnClick(true);
                Tr(res2.View != null && res2.View.HitNode != null,
                   "A10 **前提**：非中立档的整格命中层也建得出来（否则 A11 等于没查）");
                var h2 = res2.View != null ? res2.View.HitNode : null;
                Tr(h2 != null && h2.Find(ItemDrawerView.NodeIconRaycast) == null,
                   "A11 ★ `cardArmy != Neutral` ⇒ **不建**问号射线靶（原版 `randomArmyIcon.SetActive(false)`，子件跟着不画）"
                 + "｜🧨 去掉 `army == 0` 判断 ⇒ 红");
            }
            finally
            {
                if (res2.View != null) res2.View.Release();
            }
        }
        finally
        {
            if (res.View != null) res.View.Release();
            UnityEngine.Object.DestroyImmediate(root);
            PointerLayer.InvalidateHitUnion(null);      // 夹具销毁了 ⇒ 并集缓存里那几条也清掉（同 `B106_W8` 的收尾）
        }

        // A12 · 灭自证：并集机制**确实会扩**（否则 A8「不变」可能是假绿）
        var probe = new GameObject("C4_UNION_PROBE");
        try
        {
            var hit = MenuDraw.Hit(probe.transform, "Hit", new PxRect(0f, 0f, 100f, 100f), 20, null);
            Tr(hit != null, "A12a **前提**：临时钮建出来了（`MenuDraw.Hit`）");
            if (hit == null) return;
            var wb = hit.GetComponent<WindowButton>();
            Tr(wb != null, "A12b **前提**：临时钮上有 `WindowButton`");
            if (wb == null) return;

            Vector2 c0, h0; ImageQuad q0;
            bool ok0 = PointerLayer.HitBoxForTest(wb, out c0, out h0, out q0);
            Tr(ok0, "A12c **前提**：临时钮初始就命中得到（半边 = 50）");
            N(h0.x, 50f, 0.5f, "A12d 初始命中区半边 x = 50（框 100 的一半）");

            // 在同一个钮的子树里加一块**框外**的 quad（= 原版多一颗 `m_RaycastTarget 1` 的件）
            MenuDraw.Rect(hit, CardArt.Solid(), new PxRect(300f, 0f, 400f, 100f), "ProbeOut", 20, new Color(0f, 0f, 0f, 0f));
            Vector2 c1, h1; ImageQuad q1;
            bool ok1 = PointerLayer.HitBoxForTest(wb, out c1, out h1, out q1);
            Tr(ok1, "A12e **前提**：加了框外件之后仍命中得到");
            Tr(h1.x > h0.x + 1f,
               "A12 ★ 灭自证：子树里多一块**框外**的可射线件 ⇒ 命中区并集**确实变大**（半边 " + h0.x.ToString("F1")
             + " → " + h1.x.ToString("F1") + "）—— 说明 A8 那条「不变」不是假绿");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
            PointerLayer.InvalidateHitUnion(null);      // 同 A 段收尾：夹具钮的并集缓存要清
        }
    }

    // ============================================================ B · `40k_UI_bt_back_pressed`
    static void SectionB_BackPressed()
    {
        MenuCheck.Section(_sink, "B `40k_UI_bt_back_pressed` 进了 `Resources/`（预览窗返回钮 pressed 帧）");

        var tex = CardArt.MenuUi("40k_UI_bt_back_pressed");
        Tr(tex != null, "B1 ★ `CardArt.MenuUi(\"40k_UI_bt_back_pressed\")` 取得到"
                      + "｜🧨 删掉 `Resources/Art/ui_menu/40k_UI_bt_back_pressed.png` ⇒ 红（`DraftModeWindow` 那条 `Log` 会再响）");
        if (tex != null)
        {
            C(tex.width, 88, "B2 ★ 宽 = 88（`m_Rect` / `m_RD.textureRect` 都是 88×87，走 `asis`）");
            C(tex.height, 87, "B3 ★ 高 = 87");
            C(tex.name, "40k_UI_bt_back_pressed", "B4 贴图名 = 资产名（同名不同图在这个仓里踩过，顺手钉一下）");
        }

        // 前提：同族那两张（base / hover）本来就在 —— 原版 `m_SpriteState` 是三张一套
        Tr(CardArt.MenuUi("40k_UI_bt_back") != null, "B5 **前提**：base `40k_UI_bt_back` 取得到");
        Tr(CardArt.MenuUi("40k_UI_bt_back_hover_back") != null, "B6 **前提**：hover `40k_UI_bt_back_hover_back` 取得到");
        // 灭自证：loader 不是「什么都返回」
        Tr(CardArt.MenuUi("40k_UI_bt_back_pressed__C4_not_here") == null,
           "B7 灭自证：一个不存在的名字取不到（否则 B1 证明不了「这张真的进来了」）");
    }

    // ============================================================ C · `AvatarDisplay` 的语义落在 `AvatarTab`
    static void SectionC_AvatarDisplay()
    {
        MenuCheck.Section(_sink, "C `AvatarDisplay` 语义（`ChangeAvatar` / `ChangeBorder`）—— 落在 `AvatarTab` 的现有口径");

        var av = FindBuilt<AvatarTab>(t => MenuCheck.FindChild(t, "Item Drawer") != null);
        Tr(av != null, "C0 **前提**：场景里有**建好的** `AvatarTab`（`Item Drawer` 在）—— 本段要 `MainMenuScene` 那一族宿主"
                     + "｜🧨 接入宿主却没先建档案窗 ⇒ 红（⛔ 不静默跳过）");
        if (av == null) return;

        Tr(av.BigBorderTextureForTest != null, "C1 **前提**：大图边框层 `Image Container/Border` 建出来了（原版 `avatarHolder`）");

        int oldBorder = ProfileData.EquippedBorderIndex;
        LiveOpsLocalEvent movedEv = null;
        long movedOldEndMs = 0, movedOldExpireMs = 0;
        try
        {
            // ---- C2/C3 · `ChangeBorder`：大图边框图跟着「已装备那条」走（原版每次 Initialize 都重设 sprite）
            //  🔊 为什么夹具要挪活动期窗：本机下标 1..4 那 4 条边框的父活动都在 2025 / 2026-01 ⇒ 读口一读到就
            //     把它们卸回 `Default`（`ProfileData.EquippedBorder` → `RefreshExpiredBorders`）。
            //     夹具让那条活动**真的在期**（⛔ 不跳过可用性检查 —— 那是原版行为）。同一手法的先例 → `Editor/B94B10_Assert.cs` §G。
            int k = FindSecondBorderIndex(out string why);
            Tr(k >= 0, "C2a **前提**：清单里有第二条边框（" + why + "）");
            if (k >= 0)
            {
                var bi = ProfileData.Borders[k];
                string whyNow;
                if (!ProfileData.IsBorderUsable(bi, LiveOpsLocalCatalog.NowUtc, out whyNow))
                {
                    var ev = LiveOpsLocalCatalog.Find(bi.pe);
                    Tr(ev != null, "C2b **前提**：边框 `" + bi.n + "` 的父活动 `" + bi.pe + "` 在本地活动目录里");
                    if (ev != null)
                    {
                        movedEv = ev; movedOldEndMs = ev.EndMs; movedOldExpireMs = ev.ExpireMs;
                        long nowMs = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                        ev.EndMs = nowMs + 86400000L;
                        ev.ExpireMs = nowMs + 86400000L;
                    }
                }
                string whyAfter;
                Tr(ProfileData.IsBorderUsable(bi, LiveOpsLocalCatalog.NowUtc, out whyAfter),
                   "C2c **前提**：下标 " + k + " 的边框此刻可用（" + (whyAfter ?? "可用") + "）");
                if (whyAfter == null)
                {
                    ProfileData.EquippedBorderIndex = k;
                    av.Refresh();
                    string wantArt = (bi.art ?? "").Replace(' ', '_');
                    var wantTex = CardArt.MenuUi(wantArt);
                    Tr(wantTex != null, "C2d **前提**：那条边框的图 `" + wantArt + "` 在 `Resources/` 里（否则只能验「关层」那一支）");
                    if (wantTex != null)
                        C(av.BigBorderTextureForTest != null ? av.BigBorderTextureForTest.name : "<null>", wantArt,
                          "C2 ★ `Refresh()` 之后大图边框贴的就是**已装备那条**（`AvatarDisplay.ChangeBorder` 的 sprite 重设）"
                        + "｜🧨 删掉 `Refresh` 里的 `ChangeBorder()` ⇒ 仍是下标 0 那张 ⇒ 红");
                    else
                        Tr(!av.BigBorderActiveForTest,
                           "C3 边框图取不到 ⇒ 大图边框层**关掉**（原版 `avatarHolder.enabled = (sprite != null)`）");
                }
            }
        }
        catch (Exception e)
        {
            Tr(false, "C2 段异常：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (movedEv != null) { movedEv.EndMs = movedOldEndMs; movedEv.ExpireMs = movedOldExpireMs; }
            ProfileData.EquippedBorderIndex = oldBorder;
            av.Refresh();                       // 把大图恢复成「已装备那条」（⛔ 别把夹具状态漏给后面的断言）
        }

        // ---- C4 · `ChangeAvatar`：`enabled = (sprite != null)`（+ 名字）
        int sel = Mathf.Clamp(av.Selected, 0, Mathf.Max(0, AvatarTab.Avatars.Count - 1));
        try
        {
            av.ChangeAvatar(-1);
            C(av.BigNameForTest, "", "C4a ★ 越界下标 ⇒ `avatarName.text` 置空（原版 `avatar != null` 才写）");
            Tr(!av.BigArtActiveForTest, "C4b ★ 越界下标 ⇒ 立绘层**关掉**（原版 `avatarImage.enabled = (sprite != null)`）");
            av.ChangeAvatar(sel);
            bool hasArt = AvatarTab.Avatars.Count > sel && CardArt.Cosmetics(AvatarTab.Avatars[sel].Art) != null;
            C(av.BigArtActiveForTest, hasArt,
              "C4c ★ 恢复预览项 ⇒ 立绘层开/关 = 「这张图取不取得到」（今天的第 " + sel + " 条 " + (hasArt ? "取得到" : "取不到") + "）");
            C(av.BigNameForTest, AvatarTab.Avatars.Count > sel ? AvatarTab.Avatars[sel].Name : "",
              "C4d ★ 名字 = 预览项显示名（原版 `avatarName.text = avatar.LocalizedName`）");
        }
        finally
        {
            av.Refresh();                       // 收尾恢复
        }

        // ---- C5 · `ToggleHighlight(false)`：本页两个原版调用点都传 false ⇒ 大图高亮恒关
        Tr(!av.BigHighlightActiveForTest,
           "C5 ★ 大图 `Highlight` 恒关（`AvatarTab.RefreshDisplays.c` / `AvatarTab__OnAvatarClick.c` 都传 `highlight = false`）");

        // ---- C6 · 结构：大图那三层靠**渲染队列**分层（`Highlight < Border < Image`）
        //  ⚠️ **必须走显式路径**：`MenuCheck.FindChild` 是**递归**的，而 `Item Drawer` 里 469 个格子各有
        //     一颗 `Image Container`（先建）⇒ 递归会抓到**格子的**那一颗（见 `MenuCheck.FindChild` 的 doc）。
        //  ⚠️ **判据是队列、不是兄弟序**：我们建件的次序是 `Highlight → Image → Border`（`Border` 最后建），
        //     而原版兄弟序是 `Highlight → Border → Image` ⇒ 兄弟序**故意不同**、靠 `SetRenderQueue` 保证画序
        //     （`CLAUDE.md` §三「分层要用渲染队列，不能用 z」；同族注释 → `AvatarTab.Build` 的 W18 那段）。
        var ic = av.transform.Find("Selected Item Panel/Avatar Menu Item/Image Container");
        Tr(ic != null, "C6a **前提**：`Selected Item Panel/Avatar Menu Item/Image Container` 在");
        if (ic != null)
        {
            var hl = ic.Find("Highlight"); var bd = ic.Find("Border"); var im = ic.Find("Image");
            Tr(hl != null && bd != null && im != null, "C6b **前提**：`Highlight` / `Border` / `Image` 三层都在");
            int qh = QOf(hl), qb = QOf(bd), qi = QOf(im);
            Tr(qh > 0 && qb > 0 && qi > 0, "C6b2 **前提**：三层的渲染队列都读得到（" + qh + " / " + qb + " / " + qi + "）");
            Tr(qh < qb && qb < qi,
               "C6c ★ 渲染队列 `Highlight(" + qh + ") < Border(" + qb + ") < Image(" + qi + ")`"
             + "（原版画序 `Highlight → Border → Image`；⛔ 反过来边框会把立绘压黑 —— W18 那个真缺陷）");
        }
    }

    /// <summary>找一条「画名与下标 0 不同」的边框；找不到返回 -1 + 原因。</summary>
    static int FindSecondBorderIndex(out string why)
    {
        why = "";
        var list = ProfileData.Borders;
        if (list == null || list.Count < 2) { why = "`borders` 少于 2 条"; return -1; }
        string art0 = list[0] != null ? (list[0].art ?? "").Replace(' ', '_') : "";
        for (int i = 1; i < list.Count; i++)
        {
            string ai = list[i] != null ? (list[i].art ?? "").Replace(' ', '_') : "";
            if (ai != art0) { why = "下标 " + i; return i; }
        }
        why = "每条画名都与下标 0 相同";
        return -1;
    }

    // ============================================================ D · `AvatarDisplay` **不**登记成抽屉
    static void SectionD_NotADrawer()
    {
        MenuCheck.Section(_sink, "D `AvatarDisplay` **不是**抽屉档（不在 `ItemDrawerConfig` ⇒ 不登记 `DrawerImpls`）");

        // 判据：`ItemTypeSets` 的 `PlayerAvatar` 三档全是 `AvatarDrawer`；`AvatarDisplay` 不在 20 条配置类型里，
        //   而 `decomp_full` 全库 `AvatarDisplay__Draw` **只有它自己那份 `.c`**（零调用点）⇒ 工厂永远选不到它。
        C(ItemDrawer.ImplOfDrawerClass("AvatarDisplay"), (string)null,
          "D1 ★ `DrawerImpls` 里**没有** `AvatarDisplay`（⛔ 登记它 = 零调用死档；语义落在 `AvatarTab` 自绘口径）"
        + "｜🧨 加一条 `\"AvatarDisplay:\" + …` ⇒ 红（要加必须先拿出「工厂能到它」的判据）");

        var avSpec = new ItemSpec { Id = "C4_Avatar", Type = "PlayerAvatar", Kind = ItemKind.Generic, Label = "C4" };
        C(ItemDrawer.PickDrawer(avSpec, DrawerOverride.Default), ItemDrawer.DrawerAvatar,
          "D2 ★ `PlayerAvatar` 这一类型在工厂里落到 **`AvatarDrawer`**（奖励那一档），⛔ 不是 `AvatarDisplay`");
        C(ItemDrawer.PickDrawer(avSpec, DrawerOverride.Icon), ItemDrawer.DrawerAvatar,
          "D2b `Icon`(10) 档 → `AvatarDrawer`（`ItemTypeSets` 第二列）");
        C(ItemDrawer.PickDrawer(avSpec, DrawerOverride.Shop), ItemDrawer.DrawerAvatar,
          "D2c `Shop`(20) 档 → `AvatarDrawer`（`ItemTypeSets` 第四列）");
    }

    // ============================================================ 辅助
    /// <summary>一颗图块现在的渲染队列（`ImageQuad.RenderQueue`，生产分层用的同一份读数）；读不到 ⇒ -1。</summary>
    static int QOf(Transform t)
    {
        var iq = t != null ? t.GetComponent<ImageQuad>() : null;
        return iq != null ? iq.RenderQueue : -1;
    }

    /// <summary>找场景里**真的建好了**的那个组件实例（跳过资产 / prefab 模板）。</summary>
    static T FindBuilt<T>(Func<Transform, bool> built) where T : Component
    {
        var all = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c == null) continue;
            var go = c.gameObject;
            if (go == null || !go.scene.IsValid()) continue;
            if (built(c.transform)) return c;
        }
        return null;
    }
}
