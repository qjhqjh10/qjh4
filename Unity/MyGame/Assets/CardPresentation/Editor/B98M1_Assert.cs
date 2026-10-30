// B98M1_Assert.cs — 第九十八会话 · 波 10 · 执行代理 `B98_M1`（卡牌线四条）的断言
//
// ============================ 它盯的是什么（判据只写在这里一次）============================
//  A 尾随换行  —— `Core/CardView.cs` 的 `DescTextWithSuffix`：原版描述**非空时补一个换行**，
//     且可用目标那一段是拼在**换行之后**。判据 = `RawCardScript.GetLocalizedCardDescWithLineBreak()`
//     （`.c`：`decomp_full/RawCardScript__GetLocalizedCardDescWithLineBreak.c:14-20`；
//       ILSpy 同体：`cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:2460-2472`）
//     ＋ 消费点 `BasicCardUI.cs:481/487`（把它的返回值直接喂 `SetTexts`）
//     ＋ `BattleCardUI.ShowAvailableTargets`（`descWithLineBreak + "<i>" + targets + "</i>"`）。
//  B 根脉冲    —— `Effects/CardDisplayLevelUp.cs` 的 `BeginPulse/Tick/AnimationFinished` ＋
//     `Core/CardView.cs` 的 `SetExtraScale`（乘在 `ApplyScale` 里）。
//     判据 = `判据/升级动画scale曲线_节点映射.md` §一·1 ① / §五·A（5 键 Hermite，峰值 ×1.155908/×1.155920/×1.017216 @1.45 s）。
//  C 卡框表    —— `Core/CardArtClanFrames.cs`（原版 `CardFramesByArmy` SO 的 13×4×2 转录）
//     ＋ `Core/CardArt.cs` 的 `Frame/Sdf` 两个 SO 优先钩子。
//     判据 = `assets_full/bundle_staticgeneralassets_assets_all/MonoBehaviour/CardFramesByArmy.json`（208 个引用 / 104 个卡框槽位）。
//  D 能力文本 id —— `Battle/SkillPanel.cs` 的 `AbilityKeyId` ＋ `Show(..., abilityTextReferenceId)`。
//     判据 = `RawCardScript.GetLocalizedAbilityDesc`（键 = `Card_Abilities/<activeAbilityTextReferenceId ?? uniqueId>`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1：把 `DescTextWithSuffix` 的 `baseText + "\n"` 改回裸 `baseText` ⇒ A1 红（实得没有尾巴）。
//  · A2：把换行挪到后缀**之后**（`body + "<i>…</i>" + "\n"`）⇒ A2 红。A1/A2 结构上不可能同时满足
//        （同一句判据不可能既「换行在末尾」又「换行在后缀之前」）。
//  · A3：把空描述也补 `"\n"`（无条件加）⇒ 那一层会被建出来 ⇒ `KeywordsText != null` ⇒ A3 红。
//  · B2：把峰值常量改成 1.0 ⇒ B2 红；把 `Tick` 里的 `StepPulse` 删掉 ⇒ B2/B4/B5 全红。
//  · B4：把末键的 `slope` 传 0（丢掉 si/so）⇒ 曲线在 1.45→1.5833 单调不欠冲 ⇒ B4 红
//        （**端点断言抓不到这条**，只有中段采样能抓）。
//  · B6：把脉冲改成「直接写 `transform.localScale`」（不走 `SetExtraScale`）⇒ 随后一次 `SetPose`
//        的 `ApplyScale()` 会把它冲掉 ⇒ B6 红。
//  · B7：`AnimationFinished` 里只归 x/y 忘了 z ⇒ B7 的 z 那一格红。
//  · C1/C2：表被删一行 / army 少一家 ⇒ 红（这条表是 SO 的逐条转录，缺一条就是漏槽）。
//  · C6：把 `SdfTier` 的复用那支删掉 ⇒ `(60,4,false)` 回 4 ⇒ 红；写成「所有 tier4 都回 3」⇒ 计数那条红。
//  · C8：把 `CardArt.Frame` 的 SO 钩子删掉 **不会** 让 C8 变红（今天图没导，两条路都回自绘）——
//        所以 C8 只证明「零回归」。钩子本身的判别力在 `FrameTexture` 的命名/矩形那几条（C4/C5）。
//  · D1/D2：`AbilityKeyId` 改成恒回 `uniqueId` ⇒ D1 红；改成恒回 ref（空串也回）⇒ D2 红。
//  · D4：把 `Show` 的 `abilityTextReferenceId` 形参删掉 ⇒ D4 红（结构条，防「接了实现但没留接线口」）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Editor/BattleScene.cs` 或 `Editor/RuleEngineTest.cs` 的 `Run()` 里加：
//      B98M1_Assert.Run(); _pass += B98M1_Assert.Pass; _fail += B98M1_Assert.Fail;
//  （宿主由调度台代接线；本文件只碰 `CardPresentation` 的只读口 ＋ 自建临时 GameObject。）
using System;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B98_M1`：卡牌线四条（尾随换行 / 升级根脉冲 / 卡框 SO 组装 / 能力文本 id）的断言。</summary>
public static class B98M1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[M1] ") { Near = MenuNearStyle.Compact3 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B98_M1 卡牌线四条：尾随换行 / 升级根脉冲 / 卡框 SO 组装 / 能力文本 id");
        SectionA_TrailingNewline();
        SectionB_LevelUpRootPulse();
        SectionC_ClanFrames();
        SectionD_AbilityTextReferenceId();
        Debug.Log(_sink.P + "B98M1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==========================================================================================
    //  A 段 —— 效果文字少一个尾随 `\n`（`Core/CardView.cs` `DescTextWithSuffix`）
    // ==========================================================================================
    static void SectionA_TrailingNewline()
    {
        MenuCheck.Section(_sink, "A 尾随换行：原版 `GetLocalizedCardDescWithLineBreak()` = 非空描述 `AppendLine()`");

        var root = new GameObject("B98M1_A_ROOT");
        CardView v = null, vEmpty = null;
        try
        {
            // C0 = 前提硬检查（⛔ 不静默跳过）
            Tr(TmpFont.Available, "A0 **前提**：TMP 字体资产可用（`TmpFont.Available`）—— 否则文字层根本不建，A1–A3 等于没跑");

            const string desc = "Deal 3 damage to an enemy unit";
            v = CardView.Create(root.transform, Make(desc), "B98M1_A1");
            Tr(v.KeywordsText != null, "A0-a **前提**：描述那一层建出来了（`KeywordsText != null`）");
            if (v.KeywordsText == null) return;

            C(v.KeywordsText.text, desc + "\n",
              "A1 描述层文本 = 卡面正文 **+ 一个尾随换行**（原版 `AppendLine()`）"
            + "｜🧨 把 `DescTextWithSuffix` 的 `baseText + \"\\n\"` 改回裸 `baseText` ⇒ 本行红");

            v.SetDescriptionSuffix("Select 1 target");
            C(v.KeywordsText.text, desc + "\n<i>Select 1 target</i>",
              "A2 **换行在后缀之前**（原版 `descWithLineBreak + \"<i>\" + targets + \"</i>\"`）"
            + "｜🧨 把换行挪到后缀之后 ⇒ 本行红；A1/A2 结构上不可能同时满足");

            vEmpty = CardView.Create(root.transform, Make(""), "B98M1_A2");
            Tr(vEmpty.KeywordsText == null,
               "A3 **空描述不补换行**（原版的闸是 `if (sb.Length > 0)`）—— 空描述那一层不建，⛔ 不许留一个只有换行的空层"
             + "｜🧨 无条件补 `\"\\n\"` ⇒ 这层会被建出来并带一个换行 ⇒ 本行红");
        }
        finally
        {
            if (v != null) UnityEngine.Object.DestroyImmediate(v.gameObject);
            if (vEmpty != null) UnityEngine.Object.DestroyImmediate(vEmpty.gameObject);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>夹具卡（`keywords` = 卡面正文那一格）。</summary>
    static CardData Make(string keywords, string title = "B98M1")
    {
        return new CardData
        {
            id = title, title = title,
            isUnit = true, type = "troop", faction = "Ultramarines",
            cost = 1, melee = 1, health = 1,
            keywords = keywords ?? "",
        };
    }

    // ==========================================================================================
    //  B 段 —— 升级动画的「根脉冲」（×1.155908/×1.155920/×1.017216 @1.45 s）
    // ==========================================================================================
    static void SectionB_LevelUpRootPulse()
    {
        MenuCheck.Section(_sink, "B 升级根脉冲：原版 `Level Up Card` clip 第 ① 条 scale 曲线（代码补间）");

        var root = new GameObject("B98M1_B_ROOT");
        CardView view = null;
        CardDisplayLevelUp lv = null;
        try
        {
            view = CardView.Create(root.transform, Make("", "B98M1_Pulse"), "B98M1_B1");
            view.SetPose(Vector3.zero, 0f, 2f);              // 基准缩放 2（≠1，防「写死 250/108」蒙对）
            Tr(Mathf.Approximately(view.BaseScale, 2f), "B0 **前提**：`CardView.BaseScale == 2`（基准不是 1，否则「写死」也能蒙对）");
            Tr(Mathf.Approximately(view.transform.localScale.x, 2f), "B0-a **前提**：静止缩放 = 基准（`ApplyScale` 出厂一致）");

            var go = new GameObject("B98M1_LevelUp");
            go.transform.SetParent(root.transform, false);
            lv = go.AddComponent<CardDisplayLevelUp>();
            lv.pulseTarget = view;

            lv.BeginPulse(); lv.Tick(0f);
            Near(lv.PulseRatio.x, 1f, 1e-5f, "B1 t=0 ⇒ 比值 1（脉冲起点不动）");
            Near(view.transform.localScale.x, 2f, 1e-4f, "B1-a t=0 ⇒ 卡视图缩放仍 = 基准（没被预先放大）");

            lv.BeginPulse(); lv.Tick(1.2f);
            Near(lv.PulseRatio.x, 1f, 1e-5f, "B2 t=1.2 ⇒ 比值仍 1（原版前 1.4333 s 是平的）"
                                          + "｜🧨 把 `PulseRiseTime` 改成 0 ⇒ 本行红");

            lv.BeginPulse(); lv.Tick(CardDisplayLevelUp.PulsePeakTime);
            Near(lv.PulseRatio.x, 1.155907958984375f, 2e-4f, "B3 峰值 x = 288.97699/250 = 1.155908"
                                                            + "｜🧨 把峰值常量改成 1.0 / 把 `Tick` 里的 `StepPulse` 删掉 ⇒ 红");
            Near(lv.PulseRatio.y, 1.1559200439453125f, 2e-4f, "B3-b 峰值 y = 288.98001/250 = 1.155920（与 x 差 0.000012，原版就是两条曲线）");
            Near(lv.PulseRatio.z, 1.0172164306640625f, 2e-4f, "B3-c 峰值 z = 254.30411/250 = 1.017216（三轴各一条，⛔ 别拿 x 顶 z）");
            Near(view.transform.localScale.x, 2f * 1.155907958984375f, 5e-4f,
                 "B4 卡视图 `localScale` = **基准 2 × 比值**（写入口必须是 `ApplyScale` 那一跳）");

            // 末键那一对 slope ⇒ 1.45→1.5833 必须**先跌到 1 以下**再回 1（欠冲 ≈ 0.99929）
            float min = 99f, minT = 0f;
            for (float t = 1.45f; t <= 1.5834f; t += 0.0025f)
            {
                lv.BeginPulse(); lv.Tick(t);
                if (lv.PulseRatio.x < min) { min = lv.PulseRatio.x; minT = t; }
            }
            Tr(min < 0.9998f && min > 0.99f,
               "B5 末键 `inSlope = 72.93/250` 造成**欠冲**（极小 " + min.ToString("F6") + " @ t=" + minT.ToString("F4")
             + "，原版 249.822474/250 = 0.99929）｜🧨 把 `PulseCurve` 末键的 slope 传 0 ⇒ 单调不回跌 ⇒ 本行红（端点断言抓不到）");

            // 结构：脉冲在 `ApplyScale` 里 ⇒ 任何一次重排（`SetPose`）之后它**仍然在**
            lv.BeginPulse(); lv.Tick(CardDisplayLevelUp.PulsePeakTime);
            view.SetPose(new Vector3(0.3f, 0.2f, 0f), 7f, 2f);      // ← 重排 / 换位会重写缩放
            Near(view.transform.localScale.x, 2f * 1.155907958984375f, 5e-4f,
                 "B6 **灭自证**：`SetPose`（走 `ApplyScale`）之后脉冲**还在** —— 证明它乘在 `ApplyScale` 里，"
               + "不是有人直接写 `transform.localScale`｜🧨 改成直写 `localScale` ⇒ 本行红");

            // 收尾：三轴都要归 1（原版末键三轴都是 250）
            lv.AnimationFinished();
            C(lv.PulseActive, false, "B7 `AnimationFinished` ⇒ 脉冲停");
            Near(view.transform.localScale.x, 2f, 1e-4f, "B7-a 收尾 x 归 1（缩放回基准）");
            Near(view.transform.localScale.z, 2f, 1e-4f, "B7-b 收尾 **z 也要归 1**｜🧨 只归 x/y 忘了 z ⇒ 本行红");
            Near(lv.PulseRatio.x, 1f, 1e-5f, "B7-c `PulseRatio` 归 1");

            lv.BeginPulse(); lv.Tick(CardDisplayLevelUp.PulseEndTime + 0.2f);
            C(lv.PulseActive, false, "B8 超过 `PulseEndTime` 自己收尾（原版末键之后曲线恒 1）");
            Near(view.transform.localScale.x, 2f, 1e-4f, "B8-a …而且缩放已回基准（不会停在末键的中间值上）");
        }
        finally
        {
            if (lv != null) UnityEngine.Object.DestroyImmediate(lv.gameObject);
            if (view != null) UnityEngine.Object.DestroyImmediate(view.gameObject);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ==========================================================================================
    //  C 段 —— `CardFramesByArmy` SO 组装（13 阵营 × 4 档 × 2 类型 = 104 槽位）
    // ==========================================================================================
    static void SectionC_ClanFrames()
    {
        MenuCheck.Section(_sink, "C 卡框 SO 组装：原版 `CardFramesByArmy`（208 引用 / 104 卡框槽位 / 104 SDF）");

        C(CardArtClanFrames.Count, 104, "C1 表里 104 条槽位（13 阵营 × 4 档 × 2 类型）"
                                     + "｜🧨 删掉任一行 ⇒ 红");

        int[] armies = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 };
        int miss = 0, badRect = 0;
        // 🔴 2026-10-30 就地订正（`B100_F24` · 铁律 5 · `报告/B100_X3` #24）：这条原来断的是
        //   「**拍的**启发式域」(`宽 590–690 · 高 900–980`)，它把一格**真值**判成了越界 ——
        //   army 40 / tier 4 / stratagem = `(191.04291, 11, 640.93036, 998)`（`CardArtClanFrames.cs:178`）。
        //   **回源现核（本代理亲读）**：原版那个 Sprite 是
        //   `d:/2/新解包资源/assets_full/bundle_necronssautekhcardassets_assets_all/Sprite/Orc Frame_6974505386267810414.json`，
        //   它的 `m_RD.textureRect` = `{x:191.04290771484375, y:11.0, width:640.9303588867188, height:998.0}`
        //   —— 与表里那一行**逐字相同**（同 bundle 里 army40/tier4/troop 那行 `(191.04291, 29.07612, 640.93036, 961.8478)`
        //   也对上 `Orc Frame_-7166691385669469535.json`）⇒ **不是转录回退，是真的**。
        //   998 高是正当的：`m_Rect` 高也是 998、纹理 1024（`textureRect` 落 `y=11…1009`）。
        //   ⇒ 判据换成**原版自带的硬约束**：`textureRect` 必须落在 `m_Rect`（104/104 同一块 `(171,11,682,998)`，见
        //   `CardArtClanFrames.SpriteRectX/Y/W/H`）里面，外加「宽高 > 0」。⛔ 别再拿「看着像卡框的宽高」当域。
        const float MRX = CardArtClanFrames.SpriteRectX, MRY = CardArtClanFrames.SpriteRectY;
        const float MRR = CardArtClanFrames.SpriteRectX + CardArtClanFrames.SpriteRectW;   // m_Rect 右边界 = 853
        const float MRT = CardArtClanFrames.SpriteRectY + CardArtClanFrames.SpriteRectH;   // m_Rect 上边界 = 1009
        for (int a = 0; a < armies.Length; a++)
            for (int tier = 1; tier <= 4; tier++)
                for (int k = 0; k < 2; k++)
                {
                    CardArtClanFrames.Entry e;
                    if (!CardArtClanFrames.TryGet(armies[a], tier, k == 1, out e)) { miss++; continue; }
                    if (e.W <= 0f || e.H <= 0f) { badRect++; continue; }
                    if (e.X < MRX - 1e-3f || e.Y < MRY - 1e-3f) { badRect++; continue; }
                    if (e.X + e.W > MRR + 1e-3f || e.Y + e.H > MRT + 1e-3f) { badRect++; continue; }
                }
        C(miss, 0, "C2 13×4×2 = 104 格**全部**在表里（缺一格 = 漏一个原版槽位）");
        C(badRect, 0, "C3 每条 `textureRect`（`Entry.X/Y/W/H`，⛔ 不是 `m_Rect`）都落在原版 Sprite 的 "
                    + "`m_Rect (171,11,682,998)` 里（= `x≥171 ∧ y≥11 ∧ x+W≤853 ∧ y+H≤1009`）+ 宽高 > 0"
                    + "｜🧨 把某一格写成 `(0,0,0,0)`（= `frames_resolved.json` 里那种缺失回退）或越出 `m_Rect` ⇒ 红");
        Near(CardArtClanFrames.PixelsToUnits, 100f, 1e-6f, "C3-b `m_PixelsToUnits` = 100（原版 104/104 全 100）");

        CardArtClanFrames.Entry e10;
        Tr(CardArtClanFrames.TryGet(10, 1, false, out e10), "C4 **前提**：army 10 的 tier1 部队槽在表里");
        if (CardArtClanFrames.TryGet(10, 1, false, out e10))
        {
            Near(e10.X, 199.07613f, 1e-4f, "C4 Ultramarines/1 部队 卡框矩形 x = 199.07613（原版 `textureRect`）");
            Near(e10.Y, 30.068026f, 1e-4f, "C4-b …y = 30.068026");
            Near(e10.W, 611.89716f, 1e-4f, "C4-c …宽 611.89716");
            Near(e10.H, 939.85583f, 1e-4f, "C4-d …高 939.85583");
        }
        // 🆕 2026-10-30（`B100_F24` · `报告/B100_X3` #24）：把**当初被判越界的那一格**钉成逐值断言 ——
        //   这样将来「有人拿启发式把 998 改成 980」或者「这一格回退成 (0,0,0,0)」都会红，而不用再靠`域`的宽窄去猜。
        //   判据（本代理亲读原版资产）：
        //   `d:/2/新解包资源/assets_full/bundle_necronssautekhcardassets_assets_all/Sprite/Orc Frame_6974505386267810414.json`
        //   → `m_RD.textureRect` = `{191.04290771484375, 11.0, 640.9303588867188, 998.0}`（`m_Rect` = `(171,11,682,998)`）。
        CardArtClanFrames.Entry e40s;
        Tr(CardArtClanFrames.TryGet(40, 4, true, out e40s), "C4-c **前提**：army 40 的 tier4 战术槽在表里");
        if (CardArtClanFrames.TryGet(40, 4, true, out e40s))
        {
            Near(e40s.X, 191.04291f, 1e-4f, "C4-e Sautekh/4 战术 `textureRect` x = 191.04291（原版 `Orc Frame_6974505386267810414.json`）");
            Near(e40s.Y, 11f, 1e-4f, "C4-f …y = 11（= 贴到 `m_Rect` 下沿，合法）");
            Near(e40s.W, 640.93036f, 1e-4f, "C4-g …宽 640.93036");
            Near(e40s.H, 998f, 1e-4f, "C4-h …高 998 ⚠️ **不是 980**：原版就是 `y=11…1009` 占满 `m_Rect` 高"
                                    + "｜🧨 按「看着像卡框」把它压到 980 以内 ⇒ 这一行红（真值被改）");
        }
        C(CardArtClanFrames.FrameTextureName(10, 1, false), "40k_Cardframe_troop_Ultramarines_tier1",
          "C5 纹理名（army 10–90 的写法：`40k_Cardframe_` + 小写 troop）");
        C(CardArtClanFrames.FrameTextureName(90, 3, true), "40k_Cardframe_stratagem_GSC_tier3",
          "C5-b 纹理名里的阵营段 ≠ `CardDef.Faction`（`Genestealers` → **`GSC`**）");
        C(CardArtClanFrames.ResourceName(100, 4, false), "40k_Cardframes_Troop_Astra_Militarum_tier4",
          "C5-c army 100–130 是**另一种原版写法**（`Cardframes` 多一个 s、`Troop` 大写），空格换下划线");
        C(CardArtClanFrames.FrameTextureName(120, 2, true), "40k_Cardframes_stratagem_Emperor Children_tier2",
          "C5-d 原版阵营段带空格的（`Emperor Children` / `Space Wolves` / `Astra Militarum`）逐字保住");

        int ovr = 0;
        for (int a = 0; a < armies.Length; a++)
            for (int tier = 1; tier <= 4; tier++)
                for (int k = 0; k < 2; k++)
                    if (CardArtClanFrames.SdfTier(armies[a], tier, k == 1) != tier) ovr++;
        C(ovr, 1, "C6 SDF 档位复用**只有一格**（原版 SO 里 `(60, tier4, 部队)` 指向 tier3 那一张）"
                + "｜🧨 把 `SdfTier` 删掉 ⇒ 0 格；写成「tier4 全回 3」⇒ 这里变 13 格 ⇒ 红");
        C(CardArtClanFrames.SdfTier(60, 4, false), 3, "C6-b Leviathan(60) tier4 **部队** 的高亮 = tier3 那张 SDF（原版两个槽位同一个 `m_AssetGUID`）");
        C(CardArtClanFrames.SdfTier(60, 4, true), 4, "C6-c …但 tier4 **战术**仍是自己的 tier4（别把复用扩大到别人身上）");

        // C7：SDF 那半边**已经全导进来了**（13×4×2 = 104，直接按文件名核，不走 `CardArt.Sdf` 的 generic 兜底）
        string[] facs = { "ultramarines","goff","saimhann","sautekh","blacklegion","leviathan","tauempire",
                          "sororitas","genestealers","astramilitarum","darkangels","emperorschildren","spacewolves" };
        int sdfMiss = 0;
        for (int i = 0; i < facs.Length; i++)
            for (int tier = 1; tier <= 4; tier++)
                for (int k = 0; k < 2; k++)
                    if (Resources.Load<Texture2D>("Art/card_sdf/" + facs[i] + (k == 1 ? "_strat" : "") + "_tier" + tier) == null)
                        sdfMiss++;
        C(sdfMiss, 0, "C7 原版 104 张 **SDF（高亮）**图都在 `Resources/Art/card_sdf/`（13×4×2 逐条 `Resources.Load` 核，⛔ 不走 `CardArt.Sdf` 的 `generic` 兜底）");

        // C8：卡框贴图**已导完** ⇒ 必须取得到（2026-10-30 反转：原断「还没导」）
        Tr(CardArtClanFrames.FrameTexture(10, 1, false) != null,
           "C8 **原版卡框贴图 104/104 已就位** ⇒ `FrameTexture()` 取得到（🔴 2026-10-30 `B100_F12` 就地反转："
         + "原文断「还没导 ⇒ 回 null」，而 24 张带空格的文件名已归一为下划线 ⇒ 现读 104/104 命中）");
        Tr(CardArt.Frame("Ultramarines", null, false, 1) != null,
           "C8-b 取不到原版 ⇒ `CardArt.Frame` 落回自绘那套（今天**零行为变化**，不是空白框）");
        Tr(CardArt.Sdf("Leviathan", null, false, 4) != null,
           "C8-c SDF 那一路也照常取得到（复用修正只是换了档，不是把图弄丢）");
    }

    // ==========================================================================================
    //  D 段 —— `activeAbilityTextReferenceId` 消费端（`Battle/SkillPanel.cs`）
    // ==========================================================================================
    static void SectionD_AbilityTextReferenceId()
    {
        MenuCheck.Section(_sink, "D 能力文本 id：键 = `Card_Abilities/<activeAbilityTextReferenceId ?? uniqueId>`");

        C(SkillPanel.AbilityKeyId("SOR30", "SOR30_new"), "SOR30_new",
          "D1 ref 非空 ⇒ 用 **ref**（原版 `Desc()` 的 id 那一半）"
        + "｜🧨 改成恒回 `uniqueId` ⇒ 红");
        C(SkillPanel.AbilityKeyId("SOR30", null), "SOR30",
          "D2 ref 缺席 ⇒ 回 `uniqueId`（原版回退支）");
        C(SkillPanel.AbilityKeyId("SOR30", ""), "SOR30",
          "D2-b ref 是**空串** ⇒ 同样回 `uniqueId`（`IsNullOrEmpty` 语义，⛔ 别只判 `null`）"
        + "｜🧨 改成 `ref ?? uniqueId`（只判 null）⇒ 本行红");

        string oldText = Loc.T("Card_Abilities/SOR30"), newText = Loc.T("Card_Abilities/SOR30_new");
        Tr(!string.IsNullOrEmpty(oldText) && oldText != "Card_Abilities/SOR30",
           "D3 **前提**：官方表里 `Card_Abilities/SOR30` 有值（否则 D3-b 没意义）");
        Tr(!string.IsNullOrEmpty(newText) && newText != oldText,
           "D3-b 这两条键的正文**确实不同** ⇒ 不带 ref 会印成旧文本（这就是那条玩家可见的偏差）");

        var mi = typeof(SkillPanel).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);
        bool hasParam = false;
        if (mi != null)
            foreach (var p in mi.GetParameters())
                if (p.Name == "abilityTextReferenceId") hasParam = true;
        Tr(hasParam, "D4 **结构**：`SkillPanel.Show` 留了 `abilityTextReferenceId` 这个接线口"
                   + "（否则 `BattleDriver` 那边就算拿到 id 也传不进来）｜🧨 删掉那个形参 ⇒ 红");
    }
}
