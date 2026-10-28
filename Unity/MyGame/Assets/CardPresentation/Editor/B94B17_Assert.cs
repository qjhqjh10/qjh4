// B94B17_Assert.cs —— 执行代理 B17（第九十四会话 · 波 4）的断言：**分数条组件族**
//
// ============================ 它盯的是什么 ============================
//   账 430 `MilestoneScoreBar` / 账 496 `ProgressBar` / 账 295 `FactionScore` / `GenericScoreBarLine`
//   —— 四个原版 MonoBehaviour 的**本体**（字段表 + 方法体）+ `AllianceRatingDisplay`（`FactionScore` 的必要件）。
//   判据 → `资料/完全复刻/判据/B94_B6_判据补全.md` §5 / §8 与 `资料/完全复刻/报告/B94_B17.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `B94B17Assert.Run();`
//   + 宿主里两行（照 `B94B6_Assert` 的先例）：
//     `_sink.Pass += B94B17Assert.Pass; _sink.Fail += B94B17Assert.Fail;`
//   · 推荐宿主 = **`Editor/ShellScene.cs`**（这三个组件都挂在壳的窗上；`SkirmishModeEventWindow` /
//     `EnergySinglePlayerOnlyEventWindow` / `Two Sides Event Window` / `Player Profile Window` 都在壳面）。
//   · 本文件**不建持久资产**；建的 `GameObject` / `Texture2D` 全部在 `finally` 里 `DestroyImmediate`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 组：把任一字段改名 / 增删一格 ⇒ 红（它是「照 `dump.cs` 建」这件事的地基）。
//   · P2：把 `IsFilled = value >= maxValue` 写成 `>` ⇒ `SetProgress(300,300)` 那条红。
//   · P6：把 `SetProgress(ScoringDataInfo)` 改成用 `PreviousLevelScore` ⇒ 文本变 `150/999` ⇒ 红。
//   · M3：把 `normalizedValue` 改成线性 `current/max` ⇒ `0.68` 变 `0.2` ⇒ 红。
//   · G4：把 `Unlocked` 的箱子图写成 `chestOpen` ⇒ 红（原版**只有 `Collected` 换图**）。
//   · F3：把 `Big` 的第三行抄成 `Alliance Rating Display (1)` ⇒ 红（原版叫 `MaxRating`）。
//   · F5：把 `Big` 的几何抄成 `Small` 的（`icon 212.63`）⇒ 红（原版 `Big` 是 `164`）。
using System.Reflection;
using CardPresentation;
using SB = CardPresentation.ScoreBars;
using UnityEngine;

/// <summary>执行代理 B17：分数条组件族（`MilestoneScoreBar` / `ProgressBar` / `GenericScoreBarLine` /
/// `FactionScore` / `AllianceRatingDisplay`）的字段表 + 行为 + 两套变体几何。
/// <para>计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。⛔ 期望值全是**字面量**，不从被测实现里读回来。</para></summary>
public static class B94B17Assert
{
    static readonly CheckSink _sink = new CheckSink("[B17] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B17 分数条组件族：字段表 / ProgressBar / MilestoneScoreBar / GenericScoreBarLine / FactionScore 两变体");

        SectionA_字段表();
        SectionP_ProgressBar();
        SectionM_MilestoneScoreBar();
        SectionG_GenericScoreBarLine();
        SectionF_FactionScore两变体();

        Debug.Log(_sink.P + "B17 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 助手：反射
    static FieldInfo SerField<T>(string name)
        => typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

    static void HasField<T>(string name, System.Type wantType, string who)
    {
        var f = SerField<T>(name);
        Tr(f != null, who + "：`[SerializeField] " + wantType.Name + " " + name + "` 存在（字段名逐字照 `dump.cs`）");
        if (f != null)
        {
            C(f.FieldType.Name, wantType.Name, who + "：`" + name + "` 的类型");
            Tr(f.GetCustomAttributes(typeof(SerializeField), true).Length > 0,
               who + "：`" + name + "` 带 `[SerializeField]`（原版就是序列化字段）");
        }
    }

    /// <summary>只数带 `[SerializeField]` 的自身字段 —— 多一格/少一格都要红。</summary>
    static int SerFieldCount<T>()
    {
        int n = 0;
        foreach (var f in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                              BindingFlags.Public | BindingFlags.DeclaredOnly))
            if (f.GetCustomAttributes(typeof(SerializeField), true).Length > 0) n++;
        return n;
    }

