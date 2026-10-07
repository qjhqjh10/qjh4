// DraggableController.cs — 通用拖拽件（原版 `DraggableController<T>` / `Draggable<T>` / `IDropHandler<T>` 三件）
//
// 为什么单开这一个文件：原版这三件是**一套通用系统**，卡组编辑窗里**两个实例**共用它
//   （卡背 `CosmeticDraggingController : DraggableController<CosmeticItem>`、卡牌 `CardDraggingController
//    : DraggableController<RawCardScript>`；两个子类**都是空类**，行为全在基类里）。
//
// ==================================================================
//  判据（全部现读，2026-10-17）
// ==================================================================
//  ① **类形状** = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{DraggableController,Draggable,IDropHandler}.cs`
//     （签名桩：字段 / 属性 / 方法齐全，方法体是空的）。
//  ② **方法体** = 🔴 **VA 反汇编**（`decomp_full/` 里这五段**没有产物** —— 这是查证报告
//     「没查清 #1」里唯一真能补的那条；工具 `工具/disasm_va.py` + `工具/resolve_va.py`，
//     RVA 取自 `d:/2/tools/all_methods.txt` 的十进制列）：
//       · `SetDraggable`            25252768 = RVA 0x18153A0
//       · `OnBeginDrag`             25249424 = RVA 0x1814690
//       · `OnDrag`                  25249680 = RVA 0x1814790
//       · `OnEndDrag`               25251472 = RVA 0x1814E90
//       · `<OnEndDrag>b__6_2`       25253344 = RVA 0x18155E0   （松手投递那一条 lambda）
//     逐条还原（浮点实参只有读指令流才拿得到）：
//
//     `SetDraggable(T content)`：
//         byte[this+0x30] = 1                       ; dragging = true
//         tail-call [draggable.vtable + 0x178]      ; draggable.Initialize(content)
//
//     `OnBeginDrag(eventData)`：
//         先一句 `CustomDebug.Log(string.Format(fmt, this.dragging, eventData.<+0x20>))`（日志，见下 ⚠️）
//         if (!dragging) return;
//         this.draggable.gameObject.SetActive(true);          ; 预览**就在这一刻显形**
//         SoundManager.Instance.Play2D(this.dragSound);        ; rcx=返回缓冲(ValueTuple<bool,AudioSource>)、
//                                                              ; rdx=Instance、r8=dragSound、r9=0(=MixerType.FX)
//
//     `OnDrag(eventData)`：
//         if (!dragging) return;
//         var parentRect = this.draggable.transform.parent as RectTransform;
//         Vector2 local;
//         RectTransformUtility.ScreenPointToLocalPointInRectangle(
//             parentRect, eventData.position, eventData.pressEventCamera, out local);
//         this.draggable.transform.localPosition = new Vector3(local.x, local.y, 0f);
//         ; ⚠️ 用的是 `position`（**不是** `delta`）+ `pressEventCamera`（按下那一刻那台相机）
//         ;    净效果 = **预览的 pivot（= 中心）贴到指针上**
//
//     `OnEndDrag(eventData)`：
//         if (!dragging) return;
//         dragging = false;
//         this.draggable.gameObject.SetActive(false);         ; 预览**就在这一刻关回去**
//         var list = eventData.hovered
//                        .Where(go => go)                      ; `<>c.<OnEndDrag>b__6_0` = `op_Implicit`（判非空）
//                        .SelectMany(go => go.GetComponents<IDropHandler<T>>())   ; b__6_1
//                        .ToList();
//         list.ForEach(droppable => {                          ; b__6_2（`+0x60` = 那个 `List.ForEach` 的 Action）
//             if (this.draggable == null) return;              ;   ← `UnityEngine.Object.op_Implicit`
//             if (this.draggable.CurrentItem == null) return;
//             droppable.Drop(this.draggable.CurrentItem);
//         });
//
//     字段偏移（= dump.cs 的 `DraggableController<T>`，与指令流逐条吻合）：
//         `+0x20` draggable · `+0x28` dragSound · `+0x30` dragging。
//
//  🔴 **`eventData.hovered` 是什么**（这一段必须记下来，否则「投给谁」会想歪）：
//     uGUI 的 `BaseInputModule.HandlePointerExitAndEnter` 把**射线命中那一件 + 它的整条祖先链**
//     逐个 `hovered.Add(...)`（`com.unity.ugui/.../InputModules/BaseInputModule.cs:291`，默认
//     `m_SendPointerHoverToParent = true`，同文件 `:45`）⇒ **松手时指针下的「对象或它的任一祖先」
//     带 `IDropHandler<T>` 就会吃到这一投**。原版那个 handler 就是 `DeckEditingPanel`
//     （`Debug: 挂在 `Deck Editing Menu/Sidebar/Deck Details` 那一栏上，Rect `[0.25,360.97]–[335.56,1010.03]`，
//      见下面 `Deck/DeckRuntime.cs` 里 `DropFieldRect` 那一段的实读记录）。
//
// ==================================================================
//  ⚠️ 本工程的三处**有意偏离**（原版那件东西本地没有；每一处都写了为什么 + 判据在哪）
// ==================================================================
//  ① **`dragSound` 是 `string`（cue 名），不是 `AudioCue` 资产** —— 原版那一格指向
//     `bundle_soundcollection_assets_all` 里一个 `AudioCue` MonoBehaviour（外部引用
//     `m_FileID 6 / m_PathID -4308815958917459268`）。**这一格已经解出来了**（不是没查清）：
//     经 `数据/索引/anim_address_map.json` 的 `guid_to_asset` 反查 ⇒
//     **cue 名 = `CardStartDrag`**（`60fe1fac8e31f4b59ab7cb8e53696707…` 那条，`name: "CardStartDrag"`；
//     ⚠️ **2026-10-17 订正（铁律 5）**：原写尾数 `…96708` —— 索引 `anim_address_map.json` 里是 **`…96707`**，笔误）。
//     本工程声音的唯一载体就是 cue 名（同 `Battle/AnimFXController.cs:80-86` 那条口径）⇒ 存名、按名播。
//  ② **`OnDrag` 的「跟指针」换成了世界坐标** —— 原版走 `RectTransformUtility`（uGUI 的 RectTransform），
//     而本工程的界面**全是世界空间的 `ImageQuad`（没有 RectTransform）**。
//     净效果完全一样（预览中心贴到指针），判据 = 上一段那三行反汇编。
//  ③ **多了一个 `Bind(...)` 口** —— 原版那两个字段是 `[SerializeField]`（Inspector 里拖），
//     而本工程的界面是**代码建的** ⇒ 必须有程序化的绑定口。只此一处，别再加别的写入口。
//
//  🔴 **本仓没有 UGUI `EventSystem`**（`资料/历史/…` 与 `Battle/CombatCameraZoom.cs:85-86` 都记过），
//     所以 `IBeginDragHandler/IDragHandler/IEndDragHandler` 这三个回调**今天不会被 Unity 调**——
//     它们照原版实现、留着（将来真加了 `EventSystem` 就能用），而**今天真正驱动它们的是
//     `Deck/DeckRuntime.cs` 的指针层**：那条路自己合成 `PointerEventData`，再调**同三个方法**
//     （两条入口共用一个实现 —— 本仓「两处写同一条规则 = 迟早不一致」那条铁律的反面用法）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using WarpforgeVFX;

