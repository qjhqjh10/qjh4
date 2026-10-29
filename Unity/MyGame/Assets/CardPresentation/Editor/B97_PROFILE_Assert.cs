// B97_PROFILE_Assert.cs — 执行代理 `B97_PROFILE`（2026-10-29）的断言集
//
// 宿主接线（由调度台做，⛔ 本文件不改任何宿主）：
//   在 `Editor/ShellScene.cs` 的 `RunCore` 末尾、其它 `*_Assert.Run(...)` 那一串之后加一行
//     `B97ProfileAssert.Run(_sink);`
//   （`ShellScene.Run` 已经开着菜单壳、批次里也跑它 ⇒ 不额外增加一次 Unity 批处理。）
//   本组**不依赖任何宿主窗口**：自己建探针根 + 自己建窗（`RankedDivisionChangeWindow.Create(null, ctx)` 走
//   「没有 `WindowsManager` 只建出来」那条既有分支，见那个方法里 `mgr == null` 的兜底）。
//
// 它盯的是 `账/个人资料_成就_排行.tsv` 的 7 条（56 / 57 / 97 / 197 / 198 / 221 / 293）里**能断的那几条**：
//   · 221 `ChangeParticleEffectAndSpriteRank` 的 ③④（只有这条原来完全没实现 ⇒ 是本组的重点）
//   · 197 `Clean()` / 198 `OnInitialize(LiveOpsEvent)`（原来宿主卡压根不建）
//   · 56 / 57 头像边框「画的是已装备那条」（反证：没有「恒画金框」）
//   · 97 「领到边框 ⇒ 装备那一半」的等价落点 `ProfileData.SetEquippedBorderById`
//   · 293 与 56/57 的**判据空 / REMOTE** 两条没有可执行断言（判 D，理由在账的差异列）⇒ 本组不假装测了
//
// 🔴 灭自证：每条正向都配一条**结构上不可能同时满足**的反向
//   （剪影见断言文案里的「🧨」：注入 sprite 前 renderer 必须 false、注入后才 true；
//    2 个阵营时自动滚动必须 false、3 个时才 true；不存在的边框 id 必须**不改**下标）。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>`B97_PROFILE` 的断言集。入口 = <see cref="Run"/>。</summary>
public static class B97ProfileAssert
{
    const string P = "[B97_PROFILE] ";

    public static void Run(CheckSink s)
    {
        MenuCheck.Section(s, "B97_PROFILE · 个人资料/成就/排行 7 条（56 / 57 / 97 / 197 / 198 / 221 / 293）");
        SectionParticleTree(s);
        SectionChangeParticleAndSprite(s);
        SectionArmyBonusCard(s);
        SectionAvatarBorder(s);
        SectionBorderCollect(s);
    }