    // ============================================================ A 字段表（逐类钉死）
    static void SectionA_字段表()
    {
        MenuCheck.Section(_sink, "A 五个类的序列化字段表（名字 / 类型 / 格数 —— 判据 `dump.cs`）");

        // `AllianceRatingDisplay`：`+0x20 EverguildTextMeshPro ratingText` · `+0x28 Image ratingIcon`
        HasField<SB.AllianceRatingDisplay>("ratingText", typeof(Label), "AllianceRatingDisplay");
        HasField<SB.AllianceRatingDisplay>("ratingIcon", typeof(ImageQuad), "AllianceRatingDisplay");
        C(SerFieldCount<SB.AllianceRatingDisplay>(), 2, "AllianceRatingDisplay：序列化字段 = **2** 格");

        // `ProgressBar`：`+0x20 Slider slider` · `+0x28 EverguildTextMeshPro text`（`+0x30 IsFilled` 是自动属性）
        HasField<SB.ProgressBar>("slider", typeof(UnityEngine.UI.Slider), "ProgressBar");
        HasField<SB.ProgressBar>("text", typeof(Label), "ProgressBar");
        C(SerFieldCount<SB.ProgressBar>(), 2, "ProgressBar：序列化字段 = **2** 格（`IsFilled` 是自动属性、不序列化）");

        // `GenericScoreBarLine`：五格（`+0x48 originalChest` 不序列化）
        HasField<SB.GenericScoreBarLine>("score", typeof(Label), "GenericScoreBarLine");
        HasField<SB.GenericScoreBarLine>("chestReward", typeof(ImageQuad), "GenericScoreBarLine");
        HasField<SB.GenericScoreBarLine>("chestOpen", typeof(Texture2D), "GenericScoreBarLine");
        HasField<SB.GenericScoreBarLine>("readyToCollect", typeof(GameObject), "GenericScoreBarLine");
        HasField<SB.GenericScoreBarLine>("resourceIcon", typeof(ImageQuad), "GenericScoreBarLine");
        C(SerFieldCount<SB.GenericScoreBarLine>(), 5, "GenericScoreBarLine：序列化字段 = **5** 格（`originalChest` 不序列化）");

        // `MilestoneScoreBar`：两格 —— 🔴 **`progressBar` 是 `Slider`**（不是 `ProgressBar`；B6 §5 那句是误读）
        HasField<SB.MilestoneScoreBar>("progressBar", typeof(UnityEngine.UI.Slider), "MilestoneScoreBar");
        HasField<SB.MilestoneScoreBar>("scoringLines", typeof(SB.GenericScoreBarLine[]), "MilestoneScoreBar");
        C(SerFieldCount<SB.MilestoneScoreBar>(), 2, "MilestoneScoreBar：序列化字段 = **2** 格（**没有** `resourceIcon`）");

        // `FactionScore`：三格
        HasField<SB.FactionScore>("factionImage", typeof(ImageQuad), "FactionScore");
        HasField<SB.FactionScore>("ratingDisplay", typeof(SB.AllianceRatingDisplay), "FactionScore");
        HasField<SB.FactionScore>("highestRatingDisplay", typeof(SB.AllianceRatingDisplay), "FactionScore");
        C(SerFieldCount<SB.FactionScore>(), 3, "FactionScore：序列化字段 = **3** 格");
    }

