// B97_FXC_Assert.cs — 执行代理 FXC（第九十七会话 · 波 1，2026-10-29）
//
// ============================ 它盯的是什么 ============================
// 账 `特效动画.tsv` 与 `收藏.tsv` / `战斗界面_HUD.tsv` 里本代理这一轮的**两条真落地的**：
//   · `特效动画:403` `SwarmVFXController.Initialize` 的**第三档**（材质交接）——
//     原版 `CardMaterialHelper.ConfigureMaterial(mat, card)` 写 `_CardImage` / `_MatCap` 两格
//     （判据 `SwarmVFXController__Initialize.c:20-22` + `CardMaterialHelper` 全文两条 SetTexture）。
//     我们的落点 = `WarpforgeEffectPlayer.SetMatCap(Texture2D, Texture2D)` +
//     `BattleDriver.ApplySwarmCardMaterials`（`PlaySwarmMergeFeel` 里 ②·c）。
//   · `特效动画:524` `TutorialPointer.StartPointer` 的最后一条残差（阵营光标）——
//     原版 `SetupCursor(deckWarlord.cardArmy)` 按阵营换 `Pointer` / `light` 两张图
//     （判据 `TutorialPointer__SetupCursor.c` + `CursorByArmySO__GetCursor.c`）。
//     我们的落点 = `TutorialOverlay.SetupCursor(int army)` + 11 条 `CursorArts` 表。
//
// ============================ 怎么用（宿主·由调度台接）============================
//   `B97_FXC_Assert.Run();`
//   `_sink.Pass += B97_FXC_Assert.Pass; _sink.Fail += B97_FXC_Assert.Fail;`
//   · **不跑 Unity 批处理**、不建持久资产；建的 GameObject / Material 在收尾 `DestroyImmediate`。
//   · **不依赖 `BattleDriver`**（`SetMatCap` 直接 `new GameObject().AddComponent<>()` 调；
//     `SetupCursor` 的表是纯静态数据 + `CardArt.MenuUi` 取图）。
//   · 建议宿主：`Editor/BattleScene.cs`（战斗表现的主场，`CardArt` 已 `Load()` 过）。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · A2 / A3：把表里 `EmperorsChildren(120)` / `SpaceWolves(130)` 补一条「随便指一张图」⇒ A2 红。
//   · A4：删掉任意一张 `Resources/Art/ui_menu/Cursor_*.png` 的 import ⇒ A4 红（图名就是判据）。
//   · B2：把 `SetMatCap` 里的 `new Material(src)` 改成直接写 `src.SetTexture(...)`
//          （= 写进 `WarpforgeEffectBinder.Cache` 里那份**全效果共用**的材质）⇒ B2 红。
//   · B3：把 `HasProperty` 那道闸删掉（无条件 `SetTexture`）⇒ 无该槽的 shader 上返回 1 ⇒ B3 红。
using CardPresentation;
using UnityEngine;
using WarpforgeVFX;

