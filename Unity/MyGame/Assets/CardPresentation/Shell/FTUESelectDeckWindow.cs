// FTUESelectDeckWindow.cs — 原版 **`FTUESelectDeckWindow`**（prefab `Choose Army Deck FTUE Window`）
//   = 「SELECT ARMY」选阵营送卡组窗（FTUE 的一环）。界面线 **T26** / 音效线 **S02**。
//
// ============================ 出处（唯一判据，全部本地现读 2026-10-17） ============================
// · prefab 树 / 矩形 / 组件 / 文字档位（2026-10-17 现读，可复跑）：
//     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Choose Army Deck FTUE Window" --depth 6`
//   同一份记录 → `资料/普查产出_1017/UI/D_FTUE_ChooseArmyDeck_dump.txt`
// · 窗参（根 MB 逐字段实读）= `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3133570412519170200.json`：
//     `type = 0`(Fullscreen) · `windowsPlacement = 15`(Popup) · `closeOnESC = 0` · `updateNavPanel = 0` ·
//     `extraScaleSmallScreen = 1.0` · `useDefaultCloseSoundIfNull = 1` · `closeSound = {0,0}` ·
//     **`openSound = PPtr(fileID 6, pid -1332049501696130703)`** —— 那个 cue 本地已解出，文件名
//     `素材/Warpforge原版/音频/音效库/MonoBehaviour/Draft Mode Progress Jingle_-1332049501696130703.json`
//     （`clipList = [Draft Mode GainSkull]` · `minPitch/maxPitch = 1/1` · `timeToPlayAgain = 0.1`）。
//   🔴 **播哪一组：FX（不是 Jingles）** —— 判据 = 第一权威 `GameWindow.TryOpen` 的指令流：
//     VA `0x180836120` 在 `Closed` 支里 `mov r8, [rbx+0x28]`（= `openSound`，字段偏移见 `dump.cs` GameWindow）
//     → `xor r9d, r9d` → `call 0x1806FA6F0`；而 `0x1806FA6F0` = **`SoundManager.Play2D(AudioCue, MixerType)`**
//     （`d:/2/tools/il2cpp_out/dump.cs:17901`）⇒ **`MixerType = 0 = FX`**（`MixerType.cs`）。
//     ⚠️ `工具/import_original_sfx.py:112` 那句注释写「（Jingles 组）」—— **与本指令流不符**，见报告；
//     我们按**指令流**接 FX（`WindowSounds.PlayFx`），与 `GameWindow.cs` 的 `WindowSounds.PlayFx` 那条既有口径一致。
// · `starterDecks`（4 副，逐个字段实读；`deck` PPtr 指向的 PrebuiltDeck 在同包的 prebuiltdecks 包里，
//   `UnityPy` 现读四份 SO）：
//     | # | `deckInfo.mTerm` | PrebuiltDeck | 督军卡 | glowColor |
//     |---|---|---|---|---|
//     | 1 | `TutorialData/TutorialFTUEDeck/UltraMarines` | `UM_SK_Starter` | `UM47` | (0.3224012, 0.6783406, 0.9905660) |
//     | 2 | `TutorialData/TutorialFTUEDeck/Orks`         | `ORK_SK_Starter`| `GOF29`| (0.7169812, 0.6966832, 0.1454254) |
//     | 3 | `TutorialData/TutorialFTUEDeck/BlackLegion`  | `BL_SK_Starter` | `BL35` | (0.7075472, 0.0700872, 0.0700872) |
//     | 4 | `TutorialData/TutorialFTUEDeck/Sutekh`       | `SAU_SK_Starter`| `SAU36`| (0.0, 0.7830189, 0.2969818) |
//   （`deck.deckHero.targetId` = 督军卡 id；`cardLibraryIds` 也读到了 —— 本窗**不画卡表**，
//    原版这一屏只画阵营徽记/名/描述/督军立绘 + 选中角标，卡组内容在 `Continue` 之后才发。）
// · 行为（第一权威 = 反编译 `d:/2/tools/decomp_full/`）：
//   · `FTUESelectDeckWindow__Open.c`：① `GameWindow.Open()` ② `container.gameObject.SetActive(false)`（隐藏模板）
//     ③ `for each starterDecks`：`FeatureConfig.isDeckHidden(deck)` 为假 ⇒ `Instantiate(container, content)` →
//     `container.Initialize(info, OnDeckSelected)` → 加进 `currentDeckContainers` → `SetActive(true)`
//     ④ `continueButton.gameObject.SetActive(false)` · `selectedFactionIcon.enabled = false` ·
//     `continueGroup.alpha = 0`（= **没选之前 Continue 那一组不亮**）
//   · `FTUESelectDeckWindow__OnDeckSelected.c`：`foreach (currentDeckContainers) if (c != selected) c.ToggleSelected(false)`
//     → `currentSelectedDeck = selected.CurrentDeck` → `RefreshContinueButton()`
//   · `FTUESelectDeckWindow__RefreshContinueButton.c`：`if (!fadeAnimationDone) { alpha 0 → DOFade(1, 0.5s) }` →
//     `continueButton.gameObject.SetActive(true)` · `selectedFactionIcon.enabled = true` ·
//     `selectedFactionIcon.sprite = ArmyUtilities.GetArmyIcon(army)` · 停掉上一声语音/播督军 `GetChatSound`
//     （`SoundManager.Play2D(..., MixerType 3 = Jingles)`）· `selectedFactionIconShine.Play()`
//   · `FTUESelectDeckWindow__OnContinueButtonClicked.c`：`PlayfabWrapper.GrantStarterDeck(deckClass, deck, OnSuccess, OnError)`
//     —— `OnSuccess` 发奖励、`CampaignHandler.SetSelectedArmy/SaveSelectedArmy`、加 `CollectedFirstDeck` tag、
//     `WindowsManager.CloseWindow(this)`；`OnError` = `CustomDebug.LogError` + 重试。
//   · `FTUESelectDeckWindow__.ctor.c`：`currentDeckContainers = new List<FTUSelectDeckContainer>()`（唯一初始化）。
// · 关窗：`Menu Dark Background/BackgroundCloseButton.window = 0`（实读）⇒ **点暗底不关窗**；
//   `closeOnESC = 0` ⇒ **ESC 也不关**。原版的唯一出口 = 选中一副 + `Continue`（成功后 `CloseWindow`）。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版） ============================
// · **阵营 id** = 督军卡在 `cards_engine.json` 里的 `faction`（UM47 → Ultramarines / GOF29 → Goff /
//   BL35 → BlackLegion / SAU36 → Sautekh）—— 等价于原版 `deck.deckHero.Reference.cardArmy`。
// · **阵营描述**：原版 `I2(TutorialData/TutorialFTUEDeck/<阵营>)`。
//   🔴 **2026-10-18 就地订正（铁律 5，N71）**：原文写「本机查不到（9,441 条零命中）⇒ 四格留空」——
//   **已过期**：`official_loc_13lang.json` 现读 **10,862 键**，四条 `TutorialFTUEDeck/*` **都在**（zh/en 齐全）
//   ⇒ 四格**有字**（取不到时才留空 + 出声一次）。
// · **领取起始卡组**：原版走 PlayFab（`PlayfabWrapper.GrantStarterDeck(选中那副的 deckId, …)`，服务端发牌、
//   客户端 `RewardService.Collect` 收进卡库）。我们**没有后端 / 没有玩家卡库** ⇒
//   🔴 **2026-10-22（H-D）改口径：本地发放** —— `Continue` 把**选中的那一副**按原版 SO 的
//   `cardLibraryIds / deckHero.targetId / gameMode / cardbackId` 写进 `DeckLibrary` 并落盘
//   （用户 2026-09-17 边界「不做真实经济 · 全解锁」允许；牌表是真值，⛔ 不是伪造）。
//   幂等（同督军+同牌表+同模式 ⇒ 不重复发）；失败则**出声且不关窗**（= 原版 `OnError`）。
//   ⚠️ 仍没做的：`CampaignHandler.SetSelectedArmy/SaveSelectedArmy`、`AddTag(CollectedFirstDeck)`、
//   服务端那份 `rewards/extraRewards`。逐句判据 → `OnContinueClicked` 的注释。
// · **`openSound`**：原版 = `GameWindow.TryOpen` 在 `Closed ⇒ Open` 那一拍播 `openSound`（FX 组，见上）。
//   我们这一侧 `GameWindow.openSound` 是 `AudioClip`（没有「cue 名」字段）⇒ 走 `WindowSounds` 的**唯一**
//   音效表（`WindowSounds.FtueOpenCue` 常量），在 `Open()` 里 `PlayFx`（= 同一个 FX 组、同一条 cue 表；
//   ⛔ 没在窗里另写一套 `Resources.Load`）。
// · **入口**：原版是 FTUE onboarding 的一环（首启送卡组）。
//   ⚠️ **2026-10-22 就地订正（铁律 5 · 本文件 H-D 现核）**：本节原写「我们**没有菜单版教程状态机**
//   （同 `TutorialMessagePopup` / `WelcomePopup` 的已知缺口）⇒ 只提供 `OpenChooseArmyDeck`、**不伪造入口**」
//   —— **不成立**：菜单版教程状态机早就在（`Shell/MenuTutorialManager.cs`，由
//   `Shell/MainMenuRuntime.cs:636` 的 `MenuTutorialManager.Attach(this)` 挂上；它的 `SelectArmy` 阶段
//   自己就调 `WindowsManager.OpenChooseArmyDeck()` —— `Shell/MenuTutorialFlow.cs:248` / `MenuTutorialManager.cs:405`）。
//   错因：这句话写在状态机落地之前，之后没回来改。⇒ 入口**不用伪造**，照那条链开。
// · **`Rays`/`Glow (1)` 粒子**：`War ParticleSystemUI Left/Right` 在 prefab 里 `m_LocalScale = (0,0,0)`
//   （dump 的 `ls=0,0` ⇒ 视觉 0×0）⇒ 原版这一刻**不画**；节点照建（树保真）、不加粒子。
// · **tween / 淡入**：原版有 DOTween（1 s 的 glow 缩放/淡入、0.5 s 的 Continue 组淡入）。
//   本仓**没有 tween 引擎** ⇒ 一律**直接落终值**（`FTUESelectDeckContainer.ToggleSelected` 与
//   `RefreshContinueButton` 的注释里各记一次；⛔ 不假装播过动画）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `FTUESelectDeckWindow`（`GameWindow` 子类）—— **选阵营送卡组**（「SELECT ARMY」整屏）。
    /// <para>🔴 层带：自成一档 **3392–3399**（`WelcomePopup` 3382–3390 之上、`DuelPopupWindow` 3400 之下）。</para>
    /// <para>🔴 入口：原版在 FTUE onboarding 链里 —— 我们的对位 = `Shell/MenuTutorialManager` 的 `SelectArmy` 阶段
    /// （⚠️ **2026-10-22 就地订正（铁律 5）**：本行原写「我们**没有那个状态机** ⇒ 只提供
    /// `OpenChooseArmyDeck`、⛔ 不伪造调用点」—— **不成立**，状态机早就在且自己会开这扇窗；
    /// 判据 → 文件头「入口」那一条）。</para></summary>
    public class FTUESelectDeckWindow : GameWindow
    {
        // ============================================================ 队列档（自成一档 · 3392–3399）
        public const int QShade = 3392,       // `Menu Dark Background`（压暗整屏；**不挂命中区**）
                         QTitle = 3393,       // `Title` / `SubTitle`
                         QCardArt = 3394,     // 每张阵营卡的 Glow / 立绘 / 徽记 / 阴影
                         QCardText = 3395,    // 每张阵营卡的字（阵营名 / 描述 / 选中角标）
                         QContinue = 3396;    // `Continue Group`（底图 + 字 + 已选阵营图标）
        /// <summary>窗内**命中区**那一档（每张卡的 `Select Button` + Continue 钮）。</summary>
        public const int QHit = 3399;

        /// <summary>原版 prefab 根名（= `Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。</summary>
        public const string RootName = "Choose Army Deck FTUE Window";

        // ============================================================ 词条键（prefab 上 `Localize.mTerm` 实读）
        /// <summary>`Title` 上的 `Localize.mTerm`（MB `MonoBehaviour_3324683907025919848.json`）。</summary>
        public const string TitleTerm = "MainMenu/FTUE/SelectArmy";
        /// <summary>`SubTitle` 上的 `Localize.mTerm`（MB `MonoBehaviour_-7330542681603252376.json`）。</summary>
        public const string SubTitleTerm = "MainMenu/FTUE/Description";
        /// <summary>`Continue Button/Text (TMP)` 上的 `Localize.mTerm`（MB `MonoBehaviour_-954081517076684952.json`）。</summary>
        public const string ContinueTerm = "Battle/Tips/Continue";
        /// <summary>`Selected Text/SelectedText` 上的 `Localize.mTerm`（MB `MonoBehaviour_1188082097155858280.json`）。</summary>
        public const string SelectedTerm = "MainMenu/General/Selected";

        // ============================================================ 几何（dump 现读 · 1920×1080 画布 px）
        /// <summary>`Menu Dark Background`（4574.60×2572.36，**以屏幕中心为中心**）。</summary>
        public const float ShadeL = -1327.3f, ShadeT = -746.2f, ShadeR = 3247.3f, ShadeB = 1826.2f;
        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.773f);   // Image.m_Color 实读

        /// <summary>`Title`：0,55 → 1920,165.349（fs80/基准80 · Center/**Midline** · 折行）。</summary>
        public const float TitleT = 55f, TitleB = 165.349f;
        public const float TitleFont = 80f, TitleFontMin = 80f, TitleFontMax = 80f, TitleFontBase = 80f;
        /// <summary>`SubTitle`：0,140 → 1920,219.423（fs50/基准45 · auto[18~50] · Center/**Midline**）。</summary>
        public const float SubT = 140f, SubB = 219.423f;
        public const float SubFont = 50f, SubFontMin = 18f, SubFontMax = 50f, SubFontBase = 45f;

        /// <summary>`Continue Group`（748.643×84.504 @ 585.7,969）。</summary>
        public const float GrpL = 585.7f, GrpT = 969f, GrpR = 1334.343f, GrpB = 1053.504f;
        /// <summary>`Continue Group/Text (TMP)`（217.19×85.51 @ 793.8,968.5；fs72/基准24 · auto[18~72] · Center/Middle）。</summary>
        public const float ContL = 793.8f, ContT = 968.5f, ContR = 1010.99f, ContB = 1054.01f;
        public const float ContFont = 72f, ContFontMin = 18f, ContFontMax = 72f, ContFontBase = 24f;
        /// <summary>`Continue Group/Selected Faction Icon`（105.3257² @ 1020.9,958.6；`preserveAspect`）。</summary>
        public const float IconL = 1020.9f, IconT = 958.6f, IconW = 105.3257f, IconH = 105.3257f;
        /// <summary>`Continue Button` 的底图（`40k_bt_underbutton` 485×83 · 九宫 (240,0,240,0) · Sliced · tint 白）。</summary>
        public const string ContinueArt = "40k_bt_underbutton";
        public const float ContinueTexW = 485f, ContinueTexH = 83f;

        /// <summary>`Armies`（1920×762.037 @ 0,186.1；`Image`+`ScrollRect` **两个组件都 `m_Enabled = 0`**）。</summary>
        public const float ArmiesT = 186.1f, ArmiesH = 762.037f;
        /// <summary>`Armies/Viewport`（1920×745.04）。</summary>
        public const float ViewportH = 745.04f;
        /// <summary>`Armies/Viewport/Content`（1920×522.019；`HorizontalLayoutGroup`）。</summary>
        public const float ContentH = 522.019f;
        /// <summary>`Content` 的 `HorizontalLayoutGroup`：`spacing = 106.58` · `padding = (L 70, R 44, T 0, B 0)` ·
        /// `childAlignment = 4 (MiddleCenter)` · `expandH = 1`（⇒ 子件高 = Content 高）。</summary>
        public const float HlgSpacing = 106.58f, HlgPadL = 70f, HlgPadR = 44f;
        /// <summary>`Content/Deck Container FTUE` 的宽（= `FTUESelectDeckContainer.CardW`）。</summary>
        public const float CardW = FTUESelectDeckContainer.CardW, CardH = FTUESelectDeckContainer.CardH;

        /// <summary>`Continue Group/War ParticleSystemUI Left/Right`（`m_LocalScale = 0` ⇒ 原版不画）。</summary>
        public const float WarLeftL = 574.8f, WarLeftT = 937.1f, WarLeftW = 148.364f, WarLeftH = 148.364f;
        public const float WarRightL = 1215.9f, WarRightT = 927.5f, WarRightW = 148.364f, WarRightH = 148.364f;

        // ============================================================ 数据
        /// <summary>一副起始卡组（= 原版 `FTUESelectDeckWindow.StarterDeckFTUEInfo` 里**本机取得到**的那几列）。</summary>
        public struct StarterDeck
        {
            /// <summary>原版 `deck` PPtr 指向的 PrebuiltDeck（`m_Name` / `deckId`，bundle 实读）。</summary>
            public string DeckId;
            /// <summary>督军卡 id（原版 `deckHero.targetId`，bundle 实读）。</summary>
            public string HeroCardId;
            /// <summary>原版 `deckInfo.mTerm`（= 这一格的阵营描述词条）。</summary>
            public string InfoTerm;
            /// <summary>原版 `glowColor`（MB 实读，四位小数以上的原值）。</summary>
            public Color Glow;

            // ---- 下面四列 = 那副 `PrebuiltDeck` **自己的字段**（不在 prefab 上）----
            // 判据（第一权威，可逐字段复查）：`素材/Warpforge原版/卡牌/卡组数据/MonoBehaviour/<deckId>.json`
            //   （原版 SO 的 `deckName` / `cardLibraryIds` / `cardbackId` / `gameMode` 逐字实读，2026-10-22）；
            //   交叉核对：`数据/游戏数据/decklists.json` 同名四行（`cardIds` / `heroId` / `gameMode` / `name`
            //   与 SO **逐一相等** —— 四副 × 四列全部 True）。
            /// <summary>原版 `PrebuiltDeck.deckName` —— 发放后这副卡组在存档里的名字。</summary>
            public string DeckName;
            /// <summary>原版 `PrebuiltDeck.cardLibraryIds`（12 张 · **原序**，⛔ 别重排）。</summary>
            public string[] CardIds;
            /// <summary>原版 `PrebuiltDeck.cardbackId` 解出来的卡背名（`Art/cardbacks/` 里的文件名）。
            /// 四副的 GUID 都是 `025b325b599140545881b3552164e345` ⇒ `Cardback_All_Starter`（本工程有这张图）。</summary>
            public string Cardback;
            /// <summary>原版 `PrebuiltDeck.gameMode`（四副都是 **13 = Skirmish**，SO 实读）。</summary>
            public int GameMode;
        }

        /// <summary>四副起始卡组 —— **顺序 = prefab `starterDecks` 数组序**（左→右）。
        /// 🔴 逐列的判据见文件头那张表（`glowColor` 是 MB 里 32 位 float 的原值，⛔ 别四舍五入成「好看」的数）。
        /// 🔴 后四列（牌表/卡背/模式/原名）的判据见 <see cref="StarterDeck"/> 上那段 —— 是**原版 SO** 的值。</summary>
        public static readonly StarterDeck[] Starters =
        {
            new StarterDeck { DeckId = "UM_SK_Starter",  HeroCardId = "UM47",  InfoTerm = "TutorialData/TutorialFTUEDeck/UltraMarines",
                              Glow = new Color(0.3224012f, 0.6783406f, 0.9905660f, 1f),
                              DeckName = "Master of the Fleet", Cardback = "Cardback_All_Starter", GameMode = 13,
                              CardIds = new[] { "UM1", "UM6", "UM56", "UM14", "UM10", "UM15",
                                                "UM24", "UM25", "UM30", "UM41", "UM42", "UM39" } },
            new StarterDeck { DeckId = "ORK_SK_Starter", HeroCardId = "GOF29", InfoTerm = "TutorialData/TutorialFTUEDeck/Orks",
                              Glow = new Color(0.7169812f, 0.6966832f, 0.1454254f, 1f),
                              DeckName = "Da Red Waaagh", Cardback = "Cardback_All_Starter", GameMode = 13,
                              CardIds = new[] { "GOF1", "GOF55", "GOF8", "GOF9", "GOF14", "GOF13",
                                                "GOF16", "GOF33", "GOF43", "GOF40", "GOF22", "GOF25" } },
            new StarterDeck { DeckId = "BL_SK_Starter",  HeroCardId = "BL35",  InfoTerm = "TutorialData/TutorialFTUEDeck/BlackLegion",
                              Glow = new Color(0.7075472f, 0.0700872f, 0.0700872f, 1f),
                              DeckName = "Helspear Assault", Cardback = "Cardback_All_Starter", GameMode = 13,
                              CardIds = new[] { "BL4", "BL13", "BL63", "BL23", "BL24", "BL26",
                                                "BL33", "BL37", "BL9", "BL30", "BL46", "BL50" } },
            new StarterDeck { DeckId = "SAU_SK_Starter", HeroCardId = "SAU36", InfoTerm = "TutorialData/TutorialFTUEDeck/Sutekh",
                              Glow = new Color(0.0000000f, 0.7830189f, 0.2969818f, 1f),
                              DeckName = "Relentless March", Cardback = "Cardback_All_Starter", GameMode = 13,
                              CardIds = new[] { "SAU8", "SAU16", "SAU22", "SAU26", "SAU65", "SAU24",
                                                "SAU28", "SAU31", "SAU41", "SAU40", "SAU56", "SAU19" } },
        };

        /// <summary>一张阵营卡**建出来之后**要的那一份内容（图与文案都已解析好；容器只负责画）。</summary>
        public struct DeckView
        {
            public string DeckId, HeroCardId, Army, InfoTerm;
            public string ArmyName;        // 原版 `GameStaticData.ArmyToLocalizedString(army)` 的结果
            public string Description;     // 原版 `I2(deckInfo.mTerm)`；本机查不到 ⇒ null
            public Color Glow;
            public Texture2D ArmyIcon;     // 原版 `ArmyIconsSO.GetArmyIcon(army)`
            public string ArmyIconName;    // 图名（`DeckRuntime.FactionIcon` 的输出；自检读口）
            public Texture2D HeroArt;      // 原版 `deckHero.Reference.fullHeroSprite`
        }

        /// <summary>原版 `Selected Text` 上的那颗 TMP 文案（词条 `MainMenu/General/Selected`）。</summary>
        public const string SelectedText = "Selected";

        /// <summary>最近一次开出来的那一扇（自检读口）。</summary>
        public static FTUESelectDeckWindow LastOpened { get; private set; }

        // 自检读口
        Transform _shade, _title, _subTitle, _continueGroup, _continueButton, _armies, _viewport, _content;
        Label _titleLabel, _subTitleLabel, _continueLabel;
        ImageQuad _continueBg, _selectedFactionIcon;
        Transform _continueHit, _selectedFactionNode;
        readonly System.Collections.Generic.List<FTUESelectDeckContainer> _cards =
            new System.Collections.Generic.List<FTUESelectDeckContainer>();
        /// <summary>本窗取不到的图（自检读口 —— 非空就是「有件根本没画」）。</summary>
        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        StarterDeck[] _decks = Starters;
        FTUESelectDeckContainer _selected;
        bool _continueShown;
        readonly System.Collections.Generic.List<DeckView> _views = new System.Collections.Generic.List<DeckView>();

        // ============================================================ 建窗
        /// <summary>建一扇（`WindowsManager.OpenChooseArmyDeck` 的 `create` 那一跳）。
        /// <para>窗参逐字段照根 MB 实读：`type = 0 Fullscreen` · `windowsPlacement = 15 Popup` ·
        /// **`closeOnESC = 0`** · `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0` ·
        /// `useDefaultCloseSoundIfNull = 1` · `closeSound = null`（⇒ 关窗音走家族默认 `ButtonClick Back`）。</para></summary>
        public static FTUESelectDeckWindow Create(WindowsManager mgr, StarterDeck[] decks = null)
        {
            var go = new GameObject(RootName);
            var win = go.AddComponent<FTUESelectDeckWindow>();
            win.type = WindowType.Fullscreen;              // 实读 type = **0**
            win.placement = WindowsPlacement.Popup;        // 实读 windowsPlacement = 15
            win.closeOnEsc = false;                        // 实读 closeOnESC = **0**
            win.updateNavPanel = false;                    // 实读 updateNavPanel = 0
            win.extraScaleSmallScreen = 1f;                // 实读 1.0（= 不覆盖）
            win.useDefaultCloseSoundIfNull = true;         // 实读 1；closeSound 是空 PPtr
            win.Manager = mgr;
            win._decks = (decks != null && decks.Length > 0) ? decks : Starters;
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[FTUE] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            // 原版 `openSound`（= `Draft Mode Progress Jingle`，FX 组 —— 判据见文件头那条指令流）。
            // ⚠️ 我们这一侧 `GameWindow.openSound` 是 `AudioClip`、没有「cue 名」字段 ⇒ 走 `WindowSounds`
            //    那唯一一张 cue 表（⛔ 没在窗里另写一套取音）。
            WindowSounds.PlayFx(WindowSounds.FtueOpenCue, "[FTUE] `Choose Army Deck FTUE Window` 开窗音");
            Build();
        }

        // ============================================================ 建整棵树（顺序 = prefab 兄弟序）
        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            _cards.Clear();
            _views.Clear();
            _selected = null;
            _continueShown = false;
            _shade = _title = _subTitle = _continueGroup = _continueButton = null;
            _armies = _viewport = _content = null;
            _titleLabel = _subTitleLabel = _continueLabel = null;
            _continueBg = _selectedFactionIcon = null;
            _continueHit = _selectedFactionNode = null;

            // ---- 1) `Menu Dark Background`（压暗整屏；`BackgroundCloseButton.window = 0` ⇒ 点它**不**关窗）----
            _shade = MenuDraw.Node(transform, "Menu Dark Background",
                                   new PxRect(ShadeL, ShadeT, ShadeR, ShadeB));
            MenuDraw.Rect(_shade, CardArt.Solid(), new PxRect(ShadeL, ShadeT, ShadeR, ShadeB), "Image",
                          QShade, ShadeTint);

            // ---- 2) `Title`（'SELECT ARMY' · fs80/基准80 · Center/Midline）----
            _title = MenuDraw.Node(transform, "Title", new PxRect(0f, TitleT, 1920f, TitleB));
            _titleLabel = MenuDraw.Text(_title, new PxRect(0f, TitleT, 1920f, TitleB),
                                        OfficialTerm(TitleTerm, "SELECT ARMY", "选择军队"),
                                        Color.white, "EverguildTextMeshPro",
                                        TitleFont, QTitle, 1920f, TitleFontMin, TitleFontMax, TitleFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_titleLabel, Label.VAlign.Midline, new PxRect(0f, TitleT, 1920f, TitleB));

            // ---- 3) `SubTitle`（fs50/基准45 · auto[18~50] · Center/Midline）----
            _subTitle = MenuDraw.Node(transform, "SubTitle", new PxRect(0f, SubT, 1920f, SubB));
            _subTitleLabel = MenuDraw.TextBox(_subTitle, new PxRect(0f, SubT, 1920f, SubB),
                                              OfficialTerm(SubTitleTerm,
                                                  "You will receive a full deck and a Warlord from the selected army.",
                                                  "您将从所选军队中获得一副完整的卡组和一位战将。"),
                                              Color.white, "EverguildTextMeshPro",
                                              SubFont, SubFontMin, QTitle, SubFontMax, SubFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(_subTitleLabel, Label.VAlign.Midline, new PxRect(0f, SubT, 1920f, SubB));

            // ---- 4) `Continue Group`（没选之前**整组不亮**：原版 Open 把 Continue Button 关掉 + alpha 0）----
            _continueGroup = MenuDraw.Node(transform, "Continue Group",
                                           new PxRect(GrpL, GrpT, GrpR, GrpB));
            BuildWarParticles();

            var contR = new PxRect(GrpL, GrpT, GrpR, GrpB);
            _continueButton = MenuDraw.Node(_continueGroup, "Continue Button", contR);
            var contTex = Tex(ContinueArt, "`Continue Button` 的底（九宫 240,0,240,0）");
            if (contTex != null)
            {
                // `m_Type = 1 (Sliced)` · `m_Border = (240,0,240,0)` · 贴图 485×83 · tint 白 ⇒ 九宫
                var contGo = MenuDraw.Nine(_continueButton, contTex, contR, new Vector4(240f, 0f, 240f, 0f),
                                           ContinueTexW, ContinueTexH, QContinue, Color.white, true, "Image");
                if (contGo != null) _continueBg = contGo.GetComponentInChildren<ImageQuad>();
            }
            else MenuDraw.Node(_continueButton, "Image", contR);    // 图缺 ⇒ 节点照建、不画（已出声）
            _continueLabel = MenuDraw.TextBox(_continueButton,
                                              new PxRect(ContL, ContT, ContR, ContB),
                                              OfficialTerm(ContinueTerm, "Continue", "继续"),
                                              Color.white, "Text (TMP)",
                                              ContFont, ContFontMin, QContinue, ContFontMax, ContFontBase, face: TmpFont.Face.LatinCardBody);
            _selectedFactionNode = MenuDraw.Node(_continueButton, "Selected Faction Icon",
                                                 new PxRect(IconL, IconT, IconL + IconW, IconT + IconH));
            _selectedFactionNode.gameObject.SetActive(false);       // 原版 `image.enabled = false` 的等价物
            // 命中区（`EverguildButton`；原版 `m_Transition = 1` ⇒ 走默认悬停色偏）
            _continueHit = MenuDraw.Hit(_continueButton, "Hit", contR, QHit, OnContinueClicked,
                                        clickCue: WindowSounds.DefaultButtonCue);
            // 出厂态：`continueButton.gameObject.SetActive(false)`（`Open()` 的最后几句之一）
            _continueButton.gameObject.SetActive(false);

            // ---- 5) `Armies` → `Viewport` → `Content`（ScrollRect/Image 两个组件都 `m_Enabled = 0` ⇒ 不滚动）----
            _armies = MenuDraw.Node(transform, "Armies", new PxRect(0f, ArmiesT, 1920f, ArmiesT + ArmiesH));
            _viewport = MenuDraw.Node(_armies, "Viewport", new PxRect(0f, ArmiesT, 1920f, ArmiesT + ViewportH));
            _content = MenuDraw.Node(_viewport, "Content", new PxRect(0f, ArmiesT, 1920f, ArmiesT + ContentH));
            BuildCards();

            if (MissingArt.Count > 0)
                Debug.LogWarning("[FTUE] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>`War ParticleSystemUI Left/Right` —— prefab 里 `m_LocalScale = (0,0,0)`（dump `ls=0,0`）
        /// ⇒ 原版**这一刻不画**。节点照建（树保真），不加粒子。</summary>
        void BuildWarParticles()
        {
            var l = MenuDraw.Node(_continueGroup, "War ParticleSystemUI Left",
                                  new PxRect(WarLeftL, WarLeftT, WarLeftL + WarLeftW, WarLeftT + WarLeftH));
            MenuDraw.Node(l, "Rays", new PxRect(WarLeftL, WarLeftT, WarLeftL + 100f, WarLeftT + 100f));
            l.localScale = Vector3.zero;      // 原版 m_LocalScale = 0（**建完子件再置零**，免得 `PosInDesignSpace` 进退化档）
            var r = MenuDraw.Node(_continueGroup, "War ParticleSystemUI Right",
                                  new PxRect(WarRightL, WarRightT, WarRightL + WarRightW, WarRightT + WarRightH));
            MenuDraw.Node(r, "Rays", new PxRect(WarRightL, WarRightT, WarRightL + 100f, WarRightT + 100f));
            r.localScale = Vector3.zero;      // 原版 m_LocalScale = 0
        }

        /// <summary>按 `Content` 的 `HorizontalLayoutGroup` 把 4 张阵营卡摆开
        /// （`spacing 106.58` · `pad L70/R44/T0/B0` · `childAlignment = MiddleCenter` · `expandH = 1`）。
        /// ⚠️ 我们这套**没有 uGUI 布局组** ⇒ 位置按 uGUI 那套算式**自己算**（算式只有这一处）。</summary>
        void BuildCards()
        {
            int n = _decks.Length;
            float total = n * CardW + (n - 1) * HlgSpacing;
            float usable = 1920f - HlgPadL - HlgPadR;
            float x = HlgPadL + (usable - total) * 0.5f;      // MiddleCenter = 居中
            var pool = RuleEngine.CardDatabase.Load();
            for (int i = 0; i < n; i++)
            {
                var view = MakeView(_decks[i], pool);
                _views.Add(view);
                var card = new PxRect(x, ArmiesT, x + CardW, ArmiesT + CardH);
                _cards.Add(FTUESelectDeckContainer.Create(_content, card, view, OnDeckSelected, MissingArt));
                x += CardW + HlgSpacing;
            }
        }

        /// <summary>把原版 `StarterDeckFTUEInfo` 解析成「画一张卡要的东西」。
        /// <para>⚠️ 每一处取不到都**出声**（红线），⛔ 不拿别的字/图顶上。</para></summary>
        DeckView MakeView(StarterDeck d, System.Collections.Generic.List<RuleEngine.CardDef> pool)
        {
            var v = new DeckView
            {
                DeckId = d.DeckId, HeroCardId = d.HeroCardId, InfoTerm = d.InfoTerm, Glow = d.Glow,
            };
            var hero = RuleEngine.CardDatabase.FindById(pool, d.HeroCardId);
            if (hero == null)
            {
                Debug.LogWarning("[FTUE] 卡表里没有督军 `" + d.HeroCardId + "`（`" + d.DeckId + "`）⇒ "
                               + "这一张的**阵营徽记/阵营名/立绘全留空**（⛔ 不编）");
            }
            else
            {
                v.Army = hero.Faction;
                v.ArmyName = string.IsNullOrEmpty(hero.Faction) ? "" : Loc.T(CardText.ArmyTermPrefix + hero.Faction);
                v.ArmyIconName = string.IsNullOrEmpty(hero.Faction) ? null : DeckRuntime.FactionIcon(hero.Faction);
                v.ArmyIcon = string.IsNullOrEmpty(v.ArmyIconName) ? null : CardArt.MenuUi(v.ArmyIconName);
                if (v.ArmyIcon == null && !string.IsNullOrEmpty(v.ArmyIconName))
                    Debug.LogWarning("[FTUE] 阵营徽记取不到：`" + v.ArmyIconName + "`（" + hero.Faction + "）⇒ `Army Icon` 不画");
                v.HeroArt = HeroArt(hero);
            }

            // 原版 `armyDescription.text = I2(deckInfo.mTerm)`。
            // 🔴 **2026-10-18 就地订正（铁律 5，N71）**：原文写「官方表里没有 `TutorialFTUEDeck/*`（9441 条零命中）」
            //   —— **已过期**：现表 **10,862 键**、四条都在 ⇒ 正常情况下 `Loc.T` / `Lang13Of` 都能取到；
            //   下面那条「都取不到 ⇒ 留空 + 出声」只在**真缺键**时才走（⛔ 仍不编描述）。
            if (Loc.HasEntry(d.InfoTerm))
                v.Description = Loc.T(d.InfoTerm);
            else
            {
                string v13 = Loc.Lang13Of(d.InfoTerm, Loc.Current == AvailableLanguages.Chinese ? "zh" : "en");
                v.Description = v13;                      // 有就用（本机现读 = null）
            }
            if (string.IsNullOrEmpty(v.Description))
            {
                if (_warnedDesc.Add(d.InfoTerm))
                    Debug.LogWarning("[FTUE] 阵营描述词条 `" + d.InfoTerm + "` 在 `Loc.Table` 与官方 13 语言表里"
                                   + "**都取不到** ⇒ `Army Description` 那一格**留空**（⛔ 不编描述）。"
                                   + "来源 = prefab `starterDecks[].deckInfo`，本机无文本。");
            }
            return v;
        }

        /// <summary>督军立绘：先按原版 `fullHeroSprite` 那一档取**方形整身立绘**
        /// （本地命名 = `UI_Deck_Warlord_<卡名，空格换下划线>`，例 `UI_Deck_Warlord_Uriel_Ventris` 2048²）；
        /// 取不到才退回 `CardArt.Portrait(id)`（卡面插图，竖构图）。
        /// 🔴 **退回那一档是我们挑的兜底**，不是原版做法（文件头已记）。</summary>
        public static Texture2D HeroArt(RuleEngine.CardDef hero)
        {
            if (hero == null) return null;
            string n = (hero.Name ?? "").Replace(' ', '_');
            var full = string.IsNullOrEmpty(n) ? null : CardArt.MenuUi("UI_Deck_Warlord_" + n);
            if (full != null) return full;
            return CardArt.Portrait(hero.Id);
        }

        // ============================================================ 选阵营 / Continue
        /// <summary>= 原版 `OnDeckSelected(container)`：**其它卡全部取消选中** → 记住这一副 →
        /// `RefreshContinueButton()`。</summary>
        void OnDeckSelected(FTUESelectDeckContainer picked)
        {
            foreach (var c in _cards) if (c != null && c != picked) c.ToggleSelected(false);
            _selected = picked;
            RefreshContinueButton();
        }

        /// <summary>**自检 / 调用方口**：选中第 <paramref name="index"/> 张阵营卡（0 起）。
        /// <para>逐句等价于点那张卡的 `Select Button`（原版那个 lambda）：先 `ToggleSelected(true)`、
        /// 再 `OnDeckSelected(this)`（= 其它卡取消选中 + 刷新 Continue）。</para></summary>
        public void SelectDeck(int index)
        {
            if (index < 0 || index >= _cards.Count) return;
            var c = _cards[index];
            if (c == null) return;
            c.ToggleSelected(true);
            OnDeckSelected(c);
        }

        /// <summary>= 原版 `RefreshContinueButton()`：`continueButton` 亮起来 + `selectedFactionIcon` 换成
        /// 该阵营的徽记（原版 `ArmyUtilities.GetArmyIcon(army)`）。
        /// <para>⚠️ 原版那两段动画（`continueGroup` 0.5 s 淡入、`selectedFactionIconShine` 闪烁）本仓没有 tween
        /// ⇒ **直接落终值**（如实记）。原版还会播督军 `GetChatSound`（Jingles 组）—— 我们没有那条
        /// 「卡 → 语音」链（`RawCardScript.GetChatSound` 在远端音频表），⛔ 不编一声顶上。</para></summary>
        void RefreshContinueButton()
        {
            if (_selected == null) return;
            if (!_continueShown && _continueButton != null)
            {
                _continueButton.gameObject.SetActive(true);   // 原版 `continueButton.gameObject.SetActive(true)`
                _continueShown = true;
            }
            var view = _selected.View;
            if (_selectedFactionNode != null)
            {
                _selectedFactionNode.gameObject.SetActive(true);   // 原版 `image.enabled = true`
                var r = new PxRect(IconL, IconT, IconL + IconW, IconT + IconH);
                if (_selectedFactionIcon == null)
                {
                    if (view.ArmyIcon != null)
                        _selectedFactionIcon = MenuDraw.Rect(_selectedFactionNode, view.ArmyIcon, r, "Image",
                                                             QContinue, null, true);   // 实读 `preserveAspect`
                }
                else if (view.ArmyIcon != null)
                {
                    // 换了一副（再点另一张卡）⇒ 换图（本节点只建一次，图跟着走）
                    _selectedFactionIcon.SetTexture(view.ArmyIcon, true);   // `m_PreserveAspect` 实读 = 1
                }
            }
        }

        /// <summary>= 原版 `OnContinueButtonClicked`。
        ///
        /// <para>**原版这一跳**（第一权威 = 反编译，逐句实读）：
        /// `PlayfabWrapper.GrantStarterDeck(deckId, campaign, OnSuccess, OnError)`
        /// （`decomp_full/FTUESelectDeckWindow__OnContinueButtonClicked.c:56`）。第一个实参 =
        /// **选中的那一副的 `PrebuiltDeck.deckId`**：`window+0xb0` 是 `OnDeckSelected` 存进去的
        /// `container.CurrentDeck`（`FTUESelectDeckWindow__OnDeckSelected.c:52`），`+0x48` = `PrebuiltDeck.deckId`
        /// （`PrebuiltDeck__get_LocalizedName.c:15` 读 `+0x48` 当 deckId 可交叉印证）；
        /// stub 签名 `GrantStarterDeck(string deckId, Campaign, Action&lt;GrantStarterDeckResult&gt;, Action&lt;string&gt;)`
        /// 逐字对上（`Assembly-CSharp/PlayfabWrapper.cs:144`）⇒ **原版发的就是玩家在窗里选的那一副**，
        /// 不是一张固定表。服务端回 `GrantStarterDeckResult{rewards, army, deck, extraRewards}`，
        /// `OnSuccess` 里 `RewardService.Collect(extraRewards.Append(deck))` 把**这副卡组**收进卡库
        /// （`…OnContinueButtonClicked_g__OnSuccess_1.c:47-51`），末句 `WindowsManager.CloseWindow(this)`；
        /// `OnError` = `CustomDebug.LogError` + 重新调一次发放、**不关窗**（`…g__OnError_2.c:20-33`）。</para>
        ///
        /// <para>🔴 **我们这一版（单机）**：没有 PlayFab / 没有玩家卡库 ⇒ 本地等价物 = 把**同一副**
        /// （牌表/督军/模式/卡背全部来自原版 SO，见 <see cref="Starters"/> 后四列的判据）
        /// **写进 `DeckLibrary` 并落盘**。用户 2026-09-17 定的边界「不做真实经济 · 卡池/卡组全解锁」
        /// ⇒ 本地发放是允许的；发的是原版那副的**真牌表**，⛔ 不是伪造。
        /// **没做的那一半（如实记）**：`CampaignHandler.SetSelectedArmy/SaveSelectedArmy`（我们没有“当前战役势力”）、
        /// `AddTag(CollectedFirstDeck)`（没有 tag/成就存档）、服务端那份 `rewards/extraRewards`。</para></summary>
        void OnContinueClicked()
        {
            if (_selected == null) return;      // 原版也走不到（没选时 Continue 是关着的）
            var v = _selected.View;
            int si = StarterIndex(v.DeckId);
            if (si < 0 || _decks[si].CardIds == null || _decks[si].CardIds.Length == 0)
            {
                // 走到这里 = 调用方递了一份**不带牌表**的自定义 `StarterDeck[]`（`Create(mgr, decks)` 那个口）。
                // 原版没有这一态（牌表在 SO 上）；⛔ 不编一副顶上 —— 出声，然后仍然关窗
                // （`closeOnESC = 0` + 暗底不关窗 ⇒ 不关就是一扇死窗，见文件头）。
                Debug.LogError("[FTUE] 起始卡组 `" + v.DeckId + "` **没有牌表**（`Starters` 之外的调用方？）"
                             + " ⇒ 什么都没发；窗照常关掉（否则这扇窗没有出口）。");
                Close();
                return;
            }

            string why;
            var r = GrantStarter(_decks[si], out why);
            if (r == StarterGrant.Failed)
            {
                // = 原版 `OnError`：出声、**不关窗**。
                // ⚠️ 原版那条会自动重发一次；我们这两类失败（校验 / 写盘）重发不会变 ⇒ 不自动重试，
                //    由玩家再点一次 Continue（出口语义照旧：窗留着）。
                Debug.LogError("[FTUE] 起始卡组 `" + v.DeckId + "` **没发出去**：" + why
                             + " ⇒ 窗保持打开（= 原版 `OnError` 不关窗）。");
                return;
            }
            Close();      // = 原版成功支末句 `WindowsManager.CloseWindow(this)`
        }

        // ============================================================ 起始卡组发放（本地等价物）
        enum StarterGrant { Granted, AlreadyGranted, Failed }

        /// <summary>原版 `PlayfabWrapper.GrantStarterDeck` 的**本地等价物**：把这一副写进卡组库并落盘。
        ///
        /// <para>⚠️ **走 `DeckLibrary.Insert`（唯一的入库口）** —— 它内部就 `return Save();`
        /// （`RuleEngine/Data/DeckLibrary.cs:206-212`），⛔ 别在调用点再 `Save()` 一次（那会把返回值吞掉，
        /// 同 A398 那条口径）。</para>
        ///
        /// <para>⚠️ **幂等**：库里已有「督军 + 牌表（多重集）+ 模式」完全相同的一副 ⇒ 不重复插。
        /// 判据**不看名字** —— 名字随语档变（中文档取到官方中文名，英文档取英文名）⇒ 拿名字当键会在切语言后
        /// 重复发一副（同 `CollectionData.CreateDeck` 那条 A1037 的坑）。</para></summary>
        StarterGrant GrantStarter(StarterDeck d, out string why)
        {
            why = null;
            var pool = RuleEngine.CardDatabase.Load();

            // 名字：官方词条 `PrebuiltDecks/<deckId>`（= 原版 `PrebuiltDeck.LocalizedName` 的前缀 + deckId；
            //   判据 → `decomp_full/PrebuiltDeck__get_LocalizedName.c:15` + `dump.cs:22915-22916`）。
            //   取法与 `MakeView` 取 `deckInfo` 同一套（`Loc.T` 手写表 → 官方 13 语言表）；
            //   都取不到才回落原版 SO 的 `deckName`（那也是原版自己的字段，⛔ 不是我们编的串）。
            string key = "PrebuiltDecks/" + d.DeckId;
            string name = Loc.HasEntry(key) ? Loc.T(key) : null;
            if (string.IsNullOrEmpty(name))
                name = Loc.Lang13Of(key, Loc.Current == AvailableLanguages.Chinese ? "zh" : "en");
            if (string.IsNullOrEmpty(name)) name = d.DeckName;

            // 防御卡：原版 `PrebuiltDeck` **没有防御卡字段**（`cardLibraryIds` 只有 12 张；防御卡在运行期
            //   `CardDeck.defensiveCard(+0x48)`）⇒ 我们**也不带**（`DeckRules.Validate` 2026-09-26 起允许 0 张，
            //   兜底那张由 `DeckBuilder.FromDeck` 开局发 —— 与原版「带了用带的、不带就发一张」一致）。
            //   ⛔ 别照 `prebuilt_decks.json` 那批「我们补一张」的做法（那是预组页的口径，判据在那边）。
            var pd = new RuleEngine.PlayerDeck(name, d.HeroCardId, null, d.CardIds, d.GameMode);
            pd.CardbackId = d.Cardback;

            // 入库前先过一遍引擎自己的校验 —— 牌表是原版 SO 抄来的，**抄错/卡池换代**时在这里当场炸出来，
            //   而不是写进玩家存档变成一副打不了的牌（红线：不许静默失败）。
            var err = RuleEngine.DeckRules.Validate(
                pd, id => RuleEngine.CardDatabase.FindById(pool, id),
                d.GameMode == (int)RuleEngine.GameMode.Skirmish);
            if (err != RuleEngine.DeckError.None)
            {
                why = "这副牌**过不了** `DeckRules.Validate`（" + err + "）—— 卡池 id 换代 / 表抄错？";
                return StarterGrant.Failed;
            }

            var lib = RuleEngine.DeckLibrary.Load();          // 每次从磁盘重读（`Load()` 就是 `new` 一份）
            int same = IndexOfSameDeck(lib, pd);
            if (same >= 0)
            {
                LastGrantedDeckId = d.DeckId;
                Debug.Log("[FTUE] 起始卡组 `" + d.DeckId + "` 已经在卡组库里（第 " + (same + 1) + " 套）"
                        + " ⇒ **不重复发放**（幂等）。");
                return StarterGrant.AlreadyGranted;
            }
            if (!lib.Insert(pd))          // 入库 + 落盘（成败从返回值读，⛔ 不吞）
            {
                why = "**写不进存档**：" + (string.IsNullOrEmpty(lib.LastError) ? "(没给原因)" : lib.LastError);
                return StarterGrant.Failed;
            }

            // 收藏线有一份**进程级缓存**（`CollectionData.Lib`）—— 刚写盘的那一套要让它下次重读，
            //   否则回收藏页画的还是发放前那份列表（作废口的理由 → `CollectionData.InvalidateLibrary` 的注释）。
            CollectionData.InvalidateLibrary();
            LastGrantedDeckId = d.DeckId;
            Debug.Log("[FTUE] 起始卡组已发放：`" + d.DeckId + "` ⇒ 卡组「" + pd.Name + "」"
                    + "（督军 " + pd.WarlordId + " · " + pd.CardIds.Count + " 张 · 模式 " + pd.GameMode + "）"
                    + " 已写进 `" + RuleEngine.DeckStore.Path + "`");
            return StarterGrant.Granted;
        }

        /// <summary>库里的下标 —— **督军 + 牌表（多重集）+ 模式** 全同才算同一副（⛔ 不看名字，理由见 `GrantStarter`）。</summary>
        static int IndexOfSameDeck(RuleEngine.DeckLibrary lib, RuleEngine.PlayerDeck want)
        {
            for (int i = 0; i < lib.Count; i++)
            {
                var d = lib.Decks[i];
                if (d == null) continue;
                if (d.WarlordId != want.WarlordId) continue;
                if (d.GameMode != want.GameMode) continue;
                if (!SameCards(d.CardIds, want.CardIds)) continue;
                return i;
            }
            return -1;
        }

        /// <summary>两份牌表是不是**同一多重集**（顺序无关；原版牌序对发牌没影响，但 ⛔ 我们在别处也不重排）。</summary>
        static bool SameCards(System.Collections.Generic.IList<string> a, System.Collections.Generic.IList<string> b)
        {
            int na = a != null ? a.Count : 0, nb = b != null ? b.Count : 0;
            if (na != nb) return false;
            var left = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
            for (int i = 0; i < na; i++)
            {
                string id = a[i] ?? "";
                int n; left.TryGetValue(id, out n); left[id] = n + 1;
            }
            for (int i = 0; i < nb; i++)
            {
                string id = b[i] ?? "";
                int n;
                if (!left.TryGetValue(id, out n) || n <= 0) return false;
                left[id] = n - 1;
            }
            return true;
        }

        /// <summary>`_decks` 里 `deckId` 的下标（查不到 −1）。</summary>
        int StarterIndex(string deckId)
        {
            for (int i = 0; i < _decks.Length; i++)
                if (_decks[i].DeckId == deckId) return i;
            return -1;
        }

        /// <summary>**自检 / 调用方口**：逐句等价于点 `Continue Button`（原版那个 `OnContinueButtonClicked`）。
        /// ⚠️ 与 `SelectDeck(index)` 同形：**加出口、不改契约**（按钮回调仍是 `OnContinueClicked`）。</summary>
        public void ContinueForTest() { OnContinueClicked(); }

        /// <summary>最后一次**成功发放**（含「已经在库里 ⇒ 不重复发」）的 `deckId`；没发过 = null。
        /// 判据 = `Starters` 里那一副的 `DeckId` —— `PlayerDeck` 没有 deckId 字段（那条老账见
        /// `CollectionData` 文件头），所以这个读口是「发的是哪一副」的唯一直接证据。</summary>
        public string LastGrantedDeckId { get; private set; }

        // ============================================================ 文案
        static readonly System.Collections.Generic.HashSet<string> _warnedDesc =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>取一条官方词条：`Loc.Table` 优先 → 官方 13 语言表 → 文件内兜底。
        /// 🔴 **转调 `TutorialMessagePopup.OfficialTerm`**（全工程唯一一份取法），⛔ 不在这里抄第二份。</summary>
        static string OfficialTerm(string key, string en, string zh)
        {
            return TutorialMessagePopup.OfficialTerm(key, en, zh);
        }

        // ============================================================ 取图
        Texture2D Tex(string name, string what)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && MissingArt != null && !MissingArt.Contains(name))
            {
                MissingArt.Add(name);
                Debug.LogWarning("[FTUE] 图取不到：`" + name + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。导入器："
                               + "`工具/import_original_art.py` 的 `MENU_IMAGES` / `工具/sync_battle_ui_art.py`");
            }
            return t;
        }

        // ============================================================ 自检读口
        public Transform ShadeNode { get { return _shade; } }
        public Transform TitleNode { get { return _title; } }
        public Transform SubTitleNode { get { return _subTitle; } }
        public Transform ContinueGroupNode { get { return _continueGroup; } }
        public Transform ContinueButtonNode { get { return _continueButton; } }
        public Transform ContinueHit { get { return _continueHit; } }
        public Transform SelectedFactionNode { get { return _selectedFactionNode; } }
        public Transform ArmiesNode { get { return _armies; } }
        public Transform ViewportNode { get { return _viewport; } }
        public Transform ContentNode { get { return _content; } }
        public Label TitleLabel { get { return _titleLabel; } }
        public Label SubTitleLabel { get { return _subTitleLabel; } }
        public Label ContinueLabel { get { return _continueLabel; } }
        public ImageQuad ContinueBg { get { return _continueBg; } }
        public ImageQuad SelectedFactionIcon { get { return _selectedFactionIcon; } }
        public System.Collections.Generic.List<FTUESelectDeckContainer> Cards { get { return _cards; } }
        /// <summary>解析好的四份内容（自检读口；顺序同 <see cref="Starters"/>）。</summary>
        public System.Collections.Generic.List<DeckView> Views { get { return _views; } }
        /// <summary>当前选中的那一张（没选 = null）。</summary>
        public FTUESelectDeckContainer SelectedCard { get { return _selected; } }
        /// <summary>`Continue` 那一组现在亮着吗（原版 = `continueButton.gameObject.activeSelf`）。</summary>
        public bool ContinueShown { get { return _continueShown; } }

        public string DebugDump()
        {
            return "cards=" + _cards.Count
                 + " selected=" + (_selected != null ? _selected.View.DeckId : "-")
                 + " continue=" + (_continueShown ? "on" : "off")
                 + " missingArt=" + MissingArt.Count;
        }
    }
}
