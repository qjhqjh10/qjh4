// FTUESelectDeckContainer.cs — 原版 **`FTUESelectDeckContainer`**（`Deck Container FTUE` prefab 节点）
//
// ============================ 出处（唯一判据，全部本地现读 2026-10-17） ============================
// · prefab 树 / 矩形 / 组件 / 文字档位：
//     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Choose Army Deck FTUE Window" --depth 6`
//     （同一份记录 → `资料/普查产出_1017/UI/D_FTUE_ChooseArmyDeck_dump.txt`）
//   `Deck Container FTUE` 自己 = 362.548×522.019（`m_SizeDelta.y = 0`，高由父 `Content` 的
//   `HorizontalLayoutGroup.expandH = 1` 撑满 ⇒ 522.019）；组件 = `FTUESelectDeckContainer` + `SimpleArmyImage`
//   （两个都在**同一个** GameObject 上，`m_GameObject` 逐字相同 —— MB 实读）。
// · 字段（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3787119349591974040.json` 实读）：
//     `warlordImage → Warlord Image` · `armyObjects → SimpleArmyImage`（= 本节点）
//     `armyDescription → Army Description` · `highlightObjects = [Selected Text]`（1 个）
//     `button → Select Button` · `glowImage → Glow`
//   `SimpleArmyImage`（MB `-1141144213329748120`，同一个 GO）：`armyIcon → Army Icon/Army Icon` · `armyText → Army Text/Army Text`
// · 行为（第一权威 = 反编译 `d:/2/tools/decomp_full/`）：
//   · `FTUESelectDeckContainer__Initialize.c` 逐句：
//       ① `CurrentDeck = deckFTUEInfo.Deck`（`StarterDeckFTUEInfo.deck`）
//       ② `glowImage.color = deckFTUEInfo.GlowColor`（**a 写成 0** —— `FTUESelectDeckContainer__Initialize.c:46-50`
//          那四句 `local_38/uStack_34/uStack_30 = +0x30/+0x34/+0x38`、`uStack_2c = 0`）
//       ③ `ToggleSelected(false)`（出厂不选中）
//       ④ `button.onClick.RemoveAllListeners(); button.onClick.AddListener(=> { ToggleSelected(true); onDeckSelected(this); })`
//       ⑤ `armyObjects.Initialize(deck.deckHero.Reference.cardArmy)` ⇒ `SimpleArmyImage__Initialize.c`：
//          `armyIcon.sprite = ArmyIconsSO.GetArmyIcon(army)` + `armyIcon.enabled = (sprite != null)` ·
//          `armyText.text = GameStaticData.ArmyToLocalizedString(army)`
//       ⑥ `warlordImage.sprite = deck.deckHero.Reference.fullHeroSprite`
//       ⑦ `armyDescription.text = I2(deckFTUEInfo.DeckInfo)`（`LocalizedString`）
//   · `FTUESelectDeckContainer__ToggleSelected.c`（`option` = 选中与否）：
//       `DOTween.Kill(glow.transform)` → `glow.transform.DOScale(one × (option ? 1.1 : 1), 1s)`
//       （常数 1.1 = `read_literal 0x1834b31f8`、时长 1.0 = `0x1834b2bb8`）
//       → `DOTween.Kill(glow)` → `glow.DOFade(option ? 1 : 0, 1s)`（带一个「当前 alpha 已经在目标上就不播」的守卫）
//       → `foreach (highlightObjects) SetActive(option)` → `button.interactable = !option`
//       → `button.Select()/Deselect()`（虚表 0x368，我们没有键盘导航 ⇒ 不实现，如实记）
//   · `FTUESelectDeckContainer__OnDisable.c`：`AssetReferenceTyped.Release(deck)` —— 我们**没有 Addressables**
//     ⇒ 不实现（本工程资源一律 Resources 直取，无可释放项；如实记）。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版） ============================
// · **阵营 id**：原版取 `deck.deckHero.Reference.cardArmy`（卡 SO 的 `CardArmy` 枚举）。
//   我们 = `cards_engine.json` 里那张督军卡的 `faction`（窗口解析后经 `DeckView` 传进来）。
// · **阵营徽记** = `DeckRuntime.FactionIcon(faction)`（全工程**唯一一份**「阵营名 → 徽记图名」）
//   ⇒ 等价于原版 `ArmyIconsSO.GetArmyIcon(army)`。
// · **督军立绘**：原版 `RawCardScript.fullHeroSprite` = **方形**整身立绘（本地那张
//   `UI_Deck_Warlord_Uriel_Ventris.png` = 2048²）。我们按 `UI_Deck_Warlord_<卡名>` 取；取不到才退回
//   `CardArt.Portrait(id)`（卡面插图，**竖构图**）—— 后者**是我们挑的兜底**，不是原版做法。
//   原版 `Warlord Image.m_PreserveAspect = 0`（实读）⇒ 方形图拉满 = 等比，等价；竖构图那张走
//   `keepAspect = true` 等比放（⛔ 不拉变形，也不假装这是原版行为）。
// · **阵营名** = `Loc.T("Armies/" + faction)`（= 原版 `GameStaticData.ArmyToLocalizedString`）。
// · **阵营描述** = 原版 `I2(TutorialData/TutorialFTUEDeck/<阵营>)`。
//   🔴 **本机查不到**：`Resources/official_loc_13lang.json`（9441 条）与 `数据/游戏数据/官方本地化_en+zh-CN.json`
//   逐字节搜 `TutorialFTUEDeck` **零命中** ⇒ 这一格**留空 + 出声一次**（⛔ 不编描述）。
// · **选中角标** `Selected Text`：出厂 `m_IsActive = false`（实读）；`ToggleSelected(true)` 才打开。
//   它绕 z 转 **17.95°**（`m_LocalRotation.z = 0.15599912, w = 0.98775727` ⇒ 2·atan2 = 17.9495°）。
//   🔴 **2026-10-17 就地订正（铁律 5）**：原文写「底图在 `Resources/` 里没有 ⇒ 底板不画 + 出声」——
//   **已补齐**：`40k_replace_display` 已登记进 `工具/sync_battle_ui_art.py` 的 `NAMES_MENU` 并同步到
//   `Resources/Art/ui_menu/`（`CardArt.MenuUi` 取的就是这里）⇒ **底板正常画**（`N50②` 收口）。
//   ⚠️ 若哪天 `Resources/Art/**` 被清掉，重跑那个导入器即可恢复（`Tex()` 仍会对缺图出声）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `FTUESelectDeckContainer`（`Deck Container FTUE`）—— 选阵营送卡组窗里的一张阵营卡。
    /// <para>几何全部相对**本容器自己的左上角**（px，y 向下；dump 现读，见下面每个常量的注释）。</para>
    /// <para>🔴 类名 == 文件名（Unity 只给同名文件建 `MonoScript`；见 `CLAUDE.md` 里 `!u!115` 那条）。</para></summary>
    public class FTUESelectDeckContainer : MonoBehaviour
    {
        // ============================================================ 容器自己的尺寸（prefab 实读）
        /// <summary>`Deck Container FTUE` 的宽（`m_SizeDelta.x = 362.548`）。
        /// ⚠️ 高在 prefab 里是 **0**，由父 `Content` 的 `HorizontalLayoutGroup.expandH = 1` 撑成 522.019
        /// ⇒ 我们按**布局后**的 522.019 建（dump 那一列就是它）。</summary>
        public const float CardW = 362.548f, CardH = 522.019f;

        // ============================================================ 子件矩形（相对容器左上角；dump 现读）
        /// <summary>`Glow`（523.7936²，**以容器中心为中心**；`Glow UI W40K` 123×123 · Simple · 色 = glowColor/a0）。</summary>
        public const float GlowL = -80.62f, GlowT = -0.887f, GlowW = 523.794f, GlowH = 523.794f;
        /// <summary>`Warlord Image`（与 Glow 同一个框；`m_PreserveAspect = 0`）。</summary>
        public const float WarlordL = -80.62f, WarlordT = -0.887f, WarlordW = 523.794f, WarlordH = 523.794f;
        /// <summary>`Army Icon`（233.034×233.038；中心在容器中心下方 215.98）。</summary>
        public const float IconL = 64.757f, IconT = 360.47f, IconW = 233.034f, IconH = 233.038f;
        /// <summary>`Army Icon/Army Icon`（206.76×208.57，`40k_Smooth shadow background` 之上那颗徽记）。</summary>
        public const float IconArtL = 76.6f, IconArtT = 372.3f, IconArtW = 206.76f, IconArtH = 208.57f;
        /// <summary>`Army Text`（容器，441×88.92；下方那颗 TMP 才是字）。</summary>
        public const float ArmyTextL = -39.226f, ArmyTextT = 504.7f, ArmyTextW = 441f, ArmyTextH = 88.92f;
        /// <summary>`Army Text/Army Text`（526.316×75.167，fs52/基准36/auto[18~52]，Center/**Capline**）。</summary>
        public const float ArmyLabelL = -81.88f, ArmyLabelT = 517.87f, ArmyLabelW = 526.316f, ArmyLabelH = 75.167f;
        /// <summary>`Selected Text`（371.069×128.263，**出厂 INACT**，绕 z 17.95°）。</summary>
        public const float SelL = -5.26f, SelT = 233.88f, SelW = 371.069f, SelH = 128.263f;
        /// <summary>`Selected Text/SelectedTextBg`（371.069×120.518；`40k_replace_display` 468×152 · ARF 3.07895）。</summary>
        public const float SelBgL = -5.26f, SelBgT = 237.8f, SelBgW = 371.069f, SelBgH = 120.518f;
        /// <summary>`Selected Text/SelectedText`（333.57×66.22，fs69.9/基准0.3/auto[20~80]，Center/Middle）。</summary>
        public const float SelTextL = 13.5f, SelTextT = 264.9f, SelTextW = 333.57f, SelTextH = 66.22f;
        /// <summary>`Army Description`（398.639×138.208，fs45/基准36/auto[18~45]，Left/**Top**）。</summary>
        public const float DescL = -18.2f, DescT = 593.6f, DescW = 398.639f, DescH = 138.208f;
        /// <summary>`Select Button`（523.79×774.832；`NonDrawingGraphic` + `EverguildButton`，`m_Transition = 0`）。</summary>
        public const float BtnL = -80.62f, BtnT = -19.666f, BtnW = 523.79f, BtnH = 774.832f;

        /// <summary>`Selected Text` 的 z 旋转（度；`m_LocalRotation.z = 0.15599912 / w = 0.98775727`）。</summary>
        public const float SelRotDeg = 17.9495f;

        // 文字档位（prefab 逐颗实读）
        public const float ArmyFont = 52f, ArmyFontMin = 18f, ArmyFontMax = 52f, ArmyFontBase = 36f;
        public const float SelFont = 69.9f, SelFontMin = 20f, SelFontMax = 80f, SelFontBase = 0.3f;
        public const float DescFont = 45f, DescFontMin = 18f, DescFontMax = 45f, DescFontBase = 36f;

        // 图 / 九宫（prefab + 素材库实读）
        /// <summary>`Army Icon Shadow` / `Army Text Shadow` 的底图（102×102 · border 50,50,50,50 · 黑）。
        /// 🔴 **2026-10-17（第四十八会话 · 桶 G4）**：原版资产名是 `40k_Smooth shadow background`（空格），
        ///   但导入器把**空格换成了下划线** ⇒ `CardArt.MenuUi` 按原名取不到，两张影子一直**静默没画**。
        ///   同一族：`Glow UI W40K` → `Glow_UI_W40K`。</summary>
        public const string ShadowArtName = "40k_Smooth_shadow_background";
        public const float ShadowTexW = 102f, ShadowTexH = 102f;
        /// <summary>`SelectedTextBg` 的底图（prefab 实读：`40k_replace_display` 468×152 · Simple · (0.632,0,0,1)）。
        /// 🔴 **本机 `Resources/` 里没有这张** ⇒ 取不到时底板不画（文字照画）+ 出声。</summary>
        public const string SelBgArt = "40k_replace_display";
        public const float SelBgTexW = 468f, SelBgTexH = 152f;
        public static readonly Color SelBgTint = new Color(0.632f, 0f, 0f, 1f);
        /// <summary>`Glow` 的出厂色（prefab 实读 (0,0.646,0.991,1)）—— 运行期被 `glowColor` 覆盖、并把 a 置 0。</summary>
        public static readonly Color GlowFactoryTint = new Color(0f, 0.646f, 0.991f, 1f);
        /// <summary>`Glow` 的图名（prefab 实读；123×123 · Simple）。
        /// 🔴 **2026-10-17（桶 G4）**：资源落盘名是 `Glow_UI_W40K`（下划线），原版 prefab 里写的是空格。</summary>
        public const string GlowArtName = "Glow_UI_W40K";

        // ============================================================ 数据 + 状态
        /// <summary>这张卡对应的那一副起始卡组（窗口解析好后喂进来）。</summary>
        public FTUESelectDeckWindow.DeckView View { get; private set; }
        /// <summary>当前是不是「已选中」那一态（= 原版 `ToggleSelected` 的 `option`）。</summary>
        public bool IsSelected { get; private set; }

        Transform _glow, _warlord, _icon, _iconArt, _armyText, _selected, _desc, _hit;
        ImageQuad _glowQ, _warlordQ, _iconQ, _iconShadowQ, _armyShadowQ, _selBgQ;
        Label _armyLabel, _descLabel, _selLabel;
        WindowButton _button;
        System.Action<FTUESelectDeckContainer> _onSelected;

        // 自检读口
        public ImageQuad GlowQuad { get { return _glowQ; } }
        public ImageQuad WarlordQuad { get { return _warlordQ; } }
        public ImageQuad ArmyIconQuad { get { return _iconQ; } }
        public ImageQuad IconShadowQuad { get { return _iconShadowQ; } }
        public ImageQuad ArmyShadowQuad { get { return _armyShadowQ; } }
        public ImageQuad SelectedBgQuad { get { return _selBgQ; } }
        public Transform GlowNode { get { return _glow; } }
        public Transform WarlordNode { get { return _warlord; } }
        public Transform ArmyIconNode { get { return _icon; } }
        public Transform ArmyTextNode { get { return _armyText; } }
        public Transform DescriptionNode { get { return _desc; } }
        /// <summary>本卡的命中区节点（`Select Button` 那一层）。</summary>
        public Transform HitNode { get { return _hit; } }
        /// <summary>`Selected Text`（出厂 INACT，选中后才亮）。</summary>
        public Transform SelectedNode { get { return _selected; } }
        public Label ArmyLabel { get { return _armyLabel; } }
        public Label DescriptionLabel { get { return _descLabel; } }
        public Label SelectedLabel { get { return _selLabel; } }
        public WindowButton SelectButton { get { return _button; } }
        /// <summary>实际用的阵营徽记图名（`DeckRuntime.FactionIcon` 的输出；取不到图时仍记名字，出声另算）。</summary>
        public string ArmyIconName { get; private set; }

        /// <summary>建一张阵营卡（<paramref name="card"/> = 本容器在窗口坐标系里的绝对矩形）。
        /// <para><paramref name="missingArt"/> = 窗口共用的「取不到的图」清单（红线：不许静默）。</para></summary>
        public static FTUESelectDeckContainer Create(Transform parent, PxRect card,
                                                     FTUESelectDeckWindow.DeckView view,
                                                     System.Action<FTUESelectDeckContainer> onSelected,
                                                     List<string> missingArt)
        {
            var go = new GameObject("Deck Container FTUE", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.ApplyPxRect(go.transform, parent, card);
            var c = go.AddComponent<FTUESelectDeckContainer>();
            c.View = view;
            c._onSelected = onSelected;
            c.Build(card, missingArt);
            return c;
        }

        // ============================================================ 建（顺序 = prefab 兄弟序）
        void Build(PxRect card, List<string> missingArt)
        {
            // ---- 1) `Glow`（阵营色大圆；出厂 alpha 0，选中才亮）----
            var glowR = Abs(card, GlowL, GlowT, GlowW, GlowH);
            _glow = MenuDraw.Node(transform, "Glow", glowR);
            var glowTex = Tex(GlowArtName, "`Glow` 的阵营光晕", missingArt);
            var glowTint = new Color(View.Glow.r, View.Glow.g, View.Glow.b, 0f);   // = Initialize 的 a = 0
            _glowQ = MenuDraw.Rect(_glow, glowTex, glowR, "Image", FTUESelectDeckWindow.QCardArt, glowTint);

            // ---- 2) `Warlord Image`（督军立绘；原版 `m_PreserveAspect = 0`）----
            var warlordR = Abs(card, WarlordL, WarlordT, WarlordW, WarlordH);
            _warlord = MenuDraw.Node(transform, "Warlord Image", warlordR);
            if (View.HeroArt != null)
                // ⚠️ 方形整身立绘（原版 `fullHeroSprite`）拉满 = 等比；竖构图兜底图（卡面插图）必须等比放，
                //    否则会拉变形 ⇒ 统一 `keepAspect = true`。**竖图那一档是我们挑的**（文件头已记）。
                _warlordQ = MenuDraw.Rect(_warlord, View.HeroArt, warlordR, "Image",
                                          FTUESelectDeckWindow.QCardArt, null, true);

            // ---- 3) `Army Icon`（阴影 + 阵营徽记）----
            var iconR = Abs(card, IconL, IconT, IconW, IconH);
            _icon = MenuDraw.Node(transform, "Army Icon", iconR);
            var shadowTex = Tex(ShadowArtName, "`Army Icon Shadow` 的底", missingArt);
            var shadowGo = MenuDraw.Nine(_icon, shadowTex, iconR, new Vector4(50f, 50f, 50f, 50f),
                                         ShadowTexW, ShadowTexH, FTUESelectDeckWindow.QCardArt,
                                         new Color(0f, 0f, 0f, 1f), true, "Army Icon Shadow");
            if (shadowGo != null) _iconShadowQ = shadowGo.GetComponentInChildren<ImageQuad>();

            var iconArtR = Abs(card, IconArtL, IconArtT, IconArtW, IconArtH);
            _iconArt = MenuDraw.Node(_icon, "Army Icon", iconArtR);
            ArmyIconName = View.ArmyIconName;
            _iconQ = MenuDraw.Rect(_iconArt, View.ArmyIcon, iconArtR, "Image", FTUESelectDeckWindow.QCardArt);
            // 原版 `SimpleArmyImage.Initialize` 末句：`armyIcon.enabled = (sprite != null)` ⇒ 没图整层关掉
            if (View.ArmyIcon == null) _iconArt.gameObject.SetActive(false);

            // ---- 4) `Army Text`（阴影 + 阵营名）----
            var armyTextR = Abs(card, ArmyTextL, ArmyTextT, ArmyTextW, ArmyTextH);
            _armyText = MenuDraw.Node(transform, "Army Text", armyTextR);
            var armyShadowGo = MenuDraw.Nine(_armyText, shadowTex, armyTextR, new Vector4(50f, 50f, 50f, 50f),
                                             ShadowTexW, ShadowTexH, FTUESelectDeckWindow.QCardArt,
                                             new Color(0f, 0f, 0f, 1f), true, "Army Text Shadow");
            if (armyShadowGo != null) _armyShadowQ = armyShadowGo.GetComponentInChildren<ImageQuad>();
            var armyLabelR = Abs(card, ArmyLabelL, ArmyLabelT, ArmyLabelW, ArmyLabelH);
            _armyLabel = MenuDraw.TextBox(_armyText, armyLabelR, View.ArmyName ?? "", Color.white, "Army Text",
                                          ArmyFont, ArmyFontMin, FTUESelectDeckWindow.QCardText,
                                          ArmyFontMax, ArmyFontBase, face: TmpFont.Face.LatinUi);
            MenuDraw.SetVAlign(_armyLabel, Label.VAlign.Capline, armyLabelR);

            // ---- 5) `Selected Text`（出厂 INACT；选中才亮；绕 z 17.95°）----
            var selR = Abs(card, SelL, SelT, SelW, SelH);
            _selected = MenuDraw.Node(transform, "Selected Text", selR);
            _selected.localRotation = Quaternion.Euler(0f, 0f, SelRotDeg);   // 原版 quaternion 实读
            var bgTex = Tex(SelBgArt, "`SelectedTextBg` 的底", missingArt);
            _selBgQ = MenuDraw.Rect(_selected, bgTex, Abs(card, SelBgL, SelBgT, SelBgW, SelBgH),
                                    "SelectedTextBg", FTUESelectDeckWindow.QCardArt, SelBgTint);
            _selLabel = MenuDraw.TextBox(_selected, Abs(card, SelTextL, SelTextT, SelTextW, SelTextH),
                                         Loc.T(FTUESelectDeckWindow.SelectedTerm), Color.white, "SelectedText",
                                         SelFont, SelFontMin, FTUESelectDeckWindow.QCardText,
                                         SelFontMax, SelFontBase, face: TmpFont.Face.LatinCardBody);
            _selected.gameObject.SetActive(false);      // 出厂 `m_IsActive = false`（dump 的 `INACT`）

            // ---- 6) `Army Description`（Left/Top；本机没有词条 ⇒ 空串 + 窗口那边出声一次）----
            var descR = Abs(card, DescL, DescT, DescW, DescH);
            _desc = MenuDraw.Node(transform, "Army Description", descR);
            _descLabel = MenuDraw.TextBox(_desc, descR, View.Description ?? "", Color.white, "Army Description",
                                          DescFont, DescFontMin, FTUESelectDeckWindow.QCardText,
                                          DescFontMax, DescFontBase, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(_descLabel, descR);
            MenuDraw.SetVAlign(_descLabel, Label.VAlign.Top, descR);

            // ---- 7) `Select Button`（整卡的透明命中区；`m_Transition = 0` ⇒ 悬停**不**变色）----
            _hit = MenuDraw.Hit(transform, "Select Button", Abs(card, BtnL, BtnT, BtnW, BtnH),
                                FTUESelectDeckWindow.QHit, OnClicked);
            if (_hit != null)
            {
                _button = _hit.GetComponent<WindowButton>();
                if (_button != null)
                {
                    _button.tintOnHover = false;      // 实读 `m_Transition = 0 (None)`
                    _button.clickCue = WindowSounds.DefaultButtonCue;   // `EverguildButton` 默认按钮音
                }
            }

            // 出厂态 = 未选中（`Initialize` 第 ③ 句 `ToggleSelected(false)`）
            ToggleSelected(false);
        }

        /// <summary>`Select Button` 的点击 = 原版 `Initialize` 里那个 lambda：
        /// **先 `ToggleSelected(true)`，再回调窗口**（窗口再把别的卡全部取消选中）。
        /// ⚠️ 原版 `button.interactable = !selected` ⇒ 已经选中的那张**点不动**，我们照做。</summary>
        void OnClicked()
        {
            if (IsSelected) return;                   // = 原版 `Selectable.interactable = false`
            ToggleSelected(true);
            if (_onSelected != null) _onSelected(this);
        }

        /// <summary>原版 `FTUESelectDeckContainer.ToggleSelected(bool option)`。
        /// <para>🔴 **2026-10-17 如实标注一处偏离**：原版用 DOTween 在 **1 秒**里插值
        /// （`DOScale` 1→1.1、`DOFade` 0→1）。我们**没有 tween 引擎**，直接落**终值**。
        /// 终值逐字照判据：scale ∈ {1, 1.1}、glow alpha ∈ {0, 1}、`highlightObjects` 的 `SetActive(option)`、
        /// `button.interactable = !option`。</para></summary>
        public void ToggleSelected(bool option)
        {
            IsSelected = option;
            // ③ 高亮件（= `Selected Text`）
            if (_selected != null) _selected.gameObject.SetActive(option);
            // ② glow：scale 1 / 1.1 + alpha 0 / 1
            if (_glow != null)
            {
                float k = option ? 1.1f : 1f;         // `read_literal 0x1834b31f8 = 1.1`
                _glow.localScale = new Vector3(k, k, k);
            }
            if (_glowQ != null)
            {
                var c = _glowQ.Tint;
                c.a = option ? 1f : 0f;
                _glowQ.SetTint(c);
            }
            // ④ 选中之后这张卡点不动（原版 `button.interactable = !option`）
            if (_button != null) _button.Interactable = !option;
        }

        // ============================================================ 小工具
        /// <summary>容器左上角 + 相对偏移 → 窗口坐标系里的绝对矩形。</summary>
        static PxRect Abs(PxRect card, float x, float y, float w, float h)
        {
            return new PxRect(card.x1 + x, card.y1 + y, card.x1 + x + w, card.y1 + y + h);
        }

        static Texture2D Tex(string name, string what, List<string> missingArt)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && missingArt != null && !missingArt.Contains(name))
            {
                missingArt.Add(name);
                Debug.LogWarning("[FTUE] 图取不到：`" + name + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。导入器："
                               + "`工具/import_original_art.py` 的 `MENU_IMAGES` / `工具/sync_battle_ui_art.py`");
            }
            return t;
        }
    }
}
