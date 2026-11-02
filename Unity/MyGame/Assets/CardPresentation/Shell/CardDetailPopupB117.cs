// CardDetailPopupB117.cs — `B117-28`：**原版菜单窗比战斗窗多出来的两层**（`TutorialObjs` / `EffectList`）
//
// 为什么单开一个 partial 文件：`Shell/CardDetailPopup.cs` 有 **69 条账的 `文件:行号` 落点**
// （最深到 `:3578`，其中 7 条落在文件后半段），往中间插成员会整片漂移（CLAUDE.md 铁律 18·b）。
// `CardDetailPopup` 本身就是 `partial` ⇒ 新成员一律放这里，主文件**零行数变化**。

using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public partial class CardDetailPopup
    {
        // ============================================================ 🆕 2026-11-02（`B117-28` · 执行代理 `B117_F9`）
        //   原版**菜单窗**（本窗）比**战斗窗**多两层 —— 它们是 `Card Display` 树里本来就有的孩子，
        //   本窗 2026-09 建窗时只建了 `LowerSection` / `Cards` / `Card Options Panel` 三块，漏了它们。
        //
        //   ---- 判据（现读，两处同一棵树）----
        //   ① `_tmp_view/orig_ui_proj/Assets/Data/Scenes/MainMenu Warpforge.unity`（= `bundle_scenes_scenes_mainmenuwarpforge`）
        //      的 `Card Display`(RT 1416) 四个孩子：`LowerSection`(1371) · `Cards`(1589) ·
        //      **`TutorialObjs`(1403, `m_IsActive: 1`)** · **`EffectList`(591, `m_IsActive: 0`)**；
        //      窗口根 `Card Displayer Menu For Menu`(1610) 的兄弟序 = `Menu Dark Background`(0) →
        //      `Card Options Panel`(1) → `Card Display`(2) → `Item Information Panel`(3)
        //      ⇒ **卡区这两层画在右栏面板【之上】**（下面 `QCdLayer` 就是为此取的）。
        //   ② 战斗版 `Battle/CardDisplayWindow.cs` 的 `BuildEffectList` / `BuildTutorialObjs`（同样的树，
        //      数值逐位相同）—— 本件按它的结构对齐（坐标系 / 尺寸全部共用 `CardWinBox`，⛔ 不另写一套）。
        //
        //   ---- `TutorialObjs` 子树（原版）----
        //     `TutorialObjs ▸ { UnitObjs ▸ {MeleeText, RangedText, HealthText}, EnergyText ▸ Arrow }`
        //     四颗字 `Melee Attack` / `Ranged Attack` / `Health Points` / `Energy Cost`（fs 30 · 材质
        //     `Asar-Regular Light Grey w outline thin`，`_FaceColor = (0.802,0.802,0.802)`）。
        //     ⚠️ **原版每颗字下还各挂一颗 `Arrow`**（`MeleeText/Arrow` 等 4 颗）；战斗窗那件只建了
        //       `EnergyText/Arrow` 一颗 ⇒ 本件**按战斗窗对齐**（只建那 1 颗），4 颗的差异记在报告里。
        //
        //   ---- `EffectList` 子树（原版，出厂关）----
        //     `EffectList ▸ { AffectedBy, Elements ▸ EffectElement(0..4) ▸ {EffectBg, EnchanterText, EffectText, line},
        //                     CreatedBy }`
        //     `CreatedBy` 与原版一致**建出来但 `activeSelf = false`**（`m_IsActive: 0`）；它的位置是
        //     模板位（模板本就落在屏外），运行期位置/内容由那条我们还没接的 `DisplayCardEffects` 定 ⇒
        //     **只建节点，不接线**（TODO：`Item Information Panel` 原版出厂也是关的，本批不建）。
        //   ⛔ `EffectList` 的唯一写者（原版）= `DisplayCardEffects`；本窗**没有**这条链的消费方
        //      ⇒ 整块保持关闭就是原版出厂态，别为了「看得见」去打开它。

        // ⚠️ 自检读口**不在这里**：`CollectionScene.B117F9_CheckDetailLayers` 直接按名字在 `Card Display`
        //    子树里 `FindChild`（⛔ 不用 `*ForTest` 取值器 —— 那会变成「拿被测实现证明被测实现」的一半）。
        Transform _menuTutObjs, _menuTutUnit, _menuTutEnergy;
        Transform _menuFxRoot, _menuFxElements, _menuFxCreatedBy;
        Label[] _menuFxWho, _menuFxWhat;

        // 原版兄弟序里 `Card Display` 排在 `Card Options Panel` 之后（画在上面）；本窗内容带的号到 3119
        // （`QCd = 3115` 面板 / `QCdRow = 3116` / `QCdText = 3117` / `QCdHit = 3118` / `QCdIcon = 3119`）
        // ⇒ 这两层取 **3119**（与异画箭头同号，但两块在屏上不重叠：箭头在 y≈723 · x 1342/1691）。
        const int QCdLayer = QCdIcon;

        /// <summary>以中心 + 尺寸给一个绝对 `PxRect`（本窗这几层都按原版「中心锚 + 固定 anchoredPosition」记）。</summary>
        static PxRect Box(float cx, float cy, float w, float h)
        {
            return new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
        }

        // `TutorialObjs` 四块的绝对中心（1920×1080 · 左上原点 · y 向下）——
        // 由原版「中心锚 + anchoredPosition」逐条换算（父 `TutorialObjs` 的 -46.56/4.38 是 VLG 模板位、
        // 不参与：四个孩子都是中心锚，容器尺寸/模板偏移对它们没有影响 ⇒ 与战斗窗同一条口径）。
        const float TutUnitCx = 960f, TutUnitCy = 540f, TutUnitS = 100f;
        const float TutMeleeCx = 486f, TutMeleeCy = 758f;
        const float TutRangedCx = 550f, TutRangedCy = 829.59f;
        const float TutHealthCx = 1441f, TutHealthCy = 813f;
        const float TutEnergyCx = 1457f, TutEnergyCy = 288f, TutEnergyW = 340.269f, TutEnergyH = 59.342f;
        const float TutTextW = 380f, TutTextH = 55f, TutTextPx = 30f;
        /// <summary>原版 `Arrow` 的中心/尺寸（相对 `EnergyText` 中心 `(-89.2, +49.6)`，原版 y 向上 ⇒ 屏 y 向下减）。</summary>
        const float TutArrowCx = 1367.8f, TutArrowCy = 238.4f, TutArrowW = 206.222f, TutArrowH = 76.172f;
        /// <summary>四颗字的颜色 = 原版材质 `_FaceColor`（同 `Battle/CardDisplayWindow.BuildTutorialObjs`）。</summary>
        static readonly Color TutInk = new Color(0.802f, 0.802f, 0.802f, 1f);

        /// <summary>建 `Card Display/TutorialObjs`（原版菜单窗 `m_IsActive: 1`）。**建一次**；本窗每次 `Build()`
        /// 会把整棵子树销毁重建，所以这里也跟着重建（`_menuTutObjs` 在 `Build()` 开头置 null）。</summary>
        void BuildTutorialObjsMenu(Transform cardDisplay)
        {
            if (cardDisplay == null || _menuTutObjs != null) return;
            // 容器按父件满框（原版那格是 stretch 锚 + 模板 `sizeDelta (3501,2291)`，见上面那段判据）
            _menuTutObjs = MenuDraw.Node(cardDisplay, "TutorialObjs", new PxRect(CardL, CardT, CardR, CardB));
            _menuTutUnit = MenuDraw.Node(_menuTutObjs, "UnitObjs",
                                         Box(TutUnitCx, TutUnitCy, TutUnitS, TutUnitS));
            MenuDraw.Text(_menuTutUnit, Box(TutMeleeCx, TutMeleeCy, TutTextW, TutTextH), "Melee Attack",
                          TutInk, "MeleeText", TutTextPx, QCdLayer, TutTextW, 0f, 0f, face: TmpFont.Face.LatinUi);
            MenuDraw.Text(_menuTutUnit, Box(TutRangedCx, TutRangedCy, TutTextW, TutTextH), "Ranged Attack",
                          TutInk, "RangedText", TutTextPx, QCdLayer, TutTextW, 0f, 0f, face: TmpFont.Face.LatinUi);
            MenuDraw.Text(_menuTutUnit, Box(TutHealthCx, TutHealthCy, TutTextW, TutTextH), "Health Points",
                          TutInk, "HealthText", TutTextPx, QCdLayer, TutTextW, 0f, 0f, face: TmpFont.Face.LatinUi);

            _menuTutEnergy = MenuDraw.Node(_menuTutObjs, "EnergyText",
                                           Box(TutEnergyCx, TutEnergyCy, TutEnergyW, TutEnergyH));
            MenuDraw.Text(_menuTutEnergy, Box(TutEnergyCx, TutEnergyCy, TutEnergyW, TutEnergyH), "Energy Cost",
                          TutInk, "EnergyTextLabel", TutTextPx, QCdLayer, TutEnergyW, 0f, 0f, face: TmpFont.Face.LatinUi);
            // 指向箭头：原版 sprite 是外部包里的 PPtr（本地按 pid 没解出 sprite 名）⇒ 用教程线已在用的
            // `40k_general_bt_arrow`（`m_Color = 0.6698` · `PA = 0` 是原版值，同战斗窗那一处）。
            var arrowTex = CardArt.MenuUi("40k_general_bt_arrow");
            if (arrowTex != null)
                MenuDraw.Rect(_menuTutEnergy, arrowTex, Box(TutArrowCx, TutArrowCy, TutArrowW, TutArrowH),
                              "Arrow", QCdLayer, new Color(0.6698f, 0.6698f, 0.6698f, 1f), false);
            else
                Debug.LogWarning("[CardDetail] 教程指向物的 `Arrow` 贴图取不到 ⇒ 这一件不建（不静默）");

            // 原版菜单窗这一层**出厂就是开的**（场景 `m_IsActive: 1`）；屏幕上看不见是「教程没走到」那件事，不是本层关着。
            _menuTutObjs.gameObject.SetActive(true);
        }

        /// <summary>建 `Card Display/EffectList`（原版菜单窗 `m_IsActive: 0`）。结构照战斗窗那件的**同一棵**：
        /// `EffectList ▸ {AffectedBy, Elements ▸ 5×EffectElement ▸ {EffectBg, EnchanterText, EffectText}, CreatedBy}`。
        /// 坐标全走 `CardWinBox` 那份判据（两处摆放共用），⛔ 不在本文件另写一套数。</summary>
        void BuildEffectListMenu(Transform cardDisplay)
        {
            if (cardDisplay == null || _menuFxRoot != null) return;
            float top = CardWinBox.EffRowCy[0] - CardWinBox.EffRowH * 0.5f;
            float bot = CardWinBox.EffRowCy[CardWinBox.EffSlots - 1] + CardWinBox.EffRowH * 0.5f;
            var rootR = Box((CardWinBox.EffL + CardWinBox.EffR) * 0.5f, (top + bot) * 0.5f,
                            CardWinBox.EffR - CardWinBox.EffL, bot - top);
            _menuFxRoot = MenuDraw.Node(cardDisplay, "EffectList", rootR);

            // 标题 `AffectedBy`（在 `Elements` 上方）—— 文案走战斗窗那同一个词条（`Battle/HUD/AffectedBy`）
            MenuDraw.Text(_menuFxRoot, Box(CardWinBox.EffTitleCx, CardWinBox.EffTitleCy,
                                           CardWinBox.EffTitleW, CardWinBox.EffTitleH),
                          CardDisplayWindow.TitleText, Color.white, "AffectedBy", CardWinBox.EffTitlePx,
                          QCdLayer, CardWinBox.EffTitleW, 19.3f, CardWinBox.EffTitlePx, face: TmpFont.Face.LatinCardBody);

            _menuFxElements = MenuDraw.Node(_menuFxRoot, "Elements", rootR);
            _menuFxWho = new Label[CardWinBox.EffSlots];
            _menuFxWhat = new Label[CardWinBox.EffSlots];
            var bgTex = CardArt.Ui("40K_display");
            if (bgTex == null)
                Debug.LogWarning("[CardDetail] `40K_display` 不在 `Resources/Art/ui/` ⇒ 效果清单**只画字、不画底板**（不摆白方块）");
            for (int i = 0; i < CardWinBox.EffSlots; i++)
            {
                float cy = CardWinBox.EffRowCy[i];
                var elem = MenuDraw.Node(_menuFxElements,
                                         i == 0 ? "EffectElement" : "EffectElement (" + i + ")",
                                         Box(CardWinBox.EffCx, cy, CardWinBox.EffRowW, CardWinBox.EffRowH));
                if (bgTex != null)
                    MenuDraw.Rect(elem, bgTex, Box(CardWinBox.EffCx, cy, CardWinBox.EffBgW, CardWinBox.EffBgH),
                                  "EffectBg", QCdLayer, null, false);
                _menuFxWho[i] = MenuDraw.Text(elem,
                    Box(CardWinBox.EffCx, cy + CardWinBox.EffWhoDy, CardWinBox.EffWhoW, CardWinBox.EffWhoH),
                    "", Color.white, "EnchanterText", CardWinBox.EffTextPx, QCdLayer, CardWinBox.EffWhoW,
                    10f, CardWinBox.EffTextPx, face: TmpFont.Face.LatinCardBody);
                _menuFxWhat[i] = MenuDraw.Text(elem,
                    Box(CardWinBox.EffCx, cy + CardWinBox.EffWhatDy, CardWinBox.EffWhatW, CardWinBox.EffWhatH),
                    "", Color.white, "EffectText", CardWinBox.EffTextPx, QCdLayer, CardWinBox.EffWhatW,
                    10f, CardWinBox.EffTextPx, face: TmpFont.Face.LatinCardBody);
            }

            // `CreatedBy`：原版 `m_IsActive: 0`，且位置是模板位（屏外；运行期由 `DisplayCardEffects` 摆）
            // ⇒ 只建节点 + 保持关着（TODO：接线时随效果清单一起做）。
            _menuFxCreatedBy = MenuDraw.Node(_menuFxRoot, "CreatedBy", Box(1580.56f, -147.63f, 369.9f, 38.1f));
            _menuFxCreatedBy.gameObject.SetActive(false);

            // 出厂关着（原版 `m_IsActive: 0`；唯一写者 `DisplayCardEffects`，本窗还没有消费方）
            _menuFxRoot.gameObject.SetActive(false);
        }
    }
}