namespace CardPresentation
{
    /// <summary>原版 `IDropHandler.cs`。
    /// 🔴 **别和 uGUI 的 `IDropHandler` 混了** —— 那个（`UnityEngine.EventSystems.IDropHandler`）
    /// 收 `PointerEventData`、方法名 `OnDrop`、是 `IEventSystemHandler` 的一支；**这个**是泛型的、
    /// 方法名 `Drop(T)`、和 `IEventSystemHandler` 没关系。原版 `DeckEditingPanel` 两个都实现了。</summary>
    public interface IDropHandler<T>
    {
        void Drop(T content);
    }

    /// <summary>原版 `Draggable.cs` 的泛型那一半 —— **内容载体**：预览本体（`CosmeticPreview` /
    /// `CardPreview`）都派生自它，`SetDraggable` 会调它的 <see cref="Initialize"/>。
    /// <para>⚠️ **`OnDrag()` / `OnRelease()` 两个方法体没有反编译产物**（`all_methods.txt` 里也没有
    /// 带 `Draggable&lt;…&gt;` 前缀的条目）⇒ 这里留空，**如实标成「没查清」**，⛔ 别照它推断语义：
    /// 跟指针的是 <see cref="DraggableController{T}.OnDrag"/>，不是这两个。</para></summary>
    public abstract class Draggable<T> : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected Transform content;

