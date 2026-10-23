// WarpforgeUiEffect.cs — 把「按【像素】写死的 UI 特效 prefab」播到壳的设计空间里（2026-10-24 · `A31`）
//
// ============================ 这个文件解决什么 ============================
// 原版主菜单里有一族**UI 特效**：它们不是战场特效，而是挂在按钮 / 卡上的小演出。
// 原版那一跳（逐句读 `CraftingPanel__DoVFX.c`，见下）是：
//   `Instantiate(prefab, craftButton.position, …)` → `SetParent(craftButton.transform)`
//   —— `SetParent` 默认 `worldPositionStays = true` ⇒ **世界缩放保持 prefab 自带的 1**。
//   原版画布是 `Screen Space`（`CanvasScaler` 的 scaleFactor 下 **1 单位 = 1 像素**）
//   ⇒ 那批粒子的 `startSize = 5~12`、`m_LocalScale = 2` 就是**像素**。
//
// 我们这边**没有 Canvas**（壳是正交相机 + 设计空间：可见高 = 10 世界单位 / 1080 px，
// 见 `CardPresentation.LayoutSpace`）⇒ 同一份 prefab 直接 `Play` 会**大 108 倍**。
// 现有先例只在一个地方做对了：`Shell/BoosterPackOpenWindow.cs:682-686` 建了一个
// `EffectAnchor`，`localScale = CardK / (DesignPxH / DesignHeight)`，再 `Play(名字, anchor, 0, 1f)`。
// 本类把那条约定**收成一处**（CLAUDE.md §三：「两处写同一条规则 = 迟早不一致」）：
//   · `PixelScale` = `LayoutSpace.Px(1f)` = **1/108**（⛔ 别再写死 108）
//   · `PlayUi`：世界位置 → 父节点局部位置，缩放一律乘 `PixelScale`
//   · `WildcardCraftPrefab`：原版 `CraftingPanel.craftVFXs[]` 那张**按稀有度**的表
//
// 🔴 **本类只管「像素单位」的那些件**。同一批「待接线」里的 `Swarm Card Merge VFX`
//    **不是** UI 件 —— 它是**战场世界件**（原版 `CardScript.ExecuteSwarm` 把它 Instantiate 在
//    单位的 pos/rot 上、并把 `localScale = 单位的 localScale`）⇒ 那一件走普通
//    `WarpforgeEffectPlayer.Play(name, parent, pos, 1f, …)`，⛔ **不要过 `PlayUi`**。
//
// ============================ 判据（⛔ 只认原版） ============================
// ① 字段表 = 原版场景 `MainMenu Warpforge.unity` 里 `CraftingPanel`（脚本 guid
//    `6b7ef5aba15d2b19ba052e76ca88473a`）的 `craftVFXs[]`，**逐行实读**：
//      rarity 1 → Button `Wildcard_Use_Button 1 Common`  · Card `Wildcard_Use_Card 1 Common`
//      rarity 2 → Button `Wildcard_Use_Button 2 Rare`    · Card `Wildcard_Use_Card 2 Rare`
//      rarity 3 → Button `Wildcard_Use_Button 3 Epic`    · Card `Wildcard_Use_Card 3 Epic`
//      rarity 4 → Button `Wildcard_Use_Button 4 Legendary` · Card `Wildcard_Use_Card 4 Legendary`
//      rarity 5 → Button `Wildcard_Use_Button 4 Legendary` · Card `Wildcard_Use_Card 1 Common`
//    🔴 **rarity 5 那两格原版就是复用的**（不是抄错）：Button 直接复用 4、Card **回落到 1**。
//       我们这边 `special` 那 50 张**不能合成**（`Shell/CardDetailPopup.Craftable` 只放行四档，
//       判据 = 2026-09-27 用户指出 + `cards_engine.json` 全是 defence/tactic 的占位值）
//       ⇒ 这一行**今天用不上**，但**照原值抄下来**（⛔ 别猜、别删）。
//    字段顺序（序列化）：`rarity`(int) · `<ParticleButton>k__BackingField`(GameObject) ·
//       `<ParticleCard>k__BackingField`(GameObject)；与 `DoVFX` 里的 `+0x10/+0x18/+0x20` 对得上。
// ② 时机 = `CraftingPanel__TryCraftingInternal.c`：**发出合成请求之后（同一次调用里）** 就
//    `CraftingPanel__DoVFX()` —— 不是等服务端回调。按钮那道（`TryCrafting`）在传奇且
//    `owned == 1` 时先弹确认窗、**那一支不播**。
// ③ 落点 = `CraftingPanel__DoVFX.c`（逐句）：
//    · `ParticleButton` → `Instantiate(prefab, craftButton.position, <静态默认旋转>)`，
//      `SetParent(craftButton.transform)`，并存进 `+0x68`；
//    · `ParticleCard` → `Instantiate(prefab, <card>.position, <card>.rotation)`，
//      `SetParent(<card>.transform)`，并存进 `+0x70`。
//    ⇒ 两者**世界缩放都是 1**（= 像素），位置取**目标节点当时的世界位置**。
//
// ============================ 我们的消费点（⛔ 本批没接，见报告） ============================
// 落点 = `CardPresentation/Shell/CardDetailPopup.cs`：
//   · 按钮那道：`BuildCrafting()` 建的 `Craft Bg`（rect `1384,244.29 → 1654,304.95`）
//   · 卡那道：   前台那张卡（扇形槽 0，`CardFan` 的屏幕位姿）
//   · 调用点：   `OnCraftClicked()`（那个方法现在是「点了不改数据」的空实现，用户 2026-10-18 拍板）
// 本类**只提供机制**，不自己找节点、不自己猜时机 —— 接线由 `Shell/` 的代理做
// （`Shell/**` 不在 `A31` 的白名单里）。
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>UI（像素单位）特效的播放入口 + 原版 `CraftingPanel.craftVFXs` 那张表。</summary>
    public static class WarpforgeUiEffect
    {
        /// <summary>**1 设计像素 = 多少世界单位**（= `LayoutSpace.Px(1f)` = 1/108）。
        /// <para>⚠️ 这**不是**「实例的最终世界缩放」：`WarpforgeEffectPlayer.Play` 是**乘**在 prefab
        /// 自带的根缩放之上（原版 `SetParent` 默认保世界缩放，语义一致）—— 那 8 件里
        /// `Wildcard_Use_Card 2/3/4` 的根缩放是 **1.2**（现读），其余是 1。
        /// ⇒ 实例世界缩放 = **prefab 根缩放 × 本值**。</para>
        /// <para>判据：壳的可见高恒为 `LayoutSpace.DesignHeight`（10 世界单位）而设计像素是
        /// `DesignPxH`（1080）⇒ 108 px / 世界单位。⛔ **别在本类里另写一个 108**（量纲坑，
        /// `LayoutSpace.DesignHeight` 的 doc 记着踩过的那次）。</para>
        /// <para>⚠️ 只在「父节点 `lossyScale == 1`」时精确 —— 见 <see cref="PlayUi"/> 的护栏。</para></summary>
        public static float PixelScale { get { return CardPresentation.LayoutSpace.Px(1f); } }

        // ------------------------------------------------------------------ 原版表
        //
        // 🔴 **表只此一份**：原版就是 `CraftingPanel.craftVFXs[]`（见文件头 ①）。
        //    下标 0..3 = rarity 1..4；rarity 5 由 <see cref="WildcardCraftPrefab"/> 显式回落。
        static readonly string[] CraftCardFx =
        {
            "Wildcard_Use_Card 1 Common",
            "Wildcard_Use_Card 2 Rare",
            "Wildcard_Use_Card 3 Epic",
            "Wildcard_Use_Card 4 Legendary",
        };
        static readonly string[] CraftButtonFx =
        {
            "Wildcard_Use_Button 1 Common",
            "Wildcard_Use_Button 2 Rare",
            "Wildcard_Use_Button 3 Epic",
            "Wildcard_Use_Button 4 Legendary",
        };

        /// <summary>原版 `CraftingPanel.craftVFXs[]`：`rarity`（**1 基**，1=Common … 4=Legendary、
        /// 5=特殊档）→ 该播哪个 prefab。
        /// <para>⚠️ rarity 5 = **Button 复用 4** + **Card 回落到 1** —— 这是原版序列化里的**原值**，
        /// 不是笔误（`MainMenu Warpforge.unity` 的 `craftVFXs` 第 5 行实读）。</para>
        /// <para>认不出的 rarity ⇒ **返回 null 并出声**（⛔ 别默认成某一档：那会把错误静默掉）。</para></summary>
        public static string WildcardCraftPrefab(int rarity, bool onButton)
        {
            if (rarity >= 1 && rarity <= 4)
                return onButton ? CraftButtonFx[rarity - 1] : CraftCardFx[rarity - 1];
            if (rarity == 5)
                return onButton ? CraftButtonFx[3] : CraftCardFx[0];   // 原版那一行：Button 复用 4 · Card 回落 1
            Debug.LogWarning($"[WarpforgeUiEffect] 稀有度 `{rarity}` 不在原版 `craftVFXs` 的 1..5 里 ⇒ 不播"
                           + "（⛔ 不回落到某一档 —— 那会把「这一档没有素材」静默成「播了个别的」）。");
            return null;
        }

        // ------------------------------------------------------------------ 播放

        /// <summary>播一个 **UI（像素单位）** 特效，落在 `worldPos`。
        /// <para>`neutralParent` 必须是**缩放为 1** 的节点（壳根 / 窗根 / 专门的挂点）。
        /// 它只提供**父链与销毁归属**；位置用 `worldPos` 换算，缩放一律乘 <see cref="PixelScale"/>。</para>
        /// <para>⚠️ ⛔ **别把卡节点 / 按钮节点直接当 parent** —— 那些节点带着自己的缩放
        /// （卡是 `CardView.Height/108` 那一档），乘进来会让特效**大小跟着卡走**，
        /// 而原版是「世界缩放保持 1、只取那个节点的世界位置」（文件头 ③）。</para></summary>
        public static WarpforgeEffectPlayer PlayUi(string effectName, Transform neutralParent,
                                                  Vector3 worldPos, float autoDestroySeconds = -1f)
        {
            if (string.IsNullOrEmpty(effectName)) return null;

            if (neutralParent != null)
            {
                var s = neutralParent.lossyScale;
                if (Mathf.Abs(s.x - 1f) > 0.01f || Mathf.Abs(s.y - 1f) > 0.01f)
                    Debug.LogWarning($"[WarpforgeUiEffect] 父节点 `{neutralParent.name}` 的 lossyScale = {s}"
                                   + " ≠ 1 ⇒ UI 特效的大小会跟着它走（原版是世界缩放保持 1，见文件头 ③）。"
                                   + "换一个缩放为 1 的节点，或者把这一档当成已知偏离记下来。");
            }

            Vector3 local = neutralParent != null ? neutralParent.InverseTransformPoint(worldPos) : worldPos;
            return WarpforgeEffectPlayer.Play(effectName, neutralParent, local, PixelScale, autoDestroySeconds);
        }

        /// <summary>`WildcardCraftPrefab` + <see cref="PlayUi"/> 的组合口（接线时一行就够）。
        /// 名字认不出（rarity 超范围）⇒ 返回 null（已经出过声）。</summary>
        public static WarpforgeEffectPlayer PlayWildcardCraft(int rarity, bool onButton,
                                                              Transform neutralParent, Vector3 worldPos,
                                                              float autoDestroySeconds = -1f)
        {
            string name = WildcardCraftPrefab(rarity, onButton);
            if (string.IsNullOrEmpty(name)) return null;
            return PlayUi(name, neutralParent, worldPos, autoDestroySeconds);
        }
    }
}