    // ============================================================ ① 221：两棵粒子树的**结构**
    static void SectionParticleTree(CheckSink s)
    {
        var root = new GameObject("B97_PROFILE_ParticleProbe", typeof(RectTransform)).transform;
        try
        {
            var r = RankedDivisionParticles.Build(root);

            // 前提（硬检查，⛔ 不写成 `if (x != null) Check(...)`）
            MenuCheck.True(s, r.Ok, "（前提）两棵树的根都建出来了（`Particles DivisionUp` / `Particles Rank Up`）");
            MenuCheck.Check(s, r.Nodes, 16, "判据 JSON 的 16 个节点**全部**建出来（现读 `ranked_division_particles.json`）");
            MenuCheck.Check(s, r.WithPs, 14, "其中 14 颗带 `ParticleSystem`（DivisionUp 6 + RankUp 8，逐个 pid 现读）");
            MenuCheck.Check(s, r.NoMaterial, 0, "**一颗都没因材质缺失被跳过**（4 张材质都在 `Resources/Art/vfx/`）");
            if (!r.Ok) return;

            // 根系：`Particles Rank Up` 原版是**裸 Transform**（无 RectTransform）
            MenuCheck.True(s, r.Rank.GetComponent<RectTransform>() == null,
                "★ `Particles Rank Up` **没有** `RectTransform`（原版就是裸 `Transform`；有它就是建错了）");
            MenuCheck.True(s, r.Division.GetComponent<RectTransform>() != null,
                "★ `Particles DivisionUp` **有** `RectTransform`（原版是 RectTransform；两条要分开断）");

            // 坐标桥：根整体缩 1/108（`LayoutSpace`：108 px = 1 世界单位），子树以下逐字照抄
            MenuCheck.Near(s, r.Rank.localScale.x, 164.23738f / 108f, 1e-4f,
                "★ `Particles Rank Up` 根 `localScale = 164.23738 / 108`（原版 164.23738 是 1px=1 单位画布上的值）");
            MenuCheck.Near(s, r.Division.localScale.x, 1f / 108f, 1e-6f,
                "★ `Particles DivisionUp` 根 `localScale = 1 / 108`（原版 1）");
            var sbDiv = r.Division.Find("Sparks Burn");
            MenuCheck.True(s, sbDiv != null && Mathf.Abs(sbDiv.localScale.x - 250.00007f) < 1e-2f,
                "★ 子树**不**除 108：`Particles DivisionUp/Sparks Burn` 仍 `localScale ≈ 250`（原版 250.00007）");

            // 名字集合（⛔ 别只断「有 ParticleSystem」，还要断**是哪几个子件**）
            Sections(s, r.Division, new[] { "Glow below", "Sparks background", "Sparks Burn" },
                     "`Particles DivisionUp` 的子件");
            var sbRank = r.Rank.Find("Sparks Burn");
            MenuCheck.True(s, sbRank != null, "（前提）`Particles Rank Up/Sparks Burn` 在");
            if (sbRank == null) return;
            Sections(s, sbRank, new[] { "Glow Big", "Promote Letter Effect", "Sparks fast", "Wave Shine" },
                     "`Particles Rank Up/Sparks Burn` 的子件");
            var wave = sbRank.Find("Wave Shine");
            MenuCheck.True(s, wave != null, "（前提）`Wave Shine` 在");
            if (wave != null)
                Sections(s, wave, new[] { "Wave Shine R", "Trails R", "Trails L" }, "`Wave Shine` 的子件");

            // 模块：这 5 颗原版 `ShapeModule.enabled = false`（照搬 `CardDisplayEffects` 的「恒 true」就是错的）
            SameBool(s, r.Division.Find("Glow below"), false, "`Glow below` 的 `shape.enabled = false`");
            SameBool(s, r.Division.Find("Sparks Burn/Promote Letter Effect"), false,
                     "`Promote Letter Effect` 的 `shape.enabled = false`");
            SameBool(s, sbRank.Find("Glow Big"), false, "`Rank Up/Sparks Burn/Glow Big` 的 `shape.enabled = false`");
            SameBool(s, sbRank.Find("Sparks Burn"), true, "`Rank Up/Sparks Burn` 的 `shape.enabled = true`");

            // Noise（4 颗火花粒子）/ Trail（2 颗 Trails）
            MenuCheck.True(s, Ps(sbRank).noise.enabled, "★ `Rank Up/Sparks Burn` 的 **Noise** 模块开着（原版 4 颗火花都有）");
            MenuCheck.True(s, !Ps(r.Division.Find("Glow below")).noise.enabled, "🧨 反向：`Glow below` 的 Noise **不**开");
            MenuCheck.True(s, Ps(sbRank.Find("Wave Shine/Trails R")).trails.enabled,
                "★ `Trails R` 的 **Trail** 模块开着（它 `renderMode = 5 (None)`，只画拖尾）");
            MenuCheck.Check(s, (int)Ps(sbRank.Find("Wave Shine/Trails R")).GetComponent<ParticleSystemRenderer>().renderMode,
                5, "★ `Trails R` 的 `renderMode = 5 (None)`（原版不画粒子本体）");
        }
        finally { Object.DestroyImmediate(root.gameObject); }
    }

