// GiveFeedbackPopup.cs — 原版 **`GiveFeedbackMenuDemo`**
// （prefab 根 `Give Feedback Popup menu` · 反馈问卷整窗：标题 + 副标题 + 4 段问卷（长滚动）+ 自由文本框 + 提交/关闭）
//
// ============================ 出处（唯一判据，全部本地现读）============================
// · prefab 树 / 矩形 / 组件 / 文字档位（2026-10-17 现读，可复跑）：
//     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Give Feedback Popup menu" --depth 8`
//   根 = `bundle_menus_assets_all/GameObject/Give Feedback Popup menu.json`，
//   根 MB = `MonoBehaviour_5403621168596701646.json`（窗参逐字段实读）：
//     `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` · `updateNavPanel = 0` ·
//     `extraScaleSmallScreen = 1.0` · `openSound` / `closeSound` = 空 PPtr · `useDefaultCloseSoundIfNull = 1`
//   字段实读：`submitButton` / `closeButton` 两个 PPtr · `thanksForFeedbackKey = Demo/FeedbackSurvey/OnSubmitMessage`
//   · `thanksPopupButtonKey = Demo/Feedback/CloseThanksPopup` · `onCloseKey = Demo/FeedbackSurvey/BeforeClose`。
//   `Menu Dark Background/BackgroundCloseButton.window = 0` ⇒ **点暗底不关窗**。
// · 行为（第一权威 = 反编译 `d:/2/tools/decomp_full/GiveFeedbackMenuDemo__*.c`）：
//   · `Awake`：`GetComponentsInChildren<...>(true)`，把全部问卷题组件收进数组（`+0xC8`）。
//   · `Start`：`submitButton.onClick += Submit` · `closeButton.onClick += Close` · **出厂 `submit.interactable = 0`**。
//   · `Open`：**先看 `PlayerDataManager` 的「已提交过」标记（`+0x39`）⇒ 真则直接 `Close()`**；否则
//     `GameWindow.Open()` → 逐题 `Reset()`（虚表 `0x188`）并挂 `OnAnswer` → 首次进来置「已展示」标记。
//   · `OnAnswer`：`submitButton.interactable = 所有题都答了`（`Enumerable.All`）。
//   · `Submit`：`submit.interactable = 0` → 收答案 → 组装 payload → `ShowPopUp(thanksForFeedbackKey,
//     thanksPopupButtonKey)` → `GameAnalytics.SendFeedbackEvent` → 置「已提交」标记 → `UploadOnlineDemoData`
//     → `GameWindow.Close()`。
//   · `Close`：若**还有必答题没答** ⇒ `ShowPopUp(onCloseKey, …)` 询问（我们这一档没有两颗钮的确认回调，
//     如实标注）；否则 `GameWindow.Close()`。
// · 本地数据缺口（2026-10-17 现读，**别重复查**）：
//   🔴 `Demo/FeedbackSurvey/*` 那 24+ 条词条**不在** `Resources/official_loc_13lang.json`（9447 键，`grep
//   FeedbackSurvey` = **0**），`grep -rl "FeedbackSurvey/S1Q1" d:/2` 只命中**这个 prefab 自己**。
//   ⇒ 问卷正文只有**题干的英文 `m_text`**（prefab 出厂字）+ **答案的 mTerm 键名**；中文/英文答案文案本地都没有。
//   本窗的处置：题干用 prefab 的英文；答案按 **option id** 显示（`CCG` / `18-24` …）+ 出声一次，
//   ⛔ **不自译、不编文案**（铁律 3 / 红线「查不到就说查不到」）。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **入口**：原版有代码调用点（`FeedbackButton__OpenFeedbackMenu.c`），但主菜单 `Feedback Button`
//   出厂 `m_IsActive = false`、`feedbackMenu` 字段全库 0 命中 ⇒ **这个 build 里没有活入口**。
//   ⇒ 本窗**建好并注册**（`WindowsManager.OpenGiveFeedback`），**不伪造入口**（与 `TutorialMessagePopup` 同口径）。
// · **问卷结构**：4 个 `Section` / 16 道题（7 道多选 + 9 道 1–5 评分）/ **41 颗多选钮 + 9×5 = 45 颗评分钮**；
//   `Questions container` 高 **6426.93**（`Scroll Rect` 视口只有 673.64 高 ⇒ 必须滚动）。
//   滚动走全壳唯一一份 `MenuScroll`（纵向 · `m_MovementType = 1 Elastic` · `m_Inertia = 1` ·
//   `m_Elasticity = 0.1` · `m_DecelerationRate = 0.135`，prefab 实读）。
// · **文字输入**：🔴 **2026-10-17（第四十八会话 · `N48③`）就地订正（铁律 5）** —— 原文写
//   「工程没有 IME/键盘输入层」**已过期**：`Shell/PointerLayer.cs` 早有壳级输入
//   （`BeginText / TypeChar / Backspace / EndText`，卡名筛选 / `Import Deck Popup` / 改名都在用）。
//   ⇒ 两处文本框（`Free feedback/Input field` 与三道开放题）**已接**；开放题**参与必答判定**。
//   ⚠️ 能力边界（如实标）：`PointerLayer` 是**纯字符流**（`onTextInput` + Backspace + Enter/Esc），
//   **没有**光标 / 选区 / 拖选 / 多行编辑 —— 原版是 `TMP_InputField`。⛔ 别宣称「和原版一样能编辑」。
// · **`Free feedback`**：prefab 出厂 `m_IsActive = false`（旧版单行反馈面板），照建、整颗关着。
// · **提交**：`Submit()` 做完整的本地校验与交互，但**上报不实现**（原版走 `UploadOnlineDemoData` → PlayFab；
//   本地没有后端）⇒ 出声 + 照原版弹「感谢」消息窗 + `Close()`。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `GiveFeedbackMenuDemo`（`GameWindow` 子类）—— **反馈问卷整窗**。
    /// <para>🔴 **本地没有问卷正文的中文/英文文案表**（`Demo/FeedbackSurvey/*` 全缺）⇒ 题干用 prefab 的英文
    /// 出厂字、答案显示 option id，并出声一次；⛔ 不自译、不编文案（判据 → 文件头）。</para>
    /// <para>🔴 **入口缺口**：原版这个 build 里 `Feedback Button` 出厂关着、`feedbackMenu` 字段 0 命中
    /// ⇒ 本窗只提供「怎么开」，不伪造调用点。</para></summary>
    public class GiveFeedbackPopup : GameWindow
    {
        // ============================================================ 队列档（自成一档 · 3440–3449）
        // ⚠️ 层带不许与别的窗重叠（现扫 `Shell/*.cs` 的 `const int Q*`；相邻带 = 3430–3439 / 3458）。
        public const int QShade = 3440,      // `Menu Dark Background`
                         QPanel = 3441,      // `Generic Popup Background`
                         QFill = 3442,       // `Generic Popup Background/Mask/Background fill`
                         QText = 3443,       // 标题 / 副标题 / 框内文案
                         QRow = 3444,        // 问卷行（复选框 / 评分钮）
                         QButton = 3445;     // 提交 / 关闭
        public const int QHit = 3449;

        // ============================================================ 几何（dump 现读 · 1920×1080 画布 px）
        public const string RootName = "Give Feedback Popup menu";

        public static readonly PxRect DarkR = new PxRect(-1327.30f, -778.50f, 3247.30f, 1793.80f);
        public static readonly PxRect WindowR = new PxRect(-155.50f, 38.60f, 2075.50f, 975.70f);
        public static readonly PxRect PanelR = new PxRect(38.00f, 38.60f, 1863.00f, 1035.50f);     // 1824.95×996.86
        public static readonly PxRect MaskR = new PxRect(48.40f, 48.00f, 1853.10f, 1025.70f);      // 1804.68×977.62
        public static readonly PxRect TitleR = new PxRect(105.10f, 75.20f, 1710.90f, 153.10f);
        public static readonly PxRect SubtitleR = new PxRect(105.10f, 153.10f, 1710.90f, 193.30f);
        public static readonly PxRect SubmitR = new PxRect(720.60f, 909.20f, 1198.90f, 984.20f);
        public static readonly PxRect SubmitTextR = new PxRect(733.30f, 908.60f, 1185.60f, 983.60f);
        public static readonly PxRect CloseR = new PxRect(1816.50f, 10.70f, 1891.50f, 85.70f);
        public static readonly PxRect CloseIconR = new PxRect(1825.80f, 21.00f, 1882.20f, 75.50f);
        /// <summary>`Free feedback`：**出厂 `m_IsActive = false`**（整颗关着）。</summary>
        public static readonly PxRect FreeR = new PxRect(293.80f, 640.60f, 1626.20f, 870.60f);
        public static readonly PxRect InputR = new PxRect(400.50f, 640.60f, 1519.50f, 893.30f);
        public static readonly PxRect InputTextR = new PxRect(410.50f, 647.60f, 1509.50f, 887.30f);
        public static readonly PxRect InputTitleR = new PxRect(400.50f, 579.70f, 1519.50f, 640.60f);
        public static readonly PxRect CounterR = new PxRect(1407.90f, 603.20f, 1519.50f, 640.60f);
        public static readonly PxRect ScrollR = new PxRect(70.90f, 211.60f, 1836.40f, 885.30f);    // 1765.5×673.64
        public static readonly PxRect ContentR = new PxRect(86.40f, 211.60f, 1831.40f, 6638.60f);  // 1745×6426.93

        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.7725490f);
        /// <summary>`Submit button` 的 `Image.m_Color` 实读 `(0.369, 0.894, 0.587, 1)`。</summary>
        public static readonly Color SubmitTint = new Color(0.369f, 0.894f, 0.587f, 1f);
        /// <summary>`Input field` 的 `Image.m_Color` 实读 alpha `0.5725490`。</summary>
        public static readonly Color InputTint = new Color(1f, 1f, 1f, 0.5725490f);
        /// <summary>开放题 `Text Area` 的 `Image.m_Color` 实读 `(0.286, 0.965, 0.686, 1)`。</summary>
        public static readonly Color OpenAreaTint = new Color(0.286f, 0.965f, 0.686f, 1f);

        // 图（prefab 实读 / 已导入 `Resources/Art/ui_menu|ui|ui_deck`）
        public const string ArtPanel = "40k_popup";
        public const float PanelTexW = 359f, PanelTexH = 336f;
        public static readonly Vector4 PanelBorder = new Vector4(169f, 160f, 169f, 160f);
        public const string ArtFill = "40k_popup_texture";
        public const string ArtButton = "40K_button";
        public const float BtnTexW = 489f, BtnTexH = 107f;
        public static readonly Vector4 BtnBorder = new Vector4(234f, 46f, 234f, 46f);
        public const string ArtCloseRound = "UI_Button_Round_background";
        public const string ArtCloseIcon = "40k_bt_close";
        public const string ArtInputBg = "InputFieldBackground";
        public const string ArtDropdownBg = "40K_dropdown_bg";
        public const string ArtCheck = "40K_settings_icon_checkmark";
        public const string ArtScore = "Feedback_Scoring_Button";

        // 文字档位（prefab 逐颗实读）
        public const float TitleFont = 50f, TitleFontMin = 4f, TitleFontMax = 50f, TitleFontBase = 36f;
        public const float SubtitleFont = 42.45f, SubtitleFontMin = 4f, SubtitleFontMax = 50f, SubtitleFontBase = 36f;
        public const float SubmitFont = 45f, SubmitFontMin = 12f, SubmitFontMax = 45f, SubmitFontBase = 12f;
        public const float QTitleFont = 50f, QTitleFontMin = 4f, QTitleFontMax = 50f, QTitleFontBase = 36f;
        public const float QSubFont = 36f, QSubFontMin = 4f, QSubFontMax = 40f, QSubFontBase = 36f;
        public const float CheckLabelFont = 42f, CheckLabelMin = 29f, CheckLabelMax = 42f, CheckLabelBase = 36f;
        public const float NumTitleFont = 38f, NumTitleMin = 4f, NumTitleMax = 38f, NumTitleBase = 36f;
        public const float ScoreFont = 46.4f;
        public const float PlaceholderFont = 33f;

        // 官方词条（prefab `Localize.mTerm` 实读）
        public const string TitleTerm = "Demo/Feedback/Title";
        public const string SubtitleTerm = "Demo/FeedbackSurvey/SubTitle";
        public const string SubmitTerm = "Demo/Feedback/Submit";
        public const string PlaceholderTerm = "Demo/Feedback/TextHere";
        public const string InputTitleTerm = "Demo/Feedback/Question/InputFieldTitle";
        public const string OtherTerm = "MenuDeck/HUD/DeckDescription/Other";
        /// <summary>根 MB 上那三条（`thanksForFeedbackKey` / `thanksPopupButtonKey` / `onCloseKey`）。</summary>
        public const string ThanksTerm = "Demo/FeedbackSurvey/OnSubmitMessage";
        public const string ThanksButtonTerm = "Demo/Feedback/CloseThanksPopup";
        public const string OnCloseTerm = "Demo/FeedbackSurvey/BeforeClose";

        // 兜底原文（官方 13 语言表取不到的档；⛔ 不自译，中文档也回英文并在日志里出声）
        const string SubtitleEn = "It takes less than two minutes to help us make the game better!";
        const string ThanksEn = "Your opinion is really important to us. Thanks for your feedback!";
        const string OnCloseEn = "Are you sure you want to close the survey? Your answers will be lost.";

        // ============================================================ 问卷数据（prefab 实读）
        public enum ItemKind { Header, Choice, Numeric, Open }

        public struct Opt { public string Id, Term; }
        static Opt O(string id, string term) { return new Opt { Id = id, Term = term }; }

        public struct Item
        {
            public ItemKind Kind;
            public string Name;
            public float X1, Top, X2, Height;      // 块矩形（画布 px · 绝对）
            public float HdrH;                     // 表头高（Choice/Open）
            public float TitleX2, SubX1, SubX2;    // 表头里 Title / Subtitle 的横向切分
            public float Inset;                    // 表头文字上下内缩（= prefab `m_Padding` 15 那一档）
            public string TitleNode;               // `Title (1)` 或 `Question`
            public string TitleTerm, TitleEn, SubTerm, SubEn;
            public int ToggleCount, Max;
            public bool Required;
            public Opt[] Opts;
            /// <summary>评分题的题干（1–5 那 9 道是**题面在左、按钮在右**；S2Q1/S2Q2 的题干出厂关着）。</summary>
            public bool NumTitleActive;
            public float NumTitleX2;
        }

        public struct Sec
        {
            public string Name;
            public PxRect R;
            public Item[] Items;
        }

        public static readonly float RowH = 75.64f, RowGap = 10f;   // `Checkbox` 行高 / 间距（dump 实读）
        public const float ToggleW = 119f;                          // `Toggle` 的宽（dump 实读）
        public const float ScoreW = 65.09f, ScoreH = 51.78f, ScoreGap = 25.7f;

        static Item Hdr(string name, float x1, float top, float x2, float h,
                        float titleX2, float subX1, float subX2, string titleNode,
                        string term, string en, string subTerm, string subEn)
        {
            return new Item
            {
                Kind = ItemKind.Header, Name = name, X1 = x1, Top = top, X2 = x2, Height = h,
                HdrH = h, TitleX2 = titleX2, SubX1 = subX1, SubX2 = subX2, Inset = 0f,
                TitleNode = titleNode, TitleTerm = term, TitleEn = en, SubTerm = subTerm, SubEn = subEn,
            };
        }

        static Item Choice(string name, float x1, float top, float x2, float h, float hdrH,
                           float titleX2, float subX1, float subX2, float inset,
                           string term, string en, string subTerm, string subEn,
                           int max, bool required, Opt[] opts)
        {
            return new Item
            {
                Kind = ItemKind.Choice, Name = name, X1 = x1, Top = top, X2 = x2, Height = h,
                HdrH = hdrH, TitleX2 = titleX2, SubX1 = subX1, SubX2 = subX2, Inset = inset,
                TitleNode = "Title (1)", TitleTerm = term, TitleEn = en, SubTerm = subTerm, SubEn = subEn,
                Max = max, Required = required, Opts = opts,
            };
        }

        static Item Num(string name, float x1, float top, float x2, float h,
                        bool titleActive, float titleX2, string term, string en, int count, bool required)
        {
            return new Item
            {
                Kind = ItemKind.Numeric, Name = name, X1 = x1, Top = top, X2 = x2, Height = h,
                TitleNode = "Question title", NumTitleActive = titleActive, NumTitleX2 = titleX2,
                TitleTerm = term, TitleEn = en, ToggleCount = count, Required = required,
            };
        }

        static Item Open(string name, float x1, float top, float x2, float h, float hdrH,
                         float titleX2, float subX1, float subX2, float inset,
                         string term, string en, string subTerm, string subEn, bool required)
        {
            return new Item
            {
                Kind = ItemKind.Open, Name = name, X1 = x1, Top = top, X2 = x2, Height = h,
                HdrH = hdrH, TitleX2 = titleX2, SubX1 = subX1, SubX2 = subX2, Inset = inset,
                TitleNode = "Title (1)", TitleTerm = term, TitleEn = en, SubTerm = subTerm, SubEn = subEn,
                Required = required,
            };
        }

        /// <summary>4 段 / 16 题（prefab 逐节点实读：`Questions container` 下 `Section 1..4`）。</summary>
        public static readonly Sec[] Sections =
        {
            new Sec
            {
                Name = "Section 1",
                R = new PxRect(86.40f, 219.60f, 1831.40f, 754.20f),
                Items = new[]
                {
                    Choice("S1Q1", 121.40f, 219.60f, 1796.40f, 534.60f, 80f,
                           958.90f, 958.90f, 1796.40f, 0f,
                           "Demo/FeedbackSurvey/S1Q1", "Your age group:", "", "", 1, true,
                           new[] { O("-18", "Demo/FeedbackSurvey/S1A1"), O("18-24", "Demo/FeedbackSurvey/S1A2"),
                                   O("25-34", "Demo/FeedbackSurvey/S1A3"), O("35-44", "Demo/FeedbackSurvey/S1A4"),
                                   O("45+", "Demo/FeedbackSurvey/S1A5") }),
                },
            },
            new Sec
            {
                Name = "Section 2",
                R = new PxRect(86.40f, 770.20f, 1831.40f, 2484.80f),
                Items = new[]
                {
                    Hdr("S2Q1 Header", 121.40f, 770.20f, 1796.40f, 70f,
                        943.90f, 973.90f, 1796.40f, "Question",
                        "Demo/FeedbackSurvey/S2Q1", "How easy was it to get into Warpforge when you started?",
                        "Demo/FeedbackSurvey/S2Q1ST", "(1 = Very easy, 5 = Very difficult)"),
                    Num("S2Q1", 121.40f, 850.20f, 1796.40f, 62.28f, false, 0f, "", "", 5, true),
                    Hdr("S2Q2 Header", 121.40f, 922.50f, 1796.40f, 96.04f,
                        1247.10f, 1257.10f, 1745.30f, "Title (1)",
                        "Demo/FeedbackSurvey/S2Q2", "How familiar are you with Warhammer 40,000 lore and universe?",
                        "Demo/FeedbackSurvey/S2Q2ST", "(1 = Not familiar at all, 5 = Extremely familiar)"),
                    Num("S2Q2", 121.40f, 1028.50f, 1796.40f, 62.28f, false, 0f, "", "", 5, true),
                    Choice("S2Q3", 121.40f, 1100.80f, 1796.40f, 1046.80f, 80f,
                           958.90f, 958.90f, 1796.40f, 15f,
                           "Demo/FeedbackSurvey/S2Q3", "What games genre are you currently playing? (Select up to 3)",
                           "", "", 3, true,
                           new[] { O("CCG", "Demo/FeedbackSurvey/S2Q3A1"), O("Strategy", "Demo/FeedbackSurvey/S2Q3A2"),
                                   O("Shooter", "Demo/FeedbackSurvey/S2Q3A3"), O("Action", "Demo/FeedbackSurvey/S2Q3A4"),
                                   O("RPG", "Demo/FeedbackSurvey/S2Q3A5"), O("Racing", "Demo/FeedbackSurvey/S2Q3A6"),
                                   O("Simulation", "Demo/FeedbackSurvey/S2Q3A7"), O("Puzzle", "Demo/FeedbackSurvey/S2Q3A8"),
                                   O("Sport", "Demo/FeedbackSurvey/S2Q3A9"), O("Tabletop", "Demo/FeedbackSurvey/S2Q3A10"),
                                   O("Casino", "Demo/FeedbackSurvey/S2Q3A11") }),
                    Open("S2Q4", 121.40f, 2157.60f, 1796.40f, 327.16f, 80f,
                         958.90f, 958.90f, 1796.40f, 15f,
                         "Demo/FeedbackSurvey/S2Q4",
                         "What do you like and dislike about Warpforge, as compared to the other card games?", "", "", true),
                },
            },
            new Sec
            {
                Name = "Section 3",
                R = new PxRect(86.40f, 2500.80f, 1831.40f, 3700.10f),
                Items = new[]
                {
                    Hdr("S3 Header", 121.40f, 2500.80f, 1796.40f, 70f,
                        933.90f, 983.90f, 1796.40f, "Title (1)",
                        "Demo/FeedbackSurvey/S3T1", "When I am playing Warpforge, I'm looking for...",
                        "Demo/FeedbackSurvey/S3ST", "(1 = Strongly disagree, 5 = Strongly agree)"),
                    Num("S3Q1", 121.40f, 2580.80f, 1796.40f, 62.28f, true, 737.90f,
                        "Demo/FeedbackSurvey/S3Q1",
                        "...a sense of relatedness (spending time with the community and friends)", 5, true),
                    Num("S3Q2", 121.40f, 2653.10f, 1796.40f, 62.28f, true, 703.90f,
                        "Demo/FeedbackSurvey/S3Q2", "...a sense of excitement (adrenaline rush)", 5, true),
                    Num("S3Q3", 121.40f, 2725.30f, 1796.40f, 62.28f, true, 703.90f,
                        "Demo/FeedbackSurvey/S3Q3", "...a sense of competition (climbing the leaderboard)", 5, true),
                    Num("S3Q4", 121.40f, 2797.60f, 1796.40f, 62.28f, true, 703.90f,
                        "Demo/FeedbackSurvey/S3Q4", "...a sense of achievement (getting all collectibles, earning rewards)", 5, true),
                    Num("S3Q5", 121.40f, 2869.90f, 1796.40f, 62.28f, true, 703.90f,
                        "Demo/FeedbackSurvey/S3Q5", "...a sense of progression (optimizing the deck)", 5, true),
                    Num("S3Q6", 121.40f, 2942.20f, 1796.40f, 62.28f, true, 703.90f,
                        "Demo/FeedbackSurvey/S3Q6", "...a sense of mastery (seeking difficult challenges)", 5, true),
                    Num("S3Q7", 121.40f, 3014.50f, 1796.40f, 62.28f, true, 703.90f,
                        "Demo/FeedbackSurvey/S3Q7", "...a sense of escapism (relaxing while playing)", 5, true),
                    Num("S3Q8", 121.40f, 3086.70f, 1796.40f, 62.28f, true, 703.90f,
                        "Demo/FeedbackSurvey/S3Q8", "...a sense of immersion (learning about the lore)", 5, true),
                    Num("S3Q9", 121.40f, 3159.00f, 1796.40f, 62.28f, true, 703.90f,
                        "Demo/FeedbackSurvey/S3Q9", "...a sense of freedom (choosing what and how to play)", 5, true),
                    Choice("S3Q10", 121.40f, 3231.30f, 1796.40f, 468.80f, 80f,
                           958.90f, 958.90f, 1796.40f, 15f,
                           "Demo/FeedbackSurvey/S3Q10", "Do you consider Warpforge more of a:", "", "", 1, true,
                           new[] { O("Hardcore", "Demo/FeedbackSurvey/S3Q10A1"), O("Midcore", "Demo/FeedbackSurvey/S3Q10A2"),
                                   O("Casual", "Demo/FeedbackSurvey/S3Q10A3"), O("Lore", "Demo/FeedbackSurvey/S3Q10A4") }),
                },
            },
            new Sec
            {
                Name = "Section 4",
                R = new PxRect(86.40f, 3716.10f, 1831.40f, 6638.60f),
                Items = new[]
                {
                    Choice("S4Q1", 121.40f, 3716.10f, 1796.40f, 268.14f, 80f,
                           958.90f, 958.90f, 1796.40f, 15f,
                           "Demo/FeedbackSurvey/S4Q1",
                           "Have you ever stopped playing Warpforge (for more than just a short break)?", "", "", 1, true,
                           new[] { O("Yes", "Demo/FeedbackSurvey/S4Q1A1"), O("No", "Demo/FeedbackSurvey/S4Q1A2") }),
                    Choice("S4Q2", 121.40f, 3994.30f, 1796.40f, 601.25f, 80f,
                           958.90f, 958.90f, 1796.40f, 15f,
                           "Demo/FeedbackSurvey/S4Q2", "What made you stop playing? (Select all that apply)", "", "", 99, true,
                           new[] { O("NotEnoughTime", "Demo/FeedbackSurvey/S4Q2A1"), O("PlayingAnotherGame", "Demo/FeedbackSurvey/S4Q2A2"),
                                   O("NotEnoughContent", "Demo/FeedbackSurvey/S4Q2A3"), O("LostInterest", "Demo/FeedbackSurvey/S4Q2A4"),
                                   O("FriendsStopPlaying", "Demo/FeedbackSurvey/S4Q2A5"), O("Other", "Demo/FeedbackSurvey/S4Q2A6") }),
                    Open("S4Q2_Extra", 121.40f, 4605.50f, 1796.40f, 327.16f, 80f,
                         958.90f, 958.90f, 1796.40f, 15f,
                         OtherTerm, "Other", "", "", false),
                    Choice("S4Q3", 121.40f, 4942.70f, 1796.40f, 614.06f, 80f,
                           958.90f, 958.90f, 1796.40f, 15f,
                           "Demo/FeedbackSurvey/S4Q3", "What made you return? (Select all that apply)", "", "", 99, true,
                           new[] { O("MissedTheGame", "Demo/FeedbackSurvey/S4Q3A1"), O("Friends/Community", "Demo/FeedbackSurvey/S4Q3A2"),
                                   O("NewGameContent", "Demo/FeedbackSurvey/S4Q3A3"), O("PreferWarpforge", "Demo/FeedbackSurvey/S4Q3A4"),
                                   O("SpecialEventOrRewards", "Demo/FeedbackSurvey/S4Q3A5"), O("Other", "Demo/FeedbackSurvey/S4Q3A6") }),
                    Open("S4Q3_Extra", 121.40f, 5566.70f, 1796.40f, 327.16f, 80f,
                         958.90f, 958.90f, 1796.40f, 15f,
                         OtherTerm, "Other", "", "", false),
                    Choice("S4Q4", 121.40f, 5903.90f, 1796.40f, 734.68f, 80f,
                           958.90f, 958.90f, 1796.40f, 15f,
                           "Demo/FeedbackSurvey/S4Q4",
                           "What would you like to receive when returning to the game after a long break? (Select up to 3)",
                           "", "", 3, true,
                           new[] { O("BoosterPacks", "Demo/FeedbackSurvey/S4Q4A1"), O("PrebuiltDecks", "Demo/FeedbackSurvey/S4Q4A2"),
                                   O("HardCurrency", "Demo/FeedbackSurvey/S4Q4A3"), O("PremiumCosmetics", "Demo/FeedbackSurvey/S4Q4A4"),
                                   O("PremiumCampaings", "Demo/FeedbackSurvey/S4Q4A5"), O("Wildcards", "Demo/FeedbackSurvey/S4Q4A6"),
                                   O("OtherCurrencies", "Demo/FeedbackSurvey/S4Q4A7") }),
                },
            },
        };

        // ============================================================ 数据
        public struct Context
        {
            /// <summary>true ⇒ 原版 `Open()` 开头那一支「已经提交过 ⇒ 直接关掉」。</summary>
            public bool AlreadySubmitted;
        }
        public static Context DefContext() { return new Context { AlreadySubmitted = false }; }
        static Context FillDef(Context? src) { return src.HasValue ? src.Value : DefContext(); }

        public static GiveFeedbackPopup LastOpened { get; private set; }

        Context _ctx = DefContext();
        public Context Ctx { get { return _ctx; } }

        Transform _dark, _window, _panel, _mask, _fill, _title, _subtitle, _submit, _close, _free, _scrollNode, _content;
        Label _titleLabel, _subtitleLabel, _submitLabel;
        Label _freeTextLabel, _freeCounterLabel;     // 🆕 `N48③`：旧版单行反馈框（出厂关着）
        string _freeText = "";
        MenuScroll _scroll;
        ViewportClip _clip;
        readonly Dictionary<string, HashSet<int>> _selected = new Dictionary<string, HashSet<int>>();
        readonly Dictionary<string, int> _score = new Dictionary<string, int>();
        // 🆕 **2026-10-17（`N48③`）**：开放题的作答文本（键 = `Item.Name`）。
        readonly Dictionary<string, string> _text = new Dictionary<string, string>();
        public bool SubmitInteractable { get; private set; }
        public readonly List<string> MissingArt = new List<string>();

        // ============================================================ 建窗
        public static GiveFeedbackPopup Create(WindowsManager mgr, Context? ctx = null)
        {
            var go = new GameObject(RootName);
            var win = go.AddComponent<GiveFeedbackPopup>();
            win.type = WindowType.Popup;                  // 实读 type = 1
            win.placement = WindowsPlacement.Popup;       // 实读 windowsPlacement = 15
            win.closeOnEsc = true;                        // 实读 closeOnESC = 1
            win.updateNavPanel = false;                   // 实读 updateNavPanel = 0
            win.extraScaleSmallScreen = 1f;               // 实读 1.0（= 不覆盖）
            win.Manager = mgr;
            win._ctx = FillDef(ctx);
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[GiveFeedback] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            // 原版 `Open()` 首句：`PlayerDataManager` 的「已提交过」标记为真 ⇒ **直接 Close()**
            if (_ctx.AlreadySubmitted)
            {
                Debug.Log("[GiveFeedback] 原版 `Open()` 首句：`PlayerDataManager` 的「已提交过」标记为真 ⇒ **直接关窗**"
                          + "（`GiveFeedbackMenuDemo__Open.c`）。");
                Close();
                return;
            }
            Build();
        }

        public void Show(Context? ctx = null)
        {
            _ctx = FillDef(ctx);
            // 原版 `Open()` 首句那条「已提交过 ⇒ 直接关窗」在 `Show` 这一支同样成立
            // （否则复用已关的窗会把问卷又建出来，与原版相反）
            if (_ctx.AlreadySubmitted) { Close(); return; }
            Build();
        }

        // ============================================================ 建整棵树（顺序 = 原版兄弟序）
        public void Build()
        {
            PointerLayer.UnregisterOwnedBy(gameObject);
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            _selected.Clear();
            _score.Clear();
            _text.Clear();
            _dark = _window = _panel = _mask = _fill = _title = _subtitle = _submit = _close = _free = _scrollNode = _content = null;
            _titleLabel = _subtitleLabel = _submitLabel = null;
            _freeTextLabel = _freeCounterLabel = null;
            _scroll = null;
            _clip = null;
            SubmitInteractable = false;

            // ---- 1) `Menu Dark Background`（`BackgroundCloseButton.window = 0` ⇒ 点暗底**不**关窗）----
            _dark = MenuDraw.Node(transform, "Menu Dark Background", DarkR);
            MenuDraw.Rect(_dark, CardArt.Solid(), DarkR, "Image", QShade, ShadeTint);

            // ---- 2) `Window`（容器；prefab 2231×937.09）----
            _window = MenuDraw.Node(transform, "Window", WindowR);

            // ---- 3) `Generic Popup Background`（九宫 `40k_popup`，border 169/160/169/160）----
            _panel = MenuDraw.Node(_window, "Generic Popup Background", PanelR);
            MenuDraw.Nine(_panel, Tex(ArtPanel, "`Generic Popup Background` 的九宫底"),
                          PanelR, PanelBorder, PanelTexW, PanelTexH, QPanel, null, true, "Image");

            // ---- 4) `Mask`（`Image + Mask`，`showGraphic = 0` ⇒ 自己**不画**；`Background fill` 是它的子件）----
            _mask = MenuDraw.Node(_panel, "Mask", MaskR);
            //   `Background fill`：`40k_popup_texture` · `Image.Type = Tiled` · `m_PixelsPerUnitMultiplier = 2`
            //   （sprite 128² ÷ 2 ⇒ 每块 **64px**；判据 = UGUI 里 ppuMul 是除，同 `MenuDraw.Nine` 的 borderOutPx 那条口径）
            _fill = MenuDraw.Node(_mask, "Background fill", MaskR);
            MenuDraw.Tiled(_fill, Tex(ArtFill, "`Background fill` 的平铺底纹"), MaskR, 64f, QFill, "Image");

            // ---- 5) `Title` / `Subtitle`（Left/Middle）----
            _title = MenuDraw.Node(_window, "Title", TitleR);
            _titleLabel = Text(_title, TitleR, TutorialMessagePopup.OfficialTerm(TitleTerm, "Give us your feedback!",
                                                                                  "给我们你的反馈！"),
                               Color.white, "TextMeshProUGUI", TitleFont, QText, TitleFontMin, TitleFontMax, TitleFontBase, 1,
                               face: TmpFont.Face.LatinCardBody);
            _subtitle = MenuDraw.Node(_window, "Subtitle", SubtitleR);
            _subtitleLabel = Text(_subtitle, SubtitleR,
                                  TutorialMessagePopup.OfficialTerm(SubtitleTerm, SubtitleEn, SubtitleEn),
                                  Color.white, "TextMeshProUGUI", SubtitleFont, QText,
                                  SubtitleFontMin, SubtitleFontMax, SubtitleFontBase, 1,
                                  face: TmpFont.Face.LatinCardBody);

            // ---- 6) `Submit button`（`40K_button` 九宫 + 绿染 + 居中 `Submit`）----
            _submit = MenuDraw.Node(_window, "Submit button", SubmitR);
            MenuDraw.Nine(_submit, Tex(ArtButton, "`Submit button` 的按钮底"), SubmitR, BtnBorder,
                          BtnTexW, BtnTexH, QButton, SubmitTint, true, "Image");
            _submitLabel = Text(_submit, SubmitTextR,
                                TutorialMessagePopup.OfficialTerm(SubmitTerm, "Submit", "提交"),
                                Color.white, "Button Text", SubmitFont, QText,
                                SubmitFontMin, SubmitFontMax, SubmitFontBase, 0,
                                face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_submitLabel, Label.VAlign.Midline, SubmitTextR);
            MenuDraw.Hit(_submit, "Hit", SubmitR, QHit, Submit);

            // ---- 7) `Generic Close Button`（圆底 + `40k_bt_close`）----
            _close = MenuDraw.Node(_window, "Generic Close Button", CloseR);
            MenuDraw.Rect(_close, Tex(ArtCloseRound, "`Generic Close Button` 的圆底"), CloseR, "Image",
                          QButton, null, true);
            var closeIcon = MenuDraw.Node(_close, "Icon", CloseIconR);
            MenuDraw.Rect(closeIcon, Tex(ArtCloseIcon, "关闭钮的叉"), CloseIconR, "Image", QButton + 1, null, false);
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 B3）**：原版
            //   `Give Feedback Popup menu/Window/Generic Close Button` → `ButtonClick Back`
            //   （pid 8084683923307806969）⇒ 显式传关窗 cue（同 `WindowSounds.DefaultCloseCue`）。
            MenuDraw.Hit(_close, "Hit", CloseR, QHit, Close, clickCue: WindowSounds.DefaultCloseCue);

            // ---- 8) `Free feedback`（prefab 出厂 `m_IsActive = false`；旧版单行反馈面板）----
            _free = MenuDraw.Node(_window, "Free feedback", FreeR);
            var inputNode = MenuDraw.Node(_free, "Input field", InputR);
            MenuDraw.Nine(inputNode, Tex(ArtInputBg, "`Input field` 的输入框底"), InputR,
                          new Vector4(10f, 10f, 10f, 10f), 32f, 32f, QText, InputTint, true, "Image");
            var inputArea = MenuDraw.Node(inputNode, "Text Area", InputTextR);
            Text(inputArea, InputTextR,
                 TutorialMessagePopup.OfficialTerm(PlaceholderTerm, "Enter text...", "输入文字..."),
                 new Color(0.196f, 0.196f, 0.196f, 0.5f), "Placeholder", PlaceholderFont, QText, 0f, 0f, 0f, 1,
                 face: TmpFont.Face.LatinCardBody);
            _freeTextLabel = Text(inputArea, InputTextR, "\u200b", new Color(0.196f, 0.196f, 0.196f, 1f),
                 "Text", PlaceholderFont, QText, 0f, 0f, 0f, 1, face: TmpFont.Face.LatinCardBody);
            Text(inputNode, InputTitleR,
                 TutorialMessagePopup.OfficialTerm(InputTitleTerm, "Let us know your comments and feedback",
                                                    "告诉我们你的意见与反馈"),
                 Color.white, "Input Field title", 38f, QText, 4f, 38f, 36f, 1, face: TmpFont.Face.LatinCardBody);
            _freeCounterLabel = Text(inputNode, CounterR, "280/500", new Color(0.642f, 0.642f, 0.642f, 1f),
                 "Character Counter", 24.7f, QText, 0f, 0f, 0f, 2, face: TmpFont.Face.LatinCardBody);
            // 🆕 `N48③`：旧版单行反馈框也接外壳文本输入（本窗出厂整块关着 ⇒ 只有将来启用才用得上）。
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C6）**：原版 `Free feedback/Input field` 是
            //   `Image + TMP_InputField + EverguildInputField`（**没有 EverguildButton**）⇒ 点它 = 聚焦，
            //   **不该有按钮音** ⇒ 关掉。
            var freeHit = MenuDraw.Hit(inputNode, "Hit", InputR, QHit, BeginEditFree);
            var freeWb = freeHit != null ? freeHit.GetComponent<WindowButton>() : null;
            if (freeWb != null) freeWb.playClickSound = false;
            _free.gameObject.SetActive(false);           // = prefab 出厂 `m_IsActive = false`

            // ---- 9) `Scroll Rect`（`ScrollRect + RectMask2D`，`h=0 v=1 mode=1` ⇒ 纵向 Elastic）----
            //   🔴 裁切状态**长在节点上**（`ViewportClip.Hang`，A435 阶段 2 口径）——
            //   mask 参数实读：`m_Padding = (0,0,0,0)` · `m_Softness = (0,50)`。
            _clip = ViewportClip.Hang(_window, "Scroll Rect", ScrollR, Vector4.zero, new Vector2Int(0, 50));
            _scrollNode = _clip.transform;
            MenuDraw.Rect(_scrollNode, CardArt.Solid(), ScrollR, "Image", QFill, new Color(1f, 1f, 1f, 0f));
            // 🔴 **2026-10-17（`N29` / `N46①`）**：原版 `Give Feedback Popup menu/Window/Scroll Rect`
            //   `m_ScrollSensitivity = **50**`（`bundle_menus_assets_all` 现读）⇒ 每格 50 px。
            _scroll = MenuScroll.TopAligned(ScrollR, ContentR.H, 50f);
            _scroll.Owner = gameObject;
            _scroll.ClipNode = _clip;
            _scroll.Elastic = true;                       // `m_MovementType = 1`（UGUI: Elastic = 1）
            _scroll.Inertia = true;                       // `m_Inertia = 1`
            _scroll.OnChanged = BuildSurvey;
            PointerLayer.RegisterScroll(_scroll);
            _content = MenuDraw.Node(_scrollNode, "Questions container", ContentR);

            BuildSurvey();

            if (MissingArt.Count > 0)
                Debug.LogWarning("[GiveFeedback] ⚠️ 有 " + MissingArt.Count + " 张图取不到："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        // ============================================================ 问卷内容（滚动重建）
        void BuildSurvey()
        {
            if (_content == null) return;
            MenuDraw.ClearChildren(_content);
            foreach (var sec in Sections)
            {
                // 段节点：整段在视口外也照建（它只是空容器，成本可忽略；行才是按可见性裁的）
                var secNode = MenuDraw.Node(_content, sec.Name, sec.R);
                foreach (var it in sec.Items)
                {
                    switch (it.Kind)
                    {
                        case ItemKind.Header: BuildHeader(secNode, it, false); break;
                        case ItemKind.Choice: BuildChoice(secNode, it); break;
                        case ItemKind.Numeric: BuildNumeric(secNode, it); break;
                        case ItemKind.Open: BuildOpen(secNode, it); break;
                    }
                }
            }
            RefreshSubmitState();
        }

        void BuildHeader(Transform parent, Item it, bool insideBlock)
        {
            var hdrTop = it.Top + it.Inset;
            var hdrH = insideBlock ? it.HdrH - it.Inset * 2f : it.HdrH;
            var hdrR = _scroll.Shift(new PxRect(it.X1, hdrTop, it.X2, hdrTop + hdrH));
            if (!_scroll.Intersects(hdrR)) return;
            var node = MenuDraw.Node(parent, insideBlock ? "Header" : it.Name, hdrR);
            var titleR = new PxRect(it.X1, hdrR.y1, it.TitleX2, hdrR.y2);
            var subR = new PxRect(it.SubX1, hdrR.y1, it.SubX2, hdrR.y2);
            if (!string.IsNullOrEmpty(it.TitleTerm) || !string.IsNullOrEmpty(it.TitleEn))
                Text(node, titleR, TutorialMessagePopup.OfficialTerm(it.TitleTerm, it.TitleEn, it.TitleEn),
                     Color.white, it.TitleNode, QTitleFont, QText, QTitleFontMin, QTitleFontMax, QTitleFontBase, 1,
                     face: TmpFont.Face.LatinCardBody);
            if (!string.IsNullOrEmpty(it.SubTerm) || !string.IsNullOrEmpty(it.SubEn))
                Text(node, subR, TutorialMessagePopup.OfficialTerm(it.SubTerm, it.SubEn, it.SubEn),
                     new Color(1f, 1f, 1f, 0.765f), "Subtitle", QSubFont, QText,
                     QSubFontMin, QSubFontMax, QSubFontBase, 1, face: TmpFont.Face.LatinCardBody);
        }

        void BuildChoice(Transform parent, Item it)
        {
            var blk = _scroll.Shift(new PxRect(it.X1, it.Top, it.X2, it.Top + it.Height));
            if (!_scroll.Intersects(blk)) return;
            var q = MenuDraw.Node(parent, it.Name, blk);
            // 表头
            BuildHeader(q, it, true);
            // 选项列表：每行 75.64 · 间隔 10（dump 实读）
            if (!_selected.ContainsKey(it.Name)) _selected[it.Name] = new HashSet<int>();
            var sel = _selected[it.Name];
            float rowTop = it.Top + it.HdrH;
            for (int i = 0; i < it.Opts.Length; i++)
            {
                var o = it.Opts[i];
                var raw = new PxRect(it.X1, rowTop, it.X2, rowTop + RowH);
                rowTop += RowH + RowGap;
                var rowR = _scroll.Shift(raw);
                if (!_scroll.Intersects(rowR)) continue;
                var row = MenuDraw.Node(q, i == 0 ? "Checkbox" : "Checkbox (" + i + ")", rowR);
                var togR = new PxRect(rowR.x1, rowR.y1, rowR.x1 + ToggleW, rowR.y2);
                var tog = MenuDraw.Node(row, "Toggle", togR);
                // 🔴 **2026-10-18（W3）就地订正（铁律 5）**：原版这颗 `Toggle` 是
                //   **`m_Type = Simple` + `m_PreserveAspect = 1`**（贴图 `40K_dropdown_bg` 119×102）
                //   ⇒ 按比例内接画成 **88.2×75.64**，**不是**九宫拉满 119×75.64（宽 31px）。
                //   判据 = dump 原文 `40K_dropdown_bg 119×102 九宫23,20,23,20 | Simple (1,1,1,1) preserveAspect`
                //   → `资料/普查产出_1018/P2_弹窗排版_通用活动奖励.md` §9。
                MenuDraw.Rect(tog, Tex(ArtDropdownBg, "复选框底"), togR, "Image", QRow, null, true);
                var chkR = new PxRect(rowR.x1, rowR.y1, rowR.x1 + ToggleW, rowR.y2);
                var chk = MenuDraw.Node(tog, "CheckMark", chkR);
                MenuDraw.Rect(chk, Tex(ArtCheck, "复选框勾"), chkR, "Image", QRow + 1, null, true);
                chk.gameObject.SetActive(sel.Contains(i));
                var lblR = new PxRect(rowR.x1 + ToggleW, rowR.y1, rowR.x2, rowR.y2);
                var lbl = Text(row, lblR, OptionText(o), Color.white, "Label", CheckLabelFont, QRow,
                               CheckLabelMin, CheckLabelMax, CheckLabelBase, 1, face: TmpFont.Face.LatinCardBody);
                int idx = i;
                // 🆕 **2026-10-18（音效线缺口 `AUD` §三 B4）**：原版 41 颗复选框
                //（`…/Questions container/Section */S*Q*/Questions List/Checkbox*`）全部
                // `m_onClickSound = 8881786964274599484` = **`Select Toggle`** ⇒ 显式传。
                var hit = MenuDraw.Hit(row, "Hit", rowR, QHit, () => ToggleChoice(it, idx, chk),
                                       clickCue: WindowSounds.SelectToggleCue);
                if (hit == null) { /* 视口外 ⇒ 不建（框已判过；这里只是兜底） */ }
            }
        }

        void BuildNumeric(Transform parent, Item it)
        {
            var blk = _scroll.Shift(new PxRect(it.X1, it.Top, it.X2, it.Top + it.Height));
            if (!_scroll.Intersects(blk)) return;
            var row = MenuDraw.Node(parent, it.Name, blk);
            if (it.NumTitleActive)
            {
                var tR = new PxRect(blk.x1, blk.y1, it.NumTitleX2, blk.y2);
                Text(row, tR, TutorialMessagePopup.OfficialTerm(it.TitleTerm, it.TitleEn, it.TitleEn),
                     Color.white, "Question title", NumTitleFont, QText, NumTitleMin, NumTitleMax, NumTitleBase, 1,
                     face: TmpFont.Face.LatinCardBody);
            }
            // `Button Container`：分段 2 = 整行居中；分段 3 = 右对齐（dump 的 `m_ChildAlignment = 4 / 5`）
            float totalW = it.ToggleCount * ScoreW + (it.ToggleCount - 1) * ScoreGap;
            float startX = it.NumTitleActive ? (blk.x2 - totalW) : (blk.x1 + (blk.W - totalW) * 0.5f);
            float by = blk.y1 + (blk.H - ScoreH) * 0.5f;
            if (!_score.ContainsKey(it.Name)) _score[it.Name] = 0;
            int cur = _score[it.Name];
            for (int i = 0; i < it.ToggleCount; i++)
            {
                var r = new PxRect(startX + i * (ScoreW + ScoreGap), by,
                                   startX + i * (ScoreW + ScoreGap) + ScoreW, by + ScoreH);
                var btn = MenuDraw.Node(row, "Button (" + i + ")", r);
                MenuDraw.Rect(btn, Tex(ArtScore, "评分钮底"), r, "Image", QRow, null, true);
                Text(btn, r, (i + 1).ToString(), Color.white, "Scoring", ScoreFont, QRow + 2, 0f, 0f, 0f, 0,
                     face: TmpFont.Face.LatinCardBody);
                int idx = i;
                // 🆕 **2026-10-18（主对话裁定 · 按原版订正）**：原版 `Feedback Scoring Button` 是
                //   **`allowClickWhenAlreadySelected = 0`** ⇒ 已选中再点 = **什么都不做**
                //   （不播音、不改分、不取消）。改前它是普通按钮（每次都派发）⇒ 我们自设计的
                //   「再点一次 = 取消」由 `SetScore` 实现 —— 那条语义**已删**。
                var scoreHit = MenuDraw.Hit(btn, "Hit", r, QHit, () => SetScore(it.Name, idx));
                var scoreWb = scoreHit != null ? scoreHit.GetComponent<WindowButton>() : null;
                if (scoreWb != null)
                {
                    scoreWb.allowClickWhenAlreadySelected = false;
                    scoreWb.isOn = () => _score.TryGetValue(it.Name, out int s) && s == idx + 1;
                }
                // 选中态：原版靠 `Highlight` 子件（出厂关着）；我们按同一颗子件名建，答了才亮
                var hl = MenuDraw.Node(btn, "Highlight", r);
                MenuDraw.Rect(hl, Tex(ArtScore, "评分钮高亮"), r, "Image", QRow + 1,
                              new Color(0.9137255f, 0.4971045f, 0.03921568f, 1f), true);
                hl.gameObject.SetActive(cur == i + 1);
            }
        }

        void BuildOpen(Transform parent, Item it)
        {
            var blk = _scroll.Shift(new PxRect(it.X1, it.Top, it.X2, it.Top + it.Height));
            if (!_scroll.Intersects(blk)) return;
            var node = MenuDraw.Node(parent, it.Name, blk);
            BuildHeader(node, it, true);
            var areaR = new PxRect(blk.x1, blk.y1 + it.HdrH, blk.x2, blk.y1 + it.HdrH + 220f);
            var box = MenuDraw.Node(node, "Text Area", areaR);
            // 🔴 **2026-10-18（W3）就地订正（铁律 5）**：原版这颗 `Text Area` 的 `Image` 是
            //   **`Sliced` + `m_PixelsPerUnitMultiplier = 2.0`** ⇒ 画出来的角块 = 九宫 ÷ 2
            //   （L/R 23→**11.5**、B/T 20→**10**）。我们原来没传 `borderOutPx` ⇒ 角块是 23/20（大一倍）。
            //   判据 = dump 原文 `40K_dropdown_bg 119×102 九宫23,20,23,20 | Sliced … ppuMul=2.0`
            //   → `资料/普查产出_1018/P2_弹窗排版_通用活动奖励.md` §9（P2-8）。
            MenuDraw.Nine(box, Tex(ArtDropdownBg, "开放题输入框底"), areaR, new Vector4(23f, 20f, 23f, 20f),
                          119f, 102f, QRow, OpenAreaTint, true, "Image",
                          new Vector4(11.5f, 10f, 11.5f, 10f));
            var inner = new PxRect(areaR.x1 + 10f, areaR.y1 + 7f, areaR.x2 - 10f, areaR.y2 - 7f);
            var area = MenuDraw.Node(box, "Text Area", inner);
            var ph = Text(area, inner,
                          TutorialMessagePopup.OfficialTerm(PlaceholderTerm, "Enter text...", "输入文字..."),
                          new Color(1f, 1f, 1f, 0.5f), "Placeholder", 29f, QText, 0f, 0f, 0f, 1,
                          face: TmpFont.Face.LatinCardBody);
            if (ph != null) ph.gameObject.SetActive(false);
            // \ud83c\udd95 **2026-10-17\uff08`N48\u2462`\uff09**\uff1a\u5f00\u653e\u9898\u63a5\u5916\u58f3\u6587\u672c\u8f93\u5165\uff08`PointerLayer.BeginText`\uff09\u3002
            //   \u6709\u4f5c\u7b54\u5c31\u663e\u793a\u4f5c\u7b54\u3001\u5360\u4f4d\u5b57\u85cf\u8d77\u6765\uff1b\u70b9\u6846\u5f00\u59cb\u7f16\u8f91\uff08\u56de\u8f66\u63d0\u4ea4 / ESC \u53d6\u6d88\uff09\u3002
            string curTxt = _text.TryGetValue(it.Name, out var t0) ? t0 : "";
            if (ph != null) ph.gameObject.SetActive(string.IsNullOrEmpty(curTxt));
            Text(area, inner, string.IsNullOrEmpty(curTxt) ? "\n\u200b" : curTxt, Color.white,
                 "description text", 38f, QText, 0f, 0f, 0f, 1, face: TmpFont.Face.LatinCardBody);
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C7）**：原版 `…/Text Area` 是
            //   `Image + EverguildInputField`（**没有 EverguildButton**）⇒ 点它 = 聚焦，**不该有按钮音**。
            var openHit = MenuDraw.Hit(box, "Hit", areaR, QHit, () => BeginEditOpen(it));
            var openWb = openHit != null ? openHit.GetComponent<WindowButton>() : null;
            if (openWb != null) openWb.playClickSound = false;
        }

        /// <summary>\u65e7\u7248\u5355\u884c\u53cd\u9988\u6846\u7684\u8f93\u5165\u5165\u53e3\uff08\u672c\u7a97\u51fa\u5382\u6574\u5757\u5173\u7740\uff1b\u540c `BeginEditOpen` \u7684\u80fd\u529b\u8fb9\u754c\uff09\u3002</summary>
        void BeginEditFree()
        {
            var pl = PointerLayer.Instance;
            if (pl == null)
            {
                Debug.LogWarning("[GiveFeedback] `PointerLayer` \u53d6\u4e0d\u5230 \u21d2 \u8fd9\u4e2a\u8f93\u5165\u6846**\u8fd9\u6b21\u6539\u4e0d\u4e86**\uff08\u26d4 \u4e0d\u9759\u9ed8\uff09\u3002");
                return;
            }
            pl.BeginText(_freeText, 500,
                s => { _freeText = (s ?? "").Trim(); RefreshFreeText(); },
                RefreshFreeText,
                s => { if (_freeTextLabel != null) _freeTextLabel.SetText(s);
                       if (_freeCounterLabel != null) _freeCounterLabel.SetText(s.Length + "/500"); });
        }

        void RefreshFreeText()
        {
            if (_freeTextLabel != null) _freeTextLabel.SetText(_freeText);
            if (_freeCounterLabel != null) _freeCounterLabel.SetText(_freeText.Length + "/500");
        }

        /// <summary>\u5f00\u653e\u9898\u7684\u8f93\u5165\u5165\u53e3\uff08\u5916\u58f3\u7eaf\u5b57\u7b26\u6d41\uff1a\u65e0\u5149\u6807/\u9009\u533a/\u591a\u884c\uff0c\u89c1\u6587\u4ef6\u5934\uff09\u3002</summary>
        void BeginEditOpen(Item it)
        {
            var pl = PointerLayer.Instance;
            if (pl == null)
            {
                Debug.LogWarning("[GiveFeedback] `PointerLayer` \u53d6\u4e0d\u5230 \u21d2 \u8fd9\u9053\u5f00\u653e\u9898**\u8fd9\u6b21\u6539\u4e0d\u4e86**\uff08\u26d4 \u4e0d\u9759\u9ed8\uff09\u3002");
                return;
            }
            string started = _text.TryGetValue(it.Name, out var s0) ? s0 : "";
            pl.BeginText(started, 500,
                s => { _text[it.Name] = (s ?? "").Trim(); BuildSurvey(); RefreshSubmitState(); },
                () => BuildSurvey(),
                s => { var lb = FindChildDeep(FindChildDeep(_content, it.Name), "description text");
                       var l = lb != null ? lb.GetComponent<Label>() : null; if (l != null) l.SetText(s + "_"); });
            Debug.Log("[GiveFeedback] \u300c" + it.Name + "\u300d\u5f00\u653e\u9898\uff1a\u8f93\u5165\u540e**\u56de\u8f66**\u786e\u8ba4\u3001**ESC** \u53d6\u6d88"
                    + "\uff08\u5916\u58f3\u8f93\u5165\u5c42\u662f\u7eaf\u5b57\u7b26\u6d41\uff0c\u6ca1\u6709\u5149\u6807/\u9009\u533a/\u591a\u884c \u2014\u2014 \u539f\u7248\u662f `TMP_InputField`\uff09\u3002");
        }

        // ============================================================ 交互
        void ToggleChoice(Item it, int idx, Transform checkMark)
        {
            var sel = _selected[it.Name];
            if (it.Max <= 1)
            {
                bool was = sel.Contains(idx);
                sel.Clear();
                if (!was) sel.Add(idx);
                // 同一题的所有勾一起重刷（单选）
                var q = FindChildDeep(_content, it.Name);
                if (q != null)
                    for (int i = 0; i < q.childCount; i++)
                    {
                        var row = q.GetChild(i);
                        if (!row.name.StartsWith("Checkbox")) continue;
                        var chk = row.Find("Toggle/CheckMark");
                        if (chk != null) chk.gameObject.SetActive(false);
                        int n = row.name == "Checkbox" ? 0 : ParseSuffix(row.name);
                        if (n >= 0 && sel.Contains(n) && chk != null) chk.gameObject.SetActive(true);
                    }
            }
            else
            {
                if (sel.Contains(idx)) sel.Remove(idx);
                else if (sel.Count >= it.Max)
                {
                    Debug.LogWarning("[GiveFeedback] `" + it.Name + "` 最多选 " + it.Max + " 项（原版 `maxSelectedOptions`）"
                                   + " ⇒ 这一下**不生效**。");
                    return;
                }
                else sel.Add(idx);
                if (checkMark != null) checkMark.gameObject.SetActive(sel.Contains(idx));
            }
            RefreshSubmitState();
        }

        void SetScore(string qid, int idx)
        {
            // 🔴 **2026-10-18（主对话裁定 · 按原版 `allowClickWhenAlreadySelected = 0` 订正）**：
            //   原版评分钮**已选中再点 = 什么都不做**（不播音、不改分、**不取消**）。
            //   改前这里是 `_score[qid] == idx + 1 ? 0 : idx + 1`（「再点一次 = 取消」）——
            //   那是**我们自设计的语义**，已删。现在写入是幂等的；同分再点由
            //   `WindowButton.allowClickWhenAlreadySelected = false` 的守卫挡在派发之前（连声音一起）。
            //   ⚠️ 改分（点**别的**分值）照旧——那条路 `isOn` 为假、守卫不收。
            _score[qid] = idx + 1;
            BuildSurvey();                                        // 重画这一题（Highlight 只有一个亮）
        }

        /// <summary>`AllRequiredAnswered`：原版 `OnAnswer` 的 `Enumerable.All(...)` 等价物。
        /// ✅ **2026-10-17（`N48③`）**：开放题**已参与**（文本输入已接；非空才算答）。
        /// ⚠️ 输入能力是纯字符流（无光标/选区/多行），但「答没答」这条判据与原版同义。</summary>
        bool AllRequiredAnswered()
        {
            foreach (var sec in Sections)
                foreach (var it in sec.Items)
                {
                    if (!it.Required) continue;
                    if (it.Kind == ItemKind.Choice)
                    {
                        HashSet<int> s;
                        if (!_selected.TryGetValue(it.Name, out s) || s.Count == 0) return false;
                    }
                    else if (it.Kind == ItemKind.Numeric)
                    {
                        int v;
                        if (!_score.TryGetValue(it.Name, out v) || v == 0) return false;
                    }
                    else if (it.Kind == ItemKind.Open)
                    {
                        string t;
                        if (!_text.TryGetValue(it.Name, out t) || string.IsNullOrEmpty(t)) return false;
                    }
                }
            return true;
        }

        void RefreshSubmitState()
        {
            SubmitInteractable = AllRequiredAnswered();
            // 原版：`Start` 里出厂 `interactable = 0`，`OnAnswer` 里 `= All(...)`。
            // 我们这一层用「提交钮的按钮底染暗」当视觉态（没有 uGUI 的 `ColorBlock.m_DisabledColor`）。
        }

        /// <summary>原版 `Submit`：本地校验 → 收答案 → 弹「感谢」→ 关窗。
        /// 🔴 **上报未实现**（原版 `UploadOnlineDemoData` → PlayFab，本地没有后端）⇒ 出声，⛔ 不假装成功。</summary>
        public void Submit()
        {
            if (!AllRequiredAnswered())
            {
                Debug.LogWarning("[GiveFeedback] 还有必答题没答 ⇒ 原版这一刻 `submitButton.interactable = false`，"
                               + "点它不会有反应。**本窗照此不提交**。");
                return;
            }
            int answers = 0;
            foreach (var kv in _selected) answers += kv.Value.Count;
            answers += _score.Count;
            answers += _text.Count;      // 🆕 `N48③`：开放题作答也计入「收齐了多少条」
            Debug.LogWarning("[GiveFeedback] **问卷上报未实现**：原版 `Submit()` → `UploadOnlineDemoData` → PlayFab，"
                           + "本地没有后端 ⇒ 只收齐了 " + answers + " 条答案（内存里），**不发送**。"
                           + "（`GameAnalytics.SendFeedbackEvent` 同样没有对应实现。）");
            var thanks = TutorialMessagePopup.OfficialTerm(ThanksTerm, ThanksEn, ThanksEn);
            // ⚠️ `OnSubmitMessage` 本地没有官方译文 ⇒ 回 `Demo/Feedback/Thanks` 那条同义文案（见 `ThanksTerm` 的上游注释）
            if (!Loc.HasEntry(ThanksTerm) && Loc.HasEntry("Demo/Feedback/Thanks"))
                thanks = Loc.T("Demo/Feedback/Thanks");
            var ok = TutorialMessagePopup.OfficialTerm(ThanksButtonTerm, "Close", "关闭");
            var mgr = Manager;
            if (mgr != null) mgr.ShowPopUp(thanks, ok);
            Close();
        }

        // ============================================================ 小工具
        Label Text(Transform parent, PxRect r, string s, Color color, string name, float fontPx, int q,
                   float minPx, float maxPx, float basePx, int align,
                   TmpFont.Face face = TmpFont.Face.Cjk)
        {
            // 🔴 **2026-10-17（A1024③）**：可选字体面 —— 默认 `Cjk` ⇒ 旧调用点零行为变化。
            var lb = MenuDraw.Text(parent, r, s, color, name, fontPx, q, r.W, minPx, maxPx, basePx, face: face);
            if (lb != null)
            {
                if (align == 1) MenuDraw.AlignLeft(lb, r);
                else if (align == 2) MenuDraw.AlignRight(lb, r);
            }
            return lb;
        }

        string OptionText(Opt o)
        {
            // 🔴 答案文案（`Demo/FeedbackSurvey/S*Ax`）本地没有（判据 → 文件头）⇒ 显示 **option id** + 出声一次。
            //    ⛔ 不自译、不编文案。
            var v = Loc.Lang13Of(o.Term, Loc.Current == AvailableLanguages.Chinese ? "zh" : "en");
            if (!string.IsNullOrEmpty(v)) return v;
            if (_optionWarned.Add(o.Term))
                Debug.LogWarning("[GiveFeedback] 答案词条 `" + o.Term + "` 在官方 13 语言表里**取不到** "
                               + "（`Demo/FeedbackSurvey/*` 整族本地都没有）⇒ 这一条显示 **option id `" + o.Id + "`**，"
                               + "⛔ 不自译、不编文案。");
            return o.Id;
        }
        static readonly HashSet<string> _optionWarned = new HashSet<string>();

        Transform FindChildDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        static int ParseSuffix(string n)
        {
            int a = n.IndexOf('('), b = n.IndexOf(')');
            if (a < 0 || b < a) return -1;
            int v;
            return int.TryParse(n.Substring(a + 1, b - a - 1), out v) ? v : -1;
        }

        // ============================================================ 取图 / 出声（不静默）
        Texture2D Tex(string name, string what)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name))
            {
                MissingArt.Add(name);
                Debug.LogWarning("[GiveFeedback] 图取不到：`" + name + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine/Tiled` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        // ============================================================ 自检读口
        public Transform DarkNode { get { return _dark; } }
        public Transform WindowNode { get { return _window; } }
        public Transform PanelNode { get { return _panel; } }
        public Transform MaskNode { get { return _mask; } }
        public Transform FillNode { get { return _fill; } }
        public Transform TitleNode { get { return _title; } }
        public Transform SubtitleNode { get { return _subtitle; } }
        public Transform SubmitNode { get { return _submit; } }
        public Transform CloseNode { get { return _close; } }
        public Transform FreeFeedbackNode { get { return _free; } }
        public Transform ScrollNode { get { return _scrollNode; } }
        public Transform ContentNode { get { return _content; } }
        public Label TitleLabel { get { return _titleLabel; } }
        public Label SubtitleLabel { get { return _subtitleLabel; } }
        public Label SubmitLabel { get { return _submitLabel; } }
        public MenuScroll Scroll { get { return _scroll; } }
        public int AnswerCount { get { int n = 0; foreach (var kv in _selected) n += kv.Value.Count; return n + _score.Count; } }

        public string DebugDump()
        {
            return "title=\"" + (_titleLabel != null ? _titleLabel.Text : "-") + "\""
                 + " sections=" + Sections.Length
                 + " choiceRows=" + CountChoiceRows()
                 + " submit=" + SubmitInteractable
                 + " answers=" + AnswerCount
                 + " scroll=" + (_scroll != null ? _scroll.Offset.ToString("F1") : "-")
                 + " missingArt=" + MissingArt.Count;
        }

        public static int CountChoiceRows()
        {
            int n = 0;
            foreach (var s in Sections) foreach (var it in s.Items) if (it.Kind == ItemKind.Choice) n += it.Opts.Length;
            return n;
        }
    }
}
