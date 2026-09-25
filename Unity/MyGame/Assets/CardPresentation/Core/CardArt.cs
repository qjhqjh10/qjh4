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

        /// <summary>阵营卡框（**不分稀有度**，取 tier1）。阵营名大小写不敏感（"Ember" → frame_ember）。</summary>
        public static Texture2D Frame(string faction)
        {
            return Frame(faction, null, false);
        }

        /// <summary>
        /// 阵营卡框，**按稀有度取对应 tier**（原版就是分四档的），**战术卡另有一套框**。
        ///
        /// 稀有度 → tier 的对应是**实测**出来的（四张不同稀有度的原版卡面逐一比对，
        /// 对照图 `资料/留档_排查证据/卡面组装_0912/tier_map.png`）：
        /// common=1 / rare=2 / epic=3 / legendary=4。
        /// 🔴 **`special` 是例外，见 <see cref="TierOf(string,string)"/>。**
        ///
        /// 原版每个阵营有**两套**框：`troop`（部队/督军）和 `stratagem`（战术卡）——
        /// 战术卡那张的形制不一样（下半截是大片文字区）。`tactic: true` 取后者。
        ///
        /// 取不到（没导那一档 / 稀有度不认识）**逐级退回**：本类型的 tier1 → 另一类型的 tier1 →
        /// 不带 tier 的老名字 —— 美术目录缺档也不至于变成没框的占位卡。
        /// </summary>
        public static Texture2D Frame(string faction, string rarity, bool tactic = false)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            string fac = faction.ToLowerInvariant();
            string kind = tactic ? "_strat" : "";
            int tier = TierOf(rarity, faction);
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
            return kind.Length > 0 ? Frame(faction, rarity, false) : null;
        }

        /// <summary>
        /// 软光/影那层（原版 `Card Highlight And Shadow`）的 **SDF 贴图** —— 与 <see cref="Frame"/> **同一套命名与逐级退回**
        /// （`card_sdf/&lt;阵营&gt;&lt;_strat&gt;_tier&lt;N&gt;`）。
        /// 它是原版**预生成的 128² 灰度距离场**（`40k_Cardframe{ s}_{ troop|stratagem}_&lt;阵营&gt;_SDF_tier1..4`，104 张），
        /// **不是运行时算的** —— 2026-09-19 查实，正本 `资料/普查产出_0919/卡面SDF软光影_查证.md`；
        /// 导入脚本 `工具/import_original_card_sdf.py`。
        /// ⚠️ 取不到返回 null ⇒ 那一层整个不画（和 `CardArt` 其它层一个规矩：删掉 `Resources/Art/` 游戏照样跑）。
        /// </summary>
        public static Texture2D Sdf(string faction, string rarity, bool tactic = false)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            string fac = faction.ToLowerInvariant();
            string kind = tactic ? "_strat" : "";
            int tier = TierOf(rarity, faction);
            if (tier > 1)
            {
                var t = Get(Root + "card_sdf/" + fac + kind + "_tier" + tier);
                if (t != null) return t;
            }
            var one = Get(Root + "card_sdf/" + fac + kind + "_tier1");
            if (one != null) return one;
            if (kind.Length > 0)                                  // 战术卡那套没有 → 退回 troop 的
            {
                var other = Sdf(faction, rarity, false);
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

        /// <summary>3D 卡体的 matcap（材质的 `_MatCap`）。
        /// ⚠️ **本地只解出 tier1 这一张** —— 原版是按 `CardTier` 取一个数组
        /// （`CardMaterialHelper.GetCardMatCapByCardTier`），其余档的解包资源里没有 ⇒ 四档先用同一张。</summary>
        public static Texture2D Card3DMatcap() { return Get(Root + "card3d/MatCap_Card_Level1"); }

        /// <summary>🆕 2026-09-25：**卡底那枚软阴影**的图（原版 sprite `Card blob shadow`）。
        /// 原版是 128×128 @ `m_PixelsToUnits = 100`（内容 `textureRect` 124.848²）；我们导的是**裁掉透明边**
        /// 的 125×125 内容 ⇒ 按 **PPU 100** 建 sprite 时内容尺寸与原版差 **0.12%**。
        /// 🔴 它是**白图 + alpha 掩码**，黑色靠 `SpriteRenderer.color` 染（见 `BlobShadow.cs` 文件头）。</summary>
        public static Texture2D Card3DBlobShadow() { return Get(Root + "card3d/CardBlobShadow"); }

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

        /// <summary>阵营卡背（牌堆/弃牌堆用）</summary>
        public static Texture2D CardBack(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            return Get(Root + "cards/back_" + faction.ToLowerInvariant());
        }

        /// <summary>某张卡的立绘（`Art/cards/art_<键>.png`）。没有就返回 null，
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
        /// 清单由 `工具/import_original_art.py` 生成（`Resources/Art/cards/card_cutouts.json`，665 张）。
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
        /// `angleDeg` 是**屏幕空间角度**（0 = 从左到右，90 = 从下到上）。
        /// ⚠️ 分辨率取 128×128 够用（渐变是线性的，插值不会失真）；生成一次就缓存。
        /// </summary>
        public static Texture2D Gradient(Color c1, Color c2, float angleDeg)
        {
            string key = $"grad_{ColorUtility.ToHtmlStringRGBA(c1)}_{ColorUtility.ToHtmlStringRGBA(c2)}_{angleDeg:F1}";
            if (_gradients.TryGetValue(key, out var cached) && cached != null) return cached;

            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.name = key;
            float rad = angleDeg * Mathf.Deg2Rad;
            float dx = Mathf.Cos(rad), dy = Mathf.Sin(rad);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    // 把像素投到方向轴上，归一化到 0..1（再拉伸到满量程，免得整幅只用到一小段色域）
                    float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N - 0.5f;
                    float t = Mathf.Clamp01((u * dx + v * dy) / Mathf.Max(0.0001f, Mathf.Abs(dx) + Mathf.Abs(dy)) + 0.5f);
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
        public static Texture2D GradientKeys(Color c1, Color c2, float angleDeg, float[] at, float[] aa)
        {
            var key = new System.Text.StringBuilder("gradk_");
            key.Append(ColorUtility.ToHtmlStringRGBA(c1)).Append('_').Append(ColorUtility.ToHtmlStringRGBA(c2))
               .Append('_').Append(angleDeg.ToString("F1"));
            for (int i = 0; i < at.Length; i++) key.Append('_').Append(at[i].ToString("F4")).Append(':').Append(aa[i].ToString("F4"));
            string k = key.ToString();
            if (_gradients.TryGetValue(k, out var cached) && cached != null) return cached;

            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.name = k;
            float rad = angleDeg * Mathf.Deg2Rad;
            float dx = Mathf.Cos(rad), dy = Mathf.Sin(rad);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N - 0.5f;
                    float t = Mathf.Clamp01((u * dx + v * dy) / Mathf.Max(0.0001f, Mathf.Abs(dx) + Mathf.Abs(dy)) + 0.5f);
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
        /// **关键词（trait）图标**，按英文关键词取（`Trait("ephemeral")`）。
        ///
        /// 来源：原版图集 `40ktraiticonatlas`（78 个切片，80×80 RGBA），
        /// **切片文件名就是英文关键词** —— 这是「哪个图标对应哪个词」最硬的证据。
        /// 导入：`工具/import_original_art.py` 的 `TRAIT_SRC` → `Resources/Art/traits/`。
        /// 对照表：`资料/关键词图标/关键词与图标_对照表.md`（有证据的对照 vs 推测，分了两节）。
        ///
        /// **为什么要它**：临时卡（Ephemeral）的卡面标记 —— 规则书 `:183`/`:229` 说
        /// 临时卡「回合结束若在手牌则移除」，玩家得**看得出哪张是临时的**。
        /// 原版那套是 `BattleCardUI.ShowEphemeral()` + `Card2DController.ToggleGlitch()`（换 glitch 材质），
        /// ⚠️ **但 glitch 素材本地没有**（`d:/2` 全盘 `*glitch*` 零命中）
        /// ⇒ 改用原版真有的这张关键词图标（比自绘更接近原版，而且它本来就在本地）。
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

        /// <summary>全部卡背的**名字**（字典序，顺序稳定）—— Cosmetics 页铺格用。
        /// 第一次调会 `Resources.LoadAll` 一次（233 张），之后走缓存。</summary>
        public static string[] CosmeticNames()
        {
            if (_cardBackNames == null)
            {
                var all = Resources.LoadAll<Texture2D>(Root + "cardbacks");
                var names = new List<string>();
                foreach (var t in all) if (t != null) names.Add(t.name);
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
        // 出处：`工具/import_original_art.py` 的 `ALT_ART`（**本机只有 7 张** · 两种风格 `AA_HB` / `v2`）。
        // 🔴 **绑定规则**：一件异画**绑死在一张卡上**，不能给别的督军用
        //    （`AlternateArtCard.GetIdForClonedCard()` 对原卡资产 id 做一次 `String.Replace` 得到克隆 id，
        //     `AlternateArtInventory.OnFinishUnpack` 再 `AssetLocator.GetAsset(该 id)` ⇒ 一一对应）。
        //    ⇒ **「风格 × 卡」是一对多**：选一个风格，只有在该风格下画了异画的那几张卡会换。

        /// <summary>某张卡的异画立绘（按**卡 id** 取，文件 = `Art/altarts/alt_<id 小写>.png`）。没有给 null。</summary>
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
    }
}