    // ============================================================ ② 221：③④ 两跳（`ChangeParticleEffectAndSpriteRank`）
    static void SectionChangeParticleAndSprite(CheckSink s)
    {
        // ---- 默认（`rankIcon.m_Sprite == null`，= 本机常态：段位图由服务端喂）----
        var winA = RankedDivisionChangeWindow.Create(null, new RankedDivisionChangeWindow.Context { IsDivisionChange = true });
        try
        {
            MenuCheck.True(s, winA != null, "（前提）升段窗建出来了（`Create(null, …)` 走「没有 Manager 只建出来」那条分支）");
            if (winA == null) return;
            MenuCheck.True(s, winA.DivisionParticleForTest != null,
                "（前提）`Particles DivisionUp ▸ Sparks Burn ▸ Promote Letter Effect` 那颗 `ParticleSystem` 在");
            if (winA.DivisionParticleForTest == null) return;

            winA.TickForTest(RankedDivisionChangeWindow.PromoteDelay(RankedDivisionChangeWindow.StepR.Length) + 0.001f);

            MenuCheck.True(s, !winA.DivisionParticleEnabled,
                "★ ③④：`rankSprite == null` ⇒ `Promote Letter Effect` 的 `Renderer.enabled` = **false**"
              + "（判据 `ChangeParticleEffectAndSpriteRank` 第 ④ 步 `component.enabled = op_Implicit(rankSprite)`）");
            MenuCheck.True(s, !winA.NodeActiveForTest("Division Image/Rank Icon/Rank Icon Glow"),
                "★ ②：`rankSprite == null` ⇒ `Rank Icon Glow` **关着**（原版 `targetImage.gameObject.SetActive(visible)`）");
        }
        finally { if (winA != null) Object.DestroyImmediate(winA.gameObject); }

        // ---- 注入 sprite（模拟「服务端给了段位图」）----
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var sprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
        var winB = RankedDivisionChangeWindow.Create(null, new RankedDivisionChangeWindow.Context { IsDivisionChange = false });
        try
        {
            if (winB == null) { MenuCheck.True(s, false, "（前提）升段窗 B 建出来"); return; }
            MenuCheck.True(s, winB.RankParticleForTest != null, "（前提）`Rank Up ▸ Promote Letter Effect` 那颗 `ParticleSystem` 在");
            if (winB.RankParticleForTest == null) return;

            // 🧨 判别式：**同一个实例**在注入前后必须一个是 false、一个是 true
            //    （只断「注入后 true」的话，「恒 true」也能绿）
            winB.RankIconSprite = null;
            winB.TickForTest(RankedDivisionChangeWindow.PromoteDelay(RankedDivisionChangeWindow.StepR.Length) + 0.001f);
            bool before = winB.RankParticleEnabled;
            MenuCheck.Check(s, before, false, "（前提·判别式）注入前 `Renderer.enabled` = false");

            var winC = RankedDivisionChangeWindow.Create(null, new RankedDivisionChangeWindow.Context { IsDivisionChange = false });
            try
            {
                if (winC == null || winC.RankParticleForTest == null) { MenuCheck.True(s, false, "（前提）升段窗 C 建出来"); return; }
                winC.RankIconSprite = sprite;
                winC.TickForTest(RankedDivisionChangeWindow.PromoteDelay(RankedDivisionChangeWindow.StepR.Length) + 0.001f);

                MenuCheck.True(s, winC.RankParticleEnabled,
                    "★ ④ 注入 `RankIconSprite` 后 `Renderer.enabled` = **true**（与上面那条 false 构成判别式）");
                MenuCheck.Check(s, winC.RankParticleForTest.textureSheetAnimation.GetSprite(0), sprite,
                    "★ ③ `textureSheetAnimation.SetSprite(0, rankIcon.m_Sprite)` 落到了那一格");
                MenuCheck.True(s, winC.NodeActiveForTest("Division Image/Rank Icon/Rank Icon Glow"),
                    "★ ② `Rank Icon Glow` 被打开（原版 `SetActive(rankSprite != null)`）");
            }
            finally { if (winC != null) Object.DestroyImmediate(winC.gameObject); }
        }
        finally
        {
            if (winB != null) Object.DestroyImmediate(winB.gameObject);
            if (sprite != null) Object.DestroyImmediate(sprite);
            if (tex != null) Object.DestroyImmediate(tex);
        }
    }

