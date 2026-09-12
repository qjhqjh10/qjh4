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
        /// 见 `工具/import_original_art.py` 的 `RARITY_TIER` 与对照图
        /// `资料/留档_排查证据/卡面组装_0912/tier_map.png`）：common=1 / rare=2 / epic=3 / legendary=4。
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
            int tier = TierOf(rarity);
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

        /// <summary>稀有度 → 卡框档位。**判据只此一处**（和 `工具/import_original_art.py` 的
        /// `RARITY_TIER` 是同一张表，改要两边一起改）。</summary>
        public static int TierOf(string rarity)
        {
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "rare":      return 2;
                case "epic":      return 3;
                case "legendary": return 4;
                case "special":   return 4;   // ⚠️ 没实测过，原版 SO 只有 4 档
                default:          return 1;   // common / 空 / 不认识
            }
        }

        /// <summary>阵营卡背（牌堆/弃牌堆用）</summary>
        public static Texture2D CardBack(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            return Get(Root + "cards/back_" + faction.ToLowerInvariant());
        }

        /// <summary>某张卡的立绘（`Art/cards/art_<卡名小写下划线>.png`）。没有就返回 null，
        /// `CardView` 会退回程序生成的占位图。</summary>
        public static Texture2D Portrait(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            return Get(Root + "cards/art_" + Slug(cardId));
        }

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
