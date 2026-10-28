// EventImageSelector.cs — **活动资产 → Image 的两条通用接线**（原版 `EventImageSelectorController`
//   ＋ `LiveopUIDrawer_EventAssetSprites`），B93-5（2026-10-28 · 文件所有者代理 `D2`）。
//
// ============================ 这是什么 ============================
// 原版这两个类干的是同一件事：**按 `EventAssetReferenceType` 从事件资产里取一张 Sprite 灌进一颗 Image，
// 取不到就把那颗 Image 的 `enabled` 关掉**。区别只在「谁来驱动」：
//   · `EventImageSelectorController` —— 手里一串 `ImageEventDrawData`（每格 = 一个 type + 一颗 Image），
//     外部把 `List<EventAsset>` 交给它，它逐格 `TryDraw(assets)`（按**资产名**取图）。
//   · `LiveopUIDrawer_EventAssetSprites` —— 手里一串嵌套的 `EventAssetImages`（每格同上），
//     外部把事件（`ILiveOps`）交给它，它**直接问事件要**（`HasAsset` / `LoadAsset`）。
//
// ============================ 判据（全部本地现读）============================
// ① `decomp_full/EventImageSelectorController__Initialize.c`（全文 3 句：遍历 → 逐格 `TryDraw`）
// ② `decomp_full/EventImageSelectorController.ImageEventDrawData__TryDraw.c`（全文 5 句：
//      `foreach (a in assets) if (a.type == this.type) { name = a.name; sprite = Load(name);
//       image.sprite = sprite; image.enabled = true; return; }` → 循环走完 ⇒ `image.enabled = false`）
// ③ `decomp_full/LiveopUIDrawer_EventAssetSprites.EventAssetImages__Initialize.c`（全文 4 句：
//      `if (!HasAsset(event, type)) { image.enabled = false; return; }`
//      `image.enabled = true; image.sprite = LoadAsset(event, type);`）
// ④ `decomp_full/LiveopUIDrawer_EventAssetSprites__Initialize.c`（外层：遍历内层逐格 `Initialize`）
//
// ============================ 与既有件的关系（⛔ 不重造）============================
// · `EventAssetReferenceType` 的枚举 **已经存在** → `Shell/LiveOpsLocalCatalog.cs` 的 `LiveOpsAssetRef`
//   （成员名与值逐项照官方：`Icon 0` / `BackgroundImage 1` / `ForegroundImage 2` / … / `ArmyImage 5` /
//    `MainMenuContainerForeground 11` / `GameModeIcon 12`）。⛔ 本文件**不再定义第二套枚举**。
// · `LiveOpsAssetUtility.HasAsset / LoadAsset` 的本地那一层 **已经存在** →
//   `LiveOpsLocalEvent.FindAsset(int)`（静默）与 `LiveOpsAssetUtility.GetAssetReference`（取不到会喊）。
//   本文件只在它们之上补「**是否命中 + 取图 + 写 Image 的 enabled**」这一层。
// · 「`m_SubObjectName` → `Resources/Art/` 那张图」的解析先例 = `Shell/BaseEventPopup.SetEventImageRef`。
//
// ============================ 🔴 本地拿不到的那一半 ============================
// 本机 13 份活动 SO 全是过期 Expansion Pass ⇒ `FindAsset` 能命中少数几条（图名可解），但**图没导入
// `Resources/Art/`**（源：`bundle_liveopsicons_assets_all` 的 `Sprite/40k_campaign_Expansion Pass`）
// ⇒ `LoadAsset` 返回 `null` ⇒ `enabled = false`。**这正是原版缺图时的同一支行为**（③ 的第二句），
// ��是「我们没实现」。真图到了只需导入，本文件一行都不用改。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>「一格要灌图的目标」——原版是 uGUI `Image`（`sprite` + `enabled` 两格）。
    /// <para>我们这一侧不把 `UnityEngine.UI.Image` 绑死（本仓的图件是 `ImageQuad`，而自检不该为了断两条
    /// 布尔值去建 uGUI 对象）⇒ 抽成两格写口。<see cref="UnityImageTarget"/> 是给真 uGUI 用的适配器。</para></summary>
    public interface IEventImageTarget
    {
        /// <summary>= uGUI `Image.sprite`（我们这一侧是 `Texture2D`；本仓 `Resources/Art/` 里全是 Texture2D）。</summary>
        Texture2D Sprite { get; set; }
        /// <summary>= uGUI `Image.enabled`。</summary>
        bool Enabled { get; set; }
    }

    /// <summary>给真 uGUI `Image` 用的适配器（生产侧可选；本仓建树用的是 `ImageQuad`，见类头）。</summary>
    public sealed class UnityImageTarget : IEventImageTarget
    {
        readonly UnityEngine.UI.Image _img;
        public UnityImageTarget(UnityEngine.UI.Image img) { _img = img; }
        public Texture2D Sprite
        {
            get { return _img != null && _img.sprite != null ? _img.sprite.texture : null; }
            set { if (_img != null) _img.sprite = value == null ? null : MakeSprite(value); }
        }
        public bool Enabled { get { return _img != null && _img.enabled; } set { if (_img != null) _img.enabled = value; } }

        static UnityEngine.Sprite MakeSprite(Texture2D t)
        {
            return UnityEngine.Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), new Vector2(0.5f, 0.5f));
        }
    }

    /// <summary>原版 `EventImageSelectorController`（`decomp_full/EventImageSelectorController__Initialize.c`
    /// ＋ `EventImageSelectorController.ImageEventDrawData__TryDraw.c`）。
    /// <para>一串「一格 = 一个 type + 一颗 Image」，外部把 `List&lt;EventAsset&gt;` 交进来逐格试画。</para></summary>
    public static class EventImageSelectorController
    {
        /// <summary>原版 `EventAsset` 里我们用得到的两格。
        /// <para>⚠️ 原版 `EventAsset` 的**类**在本机反编译里没有独立文件（`EventImageDrawData.c` 里只有
        /// `+0x10 = type` 与「按名取」两条实据）⇒ 只保留 `Type` 与 `AssetName` 两格，⛔ 不编别的字段。</para></summary>
        public struct EventAsset
        {
            /// <summary>`EventAssetReferenceType` 的数值（用 <see cref="LiveOpsAssetRef"/> 的成员，⛔ 别另造）。</summary>
            public int Type;
            /// <summary>资产名（= `AddressablesManager.Load<object>` 的入参，原版按**名**取）。</summary>
            public string AssetName;
        }

        /// <summary>原版 `ImageEventDrawData`（一格）：`image`（+0x10）· `eventAssetType`（+0x18）· 缓存的名（+0x20）。</summary>
        public sealed class ImageEventDrawData
        {
            /// <summary>这一格要灌图的目标（原版 `+0x10` 那颗 `Image`）。为 null ⇒ 原版 NRE。</summary>
            public IEventImageTarget Image;
            /// <summary>这一格认的资产类型（原版 `+0x18`）。</summary>
            public int EventAssetType;
            /// <summary>上一次命中的资产名（原版 `+0x20`，`TryDraw` 命中时写）。**自检读口**，⛔ 不拿它判显隐。</summary>
            public string LastAssetName;

            /// <summary>原版 `TryDraw(List&lt;EventAsset&gt;)`（全文 5 句，逐句）：
            /// <code>
            /// foreach (var a in eventAssets)
            ///     if (a.type == this.eventAssetType) {          // ③ 按 type 命中
            ///         this.assetName = a.name;                    // ④ 缓存名
            ///         sprite = Addressables.Load&lt;Sprite&gt;(a.name);
            ///         image.sprite  = sprite;                     // ⑤
            ///         image.enabled = true;
            ///         return;
            ///     }
            /// image.enabled = false;                             // ★ 循环走完 ⇒ 关掉这一格
            /// </code>
            /// <returns>这一格画上了没有（= `enabled` 且 `sprite != null`）。</returns></summary>
            public bool TryDraw(List<EventAsset> eventAssets)
            {
                if (Image == null) return false;                        // 原版两处都是 NRE 那道闸
                if (eventAssets != null)
                {
                    for (int i = 0; i < eventAssets.Count; i++)
                    {
                        var a = eventAssets[i];
                        if (a.Type != EventAssetType) continue;         // ③
                        LastAssetName = a.AssetName;                    // ④
                        var sprite = EventArt.LoadByName(a.AssetName);   // 原版按 **资产名** 走 Addressables
                        Image.Sprite = sprite;                          // ⑤
                        Image.Enabled = sprite != null;                 //   原版那句 `enabled = true` 的等价（取不到 ⇒ 关）
                        return sprite != null;
                    }
                }
                Image.Enabled = false;                                  // ★ 没命中 ⇒ 关
                return false;
            }
        }

        /// <summary>原版 `Initialize(List&lt;EventAsset&gt;)`（全文 3 句）：
        /// `if (imageEventDrawDatas == null) NRE; foreach (var d in imageEventDrawDatas) d.TryDraw(eventAssets);`。</summary>
        /// <returns>画上了几格。</returns>
        public static int Initialize(List<ImageEventDrawData> drawDatas, List<EventAsset> eventAssets)
        {
            if (drawDatas == null) return 0;                            // 原版 NRE 那道闸
            int n = 0;
            for (int i = 0; i < drawDatas.Count; i++)
                if (drawDatas[i] != null && drawDatas[i].TryDraw(eventAssets)) n++;
            return n;
        }
    }

    /// <summary>原版 `LiveopUIDrawer_EventAssetSprites`（`decomp_full/LiveopUIDrawer_EventAssetSprites__Initialize.c`
    /// ＋ `.EventAssetImages__Initialize.c`）。
    /// <para>与上面那一族的差别：它**不问外部要 asset 表**，直接问事件要（`HasAsset` / `LoadAsset`）。</para></summary>
    public static class LiveopUIDrawer_EventAssetSprites
    {
        /// <summary>原版嵌套类 `EventAssetImages`（一格）：`type`（+0x10）· `image`（+0x18）。</summary>
        public sealed class EventAssetImages
        {
            /// <summary>这一格认的资产类型（原版 `+0x10`）。</summary>
            public int EventAssetType;
            /// <summary>这一格要灌图的目标（原版 `+0x18` 那颗 `Image`）。为 null ⇒ 原版 NRE。</summary>
            public IEventImageTarget Image;

            /// <summary>原版 `EventAssetImages.Initialize(ILiveOps liveopEvent)`（全文 4 句，逐句）：
            /// <code>
            /// bool has = LiveOpsAssetUtility.HasAsset(事件, this.type);      // ①
            /// if (!has) { image.enabled = false; return; }                    // ② ★ 没有就关
            /// image.enabled = true;                                          // ③
            /// image.sprite  = LiveOpsAssetUtility.LoadAsset(事件, this.type); // ④
            /// </code>
            /// <returns>这一格画上了没有。</returns></summary>
            public bool Initialize(LiveOpsLocalEvent evt)
            {
                if (Image == null) return false;                              // 原版 NRE 那道闸
                if (!EventArt.HasAsset(evt, EventAssetType))                   // ①②
                {
                    Image.Enabled = false;
                    return false;
                }
                Image.Enabled = true;                                          // ③
                Image.Sprite = EventArt.LoadAsset(evt, EventAssetType);         // ④
                return Image.Sprite != null;
            }
        }

        /// <summary>原版外层 `Initialize(ILiveOps liveopEvent)`（全文 3 句）：
        /// `foreach (var imgs in eventAssetImages) imgs.Initialize(liveopEvent);`。</summary>
        /// <returns>画上了几格。</returns>
        public static int Initialize(List<EventAssetImages> eventAssetImages, LiveOpsLocalEvent evt)
        {
            if (eventAssetImages == null) return 0;
            int n = 0;
            for (int i = 0; i < eventAssetImages.Count; i++)
                if (eventAssetImages[i] != null && eventAssetImages[i].Initialize(evt)) n++;
            return n;
        }
    }

    /// <summary>「事件资产 → 图」的那两格口（原版 `LiveOpsAssetUtility.HasAsset / LoadAsset`）。
    /// <para>⚠️ **不重造装载层**：命中判定走 `LiveOpsLocalEvent.FindAsset`（静默，= 原版 `HasAsset` 不喊），
    /// 取图走 `GetAssetReference`（取不到会喊，= 原版 `LoadAsset` 那一层）+ `CardArt.MenuUi(SubObjectName)`
    /// （先例 → `Shell/BaseEventPopup.SetEventImageRef`）。</para></summary>
    public static class EventArt
    {
        /// <summary>原版 `LiveOpsAssetUtility.HasAsset(事件, type)`：静默查「这一格有没有资产」。</summary>
        public static bool HasAsset(LiveOpsLocalEvent evt, int assetType)
        {
            return evt != null && evt.FindAsset(assetType) != null;
        }

        /// <summary>原版 `LiveOpsAssetUtility.LoadAsset(事件, type)`：取不到 ⇒ **`null`**（⛔ 不兜底别的图）。</summary>
        public static Texture2D LoadAsset(LiveOpsLocalEvent evt, int assetType)
        {
            if (evt == null) return null;
            var a = LiveOpsAssetUtility.GetAssetReference(evt, (LiveOpsAssetRef)assetType);
            if (a == null) return null;
            if (string.IsNullOrEmpty(a.SubObjectName)) return null;      // 原版 `RuntimeKeyIsValid` 那一档
            return CardArt.MenuUi(a.SubObjectName);
        }

        /// <summary>按**资产名**取图（= 原版 `AddressablesManager.Load&lt;Sprite&gt;(键)` 那条口）。</summary>
        public static Texture2D LoadByName(string artName)
        {
            return string.IsNullOrEmpty(artName) ? null : CardArt.MenuUi(artName);
        }
    }
}
