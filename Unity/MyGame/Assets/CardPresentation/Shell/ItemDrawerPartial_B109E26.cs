// ============================================================================ `Spec` 的卡牌类型补位（B109_E26 · 2026-10-31 第一百零九会话）
//
//  ── 这一件修的是什么 ──────────────────────────────────────────────────────────
//  `ItemDrawer.Spec(...)` 原来只从 `IdTypes`（4 条实据：两个卡包 / 两个掉落表）取 `ItemSpec.Type`
//  ⇒ **任何卡 id 的 `Type` 都是 `null`** ⇒ `PickDrawer` 走不到真表（`ItemTypeSets`）那一跳
//  ⇒ 落到兜底 `DrawerPlaceholder`。
//  战役奖励现读有 **15 个真卡 id**（`Shell/CampaignData.cs` 的 `Rewards` 里逐个比 `cards_engine.json` 的 id
//  得到：`UM2 UM3 UM8 UM13 UM17 UM18 UM22 UM24 UM27 UM28 UM34 UM37 UM39 UM44 UM55`，**全部 quantity = 1**）
//  ⇒ 今天这 15 格画的是**占位板**，而原版走 `CardDrawer`（= 一张真卡面）。
//
//  ── 原版判据（为什么「是卡 ⇒ RawCardScript ⇒ CardDrawer」）────────────────────
//  · 原版按**运行时类型**选抽屉：`ItemDrawer.get_DrawerConfig` → `ItemDrawerConfig.GetReference(item.GetType(), ov)`
//    —— 两轮谓词逐条现读：`decomp_full/ItemDrawerConfig.__c__DisplayClass1_0___GetReference_b__0.c`
//    = `TypeReferences_TypeReference.get_Type(entry.TypeReference) 比 item 的类型`（**精确**）·
//    `…_b__1.c` = `ReflectionHelper.Is(…)`（**is-a**）。
//    （`GetReference` 本体是**泛型**方法 ⇒ `decomp_full/` 里没有它的 `.c`，两个谓词 lambda 在、已逐行读。）
//  · 拿到的 `ItemDrawerReference` 再按键取档 ⇒ `TypeReference = RawCardScript` 那一条的主档抽屉
//    = `CardDrawer`（判据正本 = `资料/普查产出_1004/ItemDrawerConfig_映射表.md` §②；
//    我们这边落在 `Shell/ItemDrawer.cs` 的 `ItemTypeSets`：`("RawCardScript", "CardDrawer", …)`）。
//  ⇒ 原版「一格卡奖励画成卡面」的唯一前提 = **那件物品的运行时装着的是 `RawCardScript`**。
//
//  ── 我们没有运行时的 `ObtainableItem`，判据换成什么 ─────────────────────────
//  **id 在卡池里认得出** ⇒ `CollectionData.Card(id) != null`（且 `def.Id == id`，见下）。
//  这不是新发明的一条规则：`Shell/ItemDrawerCard.cs:112` 的 `CardDrawerItem`
//  **本来就是这么取卡的**（`CollectionData.Card(cardId)`，查不到才 `MissingArt` + 出声）
//  ⇒ 「`Spec` 说它是卡」与「卡抽屉画得出这张卡」用的是**同一个口**，两处永远一致。
//  ⛔ **不按前缀推**：`UM_SK_Starter` 也是 `UM` 开头，它不是卡（`cards_engine.json` 里没有这条 id）——
//     断言 C 段拿它当反例盯着。
//  ⛔ **不覆盖已有实据**：`IdTypes` 或野牌那条模式先填的 `Type` 一律不动（先填先赢）。
//  ⛔ **异画包装 id（`#HB_…`）不当 `RawCardScript`**：`CollectionData.Card` 对它是**剥 `#` 再查**回来的，
//     `def.Id != id` ⇒ 本函数**不填**（原版那种物品的类型是 `AlternateArtCard`，本机没有那条实据 ⇒ 留空出声，
//     铁律 2「查不到就说查不到」；战役奖励里现读 0 条这种 id）。
//
//  ── 落点与行号 ───────────────────────────────────────────────────────────────
//  `Shell/ItemDrawer.cs` 的 `Spec(...)` 被 `账/*.tsv` 按 `文件:行号` 钉着（A 落点 `:798`）
//  ⇒ **不能往那段里插行**（铁律 18·b）。本文件把逻辑放这儿，`ItemDrawer.cs` 只在 `Spec` 末尾那句
//  `return s;` 的**同一物理行**加一个调用（零行数变化；⚠️ 加在 `//` 注释之前，见铁律「同一物理行追加」那条）。

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `RawCardScript` —— `ItemTypeSets` / `ItemDrawerOptions` / `NonStackableTypes` 三张表里那个键（**逐字**）。</summary>
        public const string TypeRawCardScript = "RawCardScript";

        /// <summary>`Spec` 的卡牌类型补位（<see cref="Spec"/> 末尾那一句调它）。
        /// <para>只在**没有别的实据**时才动手：`s.Type` 非空（`IdTypes` 命中的那 4 条、或野牌那条模式）
        /// 一律原样返回 —— 「先填先赢」，本函数是**最后一道补充**，不是覆盖。</para>
        /// <para>判据 = `CollectionData.Card(id)`（**与 `CardDrawerItem` 同一个取卡口**，见文件头）。
        /// 查不到 / 查回来是别的 id（异画包装）⇒ **不填、不出声**（`Spec` 原本那条「类型判据空 ⇒ 上层出声 + 兜底」
        /// 的口径不变，出声由 `PickDrawer` 统一做、同 key 只报一次）。</para>
        /// <para>⚠️ **不碰 `Kind`**：`ItemKind` 只驱动 `PickDrawer` 的**兜底**那一档，而填上 `Type` 之后就走真表了
        /// （`RawCardScript` 在 `ItemTypeSets` 里，`GetDrawerClass` 一定答得出）⇒ 改 `Kind` 既无必要、又会与
        /// `ItemKind` 三个值的文档语义（「判据空」「有图但不知道抽屉」）打架。断言 A/B 两组盯的就是**抽屉名**，
        /// 不是 `Kind`。</para></summary>
        /// <remarks>**改坏法**：把本函数清空 ⇒ `Editor/B109_E26_CardReward_Assert.cs` 的 B 组 15 条全红；
        /// 把 `!string.IsNullOrEmpty(s.Type)` 那道守卫删掉 ⇒ C 组（`DT Ultramarines All` → `DropTableItem`、
        /// `WildcardUltramarines2` → `Wildcard`、`Booster Pack Ultramarines` → `Everguild.LiveOps.ShopContainer`）红。</remarks>
        internal static void FillCardType(ref ItemSpec s, string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            if (!string.IsNullOrEmpty(s.Type)) return;          // 已有实据（`IdTypes` / 野牌模式）⇒ ⛔ 不覆盖

            var def = CollectionData.Card(targetId);
            if (def == null) return;                            // 卡池里没有 ⇒ 判据空，保持原样
            if (def.Id != targetId) return;                     // 异画包装（`#HB_…` 剥前缀查回来的）⇒ 类型不是 RawCardScript，留空

            s.Type = TypeRawCardScript;
            if (string.IsNullOrEmpty(s.CardId)) s.CardId = def.Id;   // `RawCardScript` 的资产 id（`CardDrawer` 取卡用它）
        }
    }
}
