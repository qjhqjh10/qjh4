// RankedArmyBonusCard.cs — 主菜单「奖励活动卡」`Reward Event Container 1x1` = 原版类 `RankedArmyBonusContainer`
// （账 `个人资料_成就_排行:197` `Clean()` / `:198` `OnInitialize(LiveOpsEvent)`）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【为什么单开一个文件】原版这两条方法的宿主 = `RankedArmyBonusContainer`，它挂在**运行时由 liveop 数据
//   实例化**的 prefab `Reward Event Container 1x1` 上。我们过去**压根不建这张卡**（5 张模式卡里没有它）
//   ⇒ `Clean()` 没有调用对象、`OnInitialize()` 没有初始化对象。本文件把这张卡 + 两条方法一起建出来。
//   ⚠️ **接线还没落**：宿主 `Shell/MainMenuRuntime.cs` 此刻被别的代理占用 ⇒ 调用点是**可粘贴补丁**
//      （见报告 §「MainMenuRuntime 补丁」），本文件建成后由调度台插两行。
//
// 【判据（全部现读，⛔ 没有一个是猜的）】
//   · 几何 / 组件 / 字段 → `资料/完全复刻/判据/B90_C2b_远端prefab几何.md` §E-1（prefab 根 RT pid
//     `-6548592895001984001`、MB pid `5217132012044452863`、`sizeDelta 535×414`、`pivot (0.5,0.5)`、
//     `anchoredPosition (0,−43.8868)`；子件表逐格给出）。
//   · 字段 PPtr（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_5217132012044452863.json` 现读）：
//     `armyPrefab` = `5516551289399767058` → 落到 GameObject **`Simple Army Icon`**（go pid `-3315707156309839854`，
//     RT `sizeDelta (254.90341, 0)`、anchors/pivot `(0,0)/(0.5,0.5)`；`SimpleArmyImage` MB 的 `armyIcon` =
//     那颗白图 `Image`（`m_Sprite = 0`、`preserveAspect = 1`）、`armyText = null`）；
//     `armyContentAnchor` = `4818649679952315391`（= `Content`）、`thresholdForAutoScroll = 2`。
//   · 两条方法体 →
//     `decomp_full/RankedArmyBonusContainer__OnInitialize.c`（192 行，四步见 `Initialize` 的注释）
//     `decomp_full/RankedArmyBonusContainer__Clean.c`（`:14` 取 `+0x70` `armyContentAnchor` →
//     `SupportMethods.DestroyAllChildren`；`:19` 取 `+0x88` `currentArmyImages` → 清 List）。
//   · `SimpleArmyImage.Initialize` → `cpp2il/out_cpp2il_dev_ilspy/SimpleArmyImage.cs`：
//     `sprite = ArmyUtilities.Instance.armyIcons.GetArmyIcon(army)` → `armyIcon.sprite = sprite` →（取不到就关掉 Image）。
//     我们的等价物 = `Core/ArmyIcons.cs`（全工程唯一一份 `CardArmy → 40k_DeckSelection_icon_*` 映射）。
//
// 🔶 自己设计（非原版）：`Resources/Art/ranked_army_bonus.json` 那**一份本地模拟数据**（活动标题 / 剩余时间 /
//   受加成阵营清单）。原版这三样全部来自 **liveop 服务端**（`RankedScoreBoostData.get_AffectedArmies(evt[3])`），
//   服务器已关服、判据在远端，本机永远拿不到 ⇒ 按 CLAUDE.md §18·0 允许自设计，规则：
//   **① 表必须是「有内容」的**（原版 `evt == null` / 类型闸不等 ⇒ `Destroy(this)`；建成空表会得到另一种行为）；
//   **② 表缺失 ⇒ 不出这张卡**（与原版「没数据就该毁卡」同向，⛔ 不留空卡）；
//   **③ 每次用到都出声一次**（`[RankedArmyBonus]` 前缀），⛔ 不静默。
//   调度台 2026-10-29 裁定（`B97_PROFILE` 简报：「建本地模拟表 + 出声 + 打 🔶 标」）。
//
// 【没做到的（如实记）】
//   · `Background Image` / `Foreground Image` 原版是 `UIParallaxImage` + `MatModifier` + `AspectRatioFitter`，
//     **贴图由活动数据在运行时喂**（prefab 里没有图）⇒ 本文件只建节点 + 出声一次（⛔ 不拿纯色块冒充活动图）。
//   · `TimerDisplay`（`Everguild` 的计时件）我们没有等价类 ⇒ 用一句 `Timer Text` 文本 + `Clock Icon` 承载，
//     显隐/格式按 prefab 的 baked 样例（`'5d 20h 15m'`）。
//   · `UI_InfiniteScroll` 没有运行时实现（本工程没有 uGUI / 滚动组件，滚动由 `Shell/MenuScroll.cs` 那一族自绘）
//     ⇒ 只把原版那一位 `thresholdForAutoScroll(2) < list.Count` 的结果**算出来并暴露成断言读口**
//     （`AutoScrollEnabled`），⛔ 不假装滚得动。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RankedArmyBonusContainer` 的三条自设计数据（🔶，理由见文件头）。</summary>
    public static class RankedArmyBonusData
    {
        public const string DtoPath = "Art/ranked_army_bonus";
        static bool _warned;

        [Serializable]
        public class Event
        {
            /// <summary>活动标题（原版 = `evt.GetLabel()`；🔶 本地模拟）。</summary>
            public string title;
            /// <summary>剩余秒数（原版 = `TimerDisplay` 由 `evt` 的结束时间算；🔶 本地模拟）。</summary>
            public float endsInSeconds;
            /// <summary>`RankedScoreBoostData.get_AffectedArmies()` 的等价物（`CardArmy` 枚举值，10…130）。</summary>
            public int[] affectedArmies;
        }

        [Serializable] public class Doc { public Event[] events; }

        /// <summary>读本地模拟表。取不到 / 解析失败 ⇒ `null` 并**出声一次**（调用方据此不建卡）。</summary>
        public static Doc Load()
        {
            var ta = Resources.Load<TextAsset>(DtoPath);
            if (ta == null)
            {
                Warn("取不到本地模拟表 `Resources/" + DtoPath + ".json` ⇒ **不建** `Reward Event Container 1x1`"
                   + "（与原版「没数据就毁卡」同向；🔶 自己设计，判据在远端 liveop）");
                return null;
            }
            try { return JsonUtility.FromJson<Doc>(ta.text); }
            catch (Exception e)
            {
                Warn("本地模拟表解析失败：" + e.Message + " ⇒ **不建** `Reward Event Container 1x1`");
                return null;
            }
        }

        /// <summary>表里第一条**有效**活动（`affectedArmies` 非空）。空表 / 全空条目 ⇒ `null`。</summary>
        public static Event FirstValid(Doc doc)
        {
            if (doc == null || doc.events == null) return null;
            for (int i = 0; i < doc.events.Length; i++)
            {
                var e = doc.events[i];
                if (e != null && e.affectedArmies != null && e.affectedArmies.Length > 0) return e;
            }
            Warn("本地模拟表里**没有**任何带 `affectedArmies` 的活动 ⇒ **不建**卡（⛔ 不留空卡）");
            return null;
        }

        static void Warn(string m)
        {
            if (_warned) return;
            _warned = true;
            Debug.Log("[RankedArmyBonus] " + m);
        }

        /// <summary>自检用：清掉「只出声一次」的标记（每次 `Build` 都该能重新出声）。</summary>
        public static void ResetWarnForTest() { _warned = false; }
    }

    /// <summary>「奖励活动卡」`Reward Event Container 1x1`（原版类 `RankedArmyBonusContainer`）。
    /// <para>`Initialize` = 原版 `OnInitialize(LiveOpsEvent)`；`Clean` = 原版 `Clean()`。</para></summary>
    public class RankedArmyBonusCard : MonoBehaviour
    {
        /// <summary>原版 prefab 根名（⛔ 别改：`MainMenuRuntime` 的模式卡按这个名字登记）。</summary>
        public const string RootName = "Reward Event Container 1x1";

        // ---- 几何（判据 = `判据/B90_C2b_远端prefab几何.md` §E-1，单位 = 设计 px，左上原点、y 向下）----
        const float CardW = 535f, CardH = 414f;
        /// <summary>根在窗口里的 `anchoredPosition`（原版 `(0, −43.8868)`，y 向上为正）。</summary>
        public const float RootAnchoredY = -43.8868f;
        static readonly PxRect CardRel = new PxRect(0f, 0f, CardW, CardH);
        static readonly PxRect BgRel = new PxRect(-106.66f, -167.25f, 641.66f, 581.07f);
        static readonly PxRect TextDarkRel = new PxRect(2.58f, 305.60f, 532.49f, 411.47f);
        static readonly PxRect TitleRel = new PxRect(13.58f, 309.01f, 527.28f, 364.72f);
        static readonly PxRect TimerRel = new PxRect(13.58f, 367.33f, 438.29f, 408.06f);
        static readonly PxRect ClockRel = new PxRect(13.58f, 367.32f, 54.34f, 408.08f);
        static readonly PxRect TimerTextRel = new PxRect(54.34f, 364.94f, 253.33f, 410.46f);
        static readonly PxRect BorderRel = new PxRect(0f, 0f, 535f, 414f);
        static readonly PxRect ScrollRel = new PxRect(2.54f, 11.16f, 532.34f, 291.99f);
        static readonly PxRect BadgeRel = new PxRect(-4.95f, -4.02f, 185.90f, 186.44f);
        /// <summary>`Simple Army Icon` 的 `sizeDelta.x`（原版 `254.90341`；高度由布局组撑满 `Content`）。</summary>
        const float ArmyIconW = 254.90341f;
        /// <summary>`Content` 的 `HorizontalLayoutGroup.spacing`（原版 **20**）。</summary>
        const float ArmySpacing = 20f;
        /// <summary>原版序列化字段 `thresholdForAutoScroll`（**2**）。</summary>
        public const int ThresholdForAutoScroll = 2;

        Transform _card, _content, _bg, _fg;
        /// <summary>`Content` 的绝对矩形（= 建树时算出来的那一份；⛔ 别在运行期从 `localPosition` 反推 ——
        /// 它的父件是 `Viewport` 不是窗口）。</summary>
        PxRect _contentAbs = new PxRect(0f, 0f, 0f, 0f);
        Label _title, _timerText;
        readonly List<Transform> _currentArmyImages = new List<Transform>();
        readonly List<string> _missingArt = new List<string>();
        bool _warnedNoFeatureArt;

        // ---- 自检读口 ----
        /// <summary>`currentArmyImages` 的个数（原版 `+0x88` 那个 List）。</summary>
        public int ArmyImageCount { get { return _currentArmyImages.Count; } }
        /// <summary>`Content` 的子件数（`Clean()` 后必须 0）。</summary>
        public int ContentChildCount { get { return _content == null ? 0 : _content.childCount; } }
        /// <summary>原版 `*(bool*)(scroll + 0x22)` = `thresholdForAutoScroll < list.Count`。</summary>
        public bool AutoScrollEnabled { get; private set; }
        /// <summary>标题 / 计时读口。</summary>
        public string TitleText { get; private set; }
        /// <summary>计时读口（`'5d 20h 15m'` 形状，同 prefab baked 样例）。</summary>
        public string TimerTextValue { get; private set; }
        /// <summary>受加成阵营的 `CardArmy` 值（按 `affectedArmies` 原序）。</summary>
        public int[] AffectedArmies { get; private set; }
        /// <summary>取不到 / 没图的件（⛔ 静默失败是红线）。</summary>
        public IList<string> MissingArt { get { return _missingArt; } }
        /// <summary>这颗卡实例是否已按原版「没数据就毁卡」被销毁（自检读口）。</summary>
        public bool Destroyed { get; private set; }

        /// <summary>建卡（几何照 §E-1）。`caller` 只用于出声定位。</summary>
        public static RankedArmyBonusCard Create(Transform parent)
        {
            if (parent == null) return null;
            var go = new GameObject(RootName, typeof(RectTransform));
            var card = go.AddComponent<RankedArmyBonusCard>();
            card.Build(parent);
            return card;
        }

        void Build(Transform parent)
        {
            var t = transform;
            t.SetParent(parent, false);
            t.gameObject.layer = parent.gameObject.layer;

            // 根的绝对矩形：以窗口中心为原点、`anchoredPosition (0, −43.8868)`（y 向上）⇒ 屏幕 y 向下 +43.8868
            var cardAbs = new PxRect(960f - CardW * 0.5f, 540f + (-RootAnchoredY) - CardH * 0.5f,
                                     960f + CardW * 0.5f, 540f + (-RootAnchoredY) + CardH * 0.5f);
            MenuDraw.ApplyPxRect(t, parent, cardAbs);
            _card = t;

            // ① Background / Foreground（原版 `UIParallaxImage`，贴图由活动数据运行时喂 ⇒ 只建节点 + 出声）
            _bg = MenuDraw.Node(t, "Background Image", Abs(cardAbs, BgRel));
            _fg = MenuDraw.Node(t, "Foreground Image", Abs(cardAbs, BgRel));
            _missingArt.Add("Background Image / Foreground Image");
            Debug.Log("[RankedArmyBonus] `Background Image` / `Foreground Image` 只建了节点：原版贴图由 liveop 活动数据"
                    + " 运行时喂（prefab 里 `m_Sprite = 0`），本地没有活动图 ⇒ ⛔ 不拿纯色块冒充");

            // ② TextDarkening（黑 0.612）+ Event Title + Timer
            var dark = MenuDraw.Node(t, "TextDarkening", Abs(cardAbs, TextDarkRel));
            MenuDraw.Rect(dark, CardArt.Solid(), Abs(cardAbs, TextDarkRel), "Dark",
                          MenuDraw.QText, new Color(0f, 0f, 0f, 0.612f));
            _title = MenuDraw.Text(t, Abs(cardAbs, TitleRel), "GAME MODE TITLE", Color.white,
                                   "Event Title", 58.8f, MenuDraw.QText, TitleRel.W, 18f, 72f, 36f,
                                   face: TmpFont.Face.LatinUi);

            var timer = MenuDraw.Node(t, "Timer With Time Description", Abs(cardAbs, TimerRel));
            MenuDraw.Rect(timer, CardArt.MenuUi("WF_icon_clock"), Abs(cardAbs, ClockRel), "Clock Icon",
                          MenuDraw.QText, Color.white);
            _timerText = MenuDraw.Text(timer, Abs(cardAbs, TimerTextRel), "--", Color.white,
                                       "Timer Text", 40f, MenuDraw.QText, TimerTextRel.W, 20f, 40f, 30f,
                                       face: TmpFont.Face.LatinUi);

            // ③ Border（`40k_square_border` · Sliced · fillCenter=0 · ppuMul 5.0）
            var borderTex = CardArt.MenuUi("40k_square_border");
            if (borderTex != null)
                MenuDraw.Nine(t, borderTex, Abs(cardAbs, BorderRel), new Vector4(13f, 13f, 13f, 13f),
                              borderTex.width, borderTex.height, MenuDraw.QText, Color.white,
                              false, "Border");

            // ④ Feature Badge（`UI_Army_Selection_Featured`，preserveAspect）
            var badgeTex = CardArt.MenuUi("UI_Army_Selection_Featured");
            if (badgeTex != null)
                MenuDraw.Rect(t, badgeTex, Abs(cardAbs, BadgeRel), "Feature Badge",
                              MenuDraw.QText, Color.white);
            else if (!_warnedNoFeatureArt)
            {
                _warnedNoFeatureArt = true;
                _missingArt.Add("Feature Badge");
                Debug.LogWarning("[RankedArmyBonus] 取不到 `UI_Army_Selection_Featured` ⇒ `Feature Badge` **不建**");
            }

            // ⑤ Dynamic Content ▸ Viewport ▸ Content（`armyContentAnchor`，HLG spacing 20）
            var contentAbs = Abs(cardAbs, ScrollRel);
            _contentAbs = contentAbs;
            var scroll = MenuDraw.Node(t, "Dynamic Content", contentAbs);
            var view = MenuDraw.Node(scroll, "Viewport", contentAbs);
            _content = MenuDraw.Node(view, "Content", contentAbs);
        }

        static PxRect Abs(PxRect cardAbs, PxRect rel)
            => new PxRect(cardAbs.x1 + rel.x1, cardAbs.y1 + rel.y1, cardAbs.x1 + rel.x2, cardAbs.y1 + rel.y2);

        /// <summary>原版 `OnInitialize(LiveOpsEvent evt)` 的四步等价物。
        /// <para>① `evt == null` ⇒ `Destroy(this)`；② 类型闸不等 ⇒ 同样 `Destroy(this)`；
        /// ③ `DestroyAllChildren(Content)` + 清 `currentArmyImages`，再对每个加成阵营
        /// `Instantiate(armyPrefab, Content)` + `SimpleArmyImage.Initialize(army)`；
        /// ④ `*(bool*)(scroll+0x22) = thresholdForAutoScroll < list.Count`。</para>
        /// <para>我们的等价：`evt == null` / `affectedArmies` 空 ⇒ **毁卡**（⛔ 不留空卡）。</para></summary>
        public void Initialize(RankedArmyBonusData.Event evt)
        {
            if (evt == null || evt.affectedArmies == null || evt.affectedArmies.Length == 0)
            {
                Debug.Log("[RankedArmyBonus] `OnInitialize`：事件为空（或没有阵营加成）⇒ 按原版 `Destroy(this)` **毁卡**，"
                        + "⛔ 不留空卡");
                Destroyed = true;
                DestroySafe(gameObject);
                return;
            }

            Clean();                                  // ③ 的前半（原版先清再填）

            TitleText = evt.title ?? "";
            if (_title != null) _title.SetText(TitleText);
            TimerTextValue = FormatRemaining(evt.endsInSeconds);
            if (_timerText != null) _timerText.SetText(TimerTextValue);

            AffectedArmies = (int[])evt.affectedArmies.Clone();
            var contentAbs = _contentAbs;
            float h = contentAbs.H;
            float total = AffectedArmies.Length * ArmyIconW + (AffectedArmies.Length - 1) * ArmySpacing;
            float x0 = contentAbs.CX - total * 0.5f;
            for (int i = 0; i < AffectedArmies.Length; i++)
            {
                float cx = x0 + i * (ArmyIconW + ArmySpacing) + ArmyIconW * 0.5f;
                var r = new PxRect(cx - ArmyIconW * 0.5f, contentAbs.CY - h * 0.5f,
                                   cx + ArmyIconW * 0.5f, contentAbs.CY + h * 0.5f);
                var icon = BuildArmyIcon(_content, r, AffectedArmies[i]);
                _currentArmyImages.Add(icon);
            }

            // ④ `*(bool*)(scroll + 0x22) = thresholdForAutoScroll < list.Count`
            AutoScrollEnabled = ThresholdForAutoScroll < _currentArmyImages.Count;
        }

        /// <summary>原版 `Clean()`：`SupportMethods.DestroyAllChildren(armyContentAnchor)` + 清 `currentArmyImages`。</summary>
        public void Clean()
        {
            if (_content != null)
                for (int i = _content.childCount - 1; i >= 0; i--)
                    DestroySafe(_content.GetChild(i).gameObject);
            _currentArmyImages.Clear();
            AutoScrollEnabled = false;
        }

        /// <summary>一颗 `Simple Army Icon`（原版 prefab：白图 `Image`（`m_Sprite = 0`、`preserveAspect`）+ `SimpleArmyImage`）。</summary>
        Transform BuildArmyIcon(Transform parent, PxRect abs, int army)
        {
            var node = MenuDraw.Node(parent, "Simple Army Icon", abs);
            string sprite = ArmyIcons.IconName(army);          // 原版 `ArmyUtilities.GetArmyIcon(army)`
            var tex = string.IsNullOrEmpty(sprite) ? null : CardArt.MenuUi(sprite);
            var quad = MenuDraw.Rect(node, tex != null ? tex : CardArt.Solid(), abs, "Image",
                                     MenuDraw.QText,
                                     tex != null ? Color.white : new Color(1f, 1f, 1f, 0f));
            if (tex == null)
            {
                // 原版：`armyIcon.sprite = null` ⇒ 那颗 Image 关掉（`SimpleArmyImage.cs` 末句）
                if (quad != null) quad.gameObject.SetActive(false);   // 原版 `armyIcon.sprite = null` ⇒ 那颗 Image 关掉
                if (!_missingArt.Contains(sprite == null ? "army:" + army : sprite))
                    _missingArt.Add(sprite == null ? "army:" + army : sprite);
            }
            return node;
        }

        /// <summary>`'5d 20h 15m'`（= prefab baked 样例的形状；`TimerDisplay` 没有等价类，见文件头「没做到的」）。</summary>
        public static string FormatRemaining(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = Mathf.FloorToInt(seconds);
            int d = total / 86400, h = (total % 86400) / 3600, m = (total % 3600) / 60;
            if (d > 0) return d + "d " + h + "h " + m + "m";
            if (h > 0) return h + "h " + m + "m";
            return m + "m";
        }

        static void DestroySafe(GameObject go)
        {
            if (go == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) { DestroyImmediate(go); return; }
#endif
            Destroy(go);
        }
    }
}
