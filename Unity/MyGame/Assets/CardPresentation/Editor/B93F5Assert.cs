// B93F5Assert.cs — 执行代理 `F5`（第九十四会话 · 波 1）本批 28 条的断言
// 🆕 2026-10-29
//
// ============================ 它盯的是什么 ============================
// `判据/B94_派活.tsv` 里 `写手组 == F5_表现小文件族` 那 28 条中**本批真动过/真核过的那些**：
//   · `未分类_其他:195/196/197` — 原版 `ContentSizeFitterMinMax.ClampSize/SetLayoutHorizontal/SetLayoutVertical`
//       → 本批**新建** `Core/ContentSizeFitterMinMax.cs`（A 组）
//   · `未分类_其他:702/703` — 原版 `UIPreferedSizeFixer.ApplyConfig/CalculateLayoutInputHorizontal`
//       → 本批**新建** `Core/UIPreferedSizeFixer.cs`（B 组）
//   · `未分类_其他:667` — 原版 `UICornersGradient.ModifyMesh`
//       → 类体已于 2026-10-28（B93_N6）落地，本批**核过并升 A**（C 组）
//   · `战斗逻辑驱动:992` — 原版 `CardTraitCollection.GetTraitEffects(trait)`（整列）
//       → 本批给 `Core/TraitFrames.cs` **补了 `AllRowsFor(trait)`**（D 组）
//   · `战斗逻辑驱动:1101` — 原版 `MinionManager.Awake()` 的槽位式
//       → 核过 `Board/ArenaSlots.cs`（E 组）
//   · `其他_基础设施:291/293` + `泛型与启动链_补读:8/9` — 原版 `LoadableReference` 的取/缓存
//       → 核过 `Core/CardArt.cs` 的缓存三档（F 组）
//   · `战斗逻辑驱动:1081` — 原版 `MatchData.SetMatchRecording` ⇒ `Battle/ReplayStore.cs` 的可用判据（G 组）
// 逐条的判据原文、为什么这么判、以及**没落地的部分** → `资料/完全复刻/报告/B94_F5.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
//   `B93F5Assert.Run();`
//   `_sink.Pass += B93F5Assert.Pass; _sink.Fail += B93F5Assert.Fail;`
//   · **不跑 Unity 批处理**、不建持久资产；建的临时结点在收尾 `DestroyImmediate`。
//   · ⚠️ 本文件**还没接到宿主里**（`Editor/BattleScene.cs` 是别的写手的文件）⇒ 由调度台统一接线。
//
// ============================ 🔴 灭自证 ============================
//   · A 组：把 `Mathf.Clamp` 写成 `Mathf.Min`（只夹上界）⇒ A1 红；把 `clampWidth` 用到 y 轴 ⇒ A2/A5 红。
//   · B 组：把 `layoutElement.preferredWidth = rect.height` 改成 `rect.width` ⇒ B2 红；
//           删掉 `if (!preferedWidthFollowHeight) return;` 那道闸 ⇒ B1 红。
//   · C 组：把「底边插值 / 顶边插值」那两行对调 ⇒ C2 红；把「乘顶点色」改成「替换顶点色」⇒ C4 红。
//   · D 组：把 `AllRowsFor` 改成「只回 `_triggerBook` 那一本」⇒ D1（3 vs 2）红。
//   · E 组：把 `side` 取反（照旧文档「敌方镜像」那一版）⇒ E2 红；把敌 `Extra` 换回 0.09 ⇒ E3 红。
//   · F 组：把 `Get` 的负缓存那句 `_cache[path] = t;` 删掉 ⇒ F2 红（`CacheCount` 不涨）。
//   · G 组：把 `NoReplaySentinel` 从 `"-1"` 改成别的 ⇒ G1 红。
using CardPresentation;
using UnityEngine;
using UnityEngine.UI;

