// AllianceBadgeArt.cs — 联盟**徽章**（`AllianceBadge` SO）的 id → 图 映射（本地等价物）
//
// ============================ 这是什么 ============================
// 原版每个联盟有一个 `GroupBadge`（`{ featuredTrophy: LoadableReference<AllianceTrophyData>, tierLevel: int }`，
// 判据 = `d:/2/tools/il2cpp_out/dump.cs` 的 `class GroupBadge`，字段偏移 0x10 / 0x18）。
// 画徽章那一跳是 `AllianceBadgeDrawer.Draw(GroupBadge)`：
//     `badge.featuredTrophy (+0x10)` → `AllianceTrophyData.reward` → `AllianceBadge.uniqueId`
//     → `AllianceBadge.badgeImage`（`AssetReferenceTyped<Sprite>`）→ 一张 Sprite；
//     取不到 ⇒ 回落 `AlliancesTrophyController.DefaultTrophy`；连控制器都没有 ⇒ **抛异常**（⛔ 不静默）。
// 判据全文 = `资料/完全复刻/判据/B88_R8_安卓包徽章挖掘.md` §一·§二（`AllianceBadgeDrawer__Draw.c` /
//   `AllianceBadgeDrawer__SetTierFrame.c` 逐句读过）。
//
// 本文件只补**本地缺的那一环**：`AllianceBadge.uniqueId → Sprite 名`。
// 图本体早就在本地（`Resources/Art/ui_menu/Alliance_Trophies_*.png`，1024²，与
//   `d:/2/新解包资源/assets_full/bundle_alliancesbadgesimages_assets_all/Texture2D/` **22/22 逐字节相同**，
//   2026-10-28 B88-B7 现核过 md5）。
//
// ============================ 🔶 这份表是我们整理的（不是官方清单） ============================
// 🔶 自己设计（非原版）：id → 图 这张**表本身**是我们的产物 —— 原版没有「徽章清单 SO」，它是按
//   Addressables 标签 `AllianceBadge`（`catalog.bin` 里的一条地址/标签名）一次性载入全部 16 份 SO，
//   再 `Enumerable.OfType<AllianceBadge>` 筛出来（判据 `AllianceBadgeAssetProvider__GetAssets.c`）。
//   · **数据源 = `d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/*_Badge.json` 的 16 份 SO**
//     （`badgeImage.m_AssetGUID`）→ `bundle_alliancesbadgesimages_assets_all` 的 `AssetBundle.m_Container`
//     （**键就是 GUID**）→ PathID → `Sprite` 名。每个 GUID 恰好 1 Sprite + 1 Texture2D 且同名 ⇒ 16/16 无歧义。
//   · **不是官方清单**：原版没有一个「这张表」的资产，也没有 `id → sprite` 的字面映射可抄。
//   · 定表人 / 日期：**B88 资源挖掘代理 R8，2026-10-28**（调度台 B7 落成代码）。
//   · 若将来发现某个 id 显示的图不对：先怀疑这张表（它是抄的），**而不是**怀疑 `AllianceBadgeDrawer` 的判据。
// ============================ 取法 ============================
// ⚠️ **一次性全载入、按 id 索引**（照原版的载入形状，见上），⛔ 不是「按 id 逐个哈希取」。
// ⚠️ `Sprite 名` **含空格**（`Alliance_Trophies_Black Legion event 1`）⇒ 落盘时**原样保留**，
//   `CardArt.MenuUi` 那条链**不做空格→下划线替换**（同 `CardArt.Cosmetics` 的先例）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class AllianceBadgeArt
    {
        /// <summary>原版 `AlliancesTrophyController.DefaultTrophy` 落到的那一枚（`Default_Badge`）。
        /// **它是回落目标**：`AllianceBadgeDrawer.Draw` 在徽章取不到时画这一枚。</summary>
        public const string DefaultBadgeId = "Default_Badge";

        /// <summary>一条映射。`Army` = 该 SO 的 `cardArmy`（`enum CardArmy`，判据 R8 §二那张表
        /// —— 只做交叉核对用，画图不看它）。</summary>
        public struct Entry
        {
            public string Id;        // `AllianceItem.uniqueId`
            public string Sprite;    // `AllianceBadge.badgeImage` 指向的 Sprite 名（= 本地 PNG 名）
            public int Army;         // `ObtainableItem.cardArmy`（10 Ultramarines … 130 SpaceWolves）
            public Entry(string id, string sprite, int army) { Id = id; Sprite = sprite; Army = army; }
        }

        /// <summary>16 份 `AllianceBadge` SO（全量，一个不多一个不少 —— 安卓 1.35.0 同样是这 16 份）。
        /// 🔶 表本身是整理出来的，见文件头。</summary>
        public static readonly Entry[] Entries =
        {
            new Entry("AM_Forge_Badge",   "Alliance_Trophies_AM",                     100),
            new Entry("BL_Event1_Badge",  "Alliance_Trophies_Black Legion event 1",    50),
            new Entry("BL_Forge_Badge",   "Alliance_Trophies_BlackLegion",             50),
            new Entry("DA_Forge_Badge",   "Alliance_Trophies_Dark Angels",            110),
            new Entry("Default_Badge",    "Alliance_Trophies_Default_v2",               0),
            new Entry("EC_Forge_Badge",   "Alliance_Trophies_Emperors Children",      120),
            // ⚠️ GOFF 用的是 `_Ork`，**不是** `_Goff`（后者在包里，但没有任何 SO 引用它 = 遗留资产）
            new Entry("GOFF_Forge_Badge", "Alliance_Trophies_Ork",                     20),
            new Entry("GSC_Forge_Badge",  "Alliance_Trophies_GSC",                     90),
            new Entry("LEV_Forge_Badge",  "Alliance_Trophies_Leviathan",               60),
            new Entry("SAU_Forge_Badge",  "Alliance_Trophies_Sautekh",                 40),
            new Entry("SH_Forge_Badge",   "Alliance_Trophies_SaimHann",                30),
            new Entry("SOR_Forge_Badge",  "Alliance_Trophies_Sororitas",               80),
            new Entry("SW_Forge_Badge",   "Alliance_Trophies_Space Wolves",           130),
            new Entry("TAU_Forge_Badge",  "Alliance_Trophies_Tau",                     70),
            // ⚠️ UM_Event 用的是 `_Space Marine 2 event`（不是 `_Ultramarines event`，那也是遗留资产）
            new Entry("UM_Event_Badge",   "Alliance_Trophies_Space Marine 2 event",    10),
            new Entry("UM_Forge_Badge",   "Alliance_Trophies_Ultramarines",            10),
        };

        static readonly Dictionary<string, string> _byId = BuildIndex();
        static readonly HashSet<string> _warned = new HashSet<string>();

        static Dictionary<string, string> BuildIndex()
        {
            var d = new Dictionary<string, string>();
            for (int i = 0; i < Entries.Length; i++)
                if (Entries[i].Id != null) d[Entries[i].Id] = Entries[i].Sprite;
            return d;
        }

        /// <summary>表里有几条（= 原版那 16 份 SO）。</summary>
        public static int Count { get { return _byId.Count; } }

        /// <summary>查 `uniqueId` 对应的 Sprite 名。**未知 id ⇒ false**（不猜、不回落到别的那一枚）。</summary>
        public static bool TryGetSpriteName(string badgeId, out string sprite)
        {
            sprite = null;
            return !string.IsNullOrEmpty(badgeId) && _byId.TryGetValue(badgeId, out sprite);
        }

        /// <summary>= 原版 `AllianceBadgeDrawer.Draw` 的那一步「取到哪一张」：
        /// 查得到就用它；**查不到（空 id / 不认识的 id）⇒ 回落 `Default_Badge`**
        /// （判据 = `__Draw.c` 里 `uVar2 == 0 ⇒ get_DefaultTrophy()` 那一支）。</summary>
        public static string SpriteNameOf(string badgeId)
        {
            string s;
            if (TryGetSpriteName(badgeId, out s)) return s;
            return _byId[DefaultBadgeId];
        }

        /// <summary>该 id 的徽章贴图。取不到（图没导进 `Resources`）⇒ **null + 出声一次**。
        /// <para>⚠️ 这一层对应原版那句「连 `DefaultTrophy` 都没有 ⇒ **抛异常**」——
        /// 我们退化成 `LogError`（抛异常会把整扇窗建到一半打死，而图缺失是**资产问题**不是逻辑问题）。</para></summary>
        public static Texture2D Get(string badgeId)
        {
            string want = badgeId;
            string sprite;
            if (!TryGetSpriteName(badgeId, out sprite))
            {
                sprite = _byId[DefaultBadgeId];
                // 出声：**只有「给了 id 但表里没有」才算异常**（null/空 是正常的「没有徽章」⇒ 静默走默认，
                //   原版那一支也是安静回落的）。同一条 id 只报一次。
                if (!string.IsNullOrEmpty(badgeId) && _warned.Add("id:" + badgeId))
                    Debug.LogWarning("[AllianceBadge] 不认识的徽章 id `" + badgeId + "` ⇒ 按原版回落 `"
                                   + DefaultBadgeId + "`（表见 `AllianceBadgeArt.Entries`；"
                                   + "源 = cosmeticsso 的 16 份 `*_Badge` SO）");
                want = null;
            }

            var tex = CardArt.MenuUi(sprite);
            if (tex == null && _warned.Add("art:" + sprite))
                Debug.LogError("[AllianceBadge] 徽章图 `" + sprite + "` 取不到（`Resources/Art/ui_menu/`）"
                             + (want != null ? "（id `" + want + "`）" : "（回落用的默认徽章）")
                             + " ⇒ 这一处**不画徽章**。原图在 "
                             + "`bundle_alliancesbadgesimages_assets_all/Texture2D/" + sprite + ".png`");
            return tex;
        }

        /// <summary>默认那一枚（= 原版 `DefaultTrophy`）。</summary>
        public static Texture2D Default { get { return Get(null); } }

        // ============================================================ 画（两处以上共用的那一份判据）
        // 🔴 **只此一处**：`Frame` / `Badge` 两层怎么摆、要不要等比、队列谁来加 ——
        //   ⛔ 别在调用点各写一份（本仓已经因为「两处写同一条规则」返工过）。
        //   判据：
        //   · 两层都吃 `m_PreserveAspect = 1`（`Frame` 与 `Badge` 都是 —— 现读
        //     `d:/2/tools/assetripper_out/ExportedProject/Assets/Alliance Badge Drawer.prefab`：
        //     `Frame` fileID 1618682377417806 / `Badge` 1621510681728303，两者的 Image 都是 `PA=1 · type=0`）
        //     ⇒ 传 `keepAspect: true`，⛔ 不拿拉伸顶替。
        //   · `renderQueue` 由**调用方**给（每个宿主页的号段不同），本文件不自己算。

        /// <summary>画档位框那一层（原版 `tierFrames[Max(0, tier-1)]` → `Image`）。
        /// 取不到图 ⇒ **null**（⛔ 不画、也不拿别的图顶替；`AllianceTrophyArt.GetTierFrame` 已出声）。</summary>
        public static ImageQuad DrawTierFrame(Transform parent, PxRect r, int tier, string name, int renderQueue)
        {
            var tex = AllianceTrophyArt.GetTierFrame(tier);
            return tex == null ? null : MenuDraw.Rect(parent, tex, r, name, renderQueue, null, true);
        }

        /// <summary>画徽章那一层（原版 `AllianceBadge.badgeImage` → `Image`）。`badgeId` 查不到 ⇒
        /// 走原版同一条回落（`DefaultTrophy` = `Default_Badge`），见 <see cref="Get"/>。
        /// 取不到图 ⇒ **null**（`Get` 已 `LogError` 一次）。</summary>
        public static ImageQuad Draw(Transform parent, PxRect r, string badgeId, string name, int renderQueue)
        {
            var tex = Get(badgeId);
            return tex == null ? null : MenuDraw.Rect(parent, tex, r, name, renderQueue, null, true);
        }

        /// <summary>自检：表结构 + **两条「名字推不出来」的映射**（灭自证：如果有人把表改成
        /// 「id 去掉后缀就是图名」，这几条过不去）+ 回落语义。</summary>
        public static bool SelfCheck(out string why)
        {
            why = null;
            if (Entries.Length != 16) { why = "表应有 16 条，实有 " + Entries.Length; return false; }
            var ids = new HashSet<string>();
            var sprites = new HashSet<string>();
            for (int i = 0; i < Entries.Length; i++)
            {
                if (string.IsNullOrEmpty(Entries[i].Id)) { why = "第 " + i + " 条没有 id"; return false; }
                if (string.IsNullOrEmpty(Entries[i].Sprite)) { why = "第 " + i + " 条没有图名"; return false; }
                if (!ids.Add(Entries[i].Id)) { why = "id 重复：" + Entries[i].Id; return false; }
                if (!sprites.Add(Entries[i].Sprite)) { why = "图名重复：" + Entries[i].Sprite; return false; }
            }

            string s;
            if (!TryGetSpriteName("UM_Forge_Badge", out s) || s != "Alliance_Trophies_Ultramarines")
            { why = "UM_Forge_Badge 应指向 Alliance_Trophies_Ultramarines"; return false; }
            // 🔴 灭自证 ①：GOFF 的图叫 `_Ork`（**名字推不出来**）
            if (!TryGetSpriteName("GOFF_Forge_Badge", out s) || s != "Alliance_Trophies_Ork")
            { why = "GOFF_Forge_Badge 应指向 Alliance_Trophies_Ork（不是 _Goff）"; return false; }
            // 🔴 灭自证 ②：UM_Event 指向 `_Space Marine 2 event`
            if (!TryGetSpriteName("UM_Event_Badge", out s) || s != "Alliance_Trophies_Space Marine 2 event")
            { why = "UM_Event_Badge 应指向 Alliance_Trophies_Space Marine 2 event"; return false; }
            // 未知 id / 空 id ⇒ 都回落到默认那一枚，且**不等于**上面任何一条
            string dflt = SpriteNameOf(null);
            if (dflt != "Alliance_Trophies_Default_v2") { why = "空 id 应回落到 Alliance_Trophies_Default_v2"; return false; }
            if (SpriteNameOf("Nope_Badge") != dflt) { why = "未知 id 应回落到默认徽章"; return false; }
            if (SpriteNameOf("GOFF_Forge_Badge") == dflt) { why = "已知 id 不该回落到默认徽章"; return false; }
            return true;
        }
    }
}
