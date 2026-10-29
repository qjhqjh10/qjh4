// CardArt.cs — 「原版复刻」用的美术入口
//
// 全部走 `Resources/Art/`：
//     Art/arena1_bg.png            战场背景（ArtBaker 从 WarpforgeArena1 场景烘的）
//     Art/cards/frame_<阵营>.png   卡框（原版的空框，数值/名字由我们画在上面）
//     Art/ui/<名字>.png            原版战斗 UI 图（END TURN 按钮、能量球、玩家框……）
//
// ⚠️ **删掉整个 `Resources/Art/` 目录，游戏照样能跑** —— 每一处都判空，
//    没有美术就退回程序生成的占位卡面 + 纯色背景（`CardArt.Available == false`）。
//    这就是「原版复刻 → 换自己的美术」的开关：换图 = 换同名文件，删目录 = 回占位。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class CardArt
    {
        public const string Root = "Art/";

        static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();
        static bool _loaded;

        /// <summary>背景（战场图）。没有就返回 null，画面用纯色。</summary>
        public static Texture2D Backdrop { get; private set; }

        /// <summary>有没有装原版美术 —— 自检和 HUD 靠它决定走哪条路</summary>
        public static bool Available { get; private set; }

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            Backdrop = Resources.Load<Texture2D>(Root + "arena1_bg");
            Available = Backdrop != null || Ui("40k_battle_energy_full") != null;
        }

        /// <summary>阵营卡框 —— **只按阵营取**（稀有度传 null ⇒ 走 `TierOf` = 最高档 `MaxTier`，
        /// ⚠️ 2026-10-18 更正：本行原写「取 tier1」，与 `TierOf` 恒返 `MaxTier` 打架）。
        /// 阵营名大小写不敏感（"Ember" → frame_ember）。</summary>
        public static Texture2D Frame(string faction)
        {
            return Frame(faction, null, false);
        }

        /// <summary>
        /// 阵营卡框 —— **不传 `tier` 时取该阵营的【最高档】**（`MaxTier`），**战术卡另有一套框**。
        /// 🆕 **2026-10-18**：卡面那一路现在**传 `tier`**（= `CardProgress.Level(artId)`，「升级 → 换卡框」）；
        /// 本段「一律最高档」只描述**缺省档**，见下面 `<param name="tier">`。
        ///
        /// 🔴 **2026-09-27 整段作废原来那句「按稀有度取对应 tier（原版就是分四档的）」** ——
        ///   原版里**「升级档」就是「卡框档」**（`CardFramesSO.GetClanFrame(army, tier, cardType)`，
        ///   **入参里没有稀有度**），**稀有度驱动的是底部菱形宝石**、与框并存。
        ///   详见 <see cref="TierOf(string,string)"/> 的说明。
        /// ⚠️ 下面那条「common=1/rare=2/…」的实测（对照图 `…/tier_map.png`）是**当年按错前提量的**，
        ///   保留只为对照历史（`TierOfLegacy` 就是它）。
        ///
        /// 原版每个阵营有**两套**框：`troop`（部队/督军）和 `stratagem`（战术卡）——
        /// 战术卡那张的形制不一样（下半截是大片文字区）。`tactic: true` 取后者。
        ///
        /// 取不到（没导那一档 / 稀有度不认识）**逐级退回**：本类型的 tier1 → 另一类型的 tier1 →
        /// 不带 tier 的老名字 —— 美术目录缺档也不至于变成没框的占位卡。
        /// </summary>
        /// <param name="tier">🆕 **2026-10-18（用户拍板「通过升级按钮进行不同等级的卡框切换」）**：
        /// 卡框档位（1..4，= 原版 `CardTier` 的 1 基写法，显示等级与它同值）。
        /// **0（缺省）= 走 <see cref="TierOf"/>**（那一路恒返 `MaxTier`）⇒ **老调用点行为一字不变**。
        /// 传了就用它 —— 这是「升级 → 换卡框」那条链的落点：`CardView` 传 `CardProgress.Level(artId)`。
        /// 判据（原版一手）：`UpgradePanel.ChangeFrameOnLevelUp` → `BasicCardUI.RefreshCardVisualEffects(cardUI, newTier)`
        /// → `CardTierUIController.SetTier` → `CardFramesSO.GetClanFrame(army, **tier**, cardType)`
        /// （入参里没有稀有度）；全量说明 → `资料/普查产出_1018/R_卡片详情大图三问.md` §4.2。
        /// ⚠️ 档位越界按 `[1, MaxTier]` 夹（存档被手改坏也不至于取不到图）。</param>
        public static Texture2D Frame(string faction, string rarity, bool tactic = false, int tier = 0)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            string fac = faction.ToLowerInvariant();
            string kind = tactic ? "_strat" : "";
            if (tier <= 0) tier = TierOf(rarity, faction);
            tier = Mathf.Clamp(tier, 1, MaxTier); { var so = CardArtClanFrames.FrameTexture(ArmyIcons.Id(faction), tier, tactic); if (so != null) return so; }   // 原版 `CardFramesByArmy` SO 优先（`Core/CardArtClanFrames`）；未导 ⇒ 落回下面自绘那套
            if (tier > 1)
            {
                var t = Get(Root + "cards/frame_" + fac + kind + "_tier" + tier);
                if (t != null) return t;
            }
            var one = Get(Root + "cards/frame_" + fac + kind + "_tier1");
            if (one != null) return one;
            var def = Get(Root + "cards/frame_" + fac + kind);
            if (def != null) return def;
            // 战术卡框整批没有（老版本只导了 troop）→ 退回 troop 的，别让卡变空白
            return kind.Length > 0 ? Frame(faction, rarity, false, tier) : null;
        }

        /// <summary>
        /// 软光/影那层（原版 `Card Highlight And Shadow`）的 **SDF 贴图** —— 与 <see cref="Frame"/> **同一套命名与逐级退回**
        /// （`card_sdf/&lt;阵营&gt;&lt;_strat&gt;_tier&lt;N&gt;`）。
        /// 它是原版**预生成的 128² 灰度距离场**（`40k_Cardframe{ s}_{ troop|stratagem}_&lt;阵营&gt;_SDF_tier1..4`，104 张），
        /// **不是运行时算的** —— 2026-09-19 查实，正本 `资料/普查产出_0919/卡面SDF软光影_查证.md`；
        /// 导入脚本 `工具/import_original_card_sdf.py`。
        /// ⚠️ 取不到返回 null ⇒ 那一层整个不画（和 `CardArt` 其它层一个规矩：删掉 `Resources/Art/` 游戏照样跑）。
        /// </summary>
        /// <summary>🆕 2026-09-29：**卡框那圈**的原版 SDF 图 —— 原版 sprite `Card board frame SDF`
        /// （`m_RD.textureRect` **79×107 @(25,11)** · PPU **100** · pivot **(0.5,0.5)**，底图 128×128；
        /// 出处 `bundle_battleprefabs_vfxandmisc_assets_all/Sprite/Card board frame SDF.json`）。
        /// 我们存的是**已经裁好的那一块**（`card_sdf/Card_board_frame_SDF.png`，79×107）——
        /// 与工程里那份 `WarpforgeVFX/Textures/Card board frame SDF_sprite.png` **逐像素相同**。
        /// （底图那张 128×128 就是 `card_sdf/generic.png`，与原版 `Texture2D/Card board frame SDF.png`
        ///   **16384/16384 逐字节一致**。）
        /// 用途 = 原版 `MinionLight` 上那个 `FrameHighlight`（6 态状态环）。</summary>
        public static Texture2D CardFrameSdf() { return Get(Root + "card_sdf/Card_board_frame_SDF"); }

        public static Texture2D Sdf(string faction, string rarity, bool tactic = false, int tier = 0)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            string fac = faction.ToLowerInvariant();
            string kind = tactic ? "_strat" : "";
            // 🆕 2026-10-18：档位与 `Frame` **同一份口径**（0 = 走 `TierOf`，否则夹到 1..MaxTier）——
            //   判据与说明只写在 `Frame` 的 `<param name="tier">` 上，⛔ 别在这里再写一套。
            if (tier <= 0) tier = TierOf(rarity, faction);
            tier = Mathf.Clamp(tier, 1, MaxTier); tier = CardArtClanFrames.SdfTier(ArmyIcons.Id(faction), tier, tactic);   // 原版 SO 那一格（唯一复用：Leviathan tier4 部队 ⇒ tier3 的 SDF）
            if (tier > 1)
            {
                var t = Get(Root + "card_sdf/" + fac + kind + "_tier" + tier);
                if (t != null) return t;
            }
            var one = Get(Root + "card_sdf/" + fac + kind + "_tier1");
            if (one != null) return one;
            if (kind.Length > 0)                                  // 战术卡那套没有 → 退回 troop 的
            {
                var other = Sdf(faction, rarity, false, tier);
                if (other != null) return other;
            }
            // 最后兜底：原版那张**通用**的 `Card board frame SDF`（我们存成 `generic.png`）
            return Get(Root + "card_sdf/generic");
        }

        /// <summary>稀有度 → 卡框档位（不看阵营）。**判据只此一处**。</summary>
        public static int TierOf(string rarity) => TierOf(rarity, null);

        // ==================================================================
        //  场上那张 **3D 卡体**（原版 `3DBody`）的资源（2026-09-19）
        // ==================================================================
        // 规格与出处 = `资料/3DBody_原版场上卡体规格.md`；导入脚本 = `工具/import_original_3dcard.py`。
        // 和卡面其它美术同一条路：`Resources/Art/` 下、**gitignore 的本地件**，删掉就退回 2D 立绘。

        /// <summary>3D 卡体的网格（`Card 3D WH40k`，881 顶点 / 833 三角面 / 1 个 submesh）。
        /// 🔴 **三套 UV 都在**（我们导进来那份 `.asset` 的通道 4/5/6 = UV0(2维) / **UV1(2维)** / **UV2(4维)**，
        /// 2026-09-19 实读）：UV0 = 底板图集、**UV1 = 立绘那一套**、UV2.x = 正面下部计数器面板的开关。</summary>
        public static Mesh Card3DMesh()
        {
            if (_card3DMeshLoaded) return _card3DMesh;
            _card3DMeshLoaded = true;
            _card3DMesh = Resources.Load<Mesh>(Root + "card3d/Card 3D WH40k");
            return _card3DMesh;
        }

        /// <summary>3D 卡体的底板图集（512²，材质的 `_BaseMap`）</summary>
        public static Texture2D Card3DBase() { return Get(Root + "card3d/Card3D_BaseColor"); }

        /// <summary>3D 卡体的 matcap（材质的 `_MatCap`）—— **按卡等级（= 原版 `CardTier`）分四档**。
        ///
        /// 🔴 **2026-10-21（W61 第 2 批 · 第 6 条）就地订正（铁律 5）**：本行原来写
        /// 「本地只解出 tier1 这一张，其余档的解包资源里没有 ⇒ 四档先用同一张」—— **那是错的**：
        /// 四张都在本地。判据（UnityPy 直读 shipped bundle，不是按名字猜）：
        ///   `CardMaterialHelper.matcapTexturesByCardTier`（`bundle_scenes_scenes_battlearena1` 的
        ///   `MonoBehaviour_4274`）是 4 个 Texture2D 引用，逐 PathID 落到
        ///   `battlesharedresources_assets_all.bundle`：
        ///   | 数组下标 = `CardTier` | 贴图 |
        ///   |---|---|
        ///   | 0 `Tier1` | `MatCap Card Level 1` |
        ///   | 1 `Tier2` | `Matcap Bronze` |
        ///   | 2 `Tier3` | `Matcap Silver` |
        ///   | 3 `Tier4` | `Matcap Gold` |
        ///   （`CardTier.cs` = `Tier1=0 … Tier4=3`；取法 = `CardMaterialHelper__GetCardMatCapByCardTier.c`
        ///    直接拿 `CardTier` 当数组下标、越界夹到末位。）
        ///
        /// <param name="level">**1 基**的卡等级（= 我们 `CardProgress.Level(artId)`，1..4；与原版
        /// `CardTier + 1` 同值 —— 与卡框那一路 `CardArt.Frame(..., tier)` 是**同一个口径**）。
        /// `0`（缺省）= 原版 Tier1 ⇒ **老调用点行为一字不变**。</param>
        /// <para>⚠️ 这里只覆盖**死亡爆散体**那个调用点（`CardFeel.SpawnDeathBody`）；**场上那张 3D 体**
        /// 的 `_MatCap` **已按档位取**（2026-10-25 `E`）：`CardView.BuildBody3D` 改成
        /// `CardArt.Card3DMatcap(CardView.CardTierFor(Data))`，档位与卡框**同一处口径**。
        /// 判据 = 原版 `SetCardImageTo3DBase` 走同一个 `GetCardMatCapByCardTier`
        /// （`BattleCardUI__SetCardImageTo3DBase.c:58`）。</para>
        /// </summary>
        public static Texture2D Card3DMatcap(int level = 0)
        {
            string name = "MatCap_Card_Level1";
            if (level > 0)
            {
                switch (Mathf.Clamp(level, 1, MaxTier))
                {
                    case 2: name = "MatCap_Bronze"; break;
                    case 3: name = "MatCap_Silver"; break;
                    case 4: name = "MatCap_Gold"; break;
                }
            }
            var t = Get(Root + "card3d/" + name);
            // 缺那一档（`Resources/Art/card3d/` 是本地件，别的机器没跑过导入脚本）⇒ 退回 tier1，
            //  **出声一次**（不许静默：那会让「这张卡是 tier4」在画面上看不出来）。
            if (t == null && name != "MatCap_Card_Level1")
            {
                if (!_warnedMatcapTier)
                {
                    _warnedMatcapTier = true;
                    Debug.LogWarning($"[CardArt] 缺 `{Root}card3d/{name}` ⇒ 3D 卡体/爆散体的 matcap 退回 tier1"
                                   + "（判据 = 原版 `matcapTexturesByCardTier` 四档；补：跑 "
                                   + "`python 工具/import_original_3dcard.py`）");
                }
                t = Get(Root + "card3d/MatCap_Card_Level1");
            }
            return t;
        }

        /// <summary>「某一档 matcap 缺图」只出声一次。</summary>
        static bool _warnedMatcapTier;

        /// <summary>🆕 2026-09-25：**卡底那枚软阴影**的图（原版 sprite `Card blob shadow`）。
        /// 原版是 128×128 @ `m_PixelsToUnits = 100`（内容 `textureRect` 124.848²）；我们导的是**裁掉透明边**
        /// 的 125×125 内容 ⇒ 按 **PPU 100** 建 sprite 时内容尺寸与原版差 **0.12%**。
        /// 🔴 它是**白图 + alpha 掩码**，黑色靠 `SpriteRenderer.color` 染（见 `BlobShadow.cs` 文件头）。</summary>
        public static Texture2D Card3DBlobShadow() { return Get(Root + "card3d/CardBlobShadow"); }

        /// <summary>🆕 2026-09-29：**进攻卡的卡面插画**（`Resources/Art/offensive/&lt;阵营>_c&lt;槽>.png` ·
        /// 空卡是 `…_empty.png`；由 `工具/import_offensive_faces.py` 从解包资源导进来，**50/52** ——
        /// 帝皇之子槽 0/1 两张原版自己就没有（那是「原版自己就乱」的三处之一，见源表 `note`）。
        /// ⚠️ **这是插画、不是拼好的卡**（原版解包侧那批就是纯插画；卡名/效果文字我们没拿到 ——
        /// 卡引用在远端 CCD）。⚠️ 文件名规则与那个导入脚本的 `sanitize()` **必须一致**。
        /// `idx &lt; 0` = 「不使用进攻卡」那张。</summary>
        public static Texture2D OffensiveFace(string army, int idx)
        {
            if (string.IsNullOrEmpty(army)) return null;
            return Get(Root + "offensive/" + Sanitize(army) + (idx < 0 ? "_empty" : "_c" + idx));
        }

        /// <summary>只留 ASCII 字母数字、其余压下划线（与 `工具/import_offensive_faces.py` 的
        /// `sanitize()` 逐字一致 —— 两边不一致就会「图导进来了但运行时找不到」）。</summary>
        static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            bool lastUnderscore = false;
            foreach (var ch in s)
            {
                bool alnum = (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
                if (alnum) { sb.Append(ch); lastUnderscore = false; }
                else if (!lastUnderscore && sb.Length > 0) { sb.Append('_'); lastUnderscore = true; }
            }
            while (sb.Length > 0 && sb[sb.Length - 1] == '_') sb.Length--;
            return sb.ToString();
        }

        /// <summary>🆕 2026-09-29：**卡体溶解**那份材质用的噪声图（原版 shader `Everguild/Cards/3D Card Dissolve`
        /// 的 `_DissolveTex`）。
        /// ⚠️ **原版材质指向哪张贴图，判据见 `资料/待办判据_战场与战斗视图.md` 第 9 条那一轮查证**
        /// （找不到就是找不到，不许拿别的图冒充「原版那张」）。这里取 `Art/card3d/DissolveNoise`；
        /// 取不到返回 null ⇒ 溶解材质留用 shader 自带的默认值，并由 `CardView.DissolveMaterial` **出声**。</summary>
        public static Texture2D DissolveNoiseTex()
        {
            var t = Get(Root + "card3d/DissolveNoise");
            if (t == null)
                Debug.LogWarning("[CardArt] 没有 `Art/card3d/DissolveNoise` ⇒ 卡体溶解用的噪声图是 shader 默认值"
                               + "（**原版那张实现上没找齐**，见 `CardView.DissolveMaterial` 的注释）");
            return t;
        }

        /// <summary>🆕 2026-09-26：「未行动」绿光（原版 `3DBody/CanActParticles`）的 `_BaseMap`。
        /// 原版材质 = **`Circle_Hoop Additive`**，shader 就是 URP 自带的
        /// `Universal Render Pipeline/Particles/Unlit`，`_BaseMap` → 贴图 **`Circle_Hoop`**
        /// （那张图在 `bundle_duplicateassetisolation_assets_all`，不在卡那个包里）。
        /// 逐值判据 → `项目任务.md` §三 第 12 条第 3 项。</summary>
        public static Texture2D CanActCircleHoop() { return Get(Root + "card3d/CanAct_CircleHoop"); }

        /// <summary>🆕 2026-09-26：`RotatingRing`（`CanActParticles` 的子节点）的 `_MainTex`。
        /// 原版材质 = `Sparks UI Additive Scroll`（shader = `Everguild/FX/Halo UV scroll`，
        /// 运行时从随包 bundle 取 —— **名字里的 `Scroll` 就是那圈会转的原因**），贴图 = **`Spark UI`**。</summary>
        public static Texture2D CanActSparkUI() { return Get(Root + "card3d/CanAct_SparkUI"); }

        /// <summary>🆕 2026-09-29：原版 **`Minion Death Icon`** —— 「**这一下会打死它**」那个预览图标。
        /// sprite 127×180 · PPU 100 · pivot .5/.5，切自 `battleatlasui` 图集
        /// （图集矩形 `[545, 1148, 127, 180]`，见 `素材/.../battleatlasui/sliced/_atlas_rects.json`）。
        /// ⚠️ 按**普通贴图**导入（同目录这批 UI 图都是 `spriteMode: 0`）—— 我们拿 MeshRenderer 画它。
        /// ⚠️ `Resources/Art/` 整个在 `.gitignore` 里 ⇒ **新克隆没有这张图**，
        ///    `CardView.SetWillDie` 会**出声**退回（不静默）。
        /// **重建办法**：把切片缓存
        /// `素材/Warpforge原版/UI图集/图集/battleatlasui/sliced/Minion_Death_Icon.png`
        /// 拷成 `Resources/Art/ui/Minion_Death_Icon.png` 即可。</summary>
        public static Texture2D WillDieIcon() { return Ui("Minion_Death_Icon"); }

        static Mesh _canActRingMesh;
        /// <summary>🆕 2026-09-26：`RotatingRing` 那个环形网格。
        /// 🔴 **是 `FxObject_cylinder_short`，不是 `Cylinder_Ring`** —— 实据 = 用 UnityPy 直读
        /// `battleprefabs_vfxandmisc_assets_all.bundle`，`ParticleSystemRenderer_1479589607930043328`
        /// 的 `m_Mesh` pathID **4959531874643241410** ⇒ `Mesh.m_Name = "FxObject_cylinder_short"`。
        /// （`Cylinder_Ring` 确实也在同一个包里，所以「叫 Ring 所以是它」看着很像 —— **那是猜的，已订正**。）</summary>
        public static Mesh CanActRingMesh()
        {
            if (_canActRingMesh != null) return _canActRingMesh;
            _canActRingMesh = Resources.Load<Mesh>(Root + "card3d/FxObject_cylinder_short");
            if (_canActRingMesh == null)
                Debug.LogWarning("[CardArt] 取不到 `Art/card3d/FxObject_cylinder_short`"
                               + " ⇒ 未行动绿光那圈 RotatingRing 会没有网格。跑 `工具/import_original_3dcard.py` 补。");
            return _canActRingMesh;
        }

        static Mesh _card3DMesh;
        static bool _card3DMeshLoaded;

        /// <summary>
        /// 稀有度 → 卡框档位。**判据只此一处**。
        ///
        /// 🔴 **`special` 不是一个「稀有度→档位」的函数** —— 2026-09-14 逐张开卡图实测（39 张防御卡，
        /// 两套独立方法 19/19 一致）：`tier1` 24 张 / `tier2` 15 张，**差异落在阵营上**，
        /// 每阵营三张完全一致（＝**印刷批次**的边界，不是稀有度边界）。
        /// ⇒ 所以它要**按阵营**查 <see cref="SpecialTier2Factions"/>。
        /// 出处：`资料/卡表核对_卡图提取/_裁定_special卡框.md` §二 / §2.1。
        ///
        /// ⚠️ **两处旧说法都已作废**：
        /// · 原来这里写 `case "special": return 4;`（注释标「没实测过」）—— **39 张里一张 t4 都没有**；
        /// · `工具/import_original_art.py` 的 `RARITY_TIER` 原来被写成「同一张表，改要两边一起改」——
        ///   **那是错的**：它全仓**没有任何引用**（死代码），导出脚本对每个阵营**无条件导全部 4 档**，
        ///   改它不改变任何产物。**生效路径只有这一处。**
        /// </summary>
        public static int TierOf(string rarity, string faction)
        {
            // 🆕 **2026-09-27 用户拍板：所有卡一律取【最高档】卡框。**
            //
            // 🔴 **起因是一次实测更正（原版反编译一手证据）**：
            //   · **原版的「升级档」就是「卡框档」** —— `CardTier{Tier1=0..Tier4=3}`，
            //     `CardTierUIController.SetTier` 转发到 `CardFramesSO.GetClanFrame(army, **tier**, cardType)`
            //     —— **入参里根本没有稀有度**；
            //   · **稀有度驱动的是另一层**：底部菱形宝石（`CardRarityHolderController.SetCardRarity`），与框**并存**；
            //   · **升级只改美术**（框 / 高亮框 / SDF 软光 / matcap）—— **攻血甲费用效果一处都不改**；
            //   · 新卡在原版**一律 Tier1**，玩家逐级升到 Tier4。
            //   ⇒ 本文件原来按**稀有度**挑框（common1/rare2/epic3/legendary4）—— **那条映射在原版里不存在**
            //     （`资料/已知的坑.md` 早就标过，可代码只改了 `special`、其余四档一直留着 ⇒ 自相矛盾）。
            //
            // **用户 2026-09-27 的决定**：本作全解锁、不做升级/合成 ⇒ **一律按最高档画**（= 原版的「满级状态」）。
            // ⚠️ **这是一处【有意】的偏离**：原版初见一张新卡是 **Tier1** 的框。要退回「按升级档画」，
            //   把这里换成 `Mathf.Clamp(CardProgress.Level(id), 1, MaxTier)` 那条路即可（升级档本来就存在）。
            // 🔴 **2026-10-18 就地更新（铁律 5 · 用户当天拍板「通过升级按钮进行不同等级的卡框切换」）**：
            //   **那条「按升级档画」的路【已经接通了】** —— 走的是 `Frame/Sdf` 新增的 `tier` 形参
            //   （`CardView` 把 `CardProgress.Level(artId)` 传进去），**本函数保持 `MaxTier` 不动**
            //   ⇒ 好处是：① 所有**没有**卡 id 的调用点（演示卡、`BattleScene` 那几条直接调 `CardArt.Frame` 的断言）
            //   行为一字不变；② 「框档从哪来」只有 `CardView` 一条链说了算，这里不再有第二个判据。
            //   ⇒ ⛔ 别把本函数改成读 `CardProgress`（那会让「缺省 1」把无 id 的占位卡也拖成 tier1）。
            // ⚠️ 两个入参保留是为了**调用点不动**（`Frame` / `Sdf` 都按 (rarity, faction) 调）。
            return MaxTier;
        }

        /// <summary>卡框的最高档（原版 `CardTier.Tier4`）—— **本作所有卡都用它**，见 <see cref="TierOf"/>。</summary>
        public const int MaxTier = 4;

        /// <summary>（旧）按稀有度/阵营推档 —— **原版里这条映射不存在**，保留只为对照历史，**别再调用**。</summary>
        static int TierOfLegacy(string rarity, string faction)
        {
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "rare":      return 2;
                case "epic":      return 3;
                case "legendary": return 4;
                case "special":   return SpecialTier(faction);
                default:          return 1;   // common / 空 / 不认识（**兜底不能动**，自检有断言）
            }
        }

        /// <summary>`special` 里判 **tier2** 的那 5 个阵营（其余阵营 = tier1）。
        /// 实测来源同上；引擎阵营名与裁定表逐字一致（括号里是原版框资源里的名字）：
        /// `AstraMilitarum` · `DarkAngels` · `Genestealers`(=GSC) · `Goff`(=Orks) · `Sororitas`。</summary>
        static readonly HashSet<string> SpecialTier2Factions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        { "AstraMilitarum", "DarkAngels", "Genestealers", "Goff", "Sororitas" };

        /// <summary>`special` 取哪一档：判 tier2 的 5 个阵营 → 2，其余 → 1
        /// （**不是**「不认识就 tier1」那条兜底 —— 那条在 <see cref="TierOf(string,string)"/> 的 default 支）。</summary>
        static int SpecialTier(string faction) => SpecialTier2Factions.Contains(faction ?? "") ? 2 : 1;

        /// <summary>🆕 2026-10-03（A20）：**旧占位阵营名 → 真阵营名**。
        /// `Ember`/`Tide` 是我们**早期演示卡组**的占位阵营名（`CardView` 的 demo、`VfxMap`
        /// 里也各留了一条映射），实际就是**黑色军团 / 灵族**（`import_original_art.py` 的 `PORTRAITS` 表：
        /// 「Ember Legion → 黑色军团」「Tide Swarm → Saim-Hann」）。
        /// 卡背那两条查询（默认背 / 它的 SDF）要认这两个旧名，否则**战场自检那局**（我方阵营 `Ember`）
        /// 会查不到默认卡背 → 牌堆 SDF 那层不建（实测 2026-10-03 就是这么红的）。</summary>
        static string ModernFaction(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return faction;
            switch (faction.ToLowerInvariant())
            {
                case "ember": return "BlackLegion";
                case "tide":  return "SaimHann";
                default:      return faction;
            }
        }

        /// <summary>阵营卡背（牌堆/弃牌堆用）。
        /// 🆕 **2026-10-03（A20）**：改成走**原版那张「每阵营默认卡背」表** ——
        /// 原版 `ArmyUtilities.GetDefaultCardback(deckArmy)` → `DefaultCarbackByArmySO`（13 条，无 Neutral）。
        /// 表在 `Resources/Cardbacks.json` 的 `defaults`（`CardbackTable.DefaultFor`），
        /// 由 `工具/read_default_cardbacks.py` 从**原版原始字节**读出。
        /// ⚠️ **旧那 4 张 `cards/back_&lt;阵营>.png` 是我们自己挑的**（`import_original_art.py` 的 `BACKS`），
        /// 与这份表**不同**（实测：`ultramarines` 那张是 `Cardback_UM_Astartes`，而原版默认是
        /// `Cardback_UM_Campaign_Free`）⇒ 只在表里查不到时兜底，且**出声**。</summary>
        public static Texture2D CardBack(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            var name = CardbackTable.DefaultFor(ModernFaction(faction));
            if (!string.IsNullOrEmpty(name))
            {
                var t = Cosmetic(name);
                if (t != null) return t;
                Debug.LogWarning("[CardArt] 阵营默认卡背的**图**取不到：" + name + "（阵营 " + faction + "）—— "
                               + "跑 `python 工具/import_original_art.py` 补图；下面退回旧那 4 张手挑的背");
            }
            return Get(Root + "cards/back_" + faction.ToLowerInvariant());
        }

        /// <summary>🆕 2026-09-26：**战斗牌堆**那层 SDF 的掩码（`Art/cardbacks/&lt;装饰品名>_sdf`）。
        /// 拿不到（图没导 / 阵营名不对）返回 **null** ⇒ 那层不画（牌堆本体照旧）。
        /// 🔴 **2026-10-03 就地更正（A20）**：原来这里挂着一份**手写的 4 条** `BackCosmetic`（只在 4 个阵营有值），
        /// 并在注释里写「其余阵营取不到是正常的」—— **那条现在不成立了**：默认卡背来自
        /// `CardbackTable.DefaultFor(阵营)`（**13 个阵营全有**，判据 → `Core/CardbackTable.cs`）。
        /// 那张手写表已删（两处写同一条规则 = 迟早不一致）。</summary>
        public static Texture2D CardBackSdf(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            var name = CardbackTable.DefaultFor(ModernFaction(faction));
            if (string.IsNullOrEmpty(name)) return null;
            return CosmeticSdf(name);
        }

        /// <summary>某张卡的立绘（`Art/cards/art_&lt;键>.png`）。没有就返回 null，
        /// `CardView` 会退回程序生成的占位图。
        ///
        /// 🔴 **键是引擎卡 id**（`UM82` / `DA12`），**不是卡名** —— 2026-09-15 改的：
        /// 原来按卡名命名，而卡池里有 5 组**同名跨阵营**的卡，后写的那张直接覆盖前一张
        /// （实测 `art_aggressor.png` 是太空野狼那张，暗黑天使 `DA12` 挂着别人的画）。
        /// 调用方一律用 `BattleDriver.ArtKey(cardDef)`（原版卡给 id、自造的 26 张给卡名）。
        /// 出处：`资料/PnP卡图_逐张对账_0915.md` §五。</summary>
        public static Texture2D Portrait(string artKey)
        {
            if (string.IsNullOrEmpty(artKey)) return null;
            return Get(Root + "cards/art_" + Slug(artKey));
        }

        // ==================================================================
        //  🆕 2026-10-25（B2）：**场上 3D 卡体的「立绘 UV 窗」** —— 唯一实现
        // ==================================================================
        //  背景（判据全部现读，不是转抄）：
        //   · 原版 `BattleCardUI.SetCardImageTo3DBase` 只做一句
        //     `material.SetTexture(_CardImage, rawCard.cardSprite.texture)`
        //     （`d:/2/tools/decomp_full/BattleCardUI__SetCardImageTo3DBase.c`，逐行读过）
        //     ⇒ 喂进去的是 **sprite 所在的那张【整页】贴图**，不是裁好的子图。
        //   · 采样坐标 = mesh 的第二套 UV（UV1），**写死在网格上**
        //     （正面四角 0.1818–0.8203 × 0.0017–0.9978，出处 `资料/3DBody_原版场上卡体规格.md` §一
        //      + PS 反汇编：`tex2D(_CardImage, uv1)`）。
        //   ⇒ 原版不管哪张卡，**永远只采整页上那一窗**。
        //
        //  我们的 `art_*.png` 有两种来源（1168 张全量实测：672 张裁块 + 496 张整页）：
        //   ① **sprite 裁块**（宽 670/671 × 高 1024）：`Sprite/*.json` 的 `textureRect`
        //      = x176.5 y0 w670.5 h1024 ⇒ 原版那一窗换算到 PNG 坐标 =
        //        u [(186.1632−176.5)/670.5, (840.0−176.5)/670.5] = [0.014412, 0.989541]
        //        v [0.0017, 0.9978]（裁块是整高 ⇒ v 不缩放）
        //      ⚠️ 这条窗的左右边界**正好落在白边外侧 ~1.7 px** ⇒ 屏上一像素要盖住 ~4.5 个源像素、
        //        而场上卡的缩放每帧按深度重算 ⇒ 双线性采样点每帧落在不同格，会把 PNG 边那圈
        //        纯白卡框内沿采回来（= 用户报的「场上卡白边一直闪」）。
        //        🔴 2026-10-25 就地订正：这条**不是 mipmap 造成的** —— 立绘 `.meta` 是
        //        `enableMipMap: 0`（art_am1/am39/um11/sau19 逐份现读），机制是**双线性 + 每帧缩放**。
        //        实测四边近白条最大宽度 **左 8 / 右 7 / 上 2 / 下 2 px** ⇒ 统一再多让 **3 px**：
        //        左 11 · 右 10 · 上 5 · 下 5 px（窗只缩 0.66%/0.59%，不拉变形）。
        //   ② **整页方图**（1024²×495、512²×1）：这些卡的原版 sprite `m_RD.textureRect` **就是整页**
        //      （现读：`bundle_astramilitarumcardassets_assets_all/Sprite/AstraMilitarum_strat_Born Soldier.json`
        //       → `{x:0,y:0,w:1024,h:1024}`；`bundle_orksgoffcardassets_assets_all/Sprite/Ork_Goff_strat_Uge Choppa.json`
        //       → `{0,0,512,512}`）⇒ PNG 坐标里原版窗 = **归一化原值** `[0.1818,0.8203]×[0.0017,0.9978]`。
        //      🔴 这一支**不加余量**：实测窗边没有白条（方图最宽的近白竖列只占 26% 行，那是画作亮部，
        //        不是卡框内沿）⇒ 多让就成了「比原版少采」。
        //  判别式 = **方图 ⇒ 整页**。当前 1168 张只有四种尺寸（1024²/512²/671×1024/670×1024），
        //  方图与非方图正好把两种来源分开；`CardBaseDemo` 有一条断言在扫这个前提
        //  （出现「非方图且宽既不≈670 也不≈671」时红 —— 那时换算没覆盖到，别静默按裁块处理）。
        //
        //  ⚠️ 返回值喂给 `Card3D.shader` 的 `_ArtUvRect`（xy = min, zw = max）。
        //     `_ArtUvRect` 的**属性默认值必须逐位等于本函数对「裁块」的返回值** ——
        //     没被 C# 写过的材质（老调用点 / 诊断）就吃那个默认；`AssertBoardArtNoWhiteEdge` ① 会对。
        //  ⛔ 别把这两支合成一组常量：裁块是「再往里缩」、整页是「从缩过头改回原版窗」（左右各放开 18%）。
        const float SpriteCropW = 670.5f;      // 原版 sprite 的 textureRect 宽（页 1024 上 x176.5 起）
        const float SpriteCropH = 1024f;       // 裁块是整高
        const float CropPadLeft = 11f, CropPadRight = 10f, CropPadTop = 5f, CropPadBottom = 5f;
        // 原版立绘窗（页面归一化坐标；= mesh UV1 正面四角）
        const float ArtWinU0 = 0.1818f, ArtWinU1 = 0.8203f;
        const float ArtWinV0 = 0.0017f, ArtWinV1 = 0.9978f;

        /// <summary>「sprite 裁块」那一支的 UV 窗 —— 也是 `Card3D.shader` 里 `_ArtUvRect` 的属性默认值。</summary>
        public static readonly Vector4 ArtUvRectSpriteCrop = new Vector4(
            CropPadLeft / SpriteCropW,                     // 0.016406
            CropPadBottom / SpriteCropH,                   // 0.004883
            (SpriteCropW - CropPadRight) / SpriteCropW,    // 0.985086
            (SpriteCropH - CropPadTop) / SpriteCropH);     // 0.995117

        /// <summary>「整页方图」那一支的 UV 窗（= 原版窗原样）。</summary>
        public static readonly Vector4 ArtUvRectWholePage = new Vector4(ArtWinU0, ArtWinV0, ArtWinU1, ArtWinV1);

        /// <summary>这张立绘贴图对应的 **`_CardImage` UV 窗**（xy = min, zw = max）。
        /// 判据与两种来源 → 本节文件头那段注释（唯一出处）。
        /// <para>`tex == null`（占位图 / 还没建体）⇒ 返回裁块那一支（= 与改前的行为同型）。</para></summary>
        public static Vector4 ArtUvRect(Texture2D tex)
        {
            if (tex == null) return ArtUvRectSpriteCrop;
            // 方图 = 原版 sprite 的 textureRect 就是整页（见文件头 ②）
            if (Mathf.Abs(tex.width - tex.height) <= 1) return ArtUvRectWholePage;
            return ArtUvRectSpriteCrop;
        }

        /// <summary>督军的**方形整身立绘**（`UI_Deck_Warlord_&lt;督军名&gt;`，`bundle_uiwarlords_assets_all`，
        /// **2048×2048 方形**、透明底抠图、构图居中；本机现读 63 张）。
        /// <para>🔴🔴 **2026-10-18 适用范围改判（铁律 5）—— 卡组格不用它**：
        /// **卡组格（`MenuDraw.DeckCell`）不用它** —— 用户 2026-10-18 明确纠正：卡格上要的是
        /// **卡牌里的插图**（卡面那张图），不是这张立绘；判据 + 参考图逐格模板匹配 →
        /// `Shell/MenuDraw.cs` 的 `DeckCell` 里「2026-10-18 改判」那一段。⛔ 别再把本函数接回卡格。
        /// 🔴 **2026-10-22 就地订正**：本行原文写「现在唯一的调用点是 `Shell/DeckInfoPopup.cs`」——
        /// **已过期**：同一天起 `Shell/LiveOpsEventWindow.cs`（排位/遭遇窗的 `Warlod Image`）与
        /// `Shell/PracticeModePopup.cs`（`Deck info ▸ Character Image`，905²）也都改走它了。</para>
        /// <para>⚠️ 旧的这条注释写着「原版 `deckHero.cardImageReference` 指向的就是这一族」—— **那句不成立**
        /// （同一天就地推翻了）：`RawCardScript.cardImageReference` 是 `AssetReferenceTyped&lt;Sprite&gt;`
        /// = **卡面那张 sprite**；这一族是 UI 立绘，两者不是同一样东西。
        /// 旧注释里「卡面插图（668×1024）拉进方框会裁掉大半」说的**其实是画框折算错了**（已经修）——
        /// 那个 347.7 方框对应的是**整张 1024² 纹理**，可见区正好是插图整幅，见 `MenuDraw.DcImgX` 的补注。</para>
        /// <para>🔴 **2026-10-22（界面线 M3）取名改成「按原版数据取」，不再按卡名拼** —— 旧实现拿
        /// **卡名**拼 `UI_Deck_Warlord_` + `name.Replace(' ','_')`，而原版资产名与卡名**并不总是同名**：
        /// `UM76 "Lieutenant Titus"` → 原版资产是 `UI_Deck_Warlord_Titus`（不是 `…_Lieutenant_Titus`）⇒
        /// 取不到、静默退回卡面插图（= 用户报的「战将立绘残缺」的真根因：905² 方框里塞 671×1024 的卡面图，
        /// 等比内接成 593×905）。取舍名的两个**第一手来源**：
        /// ① 卡 SO 的 `fullHeroSpriteReference` **原文**（`cards_engine.json`；56 张里 **36 张**这个字段
        ///    **直接就是资产名**，如 `UI_Deck_Warlord_Titus` / `UI_Deck_Warlord_Lord Exultant`）—— 直接用，不猜；
        /// ② 剩下 **20 张**那个字段里只有 **AssetReference 的 GUID**（`A2033` 记过旧 `guid_map.tsv` 解不出）——
        ///    本轮用 **Addressables 目录 → `uiwarlords_assets_all.bundle` → `AssetBundle.m_Container`
        ///    （键 = GUID）→ 资产 `m_Name`** 现解（65/65 个容器键全解）。
        /// 两条来源的落值**合成同一张表** <see cref="_deckArtById"/>（56 行，逐行标了是哪一来源）——
        /// ⛔ 不另存一份 JSON：`CardDef` 不带这个字段（`RuleEngine` 那边只保留在 DTO），
        /// 而「两份表 = 迟早不一致」（铁律 6）。
        /// ⛔ **不要再加「卡名归一化」的猜测规则**：`AM3 Commissar Denkler` 的原版资产叫 **Commissar Deckler**
        /// （原版拼写就对不上卡名），归一化永远配不上。</para>
        /// <para>⚠️ **两档可判别**：本函数只在**真拿到方形立绘**时返回 true；取不到时**出声**（⛔ 不静默）。
        /// <see cref="DeckHero"/> 保留旧签名 = true 就给方形图、false 退回 <see cref="Portrait"/>（卡面插图，
        /// **这一档是我们挑的兜底**，不是原版做法）。</para></summary>
        public static bool TryDeckHero(string warlordId, out Texture2D tex)
        {
            tex = null;
            if (string.IsNullOrEmpty(warlordId)) return false;
            string art;
            bool known = _deckArtById.TryGetValue(warlordId, out art) && !string.IsNullOrEmpty(art);
            if (known)
            {
                tex = MenuUi(art.Replace(' ', '_'));
                if (tex != null) return true;
            }
            // 取不到 —— **一律出声**（⛔ 不许静默兜底）：分清「表里没这一位」与「表里有、工程里没图」。
            Debug.LogWarning("[CardArt] 督军 " + warlordId + " 的**整身立绘**取不到："
                             + (known
                                 ? "原版资产 `" + art + "` 在工程里没有（期望 `Resources/Art/ui_menu/"
                                   + art.Replace(' ', '_') + ".png`）"
                                 : "`_deckArtById` 表里没有这一位（新督军？⛔ 不许按卡名猜资产名）")
                             + " ⇒ 调用方按「没有方形立绘」处理（`DeckHero` 会退回卡面插图 `Portrait`）。");
            return false;
        }

        /// <summary>旧签名（兼容）：拿到方形立绘就给，否则退回 <see cref="Portrait"/>（卡面插图）。
        /// ⚠️ 退回**不是静默的** —— <see cref="TryDeckHero"/> 已经打过 Warning；
        /// 新代码要「两档可判别」就直接用 `TryDeckHero`（例如按方形/竖构图决定要不要等比内接）。</summary>
        public static Texture2D DeckHero(string warlordId)
        {
            Texture2D t;
            if (TryDeckHero(warlordId, out t)) return t;
            return Portrait(warlordId);
        }

        /// <summary>**督军 id → 原版整身立绘资产名**（唯一一张表，56 位现池督军全覆盖）。
        /// <para>判据两来源，**逐条**标在行尾：`SO 原文` = 卡 SO 的 `fullHeroSpriteReference` 字段
        /// **直接就是资产名**（36 条，`cards_engine.json` 现读，第一手，⛔ 不是我们归一化出来的）；
        /// `GUID→容器` = 那个字段里只有 **AssetReference 的 GUID**（20 条；`A2033` 记过旧 `guid_map.tsv`
        /// 解不出）⇒ 本轮用 **Addressables 目录 → `uiwarlords_assets_all.bundle` → 该 bundle 的
        /// `AssetBundle.m_Container`（键 = GUID，65 个键 / 65 张图全解）→ 资产 `m_Name`** 现解。
        /// 判据（可复查）= 该 bundle 的 `AssetBundle.m_Container`（**键 = GUID** → `m_PathID`）与各资产 `m_Name`
        /// —— UnityPy 读 `d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/uiwarlords_assets_all.bundle`
        /// （同一条链的现成工具 = `工具/_guid_resolve_probe.py`，catalog → bundle → `m_Container` → `m_Name`）。
        /// **交叉核对**：解出的 65 个名字与 `assets_full/…_uiwarlords_assets_all/Sprite/` 那 **65 份 json 的
        /// `m_Name` 逐条相同**（两条独立提取路径，65/65 一致）。</para>
        /// <para>⚠️ **六条名字与卡名对不上**（⛔ 别「按卡名去头衔/归一化」猜，那样这几条永远配不上）：
        /// `AM3 Commissar Denkler→Commissar Deckler`（原版资产名的拼写就是 Deckler）·
        /// `BL36 Sylar Hexscorn→Sylar Hexcorn`（原版也是 Hexcorn 这个拼写）·
        /// `GSC3 Iconward Malak Vorenth→Acolyte Iconward` · `GSC2 Magus Uthrel Naas→Magus` ·
        /// `EC56 Lord Kaphrael→Lord Exultant` · `TL74 Terror of Vardenghast→Winged Tyranid Prime`
        /// （后四条是「立绘用的是另一套人设名」）。</para>
        /// <para>🔴 `DA75` 那条是 **`UI_Deck_Master Zacharial`** —— ⚠️ **没有 `_Warlord_` 前缀**，而且
        /// **工程里还没导入**（原版包里有：`…_uiwarlords_assets_all/Texture2D/UI_Deck_Master Zacharial.png`，
        /// 2048²）⇒ 今天它走 `DeckHero` 的 `Portrait` 兜底并出声；补图后放
        /// `Resources/Art/ui_menu/UI_Deck_Master_Zacharial.png` 即自动生效（本表不用改）。
        /// **`noArt` 就只有这一条**（其余 55 条工程里全有，逐条核过：`Resources/Art/ui_menu/` 现读 63 张，
        /// 含 7 张 `_AA_HB` 异画与 `UI_Deck_Warlord_AzraelAA`，那 8 张不进本表 —— 异画不是本局立绘）。</para></summary>
        static readonly Dictionary<string, string> _deckArtById = new Dictionary<string, string>
        {
            { "AM1",     "UI_Deck_Warlord_Leontus" },                 // GUID→容器
            { "AM2",     "UI_Deck_Warlord_Ursula Creed" },            // GUID→容器
            { "AM3",     "UI_Deck_Warlord_Commissar Deckler" },       // GUID→容器
            { "AM66",    "UI_Deck_Warlord_Hektor Thenmann" },         // GUID→容器
            { "ASH1",    "UI_Deck_Warlord_Anvirr Keltoc" },           // SO 原文
            { "ASH2",    "UI_Deck_Warlord_Eliac Zephyrblade" },       // SO 原文
            { "ASH3",    "UI_Deck_Warlord_Medreyal Ghaelyn" },        // SO 原文
            { "ASH74",   "UI_Deck_Warlord_Jain Zar" },                // GUID→容器
            { "ASH76",   "UI_Deck_Warlord_Lhykhis" },                 // GUID→容器
            { "BL34",    "UI_Deck_Warlord_Abaddon" },                 // SO 原文
            { "BL35",    "UI_Deck_Warlord_Haarken Worldclaimer" },    // SO 原文
            { "BL36",    "UI_Deck_Warlord_Sylar Hexcorn" },           // SO 原文
            { "BL65",    "UI_Deck_Warlord_Ghallaron" },               // SO 原文
            { "DA1",     "UI_Deck_Warlord_Azrael" },                  // GUID→容器
            { "DA2",     "UI_Deck_Warlord_Belial" },                  // GUID→容器
            { "DA3",     "UI_Deck_Warlord_Asmodai" },                 // GUID→容器
            { "DA75",    "UI_Deck_Master Zacharial" },                // GUID→容器 · ⚠️工程未导入
            { "EC55",    "UI_Deck_Warlord_Lucius" },                  // GUID→容器
            { "EC56",    "UI_Deck_Warlord_Lord Exultant" },           // SO 原文
            { "EC57",    "UI_Deck_Warlord_Daemon Prince" },           // SO 原文
            { "GOF27",   "UI_Deck_Warlord_Boss Zagstruk" },           // SO 原文
            { "GOF28",   "UI_Deck_Warlord_Ghazghkull Thraka" },       // SO 原文
            { "GOF29",   "UI_Deck_Warlord_Grukk Face-Rippa" },        // SO 原文
            { "GOF75",   "UI_Deck_Warlord_Gordrang" },                // SO 原文
            { "GOF76",   "UI_Deck_Warlord_Mozrog Skragbad" },         // SO 原文
            { "GOF77",   "UI_Deck_Warlord_Beastboss Morgrim" },       // SO 原文
            { "GSC1",    "UI_Deck_Warlord_Saffa Rhiannor" },          // GUID→容器
            { "GSC2",    "UI_Deck_Warlord_Magus" },                   // GUID→容器
            { "GSC3",    "UI_Deck_Warlord_Acolyte Iconward" },        // GUID→容器
            { "GSC66",   "UI_Deck_Warlord_Lhaska Szenari" },          // GUID→容器
            { "SAU35",   "UI_Deck_Warlord_Imotekh the Stormlord" },   // SO 原文
            { "SAU36",   "UI_Deck_Warlord_Nemesor Zahndrekh" },       // SO 原文
            { "SAU37",   "UI_Deck_Warlord_Orikan the Diviner" },      // SO 原文
            { "SAU66",   "UI_Deck_Warlord_Ramatekh" },                // SO 原文
            { "SOR1",    "UI_Deck_Warlord_Erika Luminas" },           // SO 原文
            { "SOR2",    "UI_Deck_Warlord_Junith Eruita" },           // SO 原文
            { "SOR3",    "UI_Deck_Warlord_Morvenn Vahl" },            // SO 原文
            { "SOR65",   "UI_Deck_Warlord_Celestian Sacresant Aveline" }, // GUID→容器
            { "SW1",     "UI_Deck_Warlord_Logan Grimnar" },           // GUID→容器
            { "SW2",     "UI_Deck_Warlord_Njal Stormcaller" },        // GUID→容器
            { "SW3",     "UI_Deck_Warlord_Ragnar Blackmane" },        // GUID→容器
            { "TAU1",    "UI_Deck_Warlord_Shadowsun" },               // SO 原文
            { "TAU2",    "UI_Deck_Warlord_AunVa" },                   // SO 原文
            { "TAU3",    "UI_Deck_Warlord_Commander OMaisos" },       // SO 原文
            { "TAU65",   "UI_Deck_Warlord_War Shaper" },              // SO 原文
            { "TL34",    "UI_Deck_Warlord_Tervigon" },                // SO 原文
            { "TL35",    "UI_Deck_Warlord_Swarmlord" },               // SO 原文
            { "TL36",    "UI_Deck_Warlord_Neurothrope" },             // SO 原文
            { "TL74",    "UI_Deck_Warlord_Winged Tyranid Prime" },    // SO 原文
            { "UM46",    "UI_Deck_Warlord_Marneus Calgar" },          // SO 原文
            { "UM47",    "UI_Deck_Warlord_Uriel Ventris" },           // SO 原文
            { "UM48",    "UI_Deck_Warlord_Varro Tigurius" },          // SO 原文
            { "UM65",    "UI_Deck_Warlord_Chaplain Letharius" },      // SO 原文
            { "UM76",    "UI_Deck_Warlord_Titus" },                   // SO 原文
            { "UM87",    "UI_Deck_Warlord_Guilliman" },               // GUID→容器
            { "UM88",    "UI_Deck_Warlord_Valius Paxor" },            // SO 原文
        };

        /// <summary>按**卡名**取立绘 —— **只给「手上只有卡名」的那一处用**（`BattleLogPanel` 的
        /// 小头像：战斗事件里带的是卡名，见 `BattleEvent.CardId`）。
        /// ⚠️ **同名卡只能取到第一张**（`Terminator` 有两张）—— 头像这么小，认了；
        ///    新代码**不要**用这个，用 `BattleDriver.ArtKey`。</summary>
        public static Texture2D PortraitByName(string cardName)
        {
            if (string.IsNullOrEmpty(cardName)) return null;
            if (_byName == null)
            {
                _byName = new System.Collections.Generic.Dictionary<string, string>();
                foreach (var c in RuleEngine.CardDatabase.Load())
                    if (!string.IsNullOrEmpty(c.Name) && !_byName.ContainsKey(c.Name))
                        _byName[c.Name] = c.Id;
            }
            string id;
            return _byName.TryGetValue(cardName, out id) ? Portrait(id) : Portrait(cardName);
        }
        static System.Collections.Generic.Dictionary<string, string> _byName;

        /// <summary>
        /// 这张卡的立绘**有没有角色抠图**（alpha 通道）。
        /// 有的话：卡面要画**两层** —— 完整插图垫在卡框下、角色抠图盖在卡框上（角色因此"越出卡框"）。
        /// 清单由 `工具/import_original_art.py` 生成（`Resources/Art/cards/card_cutouts.json`；**张数看该文件的 `count`，别写死**）。
        /// ⚠️ 2026-10-06 更正：原写本行「665 张」，实际 **668 张**（张数会随美术增删变）。
        /// ⚠️ 单位卡基本都有、战术卡基本都没有；**但判据是清单不是卡型** ——
        ///    没有立绘（回退程序生成的占位图）的卡不在清单里，绝不能给它加前景层（那会把卡框整个盖住）。
        /// </summary>
        public static bool HasCutout(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return false;
            if (_cutouts == null)
            {
                _cutouts = new HashSet<string>();
                var ta = Resources.Load<TextAsset>("Art/cards/card_cutouts");
                if (ta != null)
                {
                    // 手写解析：只有 { "cards": ["a","b",…] } 一层，不值得为它拉一个 JSON 依赖
                    // ⚠️ `Matches` 返回**非泛型** `MatchCollection` —— 写 `var m` 会被推成 object（CS1061），
                    //    必须显式写 `Match`
                    foreach (System.Text.RegularExpressions.Match m
                             in System.Text.RegularExpressions.Regex.Matches(ta.text, "\"([a-z0-9_]+)\"\\s*[,]"))
                        _cutouts.Add(m.Groups[1].Value);
                }
            }
            return _cutouts.Contains(Slug(cardId));
        }
        static HashSet<string> _cutouts;

        /// <summary>"Ember Archer" → "ember_archer"</summary>
        public static string Slug(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s.ToLowerInvariant())
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }

        /// <summary>原版战斗 UI 图，按切片名取（不带扩展名）</summary>
        public static Texture2D Ui(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Get(Root + "ui/" + name);
        }

        /// <summary>卡片详情窗下缘那条**风味文字底图**（原版 `LowerSection/FlavourTextBG`，1320×178 的横幅）。
        ///
        /// 🔴 **2026-09-28 导入**（13 张，`Resources/Art/ui/flavourbg_&lt;faction 小写>.png`）。
        /// 原版按**阵营**选图：`FlavourTextSO.GetClanFlavorBackground(CardArmy)`
        /// → `bundle_duplicateassetisolationso_assets_all/MonoBehaviour/Flavour Text Backgrounds.json`
        /// 那张 army→资产 表（**14 条记录 / 13 个唯一 GUID / 13 张图**；`defaultBackground` 与
        /// army=0(Neutral)/10(Ultramarines) 是同一张）。
        /// ⚠️ **枚举名 → PNG 文件名有 5 处不同名**（Goff→`Orks` · SaimHann→`Aeldari` · Sautekh→`Necron` ·
        ///    Leviathan→`Tyranid` · TauEmpire→`Tau`）⇒ 工程里**按 faction key 存**，避开那个坑；
        ///    我们的 faction key 与原版 `CardArmy` 枚举名**逐字符恒等**（13/13）。
        /// ⚠️ 原版节点 `m_PreserveAspect = 0`（实读）⇒ **拉伸**铺满那个矩形，**别开 keepAspect**。
        /// ⚠️ 图本身 1024×128、**上下自带透明边**（不透明区 y 20.3%~84.4%）—— 那是原图的留白，照用。</summary>
        public static Texture2D FlavorBg(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            return Ui("flavourbg_" + faction.ToLowerInvariant());
        }

        /// <summary>
        /// 一张 **1×1 的白贴图** —— 纯色板用（配 `ImageQuad.SetTint` / `SetAspect`）。
        /// 原版有些「底板」在场景里就是一个 **Image 组件**（没有 sprite、只有 `m_Color`），
        /// 比如 `CemeteryLogPanel/BG`（色 (0,0.08,0.01)）—— 我们这边用这张白图 + tint 等价。
        /// 每次返回同一份（生成的，不进 `Resources`）。
        /// </summary>
        public static Texture2D Solid()
        {
            if (_solid != null) return _solid;
            _solid = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _solid.SetPixel(0, 0, Color.white);
            _solid.Apply();
            _solid.name = "Solid";
            return _solid;
        }
        static Texture2D _solid;

        /// <summary>
        /// **双色线性渐变**贴图（配 `ImageQuad.SetTint` 之外的用途：直接当贴图铺）。
        ///
        /// 原版主菜单那层背景就是这么来的 —— `MainMenu(内)/Background`（RT1089）是
        /// `Image`(**sprite=0**，即没有图) + 一个**双色渐变组件** `MB1931`：
        /// `m_color1=(0.224,0.012,0.020)` · `m_color2=(0.047,0,0.016)` · `m_angle=82`
        /// （出处 `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1931.json`
        ///  + `资料/主菜单_原版规格.md` §三① 第 4 条）。
        /// ⇒ **不是一张图、也不是 3D**，是**纯代码渐变** —— 所以这里按同样方式生成，不引入新资产。
        ///
        /// `angleDeg` = **调用点给的角度**（本仓约定：原版 `m_angle` 经 `90 − θ + 180` 换算后的值，
        /// 例：原版 82 ⇒ 传 188。判据 → `Shell/MainMenuSubmenuWindow` 那段注释）。
        /// ⚠️ 分辨率取 128×128 够用（渐变是线性的，插值不会失真）；生成一次就缓存。
        ///
        /// 🔴 **2026-10-25（第 3 轮 · 补 `CompensateAspectRatio`）**：原版投影前会按矩形宽高比补偿方向
        /// （`UIGradientUtils.CompensateAspectRatio.c` = `normalize(dir.y, dir.x·h/w)`）⇒ 非方框里的
        /// **有效轴是 `(sinθ, cosθ·h/w)`**。我们原来没有这一档：16:9、θ=82 家族（调用点传 188）的实际轴向
        /// 是 `atan2(|sin188|,|cos188|)` = **8.02°**，按原版应为 `atan2(cos82·9/16, sin82)` = **4.52°**。
        /// 现在把补偿加在**生成轴上**：`dy *= aspectHoverW`（`aspectHoverW = 目标矩形 高/宽`）。
        /// ⚠️ **只在生成时用**（贴图仍是正方 128×128、由 quad 拉伸）—— ⛔ 不在运行期读 `RectTransform`
        /// （批处理/自检里拿不到可靠值），所以宽高比**由调用点传**。
        /// <param name="aspectHoverW">目标矩形的 **高/宽**（`h/w`）。**1 = 不补偿**（正方形；缺省值 = 老行为）。</param>
        public static Texture2D Gradient(Color c1, Color c2, float angleDeg, float aspectHoverW = 1f)
        {
            if (!(aspectHoverW > 0.0001f)) aspectHoverW = 1f;
            string key = $"grad_{ColorUtility.ToHtmlStringRGBA(c1)}_{ColorUtility.ToHtmlStringRGBA(c2)}_{angleDeg:F1}_{aspectHoverW:F3}";
            if (_gradients.TryGetValue(key, out var cached) && cached != null) return cached;

            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.name = key;
            float rad = angleDeg * Mathf.Deg2Rad;
            // 🔴 `dy` 乘 `h/w` = 原版 `CompensateAspectRatio` 的等价物（判据见上面那段 doc）。
            float dx = Mathf.Cos(rad), dy = Mathf.Sin(rad) * aspectHoverW; float _glen = Mathf.Sqrt(dx * dx + dy * dy); if (_glen > 1e-5f) { dx /= _glen; dy /= _glen; }   // 🆕 B90-N4：单位化方向（判据 = `UIGradient__ModifyMesh.c:107-113` 先把 dir 除成单位长，再投影）
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    // 把像素投到方向轴上（方向已单位化 ⇒ 斜向不再被压缩 1/1.41），超出 [0,1] 一律夹住（= 原版 clamp01）
                    float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N - 0.5f;
                    float t = Mathf.Clamp01(u * dx + v * dy + 0.5f);   // 🆕 B90-N4：**不做满量程重映射**（判据 = `UIGradient__ModifyMesh.c:150-158`：`t = pos·(dir/w,dir/h) + 平移` → 直接 `clamp01`；原来除以 `|dx|+|dy|` 是我们自加的拉伸 ⇒ 斜向渐变带宽与原版差 1–1.41 倍）
                    px[y * N + x] = Color.Lerp(c1, c2, t);
                }
            tex.SetPixels(px);
            tex.Apply();
            _gradients[key] = tex;
            return tex;
        }
        static readonly Dictionary<string, Texture2D> _gradients = new Dictionary<string, Texture2D>();

        /// <summary>
        /// **带独立 alpha 键位**的双色渐变 —— 即原版 `Gradient2` 组件（`_gradientType=0` Horizontal ·
        /// `_blendMode=2` Multiply · `_modifyVertices=1`）的等效实现。
        ///
        /// 🔴 **为什么不能用上面那个 `Gradient`**：Unity 的 `Gradient` **颜色键与 alpha 键各有一套时间**。
        /// 实测四张任务卡的 `header`（`bundle_menus_assets_all`）：
        /// · 登录/骷髅/每日标题：颜色 (0.247,0.188,0.380)→(0.475,0.306,0.153)，**alpha 键在 0.2706 与 0.9059**
        ///   （也就是左起 27% 之前**全不透明**、到 91% 才降到 0.098）；
        /// · 周常：颜色恒为 (0.227,0.286,0.325)，**5 个 alpha 键** (1.0, 0.773, 0.498, 0.463, 0.098)。
        /// 用「RGBA 一起线性插值」近似会把左侧那片本该**实心**的区域画成半透明 —— 一眼能看出来。
        /// </summary>
        /// <param name="at">alpha 键位（0..1，升序，至少 2 个）。</param>
        /// <param name="aa">对应的 alpha 值。区间外**夹到首尾键**（Unity `Gradient` 就是这么做的）。</param>
        /// <param name="aspectHoverW">目标矩形的 **高/宽**（`h/w`），与 <see cref="Gradient"/> 的第 4 参**同一档**
        /// 宽高比补偿（原版 `UIGradientUtils.CompensateAspectRatio`）；**1 = 不补偿**。
        /// ⚠️ 现有三个调用点（`MissionsTab.HeaderGradient` ×2 · `ExpansionPassEventWindow.MissionHeaderGradient` ×2）
        /// 的角度都是 **0°（Horizontal）** ⇒ `dy = sin0 = 0`，乘任何比值都是 0 ⇒ **补偿是恒等变换**；
        /// 参数仍然接上，免得将来把某条改成非 0 角度时静默漏掉这一档（同族不一致比没做更难查）。</param>
        public static Texture2D GradientKeys(Color c1, Color c2, float angleDeg, float[] at, float[] aa,
                                             float aspectHoverW = 1f)
        {
            if (!(aspectHoverW > 0.0001f)) aspectHoverW = 1f;
            var key = new System.Text.StringBuilder("gradk_");
            key.Append(ColorUtility.ToHtmlStringRGBA(c1)).Append('_').Append(ColorUtility.ToHtmlStringRGBA(c2))
               .Append('_').Append(angleDeg.ToString("F1"))
               .Append("_a").Append(aspectHoverW.ToString("F3"));
            for (int i = 0; i < at.Length; i++) key.Append('_').Append(at[i].ToString("F4")).Append(':').Append(aa[i].ToString("F4"));
            string k = key.ToString();
            if (_gradients.TryGetValue(k, out var cached) && cached != null) return cached;

            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.name = k;
            float rad = angleDeg * Mathf.Deg2Rad;
            // 🔴 `dy` 乘 `h/w` = 原版 `CompensateAspectRatio` 的等价物（判据见 <see cref="Gradient"/> 的 doc）
            float dx = Mathf.Cos(rad), dy = Mathf.Sin(rad) * aspectHoverW; float _glen = Mathf.Sqrt(dx * dx + dy * dy); if (_glen > 1e-5f) { dx /= _glen; dy /= _glen; }   // 🆕 B90-N4：单位化方向（判据 = `UIGradient__ModifyMesh.c:107-113` 先把 dir 除成单位长，再投影）
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N - 0.5f;
                    float t = Mathf.Clamp01(u * dx + v * dy + 0.5f);   // 🆕 B90-N4：**不做满量程重映射**（判据 = `UIGradient__ModifyMesh.c:150-158`：`t = pos·(dir/w,dir/h) + 平移` → 直接 `clamp01`；原来除以 `|dx|+|dy|` 是我们自加的拉伸 ⇒ 斜向渐变带宽与原版差 1–1.41 倍）
                    var c = Color.Lerp(c1, c2, t);
                    c.a = EvalAlphaKeys(t, at, aa);
                    px[y * N + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            _gradients[k] = tex;
            return tex;
        }

        /// <summary>按键位求 alpha：区间内线性插值，区间外夹到首/尾键。</summary>
        static float EvalAlphaKeys(float t, float[] at, float[] aa)
        {
            if (at == null || aa == null || at.Length == 0) return 1f;
            if (t <= at[0]) return aa[0];
            for (int i = 1; i < at.Length; i++)
                if (t <= at[i])
                {
                    float span = at[i] - at[i - 1];
                    if (span <= 1e-6f) return aa[i];
                    return Mathf.Lerp(aa[i - 1], aa[i], (t - at[i - 1]) / span);
                }
            return aa[aa.Length - 1];
        }

        /// <summary>
        /// 卡组编辑/收藏界面的 UI 图（`Art/ui_deck/`）。
        /// 和 <see cref="Ui"/> 分开是因为两批图来自**不同的图集**：
        /// 战斗那批切自 `BattleAtlasUI`，这批切自 `0_MainMenu` + 去重资源 + 卡组选择按钮。
        /// 取法一致（都是切片名，空格已换成下划线），只是目录不同。
        /// 切片脚本：`工具/slice_ui_atlas.py`。
        /// </summary>
        public static Texture2D DeckUi(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Get(Root + "ui_deck/" + name);
        }

        /// <summary>
        /// 菜单（阶段二「游戏外壳」）的 UI 图（`Art/ui_menu/`）。
        /// 和前两批分开同样是**图集不同**：这批切自 `0_MainMenu` / 去重资源 / `boosterpacks` / `cosmeticavatarsimages` 等。
        /// 命名约定 = **切片名里的空格换成下划线**（`UI_Main_Upper bar` → `UI_Main_Upper_bar`）。
        /// 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`；逐张出处见 `资料/主菜单_原版规格.md` §二。
        /// </summary>
        public static Texture2D MenuUi(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            // 菜单要用的图**分成三批**导进来的，所以按「新 → 旧 → 战斗那批」兜底：
            //   `ui_menu/` —— 2026-09-22 专为菜单导的这批
            //   `ui_deck/` —— 0917 那批（那会儿还没有单独目录，卡组编辑的图也在里面）
            //   `ui/`      —— 战斗 HUD 那批；**有些件是两边共用的**
            //                （`UI_Settings_Icon` / `Player_Profile_Border` / `White_Square` / `40k_main_bt_nametag`
            //                  战斗里也在画 —— 见 `BattleDriver` 的 `HudImage(...)`）
            // 这是**批次与共用**的差异，不是取法不同 —— 真要改的是「把共用的图归一到一个目录」，那是另一件事。
            return Get(Root + "ui_menu/" + name) ?? Get(Root + "ui_deck/" + name) ?? Get(Root + "ui/" + name);
        }

        /// <summary>
        /// 🆕 2026-09-27：**装饰品头像**的立绘（`Art/avatars/`，玩家档案窗的 Avatar 页要列的那一批）。
        /// 🔴 **传的必须是 `Avatar_&lt;阵营>_&lt;单位名>` 原样**（**含空格，别换成下划线**）——
        /// 清单 `Resources/profile_cosmetics.json` 里的 `art` 字段就是它（= SO 的 `m_Name`），
        /// 而导入器那边**也是原样落盘**的（与 `MenuUi` 那批「空格换下划线」的规矩**不同**）。
        /// 导入器：`工具/import_original_art.py` 的 `COSMETIC_AVATAR_SRC`；清单生成器：`工具/gen_profile_cosmetics.py`。
        /// </summary>
        public static Texture2D Cosmetics(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Get(Root + "avatars/" + name);
        }

        /// <summary>
        /// **关键词（trait）图标**，按英文关键词取（`Trait("ephemeral")`）。
        ///
        /// 来源：原版图集 `40ktraiticonatlas`（78 个切片，80×80 RGBA），
        /// **切片文件名就是英文关键词** —— 这是「哪个图标对应哪个词」最硬的证据。
        /// 导入：`工具/import_original_art.py` 的 `TRAIT_SRC` → `Resources/Art/traits/`。
        /// 对照表：`资料/关键词图标/关键词与图标_对照表.md`（有证据的对照 vs 推测，分了两节）。
        ///
        /// **为什么要它**：临时卡（Ephemeral）的卡面标记 —— 规则书 `:183`/`:229` 说
        /// 临时卡「回合结束若在手牌则移除」，玩家得**看得出哪张是临时的**。
        /// 原版那套是 `BattleCardUI.ShowEphemeral()` + `Card2DController.ToggleGlitch()`（换 glitch 材质）。
        /// 🔴 **2026-10-25（R16）就地订正（铁律 5）**：本行原来写「**glitch 素材本地没有**（`d:/2` 全盘 `*glitch*` 零命中）」
        /// —— **不成立**，那句只证明「**按名字**搜不到」。实测三件都在本地：
        /// ① 材质 `Card Frame Glitch` = `assets_full/bundle_duplicateassetisolation_assets_all/Material/Material_-2877403367212247079.json`
        ///   （`m_Name = "Card Frame Glitch"`，参数已逐条读出）；② shader = **`Everguild/FX/Sprite Scan Lines`**
        ///   （`bundle_shaders_assets_all/Shader/Shader_-5025784757600742591.json`；属性表与
        ///   `WarpforgeVFX/Runtime/OriginalShaderBlendTable.cs:369` 那一行逐字对上）；
        /// ③ 噪声图 `Noise Combined.png`（同一 bundle 的 `Texture2D/`）。
        /// **错因**：原版是经 **PPtr** 引用的 —— 材质名之外没有 "glitch" 线索、shader 名里一个 "glitch" 都没有。
        /// ⇒ 现在的结论是「**要做，只差一个自建 shader**」（铁律 11：⛔ 不许按成本判「不做」），**不是**「原版没有」。
        /// 本轮**仍然先用**这张关键词图标顶上（比自绘接近原版、零风险），✅ **2026-10-25（E7 批次 3）glitch 材质已接线**（`CardView.SetGlitch` + 自建 `Resources/Shaders/CardFrameGlitch.shader`，算式照抄原版 DXBC）—— 角标**不撤**，原版是两件事。待办与判据全文 → `资料/完全复刻/报告/R16_卡牌面遗留C_第2轮.md` §没做·ToggleGlitch。
        ///
        /// ⚠️ 取不到时返回 `null` —— 调用方要判（卡面标记整块不画，而不是画个错的）。
        /// </summary>
        public static Texture2D Trait(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return null;
            return Get(Root + "traits/" + keyword);
        }

        /// <summary>
        /// **卡背**（`Art/cardbacks/`，**233 张**）—— 卡组线「Cosmetics 页」用。
        ///
        /// 来源：`bundle_cosmeticscardbacksimages_assets_all` 的 233 张 1024² 图集，
        /// **逐张按自己 `_Main` 精灵的 `textureRect` 裁**出来的（⚠️ **每张 rect 都不一样**，
        /// 实测三张分别 707×995.9 / 707×1016.9 / 707×966.9 —— 别照抄一个值当全部）。
        /// 导入器：`工具/import_original_art.py` 的 `CARDBACK_SRC`/`write_cardback`（**2026-09-23 新加的那段** ——
        /// 在此之前「导 233 张卡背」只是一句写在正本里的空头支票，脚本里根本没有这条路）。
        ///
        /// ⚠️ 文件名的**空格与撇号都原样保留**（`Cardback_AM_Cold Blood` / `…_C’tan` 里是 U+2019），
        /// 只有扩展名去掉 —— 传名字时**别自己 slug 化**。
        /// ⚠️ 方法名是 `Cosmetic` **不是 `CardBack`** —— 后者已被「牌堆用的阵营默认背」（`CardBack(阵营)`，线 175）占了。
        /// </summary>
        public static Texture2D Cosmetic(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            return Get(Root + "cardbacks/" + fileName);
        }

        /// <summary>🆕 2026-09-26：同一张卡背的 **SDF 掩码**（`Art/cardbacks/&lt;名>_sdf`）。
        ///
        /// 原版 `CosmeticItemCardback.GetCardBackSprites()` **成对返回（主卡背, SDF）**
        /// —— 喂的就是**同一张卡背自己的 `_SDF`**。实测（UnityPy 直读 `Sprite/&lt;名>_SDF.json` 的 `textureRect`）：
        /// **100×130.5**，和 `_Main`（707×996）**宽高比都不一样**（0.7663 vs 0.7099）
        /// ⇒ **不是 `_Main` 的缩放版**，两个 sprite 挤在同一张 1024² 图集里。
        /// ⚠️ 低分辨率是距离场的本意，**不是缩略图**。
        /// 导入器：`工具/import_original_art.py --only-cardback-sdf`。
        /// **用在两处**：收藏窗卡背格（`CosmeticItemCardback` 主 `+0x48` / SDF `+0x50`）与
        /// 战斗牌堆（`DeckManager` 主 `+0x50` / SDF `+0x58`，字段名就叫 `cardbackShadow`）。</summary>
        public static Texture2D CosmeticSdf(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            return Get(Root + "cardbacks/" + fileName + "_sdf");
        }

        /// <summary>卡背 SDF 那层用的噪声（原版材质 `Card Backs SDF` 的 `_Noise` → 贴图 `Noise Combined`）。</summary>
        public static Texture2D CardbackSdfNoise() { return Get(Root + "card_sdf/cardback_noise"); }

        /// <summary>全部卡背的**名字**（字典序，顺序稳定）—— Cosmetics 页铺格用。
        /// 第一次调会 `Resources.LoadAll` 一次，之后走缓存。
        /// 🔴 **必须滤掉 `&lt;名>_sdf`** —— 那些是**同一张卡背的 SDF 掩码**（2026-09-26 起放进同一个目录），
        ///    不滤的话卡背格会**从 233 变成 466**（而且多出来的每一格都画成一张灰掩码）。
        ///    ⚠️ 这是 `LoadAll` 扫目录的固有风险：**往这个目录加任何新图都要回来看这里**。</summary>
        public static string[] CosmeticNames()
        {
            if (_cardBackNames == null)
            {
                var all = Resources.LoadAll<Texture2D>(Root + "cardbacks");
                var names = new List<string>();
                foreach (var t in all)
                    if (t != null && !t.name.EndsWith("_sdf")) names.Add(t.name);
                names.Sort(System.StringComparer.Ordinal);
                _cardBackNames = names.ToArray();
            }
            return _cardBackNames;
        }
        static string[] _cardBackNames;

        /// <summary>
        /// 一副卡组**实际用哪张卡背**：选过就用选的那张，没选过（空/null）就退回**该阵营的默认卡背**。
        /// 🔴 **判据只有这一处**（两处写同一条规则 = 迟早不一致）——
        /// 原版对应 `CardDeck.GetDeckCardback()`：`cardbackId` 空 ⇒ `ArmyUtilities.GetDefaultCardback(deckArmy)`。
        /// 出处与「整副一个卡背」的判据写在 `PlayerDeck.CardbackId` 上。
        /// ⚠️ 选了但图取不到（存档跨版本/手改过）⇒ **退回默认并出声**，不静默画空白。
        /// </summary>
        public static Texture2D DeckCardback(string cardbackId, string faction)
        {
            if (!string.IsNullOrEmpty(cardbackId))
            {
                var t = Cosmetic(cardbackId);
                if (t != null) return t;
                Debug.LogWarning("[CardArt] 卡组选的卡背取不到：" + cardbackId + " → 退回阵营默认卡背");
            }
            return CardBack(faction);
        }

        // ============================================================ 督军**异画**（Alternate Art）
        //
        // 出处：`工具/import_original_art.py` 的 `ALT_ART`（**本机只有 7 张**）。
        // 🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1808`/`A1830`）**：原文写「**两种风格 `AA_HB` / `v2`**」——
        //    **不成立**：那两串是**图片文件名的两种写法**，不是两种风格；原版 `AlternateArtSyle` 只有
        //    `Default`/`HammerAndBolter` 两档，而**轮播只列非 `Default`** ⇒ **本地 7 张全是 `HammerAndBolter` 一档**
        //    （判据 → `Shell/CollectionWindow.cs` 的 `StyleLogoName` 头注）。
        // 🔴 **绑定规则**：一件异画**绑死在一张卡上**，不能给别的督军用
        //    （`AlternateArtCard.GetIdForClonedCard()` 对原卡资产 id 做一次 `String.Replace` 得到克隆 id，
        //     `AlternateArtInventory.OnFinishUnpack` 再 `AssetLocator.GetAsset(该 id)` ⇒ 一一对应）。
        //    ⇒ **「风格 × 卡」是一对多**：选一个风格，只有在该风格下画了异画的那几张卡会换。

        /// <summary>某张卡的异画立绘（按**卡 id** 取，文件 = `Art/altarts/alt_&lt;id 小写>.png`）。没有给 null。</summary>
        public static Texture2D AltArt(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            return Get(Root + "altarts/alt_" + Slug(cardId));
        }

        /// <summary>这张异画**有没有角色抠图 alpha** —— 卡面要据此决定画不画「越出卡框」的前景层。
        /// 清单由 `--only-altart` 那趟单独生成（`Art/altarts/alt_cutouts.json`），
        /// **不能并进 `card_cutouts.json`**（那是全量产物，半量跑会把它清空）。</summary>
        public static bool AltArtHasCutout(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return false;
            if (_altCutouts == null)
            {
                _altCutouts = new HashSet<string>();
                var ta = Resources.Load<TextAsset>("Art/altarts/alt_cutouts");
                if (ta != null)
                    foreach (System.Text.RegularExpressions.Match m
                             in System.Text.RegularExpressions.Regex.Matches(ta.text, "\"([a-z0-9_]+)\"\\s*[,]"))
                        _altCutouts.Add(m.Groups[1].Value);
            }
            return _altCutouts.Contains("alt_" + Slug(cardId));
        }
        static HashSet<string> _altCutouts;

        static Texture2D Get(string path)
        {
            Texture2D t;
            if (_cache.TryGetValue(path, out t)) return t;
            t = Resources.Load<Texture2D>(path);
            _cache[path] = t;                 // null 也缓存：找不到就别每次都去 IO
            if (t != null) _pathOf[t] = path; // 反查表（类尾 `Release` / `PathOf` 用）
            return t;
        }

        /// <summary>诊断用：装了什么</summary>
        public static string Describe()
        {
            Load();
            int n = 0;
            foreach (var kv in _cache) if (kv.Value != null) n++;
            return Available
                 ? $"原版美术：背景 {(Backdrop != null ? Backdrop.width + "×" + Backdrop.height : "没烘")}，"
                 + $"已加载 {n} 张"
                 : "没有原版美术（Resources/Art 空）—— 用程序生成的占位卡面";
        }

        // ============================================================ 图缓存的「释放」= 原版 `AssetReference.Release` 的等价物
        //
        // 判据（🥇 `d:/2/tools/decomp_full/`；三份方法体**第一行的函数名都与文件名一致** ⇒ 避开「文件名与内容不符」
        // 那 16.5% 的坑）：
        //  · `CosmeticItem__UnloadReferences.c:2-16`：`imageReference`（+0x40，类型 `AssetReferenceTyped<Sprite>`，🥈 `CosmeticItem.cs:10`）非空
        //    ⇒ `AssetReferenceTyped<object>.Release(cacheGroup: General(1), releaseAll: false)`（`.c:9-13` 的实参 `(handle,1,0,…)`；
        //    签名 🥈 `dump.cs:135633`；`AddressableCacheGroup.General = 1` 🥈 `dump.cs:45274-45281`）—— **一次只放这一条、不是全清**；
        //    为空 ⇒ `FUN_1803f47a0()`（`.c:14-15`，`/* Subroutine does not return */` —— 全库 11,020 份 `.c` 共用的
        //    **不返回的抛出出口** ⇒ 那一档是「抛异常」，不是「跳过」）。
        //  · `CursorByArmySO__ReleaseArmyCursor.c:2-39`：遍历表（长度 `+0x18`、元素基址 `+0x20`），命中
        //    `+0x10 == cardArmy` 的那一项 ⇒ `+0x18`(cursor) 与 `+0x20`(highlight) **两个** AssetReference 各 Release 一次（`.c:28-32`）。
        //  · `FlavourTextSO__ReleaseFlavorBackground.c:2-57`：遍历表找 `+0x10 == army` 的项，命中 ⇒ `+0x18`(flavorBg)
        //    Release（`.c:50-51`），随后**无条件** `CustomDebug.LogWarning`（`.c:28-32`，在循环外 —— 原版自己的日志噪声，
        //    不是行为，不照抄）。
        // ⇒ **原版释放的是 Addressables 句柄 = 引用计数减一，不是「立刻销毁贴图」**：`Release` 之后只要还有使用者，
        //   资产照样在；最后一个句柄放掉才卸载。
        //   我们全工程没有 Addressables：图一律 `Resources.Load` 进 `_cache`（本文件 `Get`），**那份字典就是我们的句柄表**。
        //   等价语义 = **把这一份强引用从表里放掉**；贴图本身**不销毁**（⛔ 全仓 `Resources.UnloadAsset` 零处；
        //   `UnloadUnusedAssets` 只出现在三份 Editor 探针里）⇒ 仍被 `ImageQuad` / 材质引用的图**照常显示**
        //   —— 这正是「不能释放正在用的图」的结构保证，与「Release 只减计数」同形。
        //   调用口 = `Shell/MenuDraw.cs` 的 `ReleaseArtUnder`（`ClearChildren` 在销毁每个子树前调它）：
        //   原版是按「装饰品 / 阵营 / 风味底图」的对象放句柄，我们是按**即将销毁的子树**放 —— 同一档时机
        //   （原版靠 `ItemDrawer.OnDestroy` / 各 `OnDisable`，我们靠节点销毁；先例 `Shell/ItemDrawerLeash.cs`）。

        /// <summary>`_cache` 条数（含负缓存）—— 释放的**可观察量**：`Release` 前后相减 = 放掉几条。</summary>
        public static int CacheCount { get { return _cache.Count; } }

        /// <summary>里面**真有图**的条数（负缓存不计）—— 与 <see cref="CacheCount"/> 一起读，
        /// 才分得清「放掉一张图」和「放掉一条空记录」。</summary>
        public static int LiveCacheCount
        {
            get { int n = 0; foreach (var kv in _cache) if (kv.Value != null) n++; return n; }
        }

        /// <summary>`Release` 成功放掉的累计次数（自检读口）。</summary>
        public static int ReleasedCount { get { return _released; } }

        /// <summary>贴图 → 资源路径（反查）。不是经 <see cref="Get"/> 来的（`Solid()` / 渐变 / `RenderTexture`）⇒ null。</summary>
        internal static string PathOf(Texture2D t)
        {
            string p;
            return t != null && _pathOf.TryGetValue(t, out p) ? p : null;
        }

        /// <summary>
        /// 「逐项美术」族 —— 只有这些在子树销毁时放（原版那三个方法放的也正是族内对象）。
        /// 共享件（卡框 `cards/frame_*` · SDF · `traits/` · 战斗 `ui/` 的 HUD 图 · `card3d/`）**不进这一族**：
        /// 原版对应的是 `CardFramesSO` 那种常驻 SO 的引用，三个释放方法一个都没碰它们。
        /// </summary>
        public static bool IsPerItemArt(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            for (int i = 0; i < PerItemArtPrefix.Length; i++)
                if (path.StartsWith(PerItemArtPrefix[i], System.StringComparison.Ordinal)) return true;
            return false;
        }
        static readonly string[] PerItemArtPrefix =
        {
            Root + "cards/art_",      // 卡面立绘（原版 `RawCardScript.cardSprite`）
            Root + "cardbacks/",      // 装饰品·卡背（= `CosmeticItem.UnloadReferences` 的对象）
            Root + "avatars/",        // 装饰品·头像
            Root + "altarts/",        // 异画立绘
            Root + "ui/flavourbg_",   // 风味底图（= `FlavourTextSO.ReleaseFlavorBackground` 的对象）
            Root + "ui_menu/Cursor_", // 阵营光标两图（= `CursorByArmySO.ReleaseArmyCursor` 的对象；本机只导了 SM 一族）
        };

        /// <summary>
        /// 原版 `AssetReferenceTyped&lt;object&gt;.Release(handle, …)` 的等价物：**把这一条从 `_cache` 里放掉**。
        /// ⛔ **不销毁贴图**（见本节头注）；空路径 / 有租约 / 不是缓存条目 ⇒ 拒绝并**出声**（「不许静默失败」）。
        /// ⚠️ 负缓存条目（value == null）不放：那不是句柄，`Get` 故意缓存它免得每次重试 IO。
        /// </summary>
        public static bool Release(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // 原版这一档是「不返回的抛出出口」（`CosmeticItem__UnloadReferences.c:14-15`）。我们不出炸界面的异常，
                // 改成出声 + false —— 差异如实记在账里。
                Debug.LogWarning("[CardArt] `Release` 收到空路径 ⇒ 不释放。原版同档走抛出出口"
                    + "（`CosmeticItem__UnloadReferences.c:14-15`），我们在 UI 路径上不出异常。");
                return false;
            }
            if (IsRetained(path))
            {
                Debug.LogWarning("[CardArt] `Release` 被租约挡下：「" + path + "」还有 Retain 没解（正在显示）⇒ 照旧显示，不释放");
                return false;
            }
            Texture2D t;
            if (!_cache.TryGetValue(path, out t))
            {
                Debug.LogWarning("[CardArt] `Release` 收到一条没缓存过的路径：「" + path + "」⇒ 什么都不做"
                    + "（原版 `ReleaseArmyCursor` 表里找不到那一项也是直接往下走）");
                return false;
            }
            if (t == null) return false;        // 负缓存：无句柄可放
            _cache.Remove(path);
            string mapped;
            if (_pathOf.TryGetValue(t, out mapped) && mapped == path) _pathOf.Remove(t);
            _released++;
            return true;
        }

        /// <summary>🔶 自己设计（非原版）：租约闸 —— 已知「正在显示、必须留住」的图先 `Retain`，`Release` 会被拒。
        /// 原版没有对应物（它的保护来自「句柄只由持有者放」）；我们没有句柄层，需要一个显式标记。
        /// 判据拿不到原版等价物，因为它不存在。调度台 2026-10-26 定（工作项 B8_BF）。</summary>
        public static void Retain(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            int n;
            _leases.TryGetValue(path, out n);
            _leases[path] = n + 1;
        }

        /// <summary>🔶 自己设计（非原版）：解一份租约（**不放图**）。见 <see cref="Retain"/>。</summary>
        public static bool Unretain(string path)
        {
            int n;
            if (string.IsNullOrEmpty(path) || !_leases.TryGetValue(path, out n)) return false;
            if (n <= 1) _leases.Remove(path); else _leases[path] = n - 1;
            return true;
        }

        /// <summary>这张图现在有没有租约（正在显示）。</summary>
        public static bool IsRetained(string path)
        {
            int n;
            return !string.IsNullOrEmpty(path) && _leases.TryGetValue(path, out n) && n > 0;
        }

        static readonly Dictionary<string, int> _leases = new Dictionary<string, int>();
        static readonly Dictionary<Texture2D, string> _pathOf = new Dictionary<Texture2D, string>();
        static int _released;

        // ============================================================ 模式图标的「释放」那一族（账 `收藏:203`）
        //
        // 判据（🥇 方法体 + 一份 SO 实例实读；2026-10-26 `W1K02` 现读，⛔ 不是转抄账文）：
        //  · `GameModesIconCollection.Release(PlayModes? playMode)` —— `decomp_full/GameModesIconCollection__Release.c`
        //    全文 + `cpp2il/out_cpp2il_dev_ilspy/GameModesIconCollection.cs` 的 `Release` C# 形状（两份一致）：
        //      `HasValue == false` ⇒ 放 **`defaultIcon`**(+0x20) 那一条；
        //      有值              ⇒ 遍历 `gameModesIcon`(+0x18) 表，命中 `+0x10 == value` 的那一项，放它的
        //                          `icon`(+0x18)。（`ReleaseAll()` = 表内每项各放一次 + 最后放默认那一条。）
        //  · 唯一入口 `ScriptableObjectsCollectionsUtilities.ReleaseGameModeIcon(Nullable<int>)`
        //    （`ScriptableObjectsCollectionsUtilities__ReleaseGameModeIcon.c`：`+0x20` 为 null ⇒ 那条「不返回」的抛出出口）。
        //    🔴 **订正（铁律 5）**：施工单/账原写「两个调用点都传 null ⇒ 实际只放默认图标」—— **实读不成立**，
        //    两个调用点**都带值**：
        //      - `CollectionDeck__OnDestroy.c`：`ReleaseGameModeIcon(Nullable<int>(CardDeck.get_GameMode(deck)))`
        //        ⇒ **卡组格销毁时放那副卡组自己的模式图标**（就是这一格画的那一张）；
        //      - `LiveopUIDrawer_GameModeIcon__OnDestroy.c`：同样构造了带值的 `Nullable<int>` 再调。
        //  · SO 实例实读（`d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/GameModesIconCollection.asset`）：
        //      `gameModesIcon` 两条 = playMode **0 → GUID `bb56ccdf…`**、playMode **13 → GUID `73a53afd…`**；
        //      `defaultIcon`   = **GUID `73a53afd…`**。
        //    GUID → 名（`资料/普查产出_第二十七会话/guid_map.tsv` 现查）：
        //      `bb56ccdf…` = **`40k_gamemode_icon_classic`**（PlayModes.Classic = 0）
        //      `73a53afd…` = **`40k_gamemode_icon_skirmish`**（PlayModes.Skirmish = 13）
        //    ⇒ **原版的「默认图标」就是 Skirmish 那一张** —— 数据如此，照抄（⛔ 这不是我们挑的，别「修正」成 classic）。
        //  · 全库只有这两张模式图（`assets_full` 里 `40k_gamemode_icon_*` 只有 classic / skirmish），
        //    与 `MenuDraw.GameModeIconFile` 认识的两种模式一一对上。
        //  ⚠️ **结构性差异（如实记）**：原版每个持有者各有一个 Addressables 句柄 ⇒ N 个格销毁 = N 次 Release
        //    （引用计数减 N）；我们的 `_cache` 里**一张图只有一条登记** ⇒ 放一次就没了。`ReleaseModeIcon` 因此
        //    **在「本来就没缓存」时不出声**（那是载体差异下的一次等价空操作，不是失败）；要数「真放掉几次」看
        //    `ReleasedCount`。⛔ 也**不要**把 `ui_menu/` 整个并进 `PerItemArtPrefix` —— 那一族里还装着 HUD 图 /
        //    难度角标 / 阵营图标，原版那三个释放方法一个都没碰它们（见 `PerItemArtPrefix` 上面那条注释）。

        /// <summary>模式图标这一族的**文件名前缀**（`40k_gamemode_icon_classic` / `…_skirmish`）。</summary>
        public const string ModeIconFilePrefix = "40k_gamemode_icon_";

        /// <summary>`GameModesIconCollection` 的 **`defaultIcon` 指向的那张图** —— SO 实例实读值（见上面那段）。
        /// ⚠️ 它是 **skirmish** 那张（GUID `73a53afd…`），不是 classic。</summary>
        public const string DefaultGameModeIconFile = "40k_gamemode_icon_skirmish";

        /// <summary>这条路径是不是「模式图标」那一族（`…/40k_gamemode_icon_*`）。
        /// <para>用途 = `Shell/MenuDraw.ReleaseArtUnder` 在子树销毁时把这一族也放掉（原版 `CollectionDeck.OnDestroy`
        /// → `ReleaseGameModeIcon(deck.GameMode)`）。**故意只认文件名前缀**：`ui_menu/` 下还有 HUD 图 / 难度角标 /
        /// 阵营图标，那些原版都不放 —— 把 `ui_menu/` 整目录并进 `PerItemArtPrefix` 会连带放掉它们（做反了）。</para></summary>
        public static bool IsModeIconArt(string path)
        {
            return !string.IsNullOrEmpty(path)
                && path.StartsWith(Root, System.StringComparison.Ordinal)
                && path.IndexOf(ModeIconFilePrefix, System.StringComparison.Ordinal) >= 0;
        }

        /// <summary>放掉模式图标那一族（原版 `GameModesIconCollection.Release` / `ReleaseGameModeIcon`）。
        /// <paramref name="gameMode"/> 有值 ⇒ 放**那个模式**的图标（= 原版 `CollectionDeck.OnDestroy` 那一跳）；
        /// 无值 ⇒ 放 `defaultIcon`（= SO 实读的 skirmish 那张）。
        /// <para>返回 true = 真从 `_cache` 里放掉了一条。**没缓存过 ⇒ 直接 false、不出声**（理由见上面那段
        /// 「结构性差异」：原版是引用计数，我们一张图只有一条登记）。</para>
        /// <para>⚠️ 只放缓存登记，**不销毁贴图** —— 正挂在别的活节点上的那张照常显示（同 `Release`）。</para></summary>
        public static bool ReleaseModeIcon(int? gameMode = null)
        {
            string file = gameMode.HasValue ? MenuDraw.GameModeIconFile(gameMode.Value) : DefaultGameModeIconFile;
            if (string.IsNullOrEmpty(file)) return false;   // 该模式没有图（原版表里也只有 0 / 13 两条）
            string suffix = "/" + file;
            string hit = null;
            foreach (var kv in _cache)                      // 不另拼目录：`MenuUi` 那三级兜底哪一档命中就放哪一档
            {
                if (kv.Value == null || !IsModeIconArt(kv.Key)) continue;
                if (kv.Key.EndsWith(suffix, System.StringComparison.Ordinal)) { hit = kv.Key; break; }
            }
            return hit != null && Release(hit);
        }

        /// <summary>这条路径**现在真在 `_cache` 里、并且有图**（负缓存不算）。
        /// <para>用途 = 调用点想在 `Release` **之前**挡掉「本来就没缓存」的那一档：`Release` 遇到没缓存的路径会
        /// **出声**（`Release` 那条 warning，语义是「你放了一条我给不出来的路径」），而**共享件重复放**是正常情形 ——
        /// 例：一屏 N 个卡组格都画同一张模式图标，原版是 N 个句柄各减一次，我们只有**一条登记** ⇒ 后 N-1 次
        /// 都应该静默跳过，不是 N-1 条告警。</para></summary>
        public static bool IsCached(string path)
        {
            Texture2D t;
            return !string.IsNullOrEmpty(path) && _cache.TryGetValue(path, out t) && t != null;
        }

        /// <summary>一副卡组**实际用哪张卡背的 SDF 掩码** —— <see cref="DeckCardback"/> 的成对那一半。
        /// <para>判据：原版 `CosmeticItemCardback.GetCardBackSprites()` **成对返回（主卡背, SDF）**
        /// （本文件 `CosmeticSdf` 头注），战斗侧两处用它 —— 牌堆主图 `DeckManager.cardbackMain`(+0x50) 与
        /// 掩码 `cardbackShadow`(+0x58) **同源**（`BattleCardUI.BattleCardSetup` 那一链：`GetCardback(isPlayer)`
        /// → `GetCosmeticItem` → `GetCardBackSprites` → `SetCardBack(主图, SDF)`）。</para>
        /// <para>`cardbackId` 空 / 取不到 ⇒ 退回**该阵营默认卡背**的 SDF（与原版 `CardDeck.GetDeckCardback()`
        /// 空 ⇒ `ArmyUtilities.GetDefaultCardback(deckArmy)` 同口径）；取不到时**出声**（不静默画空白）。</para></summary>
        public static Texture2D DeckCardbackSdf(string cardbackId, string faction)
        {
            if (!string.IsNullOrEmpty(cardbackId))
            {
                var t = CosmeticSdf(cardbackId);
                if (t != null) return t;
                Debug.LogWarning("[CardArt] 卡组选的卡背**没有 SDF 掩码**：" + cardbackId
                               + " → 退回该阵营默认卡背的 SDF（跑 `python 工具/import_original_art.py --only-cardback-sdf` 补图）");
            }
            return CardBackSdf(faction);
        }

        // ==================================================================
        //  🆕 2026-10-27（B87-A7 · 账 `战斗逻辑驱动:1280`）
        //     原版 `RawCardScript.ReleaseFullHeroSprite()` —— **放掉督军整身立绘**
        // ==================================================================
        //
        // 判据（🥇 亲读 `decomp_full/RawCardScript__ReleaseFullHeroSprite.c` 全文 45 行）：
        // <code>
        //   if (alternateArtCard(+0x110) != null) {
        //       ref = alternateArtCard.fullHeroSprite(+0x210);
        //       if (ref != null &amp;&amp; ref.RuntimeKeyIsValid()) { ref.Release(); return; }
        //   }
        //   ref = fullHeroSprite(+0x210);
        //   if (ref != null &amp;&amp; ref.RuntimeKeyIsValid()) ref.Release();
        //   else return;                       // ← **句柄无效 ⇒ 直接返回、不出声**
        // </code>
        // ⇒ 语义两条：① **异画优先**（异画那张自己有 `fullHeroSprite` 且句柄有效 ⇒ 放它，不放本体）；
        //   ② **放且只放一个**，无效句柄是**静默空操作**（原版没有 warning）。
        //
        // 原版 4 个活调用点（`grep -rl RawCardScript__ReleaseFullHeroSprite decomp_full`）：
        //   `DeckInfoPopup__Close` · `DraftModeMenuDemo__ReleaseMenuWarlordImage` ·
        //   `DraftRunDisplay__OnDestroy` · `PlayerDeckDemoDeckInfoWrapper`。
        //   ⚠️ 我们这侧对应窗件（`Shell/DeckInfoPopup.cs` · `Shell/DraftModeWindow.cs` · `Shell/ItemDrawer.cs`）
        //   **不在本批白名单** ⇒ 本笔只落**被调用的那一端**（接线工单已写进报告）。
        //
        // ⚠️ **与 `ReleaseModeIcon` 同一处结构性差异**（如实记）：原版每持有者一个 Addressables 句柄，
        //   N 个格销毁 = N 次 Release（引用计数减 N）；我们的 `_cache` 里**一张图只有一条登记** ⇒
        //   放一次就没了；**本来就没缓存**时**不出声**（那是载体差异下的等价空操作，不是失败）。
        //   要数「真放掉几次」看 `ReleasedCount`。

        /// <summary>原版 `RawCardScript.ReleaseFullHeroSprite()`：放掉**督军整身立绘**那条资产登记。
        /// <para>`warlordId` = 督军 id（= `_deckArtById` 的键，例如 `AM1`）；
        /// `alternateArtFile` = 异画那张的立绘文件名（原版 `alternateArtCard.fullHeroSprite`）——
        /// 非空且**在缓存里** ⇒ 优先放它（原版那一条 `if`），空 ⇒ 直接放本体。</para>
        /// <para>返回 `true` = 真从 `_cache` 里放掉了一条。**没缓存过 ⇒ false、不出声**（见上面那段 ⚠️）。</para>
        /// <para>⚠️ 与 <see cref="TryDeckHero"/> 用的是**同一条取图链**（`MenuUi` 的 `ui_menu → ui_deck → ui`
        /// 三级兜底）—— 这里按「哪一级命中就放哪一级」找登记，⛔ 别另拼目录。</para></summary>
        public static bool ReleaseFullHeroSprite(string warlordId, string alternateArtFile = null)
        {
            // ① 异画优先（原版第一条 `if`）：只有它那份**真在缓存里**才顶替本体
            if (!string.IsNullOrEmpty(alternateArtFile))
            {
                string alt = CachedMenuUiPath(alternateArtFile);
                if (alt != null) return Release(alt);
            }
            // ② 本体（原版第二条 `if`）
            string art;
            if (string.IsNullOrEmpty(warlordId)
                || !_deckArtById.TryGetValue(warlordId, out art) || string.IsNullOrEmpty(art))
                return false;          // 表里没有这一位 ⇒ 没有句柄可放（原版同档静默返回）
            string path = CachedMenuUiPath(art);
            if (path == null) return false;
            return Release(path);
        }

        /// <summary>`MenuUi` 那三级兜底里**已经缓存并且有图**的那一条路径；一条都没有 ⇒ `null`。
        /// （`MenuUi` 取图时是「新 → 旧 → 战斗那批」三级 `??`，这里按同一个次序找登记。）</summary>
        static string CachedMenuUiPath(string artName)
        {
            if (string.IsNullOrEmpty(artName)) return null;
            string file = artName.Replace(' ', '_');
            string p = Root + "ui_menu/" + file;
            if (IsCached(p)) return p;
            p = Root + "ui_deck/" + file;
            if (IsCached(p)) return p;
            p = Root + "ui/" + file;
            return IsCached(p) ? p : null;
        }
    }
}