/// <summary>执行代理 F5：本批表现层小文件族的断言。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值全是字面量 / 原版参数**（0.2 / 292 / -6.655 …），**不从被测实现里读**。</para></summary>
public static class B93F5Assert
{
    static readonly CheckSink _sink = new CheckSink("[F5] ") { Near = MenuNearStyle.Compact3 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void N(float got, float want, string msg) { MenuCheck.Near(_sink, got, want, 1e-4f, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "F5 表现小文件族：CSFMinMax / UIPreferedSizeFixer / UICornersGradient / 槽位式 / 图缓存 / 回放可用");

        // ⚠️ 一开始就带 `RectTransform`：uGUI 那些件要它，事后再加会把 `Transform` 换掉（既有件会掉引用）
        var go = new GameObject("B93F5_Tmp", typeof(RectTransform));
        try
        {
            SectionA_CSFMinMax(go);
            SectionB_UIPreferedSizeFixer(go);
            SectionC_UICornersGradient(go);
        }
        finally
        {
            Object.DestroyImmediate(go);          // 临时件一律带走（批处理里 Destroy 不生效 ⇒ 必须 DestroyImmediate）
        }

        SectionD_TraitFrames();
        SectionE_ArenaSlots();
        SectionF_CardArtCache();
        SectionG_ReplayAvailable();

        Debug.Log(_sink.P + "F5 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · ContentSizeFitterMinMax
    // 判据 = `decomp_full/ContentSizeFitterMinMax__ClampSize.c` 全文（两轴各自开关、各自 min/max、= Mathf.Clamp）
    static void SectionA_CSFMinMax(GameObject host)
    {
        MenuCheck.Section(_sink, "A · ContentSizeFitterMinMax（原版六字段 + 每轴独立夹）");

        // A0 结构：它必须仍然是 `ContentSizeFitter` 的子类（⚠️ 换了基类 = 基类那一跳没了，行为立刻不同）
        var csf = host.AddComponent<ContentSizeFitterMinMax>();
        Tr(csf is ContentSizeFitter, "A0 `ContentSizeFitterMinMax` 是 `ContentSizeFitter` 的子类（基类那一跳在）");

        // A1 只开宽：低于 min ⇒ 抬到 min（50 → 100）
        csf.ConfigureForTest(true, 100f, 400f, false, 20f, 500f);
        var r = csf.ClampForTest(new Vector2(50f, 300f));
        N(r.x, 100f, "A1 宽 50 < min 100 ⇒ 夹到 100");
        N(r.y, 300f, "A1 高轴**没开开关** ⇒ 300 原样过（⛔ 别顺手也夹）");

        // A2 只开宽：高于 max ⇒ 压到 max（900 → 400）
        r = csf.ClampForTest(new Vector2(900f, 300f));
        N(r.x, 400f, "A2 宽 900 > max 400 ⇒ 夹到 400");

        // A3 区间内 ⇒ 原值
        r = csf.ClampForTest(new Vector2(250f, 300f));
        N(r.x, 250f, "A3 宽 250 在 [100,400] 内 ⇒ 原值");

        // A4 只开高：10 → 20（min）；999 → 500（max）
        csf.ConfigureForTest(false, 100f, 400f, true, 20f, 500f);
        r = csf.ClampForTest(new Vector2(50f, 10f));
        N(r.x, 50f, "A4 宽轴**没开开关** ⇒ 50 原样过");
        N(r.y, 20f, "A4 高 10 < min 20 ⇒ 夹到 20");
        r = csf.ClampForTest(new Vector2(50f, 999f));
        N(r.y, 500f, "A5 高 999 > max 500 ⇒ 夹到 500");

        // A6 两个开关全关 ⇒ 恒等（**连 NaN 都原样过** —— 原版那两条 `Mathf.Clamp` 根本不会跑）
        csf.ConfigureForTest(false, 100f, 400f, false, 20f, 500f);
        r = csf.ClampForTest(new Vector2(-7f, 12345f));
        N(r.x, -7f, "A6 两轴都没开 ⇒ x 原样 −7");
        N(r.y, 12345f, "A6 两轴都没开 ⇒ y 原样 12345");

        Object.DestroyImmediate(csf);
    }

    // ============================================================ B · UIPreferedSizeFixer
    // 判据 = `decomp_full/UIPreferedSizeFixer__ApplyConfig.c` 全文（开关开 ⇒ HLG 四步 + preferredWidth = 源 rect.height）
    static void SectionB_UIPreferedSizeFixer(GameObject host)
    {
        MenuCheck.Section(_sink, "B · UIPreferedSizeFixer（preferredWidth = 源 RectTransform 的高）");

        var srcGo = new GameObject("B93F5_Src", typeof(RectTransform));
        var src = (RectTransform)srcGo.transform;
        src.anchorMin = src.anchorMax = new Vector2(0.5f, 0.5f);   // 锚点不拉伸 ⇒ rect == sizeDelta
        src.sizeDelta = new Vector2(777f, 123f);

        var hlg = host.AddComponent<HorizontalLayoutGroup>();
        var le = host.AddComponent<LayoutElement>();
        var fixer = host.AddComponent<UIPreferedSizeFixer>();
        fixer.ConfigureForTest(hlg, src, le, true);

        try
        {
            // B0 源 rect 的高确实是我们摆的那个值（这条不过 ⇒ 下面两条没有意义）
            N(src.rect.height, 123f, "B0 源件 `rect.height` = 123（前提断言：锚点不拉伸时 rect == sizeDelta）");

            // B1 开关**关着** ⇒ 一句都不跑（preferredWidth 保持外面写进去的值）
            le.preferredWidth = -42f;
            fixer.ConfigureForTest(hlg, src, le, false);
            fixer.RunFixForTest();
            N(le.preferredWidth, -42f, "B1 `preferedWidthFollowHeight = false` ⇒ 一个字节都不写（仍 −42）");

            // B2 开关开着 ⇒ preferredWidth = 源 rect.height（**高**，不是宽）
            le.preferredWidth = -42f;
            fixer.ConfigureForTest(hlg, src, le, true);
            fixer.RunFixForTest();
            N(le.preferredWidth, 123f, "B2 开关开 ⇒ `preferredWidth = rect.height` = 123（⛔ 不是 rect.width 777）");

            // B3 CalculateLayoutInputVertical 是空体 ⇒ 不改 preferredWidth
            le.preferredWidth = -43f;
            fixer.CalculateLayoutInputVertical();
            N(le.preferredWidth, -43f, "B3 `CalculateLayoutInputVertical()` 是空体 ⇒ 不动 preferredWidth");

            // B4 七个 getter 纯转发到 layoutElement（抽两条：preferredWidth / layoutPriority）
            le.preferredWidth = 55f;
            le.layoutPriority = 3;
            N(fixer.preferredWidth, 55f, "B4a `preferredWidth` getter 转发到 `layoutElement.preferredWidth`");
            C(fixer.layoutPriority, 3, "B4b `layoutPriority` getter 转发到 `layoutElement.layoutPriority`");
        }
        finally
        {
            Object.DestroyImmediate(fixer);
            Object.DestroyImmediate(le);
            Object.DestroyImmediate(hlg);
            Object.DestroyImmediate(srcGo);
        }
    }

    // ============================================================ C · UICornersGradient
    // 判据 = `decomp_full/UICornersGradient__ModifyMesh.c`（131 行）：
    //        归一化 (u,v) → 双线性插四角色 → **乘**顶点原色 → Clamp01 → ×255 **截断**
    static void SectionC_UICornersGradient(GameObject host)
    {
        MenuCheck.Section(_sink, "C · UICornersGradient（四角双线性 + **乘**顶点色 + 截断）");

        var go = new GameObject("B93F5_Grad", typeof(RectTransform));
        try
        {
            go.AddComponent<Image>();                 // `BaseMeshEffect` 要求同节点有 `Graphic`
            var g = go.AddComponent<UICornersGradient>();
            Tr(g is BaseMeshEffect, "C0 `UICornersGradient` 是 `BaseMeshEffect` 的子类（uGUI 回调那一跳在）");

            var rect = new Rect(0f, 0f, 100f, 100f);
            var white = new Color32(255, 255, 255, 255);

            // C1 四角全白（默认值）⇒ 恒等（乘 1）
            var o = g.ColorAt(37f, 61f, white, rect);
            C(o.r, (byte)255, "C1 默认四角全白 ⇒ 红通道 255（恒等）");

            // C2 四角分明：BL 蓝 · BR 白 · TL 红 · TR 绿 ⇒ 左下角 = 蓝、(0,0) 是 BL
            g.m_bottomLeftColor = Color.blue;
            g.m_bottomRightColor = Color.white;
            g.m_topLeftColor = Color.red;
            g.m_topRightColor = Color.green;
            var bl = g.ColorAt(0f, 0f, white, rect);
            C(bl.b, (byte)255, "C2 左下角 (0,0) ⇒ 蓝通道满");
            C(bl.r, (byte)0, "C2 左下角 (0,0) ⇒ 红通道 0（⛔ 底/顶插值别对调）");
            var tr = g.ColorAt(100f, 100f, white, rect);
            C(tr.g, (byte)255, "C3 右上角 (100,100) ⇒ 绿通道满");
            C(tr.r, (byte)0, "C3 右上角 ⇒ 红通道 0");

            // C4 **乘**顶点原色（不是替换）：角落白、顶点色 (0,255,0,255) ⇒ 绿通道满、红通道 0
            g.m_bottomLeftColor = Color.white;
            g.m_bottomRightColor = Color.white;
            g.m_topLeftColor = Color.white;
            g.m_topRightColor = Color.white;
            var mul = g.ColorAt(10f, 10f, new Color32(0, 255, 0, 255), rect);
            C(mul.g, (byte)255, "C4 顶点绿 × 四角白 ⇒ 绿 255");
            C(mul.r, (byte)0, "C4 顶点红 0 × 四角白 ⇒ 红 0（**乘**，不是替换）");

            // C5 alpha 也照乘（= 顶点原 alpha 不被抹掉）
            var a = g.ColorAt(10f, 10f, new Color32(255, 255, 255, 128), rect);
            C(a.a, (byte)128, "C5 顶点 alpha 128 × 四角 1 ⇒ 128（淡出不被抹掉）");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    // ============================================================ D · TraitFrames（GetTraitEffects = 整列）
    // 判据 = `decomp_full/CardTraitCollection__GetTraitEffects.c:1-29`（返回 `entry + 0x38` 那一整列，不分 trigger 档）
    static void SectionD_TraitFrames()
    {
        MenuCheck.Section(_sink, "D · TraitFrames.AllRowsFor（= 原版 GetTraitEffects 的「整列」）");

        Tr(TraitFrames.BindingsLoaded, "D0 绑定表载进来了（`Resources/trait_effect_bindings.json`）—— "
            + "没载到就会 `Debug.LogError`，下面几条一律红才是对的。问题：" + (TraitFrames.BindingsProblem ?? "（无）"));

        // D1 `bloodThirst` 在数据里有 **3** 行（triggerType 3 / 6 / 1）—— 整列必须 3 条
        //    ⚠️ 我们的两本字典只收 3/6（1 那档哪本都不进）⇒ 这一条正好把「整列」与「两本并集」分开
        C(TraitFrames.AllRowsFor("bloodThirst").Length, 3, "D1 `bloodThirst` 整列 3 条（含 triggerType 1 那条）");
        C(TraitFrames.BindingsFor("bloodThirst", true).Length, 2, "D1b 触发本只有 2 条（3 / 6）—— 与整列不同才是对的");
        C(TraitFrames.BindingsFor("bloodThirst", false).Length, 0, "D1c OnPlay 本 0 条");

        // D2 大小写/空白归一（数据里是 `bloodThirst`，调用方可能写小写）
        C(TraitFrames.AllRowsFor("  BLOODTHIRST ").Length, 3, "D2 名字做 `Trim().ToLowerInvariant()` 归一后仍 3 条");

        // D3 没有这个 trait ⇒ 空数组（原版那一档返回 0、调用方遍历 0 次）
        C(TraitFrames.AllRowsFor("no_such_trait_zzz").Length, 0, "D3 表里没有的 trait ⇒ 空数组");
        C(TraitFrames.AllRowsFor(null).Length, 0, "D4 `null` ⇒ 空数组");

        // D5 单条那一档（`GetTraitEffect`）：带阵营的解析口仍然通
        C(TraitFrames.PrefabFor("bloodThirst", true), "BloodThirstEffect_Gain",
          "D5 `PrefabFor(bloodThirst, trigger)` = 触发本第一条 = `BloodThirstEffect_Gain`");
        var all = TraitFrames.PrefabsFor("bloodThirst", true);
        Tr(all.Length == 2 && all[0] == "BloodThirstEffect_Gain" && all[1] == "BloodThirstEffect_Idle",
           "D5b `PrefabsFor(bloodThirst, trigger)` 按数据序 = [Gain, Idle]（实得 "
           + (all.Length == 2 ? all[0] + "," + all[1] : "长度 " + all.Length) + "）");
    }

    // ============================================================ E · ArenaSlots（MinionManager.Awake 的槽位式）
    // 判据 = `decomp_full/MinionManager__Awake.c:1-107` + 预制体字段实读
    //        （Sep 0.82/1.53 · minionExtra 0.09/0.13 · slotsPerSide 4 · heroExtraOffset.z −0.42/−0.75 · ExtraYOnBoard 0）
    static void SectionE_ArenaSlots()
    {
        MenuCheck.Section(_sink, "E · ArenaSlots（x = ±(n·Sep + Extra) · y = ExtraYOnBoard · 督军位多一个 z 偏移）");

        C(ArenaSlots.SlotsPerSide, 4, "E0 每侧 4 格（原版 `slotsPerSide` = 4）");

        // E1 督军位（4）：正中、多一个 heroExtraOffset.z
        var w = ArenaSlots.Position(ArenaSlots.WarlordSlot, false);
        N(w.x, 0f, "E1 我方督军位 x = 0（正中）");
        N(w.z, -6.655f + -0.42f, "E1 我方督军位 z = MinionArea(−6.655) + heroExtraOffset(−0.42) = −7.075");

        // E2 左右同号式（原版**不镜像**：两 MinionArea 跑同一段 Awake，left 恒 −x）
        N(ArenaSlots.Position(5, false).x, 0.82f + 0.09f, "E2 我方右 1 格 x = +(1·0.82 + 0.09) = 0.91");
        N(ArenaSlots.Position(3, false).x, -(0.82f + 0.09f), "E2b 我方左 1 格 x = −0.91（左恒负）");

        // E3 敌方那一份：Sep 1.53 · Extra 0.13（⛔ 不是 0.09）
        N(ArenaSlots.Position(5, true).x, 1.53f + 0.13f, "E3 敌方右 1 格 x = +(1·1.53 + 0.13) = 1.66（⛔ Extra 不是 0.09）");
        N(ArenaSlots.Position(0, true).x, -(4f * 1.53f + 0.13f), "E3b 敌方最左格 x = −(4·1.53 + 0.13) = −6.25");

        // E4 y 恒 = ExtraYOnBoard（13 场 × 2 实例实读全 0）
        for (int s = 0; s < ArenaSlots.SlotCount; s++)
            for (int e = 0; e < 2; e++)
                if (ArenaSlots.Position(s, e != 0).y != 0f)
                { Tr(false, $"E4 槽 {s}（enemy={e != 0}）的 y 必须 = ExtraYOnBoard = 0"); goto e4done; }
        Tr(true, "E4 全部 9 格 × 2 侧的 y 都 = 0（ExtraYOnBoard 恒 0）");
    e4done:;

        // E5 敌方督军位 z = 1.043 + (−0.75)
        N(ArenaSlots.Position(ArenaSlots.WarlordSlot, true).z, 1.043f - 0.75f, "E5 敌方督军位 z = 1.043 − 0.75 = 0.293");
    }

    // ============================================================ F · CardArt 缓存（LoadableReference 的取/缓存三档）
    static void SectionF_CardArtCache()
    {
        MenuCheck.Section(_sink, "F · CardArt 缓存（Get 的负缓存 / 命中不重复 IO / Release 放掉一条）");

        const string miss = "B93F5_NoSuchUi_zzz";
        string path = "Art/ui/" + miss;        // = `CardArt.Ui` 拼的那条路径（`Root + "ui/" + name`）
        int before = CardArt.CacheCount;
        int liveBefore = CardArt.LiveCacheCount;

        // F1 取一张不存在的图 ⇒ null（不抛、不静默造图）
        var t1 = CardArt.Ui(miss);
        Tr(t1 == null, "F1 `CardArt.Ui(不存在的名字)` ⇒ null（不抛、不伪造）");
        C(CardArt.CacheCount, before + 1, "F2 取一次 ⇒ 缓存**多一条**（原版 `LoadableReference` 也是一条登记）");
        C(CardArt.LiveCacheCount, liveBefore, "F2b 那条是**负缓存** ⇒ `LiveCacheCount` 不变");
        Tr(!CardArt.IsCached(path), "F2c `IsCached` 只认**真有图**的条目 ⇒ 负缓存 false（与 `CacheCount` 一起读才分得清）");

        // F3 再取一次 ⇒ 命中缓存（不新增条目 = 不重复 IO）
        CardArt.Ui(miss);
        C(CardArt.CacheCount, before + 1, "F3 第二次取 ⇒ `CacheCount` 不变（命中缓存，不重复 IO）");

        // F4 负缓存条目没有句柄 ⇒ `Release` 返回 false（原版那一档也只是「放掉一条登记」）
        Tr(!CardArt.Release(path), "F4 负缓存条目 `Release` ⇒ false（无句柄可放）");
    }

    // ============================================================ G · ReplayStore（MatchData.SetMatchRecording 的等价判据）
    static void SectionG_ReplayAvailable()
    {
        MenuCheck.Section(_sink, "G · ReplayStore.ReplayAvailable（原版 `replaysVersion != \"-1\"`）");

        Tr(!ReplayStore.ReplayAvailable("-1"), "G1 原版哨兵 `\"-1\"` ⇒ 没有回放");
        Tr(!ReplayStore.ReplayAvailable(""), "G2 空串 ⇒ 没有回放（我们的映射）");
        Tr(!ReplayStore.ReplayAvailable(null), "G3 null ⇒ 没有回放");
        Tr(ReplayStore.ReplayAvailable("4"), "G4 版本串 `\"4\"` ⇒ 有回放");
        Tr(ReplayStore.ReplayAvailable("replay_2026_10_29.rep"), "G5 本地录像文件名 ⇒ 有回放（我们这一侧的喂法）");
    }
}
