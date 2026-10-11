// CosmeticPreview.cs — 从 `DraggableController.cs` 拆出来的独立文件（原处是**次要类**，Unity 建不出 MonoScript）。
//
// 🔴 **为什么必须单开一个文件**（2026-10-11 · `A1506` / `A1481`）：Unity 只给**文件名 == 类名**
//   的那个类建 `MonoScript` 资产；次要类没有 MonoScript ⇒ 存场景时 `m_Script` 指不到资产 ⇒
//   Unity **当场烘一块内联 `!u!115` 存根**写进 level 文件 ⇒ player 加载时 `Position out of bounds!`
//   ⇒ `The file '.../levelN' is corrupted!` + 崩溃。判据与实测 → `资料/交接_第十七会话.md` §三 ·
//   `资料/任务_卡组卡牌战斗.md` 的 `A1506`。
//
// 卡背的拖拽预览体。

using System.Collections.Generic;
using RuleEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using WarpforgeVFX;

namespace CardPresentation
{
    public class CosmeticPreview : Draggable<string>
    {
        private ImageQuad cardbackImage;
        /// <summary>取图那一跳**由调用方注入**（= `DeckRuntime` 的 `CosmeticTexOrWarn`）——
        /// 本类**自己不写第二条取值路**（缺图就出声那条口径只留一处）。</summary>
        private System.Func<string, Texture2D> texOf;

        /// <summary>🔴 **原版那颗 `Image` 的「外接框」**（`content > Image_…` 的 `sizeDelta` **220×330**
        /// × `m_LocalScale` 0.6 = **132×198**）—— 由 <see cref="BindView"/> 在**建完那一刻**记下来。
        /// ⛔ **不能每次现读** `cardbackImage.WorldW/WorldH`：`Initialize` 每次起拖都会按贴图比例
        /// **内接**一次，现读到的就是**上一次内接之后**的框 ⇒ 每拖一次缩一点
        /// （静默，且只在拖第二张时才现形 —— 铁律 10 第 5 条那一族）。</summary>
        private float boxW, boxH;
        private bool boxOk;

        /// <summary>🆕 2026-10-19（B2）：**外接框中心那一处局部位置**（= 出厂那一刻记下的）。
        /// 原版画心**不居中**（按 sprite 的 `padding` 偏 ⇒ 见 `Core/CardbackFace.cs`），
        /// `Initialize` 每次都要「回到基准 + 偏移」—— ⛔ **不能在现位置上累加**
        /// （拖第二次就再偏一点，静默；与上面 `boxW/boxH` 那条注释是同一族坑）。</summary>
        private Vector3 baseLocalPos;
        private bool basePosOk;

        /// <summary>⚠️ 接线口（偏离 ③，同 `DraggableController.Bind`）。</summary>
        public void BindView(ImageQuad img, System.Func<string, Texture2D> textureOf)
        {
            cardbackImage = img;
            texOf = textureOf;
            if (img != null && img.WorldW > 0f && img.WorldH > 0f)
            {
                boxW = img.WorldW; boxH = img.WorldH; boxOk = true;
            }
            // 🆕 2026-10-19（B2）：记下出厂那一刻的局部位置（= 外接框中心）。见字段上的注释。
            if (img != null) { baseLocalPos = img.transform.localPosition; basePosOk = true; }
        }