    // ============================================================ ③ 197 / 198：奖励活动卡
    static void SectionArmyBonusCard(CheckSink s)
    {
        var root = new GameObject("B97_PROFILE_CardProbe", typeof(RectTransform)).transform;
        try
        {
            // 前提：本地模拟表**有内容**（🔶 自设计；⛔ 空表会得到与原版相反的行为）
            var doc = RankedArmyBonusData.Load();
            MenuCheck.True(s, doc != null, "（前提）本地模拟表 `Resources/Art/ranked_army_bonus.json` 读得到（🔶 自己设计）");
            var evt = RankedArmyBonusData.FirstValid(doc);
            MenuCheck.True(s, evt != null, "（前提）表里至少一条带 `affectedArmies` 的活动（⛔ 空表 = 与原版相反）");
            if (evt == null) return;
            MenuCheck.Check(s, evt.affectedArmies.Length, 3, "（前提）模拟数据有 3 个受加成阵营");

            // ---- 198 ③：按 `affectedArmies` 原序填 `Content` ----
            var card = RankedArmyBonusCard.Create(root);
            MenuCheck.True(s, card != null, "（前提）卡建出来了");
            if (card == null) return;
            card.Initialize(evt);

            MenuCheck.Check(s, card.ArmyImageCount, evt.affectedArmies.Length,
                "★ 198③：`currentArmyImages` 个数 == `AffectedArmies` 个数（原版逐个 `Instantiate` 后塞进那个 List）");
            MenuCheck.Check(s, card.ContentChildCount, evt.affectedArmies.Length,
                "★ 198③：`Dynamic Content ▸ Viewport ▸ Content` 下恰 N 个 `Simple Army Icon`");
            bool ordered = card.AffectedArmies != null && card.AffectedArmies.Length == evt.affectedArmies.Length;
            if (ordered)
                for (int i = 0; i < evt.affectedArmies.Length; i++)
                    if (card.AffectedArmies[i] != evt.affectedArmies[i]) { ordered = false; break; }
            MenuCheck.True(s, ordered, "★ 198③：顺序 == `AffectedArmies` 顺序（逐 id 比）");

            // ---- 198 ④：`*(bool*)(scroll+0x22) = thresholdForAutoScroll < list.Count` ----
            MenuCheck.True(s, card.AutoScrollEnabled,
                "★ 198④：3 > `thresholdForAutoScroll(2)` ⇒ 自动滚动那一位置真");

            // ---- 197：`Clean()` 清子件 + 清列表 ----
            card.Clean();
            MenuCheck.Check(s, card.ContentChildCount, 0, "★ 197：`Clean()` 后 `Content` 的 `childCount == 0`");
            MenuCheck.Check(s, card.ArmyImageCount, 0, "★ 197：`Clean()` 后 `currentArmyImages.Count == 0`");
            MenuCheck.True(s, !card.AutoScrollEnabled, "（顺带）`Clean()` 之后自动滚动那一位回到 false");

            // 🧨 判别式：2 个阵营（= `thresholdForAutoScroll` 那一档，**不**超过）⇒ 必须 false
            card.Initialize(new RankedArmyBonusData.Event { title = "T", endsInSeconds = 60f, affectedArmies = new[] { 10, 50 } });
            MenuCheck.Check(s, card.ArmyImageCount, 2, "（判别式前提）2 个阵营 ⇒ 2 颗图标");
            MenuCheck.True(s, !card.AutoScrollEnabled,
                "🧨 判别式：2 > 2 为假 ⇒ 自动滚动**关**（「恒 true」过不了这条，也过不了上面那条）");
            card.Clean();

            // ---- 198 ①②：空事件 ⇒ **毁卡**（⛔ 不留空卡）----
            var cardNull = RankedArmyBonusCard.Create(root);
            cardNull.Initialize(null);
            MenuCheck.True(s, cardNull == null, "★ 198①②：`evt == null` ⇒ 按原版 `Destroy(this)` **毁卡**（⛔ 不留空卡留场）");

            var cardEmpty = RankedArmyBonusCard.Create(root);
            cardEmpty.Initialize(new RankedArmyBonusData.Event { title = "T", affectedArmies = new int[0] });
            MenuCheck.True(s, cardEmpty == null, "★ 198①：`affectedArmies` 为空 ⇒ 同样**毁卡**");
        }
        finally { Object.DestroyImmediate(root.gameObject); }
    }