        public T CurrentItem { get; protected set; }

        public abstract void Initialize(T item);

        public void OnDrag() { }
        public void OnRelease() { }
    }

    /// <summary>原版 `DraggableController.cs` —— 通用拖拽控制器。
    /// 两个实例：卡背 <see cref="CosmeticDraggingController"/> · 卡牌 <see cref="CardDraggingController"/>
    /// （都是空子类，和原版一样）。</summary>
    public class DraggableController<T> : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Draggable<T> draggable;
        /// <summary>原版是 `AudioCue dragSound`；本工程按 **cue 名** 存（偏离 ①，文件头有判据）。
        /// 原版的真值 = `CardStartDrag`（两个实例**共用同一条 cue**，字段 PathID 都是 −4308815958917459268）。</summary>
        [SerializeField] private string dragSound;

        private bool dragging;

        // ------------------------------------------------------------ 自检口（原版没有这些，只读）
        /// <summary>`dragging`（原版那个私有布尔；`OnBeginDrag`/`OnDrag`/`OnEndDrag` 的第一道闸）。</summary>
        public bool Dragging { get { return dragging; } }
        /// <summary>预览件（原版私有字段，只为对账）。</summary>
        public Draggable<T> Preview { get { return draggable; } }
        /// <summary>这一趟拖的**内容**（原版 = `draggable.CurrentItem`）。</summary>
        public T DraggedItem { get { return draggable != null ? draggable.CurrentItem : default(T); } }
        /// <summary>这个控制器**一共投出去几次**（`b__6_2` 真跑到 `Drop` 那一句的次数）。</summary>
        public int DropCalls { get; private set; }

        /// <summary>⚠️ **我们这一档的接线口**（偏离 ③）：原版两个字段是 Inspector 里拖的，
        /// 本工程界面是代码建的 ⇒ 必须有程序化的绑定口。</summary>
        public void Bind(Draggable<T> preview, string cueName)
        {
            draggable = preview;
            dragSound = cueName;
        }

        /// <summary>原版 `DraggableController&lt;T&gt;.SetDraggable(T)`（RVA 0x18153A0，逐条见文件头）。
        /// 由**调用方**（原版 = `DeckEditingWindow.CheckCardDrag / CheckCosmeticDrag`）在「这一拖合法」时调。</summary>
        public void SetDraggable(T content)
        {
            dragging = true;
            if (draggable == null)
            {
                // 原版这一句会直接空引用炸掉（没有判空）—— 我们出声说清楚（本仓红线：不许静默失败）
                Debug.LogWarning("[Drag] `SetDraggable`：`draggable`（预览件）没绑 ⇒ 这一拖没有预览"
                                 + "（**不是静默**；接线口 = `Bind(...)`）");
                return;
            }
            draggable.Initialize(content);
        }

        /// <summary>原版 `OnBeginDrag`（RVA 0x1814690）：`if (!dragging) return;`
        /// → **预览 `SetActive(true)`** → `SoundManager.Instance.Play2D(dragSound)`。
        /// ⚠️ 原版第一句是一条 `CustomDebug.Log`（格式串没解出来，见「没查清」）——
        /// 这里换成我们自己的日志，位置与作用**与它同**（起拖那一拍记一行）。</summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log("[Drag] OnBeginDrag（dragging=" + dragging + "，指针 " + (eventData != null ? eventData.position.ToString() : "<null>") + "）");
            if (!dragging) return;
            if (draggable == null) return;
            draggable.gameObject.SetActive(true);
            PlayDragSound();
        }

        /// <summary>原版 `OnDrag`（RVA 0x1814790）：预览的 pivot（= 中心）贴到指针上。
        /// 原版走 `RectTransformUtility.ScreenPointToLocalPointInRectangle(draggable.transform.parent,
        /// eventData.position, eventData.pressEventCamera, out local)` + `localPosition = local`
        /// （用 `position`、**不是** `delta`）；本工程没有 RectTransform ⇒ 等价物 = 直接把世界坐标摆过去
        /// （偏离 ②，文件头有判据）。</summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || draggable == null || eventData == null) return;
            var cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
            var wp = LayoutSpace.ScreenToWorld(eventData.position, cam);
            var t = draggable.transform;
            t.position = new Vector3(wp.x, wp.y, t.position.z);      // 原版那一跳写的就是 `localPosition`
        }

        /// <summary>原版 `OnEndDrag`（RVA 0x1814E90）：
        /// `if (!dragging) return; dragging = false;` → **预览 `SetActive(false)`** →
        /// `eventData.hovered` 上每一件取 `GetComponents&lt;IDropHandler&lt;T&gt;&gt;()` → 逐个 `Drop(CurrentItem)`。
        /// <para>⚠️ **`hovered` 要由调用方填**（原版是 uGUI 的 `BaseInputModule` 填的：命中那一件 + 它的祖先链）。
        /// 本工程由 <see cref="DropHandlersUnder"/> 的调用点（`DeckRuntime.CollectHovered`）负责 ——
        /// 那条路写明了「本工程没有 uGUI 射线，等价物是什么」。</para></summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            dragging = false;
            if (draggable == null) return;
            draggable.gameObject.SetActive(false);
            if (eventData == null) return;

            // 原版那条链（`<>c.<OnEndDrag>b__6_0/_1/_2` 三个 lambda）逐字等价：
            //   hovered.Where(go => go).SelectMany(go => go.GetComponents<IDropHandler<T>>()).ToList()
            //          .ForEach(droppable => { if (draggable && draggable.CurrentItem != null) droppable.Drop(CurrentItem); });
            var handlers = DropHandlersUnder(eventData.hovered);
            for (int i = 0; i < handlers.Count; i++)
            {
                if (draggable == null) return;                       // b__6_2 里那两句判空，位置**在循环内**
                if (draggable.CurrentItem == null) return;
                handlers[i].Drop(draggable.CurrentItem);
                DropCalls++;
            }
        }

        /// <summary>原版那个 `SelectMany`：把 `hovered` 上每一件的 `IDropHandler&lt;T&gt;` 摊平。
        /// ⚠️ `GetComponents` **只在那一件自己身上找、不沿父链** —— 父链是 `hovered` 里**已经有**了
        /// （uGUI 填 `hovered` 时就带了祖先，见文件头那段）。</summary>
        public static List<IDropHandler<T>> DropHandlersUnder(List<GameObject> hovered)
        {
            var res = new List<IDropHandler<T>>();
            if (hovered == null) return res;
            for (int i = 0; i < hovered.Count; i++)
            {
                var go = hovered[i];
                if (go == null) continue;                            // b__6_0 = `op_Implicit(go)`
                var comps = go.GetComponents<IDropHandler<T>>();
                for (int j = 0; j < comps.Length; j++) res.Add(comps[j]);
            }
            return res;
        }

        /// <summary>原版 = `SoundManager.Instance.Play2D(dragSound)`（2D ⇒ 不衰减）。
        /// 本工程走 `WFSoundBank`/`WFSoundPlayer`（那条线已经实现过同一件事，见 `Battle/AnimFXController.cs` 口径 ②）。
        /// 🔴 **取不到 cue 必须出声**（只报一次 —— 逐次拖拽刷屏会把别的告警淹掉）。</summary>
        static readonly HashSet<string> _missingCueWarned = new HashSet<string>();
        void PlayDragSound()
        {
            if (string.IsNullOrEmpty(dragSound)) return;
            WFSoundCue cue;
            if (!WFSoundBank.TryGetCue(dragSound, out cue))
            {
                if (_missingCueWarned.Add(dragSound))
                    Debug.LogWarning("[Drag] 起拖音 `" + dragSound + "` 在 `WFSoundBank` 里取不到 ⇒ 这一次起拖**不出声**"
                                     + "（**不是静默**：原版这一格 = `AudioCue` 资产，cue 名已解出 = `CardStartDrag`；"
                                     + "缺的是 cue 表/曲线资源本身）");
                return;
            }
            WFSoundPlayer.Play(cue, Vector3.zero, true);              // is2d = true ⇒ 原版 `Play2D`
        }
    }

    // ============================================================ 两个实例（原版都是空类）

    /// <summary>原版 `CosmeticDraggingController : DraggableController&lt;CosmeticItem&gt;` —— **空类**。
    /// 「`Cosmetic Drag Controller`」是**它挂的那个 GameObject 的名字**，不是类名。
    /// <para>T 的取法：原版是 `CosmeticItem`（ScriptableObject）；本工程没有那份 SO，
    /// 卡背的等价物是 **卡背图名（`string`，`CardArt.Cosmetic(名)` 直接取得到）** —— 同 `CardbackTable`
    /// 那条「把 SO 压成表」的既有口径。</para></summary>
    public class CosmeticDraggingController : DraggableController<string> { }

    /// <summary>原版 `CosmeticPreview : Draggable&lt;CosmeticItem&gt;`（`cardbackImage` 一个字段）。
    /// `Initialize(item)` = `CurrentItem = item; cardbackImage.sprite = item.CosmeticSprite`（`CosmeticPreview__Initialize.c`）。
    /// <para>本工程把 `cardbackImage` 换成 `ImageQuad`（偏离：没有 uGUI `Image`；同 `DraggableController` 偏离 ②）。
    /// ⚠️ 取图走调用方传进来的**同一条**取值路（`DeckRuntime` 的 `CosmeticTexOrWarn`，缺图会出声）
    /// —— 别在这里再写一条 `CardArt.Cosmetic` 静默取值路。</para></summary>
    public class CosmeticPreview : Draggable<string>
    {
        private ImageQuad cardbackImage;
        /// <summary>取图那一跳**由调用方注入**（= `DeckRuntime` 的 `CosmeticTexOrWarn`）——
        /// 本类**自己不写第二条取值路**（缺图就出声那条口径只留一处）。</summary>
        private System.Func<string, Texture2D> texOf;

        /// <summary>⚠️ 接线口（偏离 ③，同 `DraggableController.Bind`）。</summary>
        public void BindView(ImageQuad img, System.Func<string, Texture2D> textureOf)
        {
            cardbackImage = img;
            texOf = textureOf;
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
            //   —— 本处**正是漏的那一处**：`DeckRuntime` 建这一格时给的是 `SetAspect(250 / 405)`
            //   （= 布局框 250×405，×`m_LocalScale 0.6` 后**画出来 150×243**，原版 `Collection Cosmetic`），
            //   被换成贴图比例之后画出来就成了 `243 × (707/981) = **175.1284**`（实测逐位吻合：
            //   拖的是 `Cardback_AM_Shield of Humanity` = 707×981）⇒ `DeckScene` 那条「预览画出来的宽 = 150」红。
            if (t != null) cardbackImage.SetTexture(t, keepAspect: true);
        }
    }

    /// <summary>原版 `CardDraggingController : DraggableController&lt;RawCardScript&gt;` —— **空类**。
    /// <para>本工程对应 `CardDef`（我们的卡数据类，`cards_engine.json` 那一份；原版 `RawCardScript`
    /// 的字段/数值本地没有，见 `资料/战斗资源_覆盖清单_0929.md` §三 表 1）。</para></summary>
    public class CardDraggingController : DraggableController<CardDef> { }

    /// <summary>原版 = 拖影那一行（`Card Drag Controller > Deck Selector Card Info button`，
    /// **287.9 × 55.7**，出厂 `m_IsActive: false`）：`Background` + `Card Name` + `Cost Image`/`Cost`。
    /// 判据：`menu_rect.py bundle_menus_assets_all "Card Drag Controller" --depth 2`
    /// （节点 `[993.59,525.50]–[1093.59,625.50]`，子件 `Deck Selector Card Info button (inactive) [414,555 288x56]`）。</summary>
    public class CardPreview : Draggable<CardDef>
    {
        private Label name;
        private Label cost;

        public void BindView(Label nameLabel, Label costLabel)
        {
            name = nameLabel; cost = costLabel;
        }

        public override void Initialize(CardDef card)
        {
            CurrentItem = card;
            if (card == null) return;
            if (name != null) name.SetText(CardText.Name(card.Name, card.NameZh));
            if (cost != null) cost.SetText(card.Cost.ToString());
        }
    }
}