        /// <summary>🔴 **`Image.m_PreserveAspect = 1` 的等价物**（本仓**唯一**一份口径 ——
        /// 卡背格那条（`DeckRuntime` 的 `q.SetAspect(CosmoCellW / CosmoCellH)`）要复刻同一件事时
        /// **调这个口**，⛔ 别抄第二份）。
        /// <para>🔴 **2026-10-19（B2）就地订正（铁律 5）**：本句原来写「把贴图**按它自己的比例**内接进
        /// `boxW×boxH`」—— **不全对**。uGUI 是**两段**：定框比的是 **sprite 的 `rect`（`m_Rect`）**、
        /// 贴图再按 **`padding`** 内缩（⇒ 画心不居中）。算式与判据全文 → `Core/CardbackFace.cs`；
        /// 本函数**只转调**它。对已裁成 `textureRect` 的卡背：**比例不变、尺寸缩小、位置偏**
        /// （老写法的「按贴图自身比例内接、居中」在**没登记的贴图**上逐字保留）。</para>
        /// <para><b>判据（两条，都是原版侧）</b>：
        /// ① 原版那两颗 `Image` 都带 **`m_PreserveAspect = 1`**（现读
        ///    `python 工具/menu_dump.py bundle_menus_assets_all "Collection Cosmetic" --depth 4`：
        ///    预览那颗 `content > Image` = **220×330** + `Cardback_UM_Campaign_Premium_Main 707×1020` +
        ///    `preserveAspect`；卡背格那颗 `Cardback Container > Cardback` = **250×405** 同样带
        ///    `preserveAspect` —— 两个 prefab 同名不同类：`CosmeticPreview` vs `CollectionCosmetic`）。
        /// ② 它怎么算：uGUI 源码 `Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/Image.cs`
        ///    的 `PreserveSpriteAspectRatio(ref rect, spriteSize)`（本机现读）——
        ///    `spriteRatio > rectRatio` ⇒ `rect.height = rect.width / spriteRatio`（宽定），
        ///    否则 `rect.width = rect.height * spriteRatio`（高定）；两支都绕 `rectTransform.pivot` 居中
        ///    （这里是 `.5/.5` ⇒ **绕中心**，与 `ImageQuad` 的居中 quad 同构）。</para>
        /// <para>⚠️ **定框比的是「sprite 的 `m_Rect` 比例」，⛔ 不是「框的比例」** —— 2026-10-18 `A944` 那轮把
        /// `m_PreserveAspect=1` 读成了「比例就取 220/330」（= **拉伸**）⇒ 预览被竖向拉长
        /// （`Cardback_AM_Shield of Humanity` 707×981：内接 **132×183.2** vs 拉伸 **132×198**，
        /// 差 **14.8**px）；`A994③` 就地订正。贴图取不到 / 尺寸为 0 ⇒ 返回 `false`、⛔ **不改几何**。</para></summary>
        /// ① 原版那两颗 `Image` 都带 **`m_PreserveAspect = 1`**（现读
        ///    `python 工具/menu_dump.py bundle_menus_assets_all "Collection Cosmetic" --depth 4`：
        ///    预览那颗 `content > Image` = **220×330** + `Cardback_UM_Campaign_Premium_Main 707×1020` +
        ///    `preserveAspect`；卡背格那颗 `Cardback Container > Cardback` = **250×405** 同样带
        ///    `preserveAspect` —— 两个 prefab 同名不同类：`CosmeticPreview` vs `CollectionCosmetic`）。
        /// ② 它怎么算：uGUI 源码 `Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/Image.cs`
        ///    的 `PreserveSpriteAspectRatio(ref rect, spriteSize)`（本机现读）——
        ///    `spriteRatio > rectRatio` ⇒ `rect.height = rect.width / spriteRatio`（宽定），
        ///    否则 `rect.width = rect.height * spriteRatio`（高定）；两支都绕 `rectTransform.pivot` 居中
        ///    （这里是 `.5/.5` ⇒ **绕中心**，与 `ImageQuad` 的居中 quad 同构）。</para>
        /// <para>⚠️ **比的是「贴图自己的比例」，⛔ 不是「框的比例」** —— 2026-10-18 `A944` 那轮把
        /// `m_PreserveAspect=1` 读成了「比例就取 220/330」（= **拉伸**）⇒ 预览被竖向拉长
        /// （`Cardback_AM_Shield of Humanity` 707×981：内接 **132×183.2** vs 拉伸 **132×198**，
        /// 差 **14.8**px）；`A994③` 就地订正。贴图取不到 / 尺寸为 0 ⇒ 返回 `false`、⛔ **不改几何**。</para></summary>
        public static bool PreserveAspectSize(Texture2D t, float boxW, float boxH, out float w, out float h)
        {
            float dx, dyUp;
            return PreserveAspectSize(t, boxW, boxH, out w, out h, out dx, out dyUp);
        }

