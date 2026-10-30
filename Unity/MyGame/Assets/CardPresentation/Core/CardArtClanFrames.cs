// CardArtClanFrames.cs — 原版 `CardFramesByArmy` SO 的**逐条组装表**（卡框 = 阵营 × 升级档 × 卡类型）
//
// ============================ 它是什么 ============================
//  原版卡框的分派只走一条链：`CardTierUIController.SetTier/SetFrame`
//  → `CardFramesSO.GetClanFrame(army, tier, cardType)`（`decomp_full/CardFramesSO__GetClanFrame.c`）
//  → 返回一对 Sprite：`.Key` = **卡框**（给 `+0x38` 那颗 Image）、`.Value` = **高亮/软光**（给 `+0x40`）。
//  而 `GetClanFrame` 取的是 `CardFramesSO.TierCardFrameAddressableReference` 的四个引用：
//    cardType 0 / 10（部队） ⇒ `+0x30`（troopFrame）/ `+0x28`（troopHighlightFrame）
//    cardType 20（战术）      ⇒ `+0x20`（stratagemFrame）/ `+0x18`（stratagemHighlightFrame）
//  （`CardFramesSO.TierCardFrameAddressableReference__GetReferences.c:1-30`，两份产物逐句一致。）
//  这份表 = 那个 SO 的**离线烘焙形**（`CardFramesByArmy` 这个 MonoBehaviour 的 `clanFrames` 字段），
//  也就是【原版资产字段的直接转录】—— 不是我们挑的、也不是按名字猜的。
//
//  数据源（判据，可复跑）：
//    `d:/2/新解包资源/assets_full/bundle_staticgeneralassets_assets_all/MonoBehaviour/CardFramesByArmy.json`
//    · `clanFrames` **13 条**（army 10…130，每个阵营一条），每条 `framesByTier` **4 条**（`cardTier` 0…3）
//    · 每条 tier 四格：`stratagemHighlightFrame` / `stratagemFrame` / `troopHighlightFrame` / `troopFrame`
//    · 合计引用 **208 个**、去重 **207 个 GUID**（唯一重复 = 见下面 `SdfTier`）
//  取值链（本表就是把这条链在离线时跑完的结果）：
//    `m_AssetGUID` → Sprite 的 `m_RenderDataKey`（AssetRipper 导出里逐条对得上）→
//    该 Sprite 的 `m_Rect`（纹理像素，**左下原点**）+ `m_PixelsToUnits = 100`；
//    它的 `texture.guid` → Texture2D 真名（`Texture2D/*.png` / `*.png.meta`）。
//
// ============================ 规模（本会话现算，旧数「120 张」作废）============================
//  · 卡框槽位 = 13 阵营 × 4 档 × 2 类型 = **104**（+ 高亮槽位 104 = 208 个引用）
//  · 原版**卡框贴图**（Texture2D，各阵营各档各类型一张）= **104 张**，**全部 1024 × 1024**
//    （2026-10-29 `B98_P1` 逐张 `PIL.Image.size` 现读：104/104 = 1024×1024）。
//    `m_Border` 全 0（**不是九宫格**，是整张拉伸）。
//  · 每张 Texture2D 只有**一个 Sprite**，其 `m_Rect` **104/104 完全一样 = (171, 11, 682, 998)**、
//    `m_PixelsToUnits = 100`、`m_Pivot = (0.5, 0.5)`、`m_Offset = (0,0)`
//    （2026-10-29 `B98_P1` 用 UnityPy 直读原版 bundle 现核：Ultramarines / SpaceWolves /
//     EmperorsChildren / AstraMilitarum 四家抽读 + `assets_full/Sprite/*.json` 全表，**0 变体**）。
//    ⚠️ 原版 Sprite 的**名字**很坑：army 10–110 的那 88 个槽位全都叫 **`Orc Frame`**（同名 88 个、PathID 各不相同），
//       只有 army 120（EmperorsChildren）/ 130（SpaceWolves）才叫 `40k_Cardframes_…_tier{N}`。
//       ⇒ **判身份只能按 texture / Rect，不能按 Sprite 名**（这正是「同名 88 个」那条老坑）。
//  · 原版**高亮（SDF）贴图** = 128×128 灰度距离场，Sprite 的 `m_Rect` 恒 `(24, 6, 81, 110)`；
//    我们**已经全部导进来了**：`Resources/Art/card_sdf/<faction>[_strat]_tier<N>.png`（13×4×2 = 104/104 现读齐），
//    且与原版 Texture2D **逐字节相同**（2026-10-29 `B98_M1` 用 md5 核过 troop 与 stratagem 两族）。
//  · 原版**卡框贴图 104/104 已导**（2026-10-30 `B100_F12` 现核）：`CardArt.Frame()` 先取它们、取不到才落回自绘的
//    `Art/cards/frame_*.png`（**自绘兜底** 1024×1024；15 个阵营 × 10 个文件 = 150 张；多出来的 `frame_ember` / `frame_tide` 是**我们自己设计**的
//    两个阵营 `Ember` / `Tide`（`ArmyIcons.Id` 认不出、恒 `-1`）⇒ 它们**只能**走自绘那一路，⛔ 不是死文件）。
//    ⇒ **导入目标名 = `ImportResourceName()`（`frame_clan_<ResourceName>`）**：原图 **1024² 原样**拷进
//    `Resources/Art/cards/clan/frame_clan_<ResourceName>.png`（**⛔ 不裁、不缩放**；为什么必须 1024² + `frame_`
//    前缀 → `ImportResourceName` 的 doc），`FrameTexture()` 立刻就能取到（不用改一行调用点）。
//    🔴 **2026-10-29 订正**：这里原来写「按 `m_Rect` 裁出来」——裁了会掉进 alpha-bbox 分支（见那一段）。
//
// ============================ 一处原版「档位复用」（照抄，别理顺）============================
//  army 60（Leviathan）的 `troopHighlightFrame`：`cardTier 2` 与 `cardTier 3` **存的是同一个 GUID**
//  ⇒ 原版**tier4 的部队卡高亮用的是 tier3 那张 SDF**。我们原来按「档位=文件名」取，会取到 `leviathan_tier4.png`
//  （那张图原版有、但 SO 没在 tier4 用）⇒ `SdfTier()` 就是这唯一一处修正。
//
// ============================ 还没做的（如实记）============================
//  · ~~104 张卡框贴图**没导**~~ **2026-10-30 `B100_F12` 订正：已导 104/104**（其中 24 张原先把空格留在了文件名里、已归一下划线；
//    `.meta` 同改、GUID 不变）⇒ `FrameTexture()` 104 格全命中，取不到时才落回自绘那套。导入清单 → `资料/完全复刻/报告/B98_P1.md` §甲。
//
// ============================ 🔴 2026-10-29 订正（`B98_P1` · 铁律 5）============================
//  本文件头原来（`B98_M1` 写）说：「那颗 RectTransform 的尺寸还没读 ⇒ 直接拉伸会随档位变形
//  （各档 `m_Rect` 的宽高并不相同：611.9×939.9 … 679.9×948.8）」。**这半句不成立，撤回**：
//   ① **`m_Rect` 不是各档不同** —— 原版 104 个 Sprite 的 `m_Rect` **全是 (171,11,682,998)**
//      （判据见上面「规模」那段：UnityPy 直读 bundle）。611.9×939.9 那组数是 **`textureRect`
//      （不透明内容的紧包围盒）**，不是 `m_Rect` —— `Rows` 里存的 X/Y/W/H 就是它（见 `Entry` 的 doc）。
//      `B98_M1` 的数值源 = AssetRipper 导出的 `.asset`，而 AssetRipper 把 `m_Rect` **改写成了
//      `textureRect`**（同一份 Sprite，UnityPy 读 682×998、AssetRipper 读 611.9×939.9）⇒ 数值本身
//      是真的（那确实是 `textureRect`），只是**列名/语义标错了**。
//   ② **卡框的显示矩形也不随档位变** —— 原版 `CardTierUIController.SetTier/SetFrame` 只
//      `Image.set_sprite`（`decomp_full/Card2DController__SetFrame.c` / `CardTierUIController__SetTier.c`），
//      **不写任何 RectTransform**；`Card2DController` 全族 14 个方法里 `sizeDelta` 0 命中。
//      实测 44 份带 `CardFrame` 节点的 prefab（`assetripper_out/ExportedProject/Assets/**/*.prefab`
//      全扫）**锚点与尺寸完全一致**：`m_AnchorMin == m_AnchorMax == (0.5, 0.5)`（**居中，不是拉伸**）
//      ⇒ **`rect.rect` = `m_SizeDelta` = 2.2452 × 3.2572**，`m_AnchoredPosition.y = -0.03`。
//      ⇒ 各档「变形」这件事在原版里**不存在**：所有档共用同一个固定矩形，靠 `m_PreserveAspect = 0`
//      把整块 sprite rect 拉伸进去（这正是 A2138 那条结论）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `CardFramesByArmy` SO 的转录表（卡框 13 阵营 × 4 档 × 2 类型）。
    /// <para>判据与规模 → 本文件头。别手工改这张表：它是资产字段的逐条转录。</para></summary>
    public static class CardArtClanFrames
    {
        /// <summary>一条槽位（一个阵营 + 一档 + 一个卡类型）。
        /// <para><see cref="Tier"/> 是 **1 基**（原版 `cardTier` + 1，与我们 `CardProgress.Level` 同一口径）。</para></summary>
        public struct Entry
        {
            /// <summary>`CardArmy` 枚举值（10…130）。</summary>
            public int Army;
            /// <summary>**1 基**的升级档（1…4）。</summary>
            public int Tier;
            /// <summary>`true` = 原版 `stratagemFrame`（战术卡，cardType 20）；`false` = `troopFrame`（部队/督军，cardType 0 / 10）。</summary>
            public bool Tactic;
            /// <summary>原版 Sprite 的 **`textureRect`（不透明内容的紧包围盒）**，纹理像素、**左下原点**。
            /// <para>🔴 **2026-10-29 订正（`B98_P1`）**：这里原来写的是「`m_Rect`」——**标错了**。
            /// 原版 104 个 Sprite 的 `m_Rect` 是**常量** `(171, 11, 682, 998)`（见 <see cref="SpriteRectW"/>），
            /// 而这四格是逐槽位不同的 **`textureRect`**（来源 = AssetRipper 导出 `.asset`，它把 `m_Rect`
            /// 改写成了 `textureRect`）。**它不是裁切矩形、也不是显示尺寸** ——
            /// 导入/渲染都按 <see cref="SpriteRectX"/> 那一组常量，⛔ 别拿这四格去裁图。</para></summary>
            public float X, Y, W, H;
        }

        /// <summary>原版 Sprite 的 `m_PixelsToUnits`（104/104 全 = 100）。</summary>
        public const float PixelsToUnits = 100f;

        /// <summary>原版 Sprite 的 `m_Rect`（纹理像素，**左下原点**）—— **104/104 全一样**。
        /// <para>判据（2026-10-29 `B98_P1`）：UnityPy 直读原版 bundle
        /// （`…/StreamingAssets/aa/StandaloneWindows64/*cardassets_assets_all.bundle`）的 Sprite 对象，
        /// 四家抽读 + `assets_full/Sprite/*.json` 全表 ⇒ **0 变体**。
        /// ⚠️ 与 `Entry.X/Y/W/H`（= `textureRect`，逐槽位不同）**不是同一个字段**，⛔ 别混。</para>
        /// <para>它是**裁切矩形**（如果走裁图那条路）也是**渲染映射区**：原版 `Image`（`m_Type=0` /
        /// `m_PreserveAspect=0` / **`m_UseSpriteMesh=1`**）把整块 `m_Rect` 拉进 `CardFrame` 的 RectTransform。</para></summary>
        public const float SpriteRectX = 171f, SpriteRectY = 11f, SpriteRectW = 682f, SpriteRectH = 998f;

        /// <summary>原版卡框贴图的画布边长（`Texture2D` **1024 × 1024**，104/104）。</summary>
        public const int TextureSize = 1024;

        /// <summary>原版 `2DCard/CardFrame` 那颗 RectTransform 的**显示矩形**（卡单位；卡本体 = 2.0927 × 3.3313）。
        /// <para>判据（2026-10-29 `B98_P1`）：`assetripper_out/ExportedProject/Assets/**/*.prefab` 全扫，
        /// 44 份带 `CardFrame` 节点的 prefab **锚点与尺寸完全一致** ——
        /// `m_AnchorMin == m_AnchorMax == (0.5, 0.5)`（**居中锚点，不是拉伸** ⇒ `rect.rect == sizeDelta`）、
        /// `m_SizeDelta = (2.2452, 3.2572)`、`m_AnchoredPosition.y = -0.03`、`m_LocalScale = 1`。
        /// 原版 `CardTierUIController.SetTier/SetFrame` 只换 Sprite、**不写 RectTransform**
        /// （`decomp_full/CardTierUIController__SetTier.c` / `Card2DController__SetFrame.c`）⇒ **各档同一尺寸**。</para>
        /// <para>⚠️ 量拉伸锚点的节点要量 `rect.rect` 不是 `sizeDelta`（本项目踩过）—— 这里锚点**不是拉伸**，
        /// 两者相等，所以这条判据不受影响；但换 prefab 前请重核锚点。</para></summary>
        public const float DisplayW = 2.2452f, DisplayH = 3.2572f, DisplayY = -0.03f;

        /// <summary>`m_Rect` 在贴图上的**归一化 UV 矩形**（`(171,11,682,998) / 1024`，左下原点）。
        /// <para>= `CardView.FrameUv` 对「原版卡框贴图」必须返回的那一格；自检直接对它比。</para></summary>
        public static Rect SpriteRectUV
        {
            get { return new Rect(SpriteRectX / TextureSize, SpriteRectY / TextureSize, SpriteRectW / TextureSize, SpriteRectH / TextureSize); }
        }

        /// <summary>表里有多少条槽位（自检读口；应为 104）。</summary>
        public static int Count { get { return Rows.Length; } }

        struct R
        {
            public int Army, Tier; public bool Tactic; public float X, Y, W, H;
            public R(int army, int tier, bool tactic, float x, float y, float w, float h)
            { Army = army; Tier = tier; Tactic = tactic; X = x; Y = y; W = w; H = h; }
        }

        //  ⚠️ 顺序 = `clanFrames` 的序列化顺序（army 10…130）× `framesByTier` 顺序（tier 1…4）×（troop, stratagem）。
        //  每行末尾注释 = 原版 **Texture2D** 名（`Orc Frame` 那种同名 Sprite 名在这里没有信息量）。
        static readonly R[] Rows =
        {
            new R(10, 1, false, 199.07613f, 30.068026f, 611.89716f, 939.85583f),   // 40k_Cardframe_troop_Ultramarines_tier1.png
            new R(10, 1, true, 212.04291f, 29.07612f, 599.8656f, 938.8478f),   // 40k_Cardframe_stratagem_Ultramarines_tier1.png
            new R(10, 2, false, 199.02675f, 30.068026f, 612.94653f, 970.85583f),   // 40k_Cardframe_troop_Ultramarines_tier2.png
            new R(10, 2, true, 211.02675f, 29.07612f, 600.937f, 948.8478f),   // 40k_Cardframe_stratagem_Ultramarines_tier2.png
            new R(10, 3, false, 172.02675f, 30.07612f, 678.93036f, 941.8478f),   // 40k_Cardframe_troop_Ultramarines_tier3.png
            new R(10, 3, true, 173.02675f, 29.07612f, 678.93036f, 941.8478f),   // 40k_Cardframe_stratagem_Ultramarines_tier3.png
            new R(10, 4, false, 172.02675f, 29.07612f, 679.94653f, 948.8478f),   // 40k_Cardframe_troop_Ultramarines_tier4.png
            new R(10, 4, true, 172.02675f, 29.07612f, 679.94653f, 948.8478f),   // 40k_Cardframe_stratagem_Ultramarines_tier4.png
            new R(20, 1, false, 199.07613f, 29.07612f, 643.89716f, 957.89716f),   // 40k_Cardframe_troop_Orks_tier1.png
            new R(20, 1, true, 212.07613f, 29.07612f, 630.89716f, 957.89716f),   // 40k_Cardframe_stratagem_Orks_tier1.png
            new R(20, 2, false, 199.07613f, 29.07612f, 643.89716f, 957.89716f),   // 40k_Cardframe_troop_Orks_tier2.png
            new R(20, 2, true, 212.07613f, 29.07612f, 630.89716f, 957.89716f),   // 40k_Cardframe_stratagem_Orks_tier2.png
            new R(20, 3, false, 199.05132f, 22.07612f, 643.92194f, 964.89716f),   // 40k_Cardframe_troop_Orks_tier3.png
            new R(20, 3, true, 214.0639f, 29.07612f, 628.90936f, 957.89716f),   // 40k_Cardframe_stratagem_Orks_tier3.png
            new R(20, 4, false, 199.07613f, 28.07612f, 643.89716f, 958.89716f),   // 40k_Cardframe_troop_Orks_tier4.png
            new R(20, 4, true, 202.02675f, 29.069195f, 640.94653f, 957.90405f),   // 40k_Cardframe_stratagem_Orks_tier4.png
            new R(30, 1, false, 199.07613f, 30.07612f, 612.89716f, 928.881f),   // 40k_Cardframe_troop_SaimHann_tier1.png
            new R(30, 1, true, 209.02675f, 29.07612f, 605.89716f, 928.881f),   // 40k_Cardframe_stratagem_SaimHann_tier1.png
            new R(30, 2, false, 199.02675f, 29.07612f, 615.89716f, 929.8478f),   // 40k_Cardframe_troop_SaimHann_tier2.png
            new R(30, 2, true, 209.02675f, 29.07612f, 607.94653f, 928.881f),   // 40k_Cardframe_stratagem_SaimHann_tier2.png
            new R(30, 3, false, 199.07613f, 29.07612f, 614.89716f, 929.8478f),   // 40k_Cardframe_troop_SaimHann_tier3.png
            new R(30, 3, true, 203.00194f, 29.07612f, 613.9713f, 929.8478f),   // 40k_Cardframe_stratagem_SaimHann_tier3.png
            new R(30, 4, false, 171f, 11f, 682f, 949.97327f),   // 40k_Cardframe_troop_SaimHann_tier4.png
            new R(30, 4, true, 172.04291f, 29.036285f, 680.9571f, 931.937f),   // 40k_Cardframe_stratagem_SaimHann_tier4.png
            new R(40, 1, false, 199.07613f, 29.07612f, 611.881f, 934.8478f),   // 40k_Cardframe_troop_Sautekh_tier1.png
            new R(40, 1, true, 204.02675f, 30.07612f, 616.94653f, 933.8478f),   // 40k_Cardframe_stratagem_Sautekh_tier1.png
            new R(40, 2, false, 199.02675f, 29.07612f, 610.89716f, 934.8478f),   // 40k_Cardframe_troop_Sautekh_tier2.png
            new R(40, 2, true, 204.02675f, 30.07612f, 616.94653f, 933.8478f),   // 40k_Cardframe_stratagem_Sautekh_tier2.png
            new R(40, 3, false, 198.07613f, 30.07612f, 611.881f, 942.89716f),   // 40k_Cardframe_troop_Sautekh_tier3.png
            new R(40, 3, true, 203.07613f, 30.07612f, 616.8478f, 941.89716f),   // 40k_Cardframe_stratagem_Sautekh_tier3.png
            new R(40, 4, false, 191.04291f, 29.07612f, 640.93036f, 961.8478f),   // 40k_Cardframe_troop_Sautekh_tier4.png
            new R(40, 4, true, 191.04291f, 11f, 640.93036f, 998f),   // 40k_Cardframe_stratagem_Sautekh_tier4.png
            new R(50, 1, false, 199.07613f, 30.07612f, 650.8478f, 939.8478f),   // 40k_Cardframe_troop_BlackLegion_tier1.png
            new R(50, 1, true, 206.07613f, 30.02675f, 643.8478f, 940.89716f),   // 40k_Cardframe_stratagem_BlackLegion_tier1.png
            new R(50, 2, false, 199.02675f, 30.02675f, 649.89716f, 939.89716f),   // 40k_Cardframe_troop_BlackLegion_tier2.png
            new R(50, 2, true, 200.02675f, 30.02675f, 649.89716f, 940.89716f),   // 40k_Cardframe_stratagem_BlackLegion_tier2.png
            new R(50, 3, false, 190.07613f, 30.07612f, 658.8478f, 938.8478f),   // 40k_Cardframe_troop_BlackLegion_tier3.png
            new R(50, 3, true, 190.07613f, 30.02675f, 659.8478f, 940.89716f),   // 40k_Cardframe_stratagem_BlackLegion_tier3.png
            new R(50, 4, false, 189.07613f, 29.07043f, 659.8478f, 957.85345f),   // 40k_Cardframe_troop_BlackLegion_tier4.png
            new R(50, 4, true, 190.07613f, 29.07612f, 659.8478f, 963.8478f),   // 40k_Cardframe_stratagem_BlackLegion_tier4.png
            new R(60, 1, false, 199.04291f, 29.071733f, 619.9142f, 912.8522f),   // 40k_Cardframe_troop_Leviathan_tier1.png
            new R(60, 1, true, 223.07613f, 29.07612f, 597.8478f, 913.89716f),   // 40k_Cardframe_stratagem_Leviathan_tier1.png
            new R(60, 2, false, 199.07613f, 29.07612f, 625.8478f, 918.89716f),   // 40k_Cardframe_troop_Leviathan_tier2.png
            new R(60, 2, true, 215.07613f, 29.07612f, 611.89716f, 919.89716f),   // 40k_Cardframe_stratagem_Leviathan_tier2.png
            new R(60, 3, false, 199.07613f, 29.07612f, 630.8478f, 918.9239f),   // 40k_Cardframe_troop_Leviathan_tier3.png
            new R(60, 3, true, 212.07613f, 29.07612f, 620.89716f, 918.89716f),   // 40k_Cardframe_stratagem_Leviathan_tier3.png
            new R(60, 4, false, 187.07613f, 29.07612f, 652.89716f, 918.89716f),   // 40k_Cardframe_troop_Leviathan_tier4.png
            new R(60, 4, true, 189.04291f, 29.07612f, 652.93036f, 918.91095f),   // 40k_Cardframe_stratagem_Leviathan_tier4.png
            new R(70, 1, false, 198.07613f, 29.02675f, 627.881f, 927.93036f),   // 40k_Cardframe_troop_Tau_tier1.png
            new R(70, 1, true, 205.07613f, 30.07612f, 620.8478f, 926.8478f),   // 40k_Cardframe_stratagem_Tau_tier1.png
            new R(70, 2, false, 198.07613f, 29.02675f, 625.8478f, 927.89716f),   // 40k_Cardframe_troop_Tau_tier2.png
            new R(70, 2, true, 202.06528f, 30.07612f, 621.8586f, 925.8478f),   // 40k_Cardframe_stratagem_Tau_tier2.png
            new R(70, 3, false, 197.07613f, 29.042908f, 629.86414f, 927.881f),   // 40k_Cardframe_troop_Tau_tier3.png
            new R(70, 3, true, 197.07613f, 30.07612f, 629.8478f, 925.8478f),   // 40k_Cardframe_stratagem_Tau_tier3.png
            new R(70, 4, false, 184.04291f, 30.07612f, 655.93036f, 941.89716f),   // 40k_Cardframe_troop_Tau_tier4.png
            new R(70, 4, true, 184.02675f, 30.07612f, 655.94653f, 941.89716f),   // 40k_Cardframe_stratagem_Tau_tier4.png
            new R(80, 1, false, 199.06306f, 29.036285f, 644.86084f, 943.8876f),   // 40k_Cardframe_troop_Sororitas_tier1.png
            new R(80, 1, true, 199.07613f, 29.07612f, 644.8478f, 944.8478f),   // 40k_Cardframe_stratagem_Sororitas_tier1.png
            new R(80, 2, false, 199.06306f, 29.036285f, 644.86084f, 963.8876f),   // 40k_Cardframe_troop_Sororitas_tier2.png
            new R(80, 2, true, 199.07613f, 29.07612f, 644.8478f, 963.8478f),   // 40k_Cardframe_stratagem_Sororitas_tier2.png
            new R(80, 3, false, 183.07613f, 29.07612f, 660.8478f, 963.8478f),   // 40k_Cardframe_troop_Sororitas_tier3.png
            new R(80, 3, true, 183.07613f, 29.07612f, 660.8478f, 964.8478f),   // 40k_Cardframe_stratagem_Sororitas_tier3.png
            new R(80, 4, false, 171f, 29.07612f, 682f, 963.8478f),   // 40k_Cardframe_troop_Sororitas_tier4.png
            new R(80, 4, true, 171f, 29.07612f, 682f, 963.8478f),   // 40k_Cardframe_stratagem_Sororitas_tier4.png
            new R(90, 1, false, 199.07613f, 29.02675f, 632.8478f, 929.89716f),   // 40k_Cardframe_troop_GSC_tier1.png
            new R(90, 1, true, 212.07613f, 29.02675f, 619.8478f, 929.89716f),   // 40k_Cardframe_stratagem_GSC_tier1.png
            new R(90, 2, false, 194.02675f, 28.07612f, 637.93036f, 965.91095f),   // 40k_Cardframe_troop_GSC_tier2.png
            new R(90, 2, true, 195.02675f, 28.07612f, 636.90936f, 960.8478f),   // 40k_Cardframe_stratagem_GSC_tier2.png
            new R(90, 3, false, 199.07613f, 29.02675f, 640.89716f, 961.89716f),   // 40k_Cardframe_troop_GSC_tier3.png
            new R(90, 3, true, 205.07613f, 29.02675f, 634.89716f, 961.89716f),   // 40k_Cardframe_stratagem_GSC_tier3.png
            new R(90, 4, false, 174.07613f, 28.07612f, 676.89716f, 965.91095f),   // 40k_Cardframe_troop_GSC_tier4.png
            new R(90, 4, true, 174.07613f, 29.07612f, 676.89355f, 964.87256f),   // 40k_Cardframe_stratagem_GSC_tier4.png
            new R(100, 1, false, 199.02675f, 28.07612f, 653.93036f, 929.8478f),   // 40k_Cardframes_Troop_Astra Militarum_tier1.png
            new R(100, 1, true, 206.02675f, 28.07612f, 646.89716f, 929.8478f),   // 40k_Cardframes_stratagem_Astra Militarum_tier1.png
            new R(100, 2, false, 194.07613f, 28.07612f, 658.8478f, 929.8478f),   // 40k_Cardframes_Troop_Astra Militarum_tier2.png
            new R(100, 2, true, 194.02675f, 28.07043f, 658.89716f, 929.85345f),   // 40k_Cardframes_stratagem_Astra Militarum_tier2.png
            new R(100, 3, false, 194.05412f, 29.07612f, 658.90295f, 963.8478f),   // 40k_Cardframes_Troop_Astra Militarum_tier3.png
            new R(100, 3, true, 195.07613f, 29.07612f, 657.89716f, 963.8478f),   // 40k_Cardframes_stratagem_Astra Militarum_tier3.png
            new R(100, 4, false, 179.05132f, 29.07612f, 673.87256f, 963.8478f),   // 40k_Cardframes_Troop_Astra Militarum_tier4.png
            new R(100, 4, true, 174.07613f, 29.02675f, 678.8478f, 963.89716f),   // 40k_Cardframes_stratagem_Astra Militarum_tier4.png
            new R(110, 1, false, 199.07613f, 28.07612f, 623.89716f, 956.8478f),   // 40k_Cardframes_Troop_DarkAngels_tier1.png
            new R(110, 1, true, 213.02675f, 28.07612f, 609.94653f, 956.8478f),   // 40k_Cardframes_stratagem_DarkAngels_tier1.png
            new R(110, 2, false, 199.07613f, 28.07612f, 623.89716f, 956.8478f),   // 40k_Cardframes_Troop_DarkAngels_tier2.png
            new R(110, 2, true, 213.02675f, 28.07612f, 609.94653f, 956.8478f),   // 40k_Cardframes_stratagem_DarkAngels_tier2.png
            new R(110, 3, false, 199.07613f, 28.07612f, 623.89716f, 956.8478f),   // 40k_Cardframes_Troop_DarkAngels_tier3.png
            new R(110, 3, true, 213.02675f, 28.07612f, 609.94653f, 956.8478f),   // 40k_Cardframes_stratagem_DarkAngels_tier3.png
            new R(110, 4, false, 179.07613f, 28.07612f, 653.88763f, 956.8478f),   // 40k_Cardframes_Troop_DarkAngels_tier4.png
            new R(110, 4, true, 179.07613f, 28.07612f, 653.88763f, 956.8478f),   // 40k_Cardframes_stratagem_DarkAngels_tier4.png
            new R(120, 1, false, 199.07613f, 28.07612f, 632.8478f, 956.8478f),   // 40k_Cardframes_Troop_Emperor Children_tier1.png
            new R(120, 1, true, 215.02675f, 28.07612f, 616.89716f, 956.8478f),   // 40k_Cardframes_stratagem_Emperor Children_tier1.png
            new R(120, 2, false, 199.07613f, 28.07612f, 632.8478f, 956.8478f),   // 40k_Cardframes_Troop_Emperor Children_tier2.png
            new R(120, 2, true, 215.02675f, 28.07612f, 616.89716f, 956.8478f),   // 40k_Cardframes_stratagem_Emperor Children_tier2.png
            new R(120, 3, false, 194.07613f, 28.072264f, 638.89716f, 956.8516f),   // 40k_Cardframes_Troop_Emperor Children_tier3.png
            new R(120, 3, true, 194.04291f, 28.072264f, 638.93036f, 956.8516f),   // 40k_Cardframes_stratagem_Emperor Children_tier3.png
            new R(120, 4, false, 194.02675f, 28.072264f, 638.93036f, 956.8516f),   // 40k_Cardframes_Troop_Emperor Children_tier4.png
            new R(120, 4, true, 194.02675f, 28.072264f, 638.93036f, 956.8516f),   // 40k_Cardframes_stratagem_Emperor Children_tier4.png
            new R(130, 1, false, 199.07613f, 29.036285f, 614.8478f, 926.937f),   // 40k_Cardframes_Troop_Space Wolves_tier1.png
            new R(130, 1, true, 213.07613f, 29.02675f, 600.8478f, 926.94653f),   // 40k_Cardframes_stratagem_Space Wolves_tier1.png
            new R(130, 2, false, 199.07613f, 29.036285f, 614.8478f, 926.937f),   // 40k_Cardframes_Troop_Space Wolves_tier2.png
            new R(130, 2, true, 213.03876f, 29.02675f, 600.88513f, 926.94653f),   // 40k_Cardframes_stratagem_Space Wolves_tier2.png
            new R(130, 3, false, 192.07613f, 29.02675f, 626.87616f, 941.89716f),   // 40k_Cardframes_Troop_Space Wolves_tier3.png
            new R(130, 3, true, 192.07613f, 29.02675f, 626.87616f, 941.89716f),   // 40k_Cardframes_stratagem_Space Wolves_tier3.png
            new R(130, 4, false, 176.04291f, 29.036285f, 648.93036f, 954.8876f),   // 40k_Cardframes_Troop_Space Wolves_tier4.png
            new R(130, 4, true, 176.02675f, 29.02675f, 648.89716f, 954.89716f),   // 40k_Cardframes_stratagem_Space Wolves_tier4.png
        };

        static readonly int[] Armies = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 };

        /// <summary>原版**纹理名**里的阵营段（含空格）—— 与 `CardDef.Faction` 不是一回事
        /// （`Goff→Orks` · `Genestealers→GSC` · `TauEmpire→Tau` · `EmperorsChildren→Emperor Children` ·
        /// `SpaceWolves→Space Wolves` · `AstraMilitarum→Astra Militarum`）。</summary>
        static readonly string[] ArmyNames =
        {
            "Ultramarines", "Orks", "SaimHann", "Sautekh", "BlackLegion", "Leviathan", "Tau",
            "Sororitas", "GSC", "Astra Militarum", "DarkAngels", "Emperor Children", "Space Wolves",
        };

        /// <summary>`true` = 纹理名走 `40k_Cardframes_{Troop|stratagem}_<阵营>`（army 100–130 这四家的原版写法），
        /// `false` = `40k_Cardframe_{troop|stratagem}_<阵营>`（army 10–90）。
        /// <para>这不是我们起的名字 —— 原版资产本身就有这两种写法（`Cardframes` 多个 s、`Troop` 大写），
        /// 逐条现读自 `Texture2D/*` 的真名；别「统一理顺」。</para></summary>
        static readonly bool[] LongForm =
        {
            false, false, false, false, false, false, false, false, false, true, true, true, true,
        };

        static int ArmyIndex(int army)
        {
            for (int i = 0; i < Armies.Length; i++) if (Armies[i] == army) return i;
            return -1;
        }

        /// <summary>`(army, tier, tactic)` → 那一条；认不出/越界 ⇒ `false`（`out` 是 `default`，别拿它的字段当查到了）。</summary>
        public static bool TryGet(int army, int tier, bool tactic, out Entry e)
        {
            e = default(Entry);
            if (tier < 1 || tier > 4) return false;
            for (int i = 0; i < Rows.Length; i++)
            {
                var r = Rows[i];
                if (r.Army != army || r.Tier != tier || r.Tactic != tactic) continue;
                e = new Entry { Army = r.Army, Tier = r.Tier, Tactic = r.Tactic, X = r.X, Y = r.Y, W = r.W, H = r.H };
                return true;
            }
            return false;
        }

        /// <summary>原版 **Texture2D** 名（含空格，无扩展名）。例：`40k_Cardframe_troop_Ultramarines_tier1`。</summary>
        public static string FrameTextureName(int army, int tier, bool tactic)
        {
            int i = ArmyIndex(army);
            if (i < 0 || tier < 1 || tier > 4) return null;
            bool lf = LongForm[i];
            string kind = tactic ? "stratagem" : (lf ? "Troop" : "troop");
            return (lf ? "40k_Cardframes_" : "40k_Cardframe_") + kind + "_" + ArmyNames[i] + "_tier" + tier;
        }

        /// <summary>我们 `Resources` 下的名字（原版纹理名把空格换成 `_`，与 `ArmyIcons.ResourceNameOf` 同一口径）。</summary>
        public static string ResourceName(int army, int tier, bool tactic)
        {
            var n = FrameTextureName(army, tier, tactic);
            return n == null ? null : n.Replace(' ', '_');
        }

        /// <summary>原版那一格的**高亮（SDF）**Sprite 名（128² 那族，我们已按阵营/类型/档导进 `card_sdf/`）。
        /// <para>取的是 **`SdfTier()` 修正之后**的档 —— 原版 SO 里有一处档位复用，见 <see cref="SdfTier"/>。</para></summary>
        public static string SdfSpriteName(int army, int tier, bool tactic)
        {
            int i = ArmyIndex(army);
            if (i < 0 || tier < 1 || tier > 4) return null;
            bool lf = LongForm[i];
            string kind = tactic ? "stratagem" : (lf ? "Troop" : "troop");
            return (lf ? "40k_Cardframes_" : "40k_Cardframe_") + kind + "_" + ArmyNames[i] + "_SDF_tier" + SdfTier(army, tier, tactic);
        }

        /// <summary>原版 SO 里**唯一一处档位复用**：army 60（Leviathan）`cardTier 3`（我们的 tier4）的
        /// **部队**高亮指向 `cardTier 2`（tier3）那一张（两个槽位同一个 `m_AssetGUID`，逐字节核过）。
        /// <para>除这一格之外，返回值恒等于入参。</para></summary>
        public static int SdfTier(int army, int tier, bool tactic)
        {
            return (army == 60 && tier == 4 && !tactic) ? 3 : tier;
        }

        /// <summary>导入目标名（`Resources` 下**不带扩展名**）：`frame_clan_&lt;ResourceName&gt;`。
        /// <para>🔴 **为什么必须带 `frame_` 前缀、且必须保持 1024 × 1024**：`CardView.FrameUv` 认「原版卡框」的
        /// 判据 = **名字以 `frame_` 开头 + 宽高都是 1024**（`CardView.IsCardFrame`）；命中它才按
        /// **sprite rect `(171,11,682,998)`** 映射 —— 那正是原版 `Image(m_UseSpriteMesh=1)` 的行为。
        /// 一旦掉进另一支（alpha bbox），可见内容会被放大 **≈11.5% 宽 / ≈6.2% 高**
        /// （682/611.9 = 1.115、998/939.9 = 1.062，按 `textureRect` 那组数算）。</para>
        /// <para>⛔ **别先裁成 682×998 再导**：那样既掉进 alpha-bbox 分支，又白丢像素。
        /// ⇒ 导入 = **原图 1024² 原样拷进来、只改名**（不裁、不缩放）。</para></summary>
        public static string ImportResourceName(int army, int tier, bool tactic)
        {
            var n = ResourceName(army, tier, tactic);
            return n == null ? null : "frame_clan_" + n;
        }

        /// <summary>取卡框贴图（`Resources/Art/cards/clan/&lt;ImportResourceName&gt;`）。
        /// <para>**已就位 104/104**（2026-10-30 `B100_F12` 现核）：104 张原版贴图全在盘上；在此之前只有 80 格取得到，
        /// 是因为 24 张文件名**保留了空格**（`Astra Militarum`/`Emperor Children`/`Space Wolves` × 4 档 × 2 类）—— 已按盘侧重命名成下划线形（`.meta` 同改、GUID 不变）⇒ 104 格全命中；仍取不到时返回 `null`、落回自绘那套。</para>
        /// <para>⚠️ 导入目标名 = <see cref="ImportResourceName"/>（`frame_clan_*`），⛔ 不是 <see cref="ResourceName"/>。</para></summary>
        public static Texture2D FrameTexture(int army, int tier, bool tactic)
        {
            var n = ImportResourceName(army, tier, tactic);
            if (n == null) return null;
            return Resources.Load<Texture2D>(CardArt.Root + "cards/clan/" + n);
        }
    }
}