/// <summary>执行代理 FXC：账 403（虫群合并件材质交接）与账 524（阵营光标）的真读数口。
/// <para>⛔ **期望值全是字面量 / 独立算出来的量**，不从被测实现里读 —— 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class B97_FXC_Assert
{
    static readonly CheckSink _sink = new CheckSink("[FXC] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "FXC 账 524 阵营光标（TutorialPointer.SetupCursor ← CursorByArmySO）+ 账 403 虫群合并件材质交接");

        SectionA_阵营光标表();
        SectionB_SetMatCap材质交接();

        Debug.Log(_sink.P + "FXC 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 阵营光标
    // 11 个阵营 = `bundle_armycursors_assets_all/Sprite/` 里真有图的那 11 个（原版 21 张：11 本体 + 10 Glow）。
    static readonly int[] CursorArmies = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110 };

    static void SectionA_阵营光标表()
    {
        MenuCheck.Section(_sink, "A 账 524：`CursorByArmySO` 的等价表（11 阵营 / EmperorsChildren 与 SpaceWolves 无图）");

        // A1 表的条数 —— 判据 = 原版 21 张图覆盖的阵营数（不是 13）
        C(TutorialOverlay.CursorArtCountForTest, CursorArmies.Length,
          "A1 表里 11 条（= 原版 21 张光标图覆盖的阵营数，⛔ 不是 13 个阵营全有）");

        // A2 该有的都有、不该有的都没有
        int hit = 0;
        string miss = "";
        for (int i = 0; i < CursorArmies.Length; i++)
        {
            string n, g;
            if (TutorialOverlay.CursorArtForTest(CursorArmies[i], out n, out g) && !string.IsNullOrEmpty(n)) hit++;
            else miss += CursorArmies[i] + " ";
        }
        C(hit, CursorArmies.Length, "A2 11 个阵营逐个命中（没命中的：" + (miss.Length == 0 ? "无" : miss.Trim()) + "）");
        string nn, gg;
        Tr(!TutorialOverlay.CursorArtForTest(120, out nn, out gg), "A2b `EmperorsChildren(120)` 表里没有（原版 21 张图里没有它）");
        Tr(!TutorialOverlay.CursorArtForTest(130, out nn, out gg), "A2c `SpaceWolves(130)` 表里没有（同上）");
        Tr(!TutorialOverlay.CursorArtForTest(0, out nn, out gg), "A2d `Neutral(0)` 表里没有（= 原版 `GetCursorForArmy` 返 false）");
        Tr(!TutorialOverlay.CursorArtForTest(-1, out nn, out gg), "A2e 认不出的 `-1` 表里没有（⛔ 不回落成 Space Marine）");

        // A3 只有 Dark Angels 没有 Glow —— 判据 = 原版 bundle 里 `Cursor_Dark Angels` 只有本体那张
        string dg;
        TutorialOverlay.CursorArtForTest(110, out nn, out dg);
        Tr(string.IsNullOrEmpty(dg), "A3a `DarkAngels(110)` 的 Glow 为空（原版 21 张里就是 10 张 Glow）");
        int glowOk = 0;
        for (int i = 0; i < CursorArmies.Length; i++)
        {
            if (CursorArmies[i] == 110) continue;
            string n2, g2;
            if (TutorialOverlay.CursorArtForTest(CursorArmies[i], out n2, out g2) && !string.IsNullOrEmpty(g2)) glowOk++;
        }
        C(glowOk, 10, "A3b 另外 10 个阵营都有 Glow 名");

        // A4 表里点名的图**真的在 `Resources/Art/ui_menu/`**（缺一张 import 就红 —— 图名就是判据）
        int artOk = 0, artNeed = 0;
        string artMiss = "";
        for (int i = 0; i < CursorArmies.Length; i++)
        {
            string n3, g3;
            if (!TutorialOverlay.CursorArtForTest(CursorArmies[i], out n3, out g3)) continue;
            artNeed++;
            if (CardArt.MenuUi(n3) != null) artOk++; else artMiss += n3 + " ";
            if (!string.IsNullOrEmpty(g3)) { artNeed++; if (CardArt.MenuUi(g3) != null) artOk++; else artMiss += g3 + " "; }
        }
        C(artOk, artNeed, "A4 表里点名的 21 张图全部取得到（缺的：" + (artMiss.Length == 0 ? "无" : artMiss.Trim()) + "）");

        // A5 口径：图名都以 `Cursor_` 开头（= 原版 `Cursor_<阵营>_01[Glow]` 的命名，⛔ 不是我们另挑的图）
        int prefixOk = 0;
        for (int i = 0; i < CursorArmies.Length; i++)
        {
            string n4, g4;
            if (!TutorialOverlay.CursorArtForTest(CursorArmies[i], out n4, out g4)) continue;
            if (n4 != null && n4.StartsWith("Cursor_")) prefixOk++;
        }
        C(prefixOk, 11, "A5 11 条本体的图名都以 `Cursor_` 开头（图名就是阵营判据）");
    }

    // ------------------------------------------------------------ B 材质交接
    /// <summary>`SetMatCap` 的行为：写进去 / 复制材质（⛔ 不污染共享缓存那份）/ 无该槽时返 0。</summary>
    static void SectionB_SetMatCap材质交接()
    {
        MenuCheck.Section(_sink, "B 账 403：`WarpforgeEffectPlayer.SetMatCap`（← `CardMaterialHelper.ConfigureMaterial`）");

        // B4 取数口：四档 matcap 各是一张、且互相不同（档位口径与卡框共用 `CardArt.Card3DMatcap`）
        var m1 = CardArt.Card3DMatcap(1);
        var m2 = CardArt.Card3DMatcap(2);
        var m3 = CardArt.Card3DMatcap(3);
        var m4 = CardArt.Card3DMatcap(4);
        Tr(m1 != null && m2 != null && m3 != null && m4 != null,
           "B4a 四档 matcap 都取得到（`Resources/Art/card3d/MatCap_*`）");
        Tr(m1 != null && m2 != null && !ReferenceEquals(m1, m2)
           && m3 != null && !ReferenceEquals(m2, m3)
           && m4 != null && !ReferenceEquals(m3, m4),
           "B4b 四档是**四张不同的图**（⛔ 别退化成同一张）");
        Tr(ReferenceEquals(CardArt.Card3DMatcap(0), m1), "B4c `level 0`（缺省）= tier1（老调用点行为不变）");

        // B1/B2 材质真的换了，而且**没污染共享缓存那份**（灭自证）
        var sh = Shader.Find("CardPresentation/Card3D");
        Tr(sh != null, "B1a 找得到 `CardPresentation/Card3D`（有 `_MatCap` / `_CardImage` 两个槽）");
        if (sh == null) return;

        var srcTex0 = m1;
        var src = new Material(sh) { name = "FXC_Src" };
        src.SetTexture("_MatCap", srcTex0);
        var go = new GameObject("FXC_SetMatCap_Probe");
        try
        {
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { src };
            var p = go.AddComponent<WarpforgeEffectPlayer>();

            int n = p.SetMatCap(m4, m3);
            C(n, 1, "B1b 写进 1 个材质槽（本件只有那颗 renderer）");
            C(mr.sharedMaterials.Length, 1, "B1c 槽数没变（只换材质实例，不增删槽）");
            var written = mr.sharedMaterials[0];
            Tr(written != null && !ReferenceEquals(written, src), "B1d 换成了**新的材质实例**（不是原地改）");
            Tr(written != null && ReferenceEquals(written.GetTexture("_MatCap"), m4), "B1e `_MatCap` 槽真是那张 tier4");
            Tr(written != null && ReferenceEquals(written.GetTexture("_CardImage"), m3), "B1f `_CardImage` 槽也写了（原版那两条 SetTexture 的第 1 条）");

            // 🔴 灭自证：源材质 = `WarpforgeEffectBinder.Cache` 里那份**全效果共用**的
            Tr(ReferenceEquals(src.GetTexture("_MatCap"), srcTex0), "B2 源材质（共享缓存那份）**没被改** —— 复制语义生效");

            // B3 shader 上没有那两个槽 ⇒ 一个都不写、返 0（⛔ 不静默「写了」）
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            Tr(unlit != null, "B3a 找得到 `Universal Render Pipeline/Unlit`（本工程 prefab 里的降级占位 shader）");
            if (unlit != null)
            {
                var src2 = new Material(unlit) { name = "FXC_Unlit" };
                var go2 = new GameObject("FXC_SetMatCap_NoSlot");
                try
                {
                    var mr2 = go2.AddComponent<MeshRenderer>();
                    mr2.sharedMaterials = new[] { src2 };
                    var p2 = go2.AddComponent<WarpforgeEffectPlayer>();
                    int before = WarpforgeEffectPlayer.MatCapNoSlotCalls;
                    C(p2.SetMatCap(m4, m3), 0, "B3b 没有 `_MatCap`/`_CardImage` 槽 ⇒ 写 0 个（返 0）");
                    C(WarpforgeEffectPlayer.MatCapNoSlotCalls - before, 1, "B3c 「0 槽」被计数（⛔ 不许静默失败）");
                    Tr(ReferenceEquals(mr2.sharedMaterials[0], src2), "B3d 一个槽都没换 ⇒ 渲染器上还是原来那份材质");
                }
                finally { Object.DestroyImmediate(go2); Object.DestroyImmediate(src2); }
            }

            // B5 两个入参都空 ⇒ 返 0、不炸、不计数
            int beforeNull = WarpforgeEffectPlayer.MatCapNoSlotCalls;
            C(p.SetMatCap(null, null), 0, "B5 两个贴图都空 ⇒ 返 0（调用方拿不到图时不该炸）");
            C(WarpforgeEffectPlayer.MatCapNoSlotCalls - beforeNull, 0, "B5b 空入参**不算**「0 槽」失败（那是调用方的选择，不是缺陷）");
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(src);
        }
    }
}