        /// <summary>同上，另外给出**画心相对框中心的偏移**（`dyUp` 向上为正 —— uGUI 的方向）。
        ///
        /// <para>🔴 **2026-10-19（B2）就地订正（铁律 5）**：本函数原来自己算「按**贴图自己**的比例内接」——
        /// 那只是 uGUI 的**第一段**。原版是**两段**：① 按 sprite 的 **`m_Rect`** 定框；
        /// ② 贴图再按 **`padding`** 内缩 ⇒ 画心**既不等于框、也不居中**。
        /// 算式与判据全文 → `Core/CardbackFace.cs`（**本仓唯一一份**；这里只是一层转调，
        /// 所以「改口径」不会再漏掉本处）。</para>
        /// <para>⚠️ 对本仓那 233 张**已裁成 `textureRect` 的卡背**，净效果 = **比例不变、尺寸乘 `texRect/m_Rect`、
        /// 位置按 `padding` 偏**；**没登记的贴图**在 `Fit` 里退化成老写法（= 本函数改动前逐字同值，
        /// `dx/dyUp` 恒 0）。取不到贴图 / 尺寸为 0 ⇒ 返回 `false` 且 `w,h = boxW,boxH`、偏移归零
        /// （**调用方一律不改几何**，与改动前同一条口径）。</para></summary>
        public static bool PreserveAspectSize(Texture2D t, float boxW, float boxH,
                                              out float w, out float h, out float dx, out float dyUp)
        {
            if (!CardbackFace.Fit(t, boxW, boxH, out w, out h, out dx, out dyUp))
            {
                w = boxW; h = boxH; dx = 0f; dyUp = 0f;
                return false;
            }
            return true;
        }

        public override void Initialize(string name)
        {
            CurrentItem = name;
            if (cardbackImage == null) return;
            var t = texOf != null ? texOf(name) : null;
            // 🔴 **2026-10-17（F2）**：必须走 `SetTexture(t, keepAspect: true)` —— 单参重载**会静默把
            //   `_aspect` 换成新贴图自己的比例**（`ImageQuad.SetTexture` 的 doc：那个重载从写下那天起
            //   就是这个行为，是本仓 A292 专门开 `keepAspect` 那个口的原因；`Battle/ImageQuad.cs` 的文件头
            //   写着「6 个调用点各自紧跟一句 `SetAspect(...)`，**漏一处就是那个 quad 的显示比例被静默改掉**」）
            //   —— 本处**正是漏的那一处**：`DeckRuntime` 建这一格时给的是 `SetAspect(CosmImgW / CosmImgH)`
            //   （= 图框 `CosmImgW`×`CosmImgH` = **220×330**，×`m_LocalScale 0.6` 后**画出来 132×198**，
            //   原版 `Collection Cosmetic` 的 `content > Image_…` 那颗 `Image`；⚠️ 那两个量在
            //   `DeckRuntime` 里是**常量** `CosmImgW` / `CosmImgH` —— ⛔ 按【符号】认，别抄行号），
            //   被换成贴图比例之后画出来就成了 `198 × (707/981) ≈ **142.7**`（= 上面那两个数算出来的，
            //   ⛔ 不是实跑读数；同一张图：拖的是 `Cardback_AM_Shield of Humanity` = 707×981）⇒
            //   `DeckScene` 那条「预览**画出来**的宽 = **132**」红。
            if (t != null) cardbackImage.SetTexture(t, keepAspect: true);

            // 🔴 **2026-10-18（`A994③`）**：原版那颗 `Image` 还带 **`m_PreserveAspect = 1`**
            //   ⇒ 换完图要**按原版那套内接**进 `boxW×boxH`（口径与判据全文 → `PreserveAspectSize`）。
            //   ⚠️ 这一步**不能省**：上一条 `keepAspect: true` 只是「别把框比例冲掉」，
            //   把贴图**拉满**那个 132×198 的框是**另一件事**（原版从不拉满 —— 那是 preserveAspect 的反面）。
            //   🔴 **2026-10-19（B2）就地订正（铁律 5）**：本句原来写「按**贴图自己**的比例内接」——
            //   那只是 uGUI 的**第一段**。定框比的是 sprite 的 `m_Rect`，贴图还要按 `padding` 内缩并**挪位**。
            if (boxOk)
            {
                float w, h, dox, doyUp;
                if (PreserveAspectSize(t, boxW, boxH, out w, out h, out dox, out doyUp))
                {
                    cardbackImage.SetWorldHeight(h);        // ⚠️ 先改高、再把比例拉回 ⇒ `WorldW = WorldH × _aspect`
                    cardbackImage.SetAspect(w / h);
                    // 🔴 **2026-10-19（B2）**：画心**不居中** —— 原版按 sprite 的 `padding` 偏
                    //   （算式/判据 → `Core/CardbackFace.cs`）。本处的框是**世界单位**、y 向上
                    //   （`DeckRuntime.Pos` 的 y 就是这么算的）⇒ `doyUp` **直接加**，⛔ 别在这里反号。
                    if (basePosOk)
                        cardbackImage.transform.localPosition =
                            baseLocalPos + new Vector3(dox, doyUp, 0f);
                }
            }
        }
    }
}