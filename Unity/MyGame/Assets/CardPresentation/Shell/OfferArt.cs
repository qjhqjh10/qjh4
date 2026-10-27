// OfferArt.cs — 「报价底图」这一族（`background` / `foreground`）的**取图接线**（界面线 R3）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【为什么单开一个文件】`OfferContainer.cs` / `BaseOfferPopup.cs` 都被 `账/*.tsv` 按 `文件:行号` 引用
//   （39 条 / 34 条 A 行）⇒ **在那两个文件里插一行就会让账落点整体漂移**（CLAUDE.md 铁律 18·b）。
//   本件用 `partial` 把「新成员」放到**新文件**里，原文件只改**已存在的行**（行数一字不变）。
//
// 【它解决什么】原版报价的底图**不是 prefab 里画死的**，是运行时按 `EventAssets` 灌进去的：
//   · `PurchaseableOfferContainer.OnInitialize`（`PurchaseableOfferContainer__OnInitialize.c`，逐句实读）：
//       `foreground.sprite = LoadAsset(offer, ForegroundImage=2)` → `foreground.enabled = (sprite != null)`；
//       `background.sprite = LoadAsset(offer, BackgroundImage=1)` → `background.enabled = (sprite != null)`。
//     （字段偏移：`foreground +0x58` · `background +0x60`，见 `dump.cs` 的 `PurchaseableOfferContainer`。）
//     ⇒ **两条语义**：① 有图才画（没图**连白色块都不画**）；② `background` 拉伸、`foreground` `m_PreserveAspect`。
//   · `BaseOfferPopup.SetEventData`（同名 `.c` + ILSpy `BaseOfferPopup.cs:260-270`）：
//       `foregroundImage.sprite = LiveOpsAssetUtility.LoadAsset<Sprite>(offer, ForegroundImage)`。
//
// 【GUID → 图名】离线 SO 的 `EventAssets` 只给 addressable GUID、`m_SubObjectName` 是空的
//   ⇒ 名字只能从**图集容器表**反推：`bundle_liveopsmenuimages_assets_all/AssetBundle/AssetBundle_1.json`
//   的 `m_Container[<GUID>].asset.m_PathID` ↔ `SpriteAtlas_6374560847574922512.json` 的
//   `m_PackedSprites` 顺序 → `m_PackedSpriteNamesToIndex`。逐条实测：
//     `117e7654c91ec124db66c365be480065` → `2525858753905376748` → **`WF_Special offer_Bck_tier1`**
//     `d8cb224a827f48a4a8b7a03e616833e2` → `-3513046522754821743` → **`WF_Special offer_Bck_tier2`**
//     `64795275d6972e943b87a65d6b02ef30` → `-7955982953250941539` → **`WF_Special offer_Bck_tier3`**
//   · 4 份 `Premium Item Offer` SO（Expansion UM1 / Expansion Orks1 / Release SW / Release EC）的
//     `drawData.EventAssets[key=1]` **都指向 tier1**（tier2/tier3 在离线数据里 0 个引用者 —— 服务端报价那边用）。
//   · 图怎么进工程：`工具/import_offer_backgrounds.py`（从 `Offers Atlas` 按 `m_RenderDataMap` 切，⛔ 不手抄矩形）。
//
// ⚠️ **没查清的部分（如实记，铁律 2）**：`background`/`foreground` 这两格**在离线 UI 里由谁触发重画**
//   只有容器类与 `BaseOfferPopup` 两处对得上；更上层的「报价列表」不在离线数据里（P3 服务端）。
//   本件只提供**取图 + 画法**，入口仍由调用方给（与 `OfferContainer.Build` 原有的「不编入口」同口径）。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static partial class OfferContainer
    {
        /// <summary>`Content` 的两格「底图名」（`CardArt.MenuUi` 的键；空 = 原版那一格没有图 ⇒ **不画**）。
        /// <para>为什么放在 partial 里：见文件头「为什么单开一个文件」。语义 = 原版
        /// `PurchaseableOfferContainer.background/foreground`（运行期 `LoadAsset` 灌）。</para></summary>
        public partial struct Content
        {
            /// <summary>`background`（原版 `EventAssetReferenceType.BackgroundImage`=1）。**拉伸**（prefab 无 preserveAspect）。</summary>
            public string BgArt;
            /// <summary>`foreground`（`ForegroundImage`=2）。**`m_PreserveAspect`**（prefab 那颗 Image 实读）。
            /// ⚠️ 与 `background` **不是同一批**：`41 份` 里两格的 `m_Enabled` 各自独立（判据 → `OfferContainer.Geo.BgOn/FgOn`）。</summary>
            public string FgArt;
        }

        /// <summary>GUID → 导入后的图名（出处逐条见文件头）。空表项 = 没查到，调用方会**出声**。</summary>
        static readonly Dictionary<string, string> KnownEventArt = new Dictionary<string, string>
        {
            { "117e7654c91ec124db66c365be480065", "WF_Special_offer_Bck_tier1" },
            { "d8cb224a827f48a4a8b7a03e616833e2", "WF_Special_offer_Bck_tier2" },
            { "64795275d6972e943b87a65d6b02ef30", "WF_Special_offer_Bck_tier3" },
        };

        /// <summary>已经出过声的图名 / GUID（避免每次建件都刷一行日志）。</summary>
        static readonly HashSet<string> _artWarned = new HashSet<string>();

        /// <summary>取图：空名 ⇒ `null`（**不说话**，那是「原版这一格没图」的正常态）；
        /// 有名字但 `Resources` 里没有 ⇒ `null` + **出声一次**（⛔ 不许静默）。</summary>
        public static Texture2D ArtOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && _artWarned.Add("tex:" + name))
                Debug.LogWarning("[OfferArt] 图 `" + name + "` 在 `Resources/Art/{ui_menu,ui_deck,ui}/` 里都没有 ⇒ "
                                 + "**这一格不画**（原版语义：`sprite == null ⇒ Image.enabled = false`）。"
                                 + "导入入口 = `工具/import_offer_backgrounds.py`。");
            return t;
        }

        /// <summary>一条 `EventAssets` → 导入后的图名。
        /// 优先用 SO 自带的 `SubObjectName`（有就直接用）；空 ⇒ 查 <see cref="KnownEventArt"/>；
        /// 都查不到 ⇒ `null` + **出声一次**（⛔ 不猜）。</summary>
        public static string ArtForAsset(LiveOpsLocalAsset a)
        {
            if (a == null) return null;
            if (!string.IsNullOrEmpty(a.SubObjectName))
                return a.SubObjectName.Trim().Replace(' ', '_');   // 与 `CardArt.MenuUi` 的命名约定一致
            string n;
            if (!string.IsNullOrEmpty(a.Guid) && KnownEventArt.TryGetValue(a.Guid, out n)) return n;
            if (_artWarned.Add("guid:" + (a.Guid ?? "<null>")))
                Debug.LogWarning("[OfferArt] `EventAssets` 的 GUID `" + (a.Guid ?? "<null>") + "` 查不到图名"
                                 + "（`SubObjectName` 也是空的）⇒ 这一格不画。判据表 = 本文件 `KnownEventArt`。");
            return null;
        }

        /// <summary>按活动 id 取它的 `BackgroundImage` / `ForegroundImage` 两格图名，填进 `Content`。
        /// <para>⚠️ 活动不在 <see cref="LiveOpsLocalCatalog"/> 里（或那两格没有资产）⇒ **原样返回**，不编图。</para></summary>
        public static Content WithEventArt(Content c, string eventId)
        {
            var ev = LiveOpsLocalCatalog.Find(eventId);
            if (ev == null) return c;
            if (c.BgArt == null) c.BgArt = ArtForAsset(ev.FindAsset((int)LiveOpsAssetRef.BackgroundImage));
            if (c.FgArt == null) c.FgArt = ArtForAsset(ev.FindAsset((int)LiveOpsAssetRef.ForegroundImage));
            return c;
        }

        /// <summary>`BaseOfferPopup` 的 `Artwork/foreground`：建节点 + **有图就画**（原版 `SetEventData` 那一跳）。
        /// <para>返回**节点**（调用方拿它做 `SetActive`）；画出来的 quad 是它的子件、名字 `<名字> Gfx`
        /// （与 `Build` 里 `background Gfx` / `foreground Gfx` 同一套命名）。</para>
        /// <para>⛔ 空图时**不建 quad**（原版 `foregroundImage.enabled = (sprite != null)`）。</para></summary>
        public static Transform BuildForeground(Transform parent, PxRect r, string art, int q)
        {
            var node = MenuDraw.Node(parent, "foreground", r);
            var tex = ArtOf(art);
            if (tex != null && node != null)
                MenuDraw.Rect(node, tex, r, "foreground Gfx", q, new Color(1f, 1f, 1f, 1f), keepAspect: true);
            return node;
        }
    }
}