    // ============================================================ ④ 56 / 57：头像边框画的是**已装备那条**
    static void SectionAvatarBorder(CheckSink s)
    {
        var equipped = ProfileData.EquippedBorderArtName;
        string want = string.IsNullOrEmpty(equipped) ? AvatarTab.ArtBorder : equipped;
        MenuCheck.Check(s, AvatarTab.BorderArtName(), want,
            "★ 56/57：边框取图口 == `ProfileData.EquippedBorderArtName`（空则回落 `ArtBorder`）"
          + " —— 原版那个 `AvatarBorderController` 全库**零调用点**，⛔ 不是「恒画金框」");

        // 🧨 反证：金框那张图的资源名**不能**是这里的结果（否则说明有人把「恒画金框」落了地）
        MenuCheck.True(s, AvatarTab.BorderArtName() != "Player_Profile_Border_Gold"
                        || ProfileData.EquippedBorderArtName == "Player_Profile_Border_Gold",
            "🧨 反证：只有**真的装备了金框**时取到的才该是金框图（判据 `B92-N4b`：判据闭合前不许落地「恒画金框」）");
    }

    // ============================================================ ⑤ 97：领到边框 ⇒ 装备那一半
    static void SectionBorderCollect(CheckSink s)
    {
        var list = ProfileData.Borders;
        MenuCheck.True(s, list != null && list.Count > 0, "（前提）`profile_cosmetics.json` 的边框清单读得到");
        if (list == null || list.Count == 0) return;

        int idx0 = ProfileData.EquippedBorderIndex;
        string id0 = idx0 >= 0 && idx0 < list.Count ? list[idx0].id : null;

        // 🧨 判别式①：不存在的 id ⇒ false **且不改下标**（静默改 = 玩家装备态被莫名换掉）
        MenuCheck.True(s, !ProfileData.SetEquippedBorderById("__B97_PROFILE_不存在的边框__"),
            "★ 97：`SetEquippedBorderById(不存在的 id)` 返回 **false**（且出声 `LogWarning`）");
        MenuCheck.Check(s, ProfileData.EquippedBorderIndex, idx0, "★ 97：失败路径**没有改** `EquippedBorderIndex`");

        // 判别式②：清单里真实存在的一条 ⇒ true 且下标跟着动
        int target = -1;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && !string.IsNullOrEmpty(list[i].id) && i != idx0) { target = i; break; }
        if (target < 0)
        {
            MenuCheck.True(s, false, "（前提）清单里还有第二条边框可用来做正向判别式");
            return;
        }
        MenuCheck.True(s, ProfileData.SetEquippedBorderById(list[target].id),
            "★ 97：真实存在的 id ⇒ 返回 **true**（= 原版「收到边框奖励 ⇒ 自动装备」的装备那一半）");
        MenuCheck.Check(s, ProfileData.EquippedBorderIndex, target, "★ 97：下标跟着换到那一条");

        // 还原（⛔ 别把玩家装备态留在测试值上）
        if (!string.IsNullOrEmpty(id0)) ProfileData.SetEquippedBorderById(id0);
        MenuCheck.Check(s, ProfileData.EquippedBorderIndex, idx0, "（还原）跑完把装备态放回原样");
    }

    // ============================================================ 小工具
    static ParticleSystem Ps(Transform t) => t == null ? null : t.GetComponent<ParticleSystem>();

    static void SameBool(CheckSink s, Transform t, bool want, string msg)
    {
        var ps = Ps(t);
        MenuCheck.True(s, ps != null, "（前提）" + msg + " —— 那颗节点上有 `ParticleSystem`");
        if (ps == null) return;
        MenuCheck.Check(s, ps.shape.enabled, want, "★ " + msg);
    }

    /// <summary>子件名集合**逐字相等**（不是「包含」）：`Content` 那类还会多东西，粒子树必须严丝合缝。</summary>
    static void Sections(CheckSink s, Transform parent, string[] want, string what)
    {
        if (parent == null) { MenuCheck.True(s, false, "（前提）" + what + " 的父件在"); return; }
        var have = new List<string>();
        for (int i = 0; i < parent.childCount; i++) have.Add(parent.GetChild(i).name);
        have.Sort();
        var w = new List<string>(want); w.Sort();
        MenuCheck.Check(s, string.Join(",", have.ToArray()), string.Join(",", w.ToArray()),
            "★ " + what + " 名集合逐字相等（多一个少一个都红）");
    }
}
