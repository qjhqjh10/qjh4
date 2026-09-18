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

        /// <summary>稀有度 → 卡框档位（不看阵营）。**判据只此一处**。</summary>
        public static int TierOf(string rarity) => TierOf(rarity, null);

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