    // ============================================================ P `ProgressBar`
    static void SectionP_ProgressBar()
    {
        MenuCheck.Section(_sink, "P 原版 `ProgressBar.SetProgress`（VA 反汇编 `0x1807BA970`）");

        var root = new GameObject("B17_P", typeof(RectTransform));
        try
        {
            var go = new GameObject("ProgressBar", typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var slider = go.AddComponent<UnityEngine.UI.Slider>();
            var label = Label.Create(go.transform, "0/0", Vector3.zero, 4, Color.white,
                                     new Vector2(0.5f, 0.5f), "counter");
            Tr(label != null, "P0 前提：`counter` 标签建出来了（后面几条读它的 `Text`）");
            var pb = go.AddComponent<SB.ProgressBar>();
            pb.Bind(slider, label);

            pb.SetProgress(150, 300);
            Near(slider.maxValue, 300f, 1e-3f, "P1a `slider.maxValue` = maxValue 形参");
            Near(slider.value, 150f, 1e-3f, "P1b `slider.value` = value 形参");
            C(label != null ? label.Text : null, "150/300", "P1c 文本 = `String.Format(\"{0}/{1}\", value, maxValue)`");
            C(pb.IsFilled, false, "P1d `IsFilled` = false（150 < 300）");
            C(slider.wholeNumbers, true, "P1e 默认 `useWholeNumbers = true` 真写进了 `slider.wholeNumbers`（ILSpy 把它丢成 default）");

            // 🔴 灭自证：`setge`（等号算满）；写成 `>` 这条就红
            pb.SetProgress(300, 300);
            C(pb.IsFilled, true, "P2 ★ `IsFilled = value >= maxValue`（**等号算满**；写成 `>` ⇒ 红）");

            pb.SetProgress(10, 100, "{0} of {1}", false);
            C(label != null ? label.Text : null, "10 of 100", "P3a `format` 形参生效");
            C(slider.wholeNumbers, false, "P3b `useWholeNumbers = false` 写下去了");

            // 外层那一跳：CurrentScore(+0x10) / NextLevelScore(+0x18)；PreviousLevelScore(+0x14) **不参与**
            pb.SetProgress(new ScoringDataInfo(150, 999, 300));
            Near(slider.maxValue, 300f, 1e-3f, "P4a ★ `SetProgress(ScoringDataInfo)` 的 maxValue = `NextLevelScore`（300）");
            C(label != null ? label.Text : null, "150/300", "P4b ★ 用 `CurrentScore/NextLevelScore`（改用 `PreviousLevelScore` ⇒ `150/999`）");
            C(pb.IsFilled, false, "P4c 150 < 300 ⇒ false");

            pb.SetProgress((ScoringDataInfo)null);
            C(label != null ? label.Text : null, "150/300", "P5 `null` ⇒ **不改任何值**（原版这一跳是 NRE；我们出声返回）");
        }
        finally { Object.DestroyImmediate(root); }
    }

    // ============================================================ M `MilestoneScoreBar`
    static void SectionM_MilestoneScoreBar()
    {
        MenuCheck.Section(_sink, "M 原版 `MilestoneScoreBar`（`SetProgressBar` 用的是 `InterpolateMilestones`，**不是**线性归一）");

        var root = new GameObject("B17_M", typeof(RectTransform));
        try
        {
            var barGo = new GameObject("MilestoneScoreBar", typeof(RectTransform));
            barGo.transform.SetParent(root.transform, false);
            var slider = barGo.AddComponent<UnityEngine.UI.Slider>();
            var msb = barGo.AddComponent<SB.MilestoneScoreBar>();

            var lines = new SB.GenericScoreBarLine[4];
            var labels = new Label[4];
            for (int i = 0; i < 4; i++)
            {
                var lg = new GameObject("Score Bar Line Level " + (i + 1), typeof(RectTransform));
                lg.transform.SetParent(barGo.transform, false);
                labels[i] = Label.Create(lg.transform, "?", Vector3.zero, 4, Color.white,
                                         new Vector2(0.5f, 0.5f), "Score");
                lines[i] = lg.AddComponent<SB.GenericScoreBarLine>();
                lines[i].Bind(labels[i], null, null, null, null);   // 🔴 前四个实参：score / chest / open / ready / icon
            }
            msb.Bind(slider, lines);

            // 阈值**故意非等距**（{100, 500, 3000}）—— 等距时「按档等分」与「线性归一」会撞在一起，分不出来
            var ms = new[] { M(100), M(500), M(3000) };
            msb.Initialize(600, ms);

            Near(slider.maxValue, 3000f, 1e-3f, "M1 `progressBar.maxValue = thresholds.Last()`");
            C(slider.wholeNumbers, false, "M2 `progressBar.wholeNumbers = false`（**常量**，不是形参）");
            Near(slider.normalizedValue, 0.68f, 1e-3f,
                 "M3 ★ `InterpolateMilestones(600, {100,500,3000})` = **0.68**（按档等分 2/3→1 的段内插值）；"
               + "写成线性 `600/3000` ⇒ 0.2 ⇒ 红");
            C(msb.LastThresholds.Length, 3, "M4 阈值表长度 = 3");
            C(msb.LastThresholds[0], 100, "M5a 阈值[0] = 100（= `milestone.score`）");
            C(msb.LastThresholds[1], 500, "M5b 阈值[1] = 500");
            C(msb.LastThresholds[2], 3000, "M5c 阈值[2] = 3000");

            C(labels[0] != null ? labels[0].Text : null, "100", "M6a 第 1 行的 `Score` = 阈值 100");
            C(labels[1] != null ? labels[1].Text : null, "500", "M6b 第 2 行 = 500");
            C(labels[2] != null ? labels[2].Text : null, "3000", "M6c 第 3 行 = 3000");
            C(labels[3] != null ? labels[3].Text : null, "?",
              "M6d ★ **行多于阈值 ⇒ 第 4 行不初始化**（原版打完那句 `[ERROR] … more scoring lines that thresholds` 就 return）");

            // 空阈值表：`maxValue = LastOrDefault() = 0`、进度 0
            msb.Initialize(10, new GenericScoringMilestone[0]);
            Near(slider.maxValue, 0f, 1e-3f, "M7a 空阈值表 ⇒ `maxValue = 0`（`LastOrDefault`）");
            Near(slider.normalizedValue, 0f, 1e-3f, "M7b 空阈值表 ⇒ 进度 0（⛔ 不除零、不猜）");
            C(msb.LastThresholds.Length, 0, "M7c 空阈值表 ⇒ `GetScoringThresholds` 回空表");

            // null 阈值表：原版 `break` 出循环后落到 NRE；我们出声返回（前一行都不该被改）
            msb.Initialize(10, null);
            C(labels[0] != null ? labels[0].Text : null, "100", "M8 null 阈值表 ⇒ 一行都不初始化（原版是 NRE；我们出声返回）");
        }
        finally { Object.DestroyImmediate(root); }
    }

    static GenericScoringMilestone M(int score, GenericScoringMilestoneState state = GenericScoringMilestoneState.Locked)
        => new GenericScoringMilestone { score = score, state = state, rewards = new CampaignData.RewardSpec[0] };

    // ============================================================ G `GenericScoreBarLine`
    static void SectionG_GenericScoreBarLine()
    {
        MenuCheck.Section(_sink, "G 原版 `GenericScoreBarLine.Initialize` 三档（`Locked` 与 `Unlocked` **箱子图同一张**）");

        var root = new GameObject("B17_G", typeof(RectTransform));
        var texFactory = new Texture2D(4, 4); texFactory.name = "B17_chestFactory";
        var texOpen = new Texture2D(4, 4); texOpen.name = "B17_chestOpen";
        try
        {
            var lg = new GameObject("Score Bar Line Level 1", typeof(RectTransform));
            lg.transform.SetParent(root.transform, false);
            var label = Label.Create(lg.transform, "?", Vector3.zero, 4, Color.white, new Vector2(0.5f, 0.5f), "Score");
            var chest = ImageQuad.Create(lg.transform, texFactory, Vector3.zero, 0.1f, new Vector2(0.5f, 0.5f), "Chest");
            Tr(chest != null, "G0 前提：`Chest` 的 quad 建出来了（下面按它的 `Texture` 判档）");
            var ready = new GameObject("Highlight Crate", typeof(RectTransform));
            ready.transform.SetParent(lg.transform, false);
            ready.SetActive(false);
            var line = lg.AddComponent<SB.GenericScoreBarLine>();
            line.Bind(label, chest, texOpen, ready, null);   // 第五个实参 = resourceIcon（这一节不测换图）

            // ① Locked：出厂箱子图 + 高亮**关**
            line.Initialize(M(100, GenericScoringMilestoneState.Locked));
            Tr(chest != null && chest.Texture == (Texture)texFactory, "G1a `Locked` ⇒ `Chest` = **出厂图**（第一次调用时快照下来的 `originalChest`）");
            C(line.ReadyToCollectVisible, false, "G1b `Locked` ⇒ `Highlight Crate` 关");
            C(label != null ? label.Text : null, "100", "G1c 行上的 `Score` = `milestone.score`（`+0x14`）");

            // ② Unlocked：**同一张出厂图**，只把高亮打开 —— 灭自证的那一条
            line.Initialize(M(500, GenericScoringMilestoneState.Unlocked));
            Tr(chest != null && chest.Texture == (Texture)texFactory,
               "G2a ★ `Unlocked` ⇒ `Chest` **仍然是出厂图**（原版 `Locked`/`Unlocked` 同一张；写成换开箱图 ⇒ 红）");
            C(line.ReadyToCollectVisible, true, "G2b ★ `Unlocked` ⇒ `Highlight Crate` **开**（与 `Locked` 的唯一区别）");
            C(label != null ? label.Text : null, "500", "G2c `Score` = 500");

            // ③ Collected：换成 `chestOpen` + 高亮关
            line.Initialize(M(3000, GenericScoringMilestoneState.Collected));
            Tr(chest != null && chest.Texture == (Texture)texOpen, "G3a `Collected` ⇒ `Chest` = `chestOpen`");
            C(line.ReadyToCollectVisible, false, "G3b `Collected` ⇒ `Highlight Crate` 关");
            C(label != null ? label.Text : null, "3000", "G3c `Score` = 3000");

            // ④ `originalChest` 是**一次性快照**：先 Collected 再 Locked，要能还原回出厂图
            line.Initialize(M(100, GenericScoringMilestoneState.Locked));
            Tr(chest != null && chest.Texture == (Texture)texFactory,
               "G4 ★ 快照是一次性的：`Collected`→`Locked` 能还原成**出厂图**（每次 `Initialize` 重新抓 `chestReward` 的话，这里会停在开箱图上）");

            // ⑤ `milestone == null` ⇒ 原版直接 return，一行都不动
            line.Initialize(null);
            C(label != null ? label.Text : null, "100", "G5 `milestone == null` ⇒ 什么都不做（原版那一支就是 `return`）");
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(texFactory); Object.DestroyImmediate(texOpen); }
    }

    // ============================================================ F `FactionScore` 两套变体
    static void SectionF_FactionScore两变体()
    {
        MenuCheck.Section(_sink, "F 原版 `FactionScore` 两套 prefab（`Small` / `Big`：节点名与几何**都不同**）");

        // ---- Small：standalone prefab（根 438.74 × 117.71）
        var rootS = new GameObject("B17_F_Small", typeof(RectTransform));
        try
        {
            var at = new PxRect(0f, 0f, SB.FactionScore.SmallSize.x, SB.FactionScore.SmallSize.y);
            var fs = SB.FactionScore.Create(rootS.transform, at, SB.FactionScoreVariant.Small);
            C(fs.Variant, SB.FactionScoreVariant.Small, "F1a `Variant` = Small");
            C(fs.transform.name, "FactionScoreSmall", "F1b 根节点名 = `FactionScoreSmall`");
            C(fs.transform.childCount, 3, "F1c 三行（`icon` / 两颗评级）");
            C(fs.transform.GetChild(0).name, "icon", "F1d 第 1 行名 = `icon`");
            C(fs.transform.GetChild(1).name, "Alliance Rating Display", "F1e 第 2 行名 = `Alliance Rating Display`");
            C(fs.transform.GetChild(2).name, "Alliance Rating Display (1)", "F1f 第 3 行名 = `Alliance Rating Display (1)`");
            SizePx(fs.transform.GetChild(0) as RectTransform, 212.63f, 128.50f, "F2a `icon` 尺寸（standalone）");
            SizePx(fs.transform.GetChild(1) as RectTransform, 448.89f, 110.91f, "F2b 当前分那颗评级尺寸");
            SizePx(fs.transform.GetChild(2) as RectTransform, 448.89f, 72.34f, "F2c 最高分那颗评级尺寸");
        }
        finally { Object.DestroyImmediate(rootS); }

        // ---- Big：#1–#4 那一格（根 190 × 230.45）
        var rootB = new GameObject("B17_F_Big", typeof(RectTransform));
        try
        {
            var at = new PxRect(0f, 0f, SB.FactionScore.BigSize.x, SB.FactionScore.BigSize.y);
            var fs = SB.FactionScore.Create(rootB.transform, at, SB.FactionScoreVariant.Big);
            C(fs.Variant, SB.FactionScoreVariant.Big, "F3a `Variant` = Big");
            C(fs.transform.name, "FactionScoreBig", "F3b 根节点名 = `FactionScoreBig`");
            C(fs.transform.childCount, 3, "F3c 三行（`icon` / 评级 / `MaxRating`）");
            C(fs.transform.GetChild(0).name, "icon", "F3d 第 1 行名 = `icon`");
            C(fs.transform.GetChild(1).name, "Alliance Rating Display", "F3e 第 2 行名 = `Alliance Rating Display`");
            C(fs.transform.GetChild(2).name, "MaxRating",
              "F3f ★ 第 3 行名 = **`MaxRating`**（照 `Small` 抄成 `Alliance Rating Display (1)` ⇒ 红）");
            SizePx(fs.transform.GetChild(0) as RectTransform, 164f, 145.80f, "F4a `icon` 尺寸（`Big` 是 164×145.8，`Small` 是 212.63×128.5）");
            SizePx(fs.transform.GetChild(1) as RectTransform, 164f, 41f, "F4b 当前分那颗评级尺寸");
            SizePx(fs.transform.GetChild(2) as RectTransform, 164f, 26.76f, "F4c `MaxRating` 尺寸");
        }
        finally { Object.DestroyImmediate(rootB); }

        // ---- 两套**必须不同**（这一条把「照一份建两处」直接钉死）
        Tr(SB.FactionScore.SmallSize != SB.FactionScore.BigSize, "F5 两套变体的出厂根框不同（Small 438.74×117.71 / Big 190×230.45）");
        C(SB.FactionScore.FactionIconTexture(CardArmy.Neutral), null,
          "F6 `CardArmy.Neutral` ⇒ 没有阵营图（`ArmyIcons.Neutral = 0` 在原版 `ArmyIconsSO` 里没有条目）");
    }

    /// <summary>量设计尺寸：**`rect.rect × 108`**（⛔ 不是 `sizeDelta` —— 拉伸锚点上它是内缩量；
    /// `MenuDraw.SetPxSize` 把锚点写成重合 ⇒ 这里 `rect` 与 `sizeDelta` 同值，但口径仍按 `rect` 走）。</summary>
    static void SizePx(RectTransform rt, float wantW, float wantH, string msg)
    {
        if (rt == null) { Tr(false, msg + "：（节点没有 `RectTransform`）"); return; }
        var r = rt.rect;
        Near(r.width * 108f, wantW, 0.05f, msg + "：宽 ×108");
        Near(r.height * 108f, wantH, 0.05f, msg + "：高 ×108");
    }
}
