// DraftWarlordTriage.cs — 轮抽「谁能当候选战将」的**原版尾巴三档剔除**（账 `轮抽:14` · 执行代理 `B97_SMALL` 2026-10-29）
//
// ============================ 这是什么 / 判据在哪 ============================
// 原版：`DraftEventBase<TS,TD>.GetPossibleWarlords()`（**泛型类方法 ⇒ `decomp_full` 无独立产物**）。
// 真体 = `d:/2/tools/decomp_generic_trial/out_b94/DraftEventBase_object__object_.GetPossibleWarlords__18109d0.c`
// （251 行，2026-10-29 本代理**现读全文**）。它分两段：
//
// ① **取候选源**（三支互斥）——
//    · `data.specificWarlords(+0xb8)` 有货（`Count >= *(data+0xa0)`）⇒ 「显式名单」支；
//    · 否则 `data.specificArmies(+0xc0)` 非空 ⇒ `Where(w => specificArmies.Contains(w.<cardArmy>))`
//      —— 判据 = lambda `DraftEventBase_object__object_._GetPossibleWarlords_b__20_1__1813f80.c`：
//      `Enumerable.Contains(*(this+0x18)+0xc0, *(int*)(w+0x2c))`，`w+0x2c` = `CardArmy`（`PlayerItem` 基底字段）；
//    · 两者都空 ⇒ 取**玩家自己的战将表**（`PlayerDataManager` 那一侧），无过滤；
//    最后 `data.blacklistedWarlords(+0xd0)` 逐条 `list.Remove(entry.Reference)`（`.c` 尾部那个 `while`）。
//
// ② **逐张剔除三档**（`.c` 倒数第二个 `while`，`i` 从 `Count-1` 递减；命中任一 ⇒ `RemoveAt(i)`）——
//    · **档 A**：`*(int *)(card + 0x4c) == 10`。
//      `0x4c` = `RawCardScript.inventoryOptions`（`d:/2/tools/il2cpp_out/dump.cs:23252`）；
//      `10` = `CardInventoryOptions.NotInInventory`（同文件 `:45676` 起那份枚举：`InInventory=0` / `CantAddToDeck=5` / `NotInInventory=10`）
//      ⇒ **「不在库存里」的卡不能当候选**。
//    · **档 B**：`(**(code **)(*card + 0x2a8))(card, *(longlong *)(*card + 0x2a8 + 8))`。
//      🔴 **不是「虚槽 83」**（`判据/B96_商店轮抽个人32_分诊.md` 里那一行的旧读数——本代理就地订正）：
//      IL2CPP 的虚表项是 **`VirtualInvokeData{ methodPtr; methodInfo; }` = 16 字节**
//      （`d:/2/tools/il2cpp_out/il2cpp.h` 的 `struct Il2CppClass` 末尾 `VirtualInvokeData vtable[255]`），
//      所以 `klass+0x2a8` = **方法指针**、`klass+0x2b0` = **同一项的 `MethodInfo*`**
//      ⇒ 那个「第二个实参」**根本不是实参**（`B96_L3` 把它读成 `*(p + 0x1580)` 是同一个误读的另一半）。
//      vtable 基址 = `sizeof(Il2CppClass_1) = 0xB8` + `static_fields 8` + `rgctx_data 8` + `sizeof(Il2CppClass_2) = 0x70`
//      = **0x138** ⇒ 槽号 = `(0x2a8 − 0x138) / 16` = **23**
//      = `PlayerItem.IsHidden()`（`dump.cs:18906` 那行 `Slot: 23`，`public virtual bool IsHidden()`）——**无参虚 bool**，与形状吻合。
//    · **档 C**：`RawCardScript.IsAlternateArt()`（`decomp_full/RawCardScript__IsAlternateArt.c`：取 `+0x88 currentAlternateArt`
//      再走 `UnityEngine.Object.op_Implicit` ⇒ **非 null 就剔除**）。
//
// ============================ 我们落哪几档（逐档如实标）============================
// ✅ **② 的三档全部照抄** —— 判据齐、落点就在本类；调用点 = `DraftModeWindow.RollWarlords` 建候选池的那一行。
// ⛔ **① 的三支本地做不到**：`specificWarlords` / `specificArmies` / `blacklistedWarlords` 全在 **LiveOps 事件数据**（服务端）。
//    实证（本代理 2026-10-29 现读）：`CardPresentation/Resources/draft_packs.json` 只有 `PrebuiltPack`（`count=882`，无事件、无日期）；
//    `判据/B96_商店轮抽个人32_分诊.md` §二已核「三个数据面全在远端」；`_tmp_view/apk135/` 的远端 `catalog_strings.txt` 里也没有 draft 事件。
//    ⇒ 只能走「两份名单都空」那一支（= 改前行为）——**这不是偏离，是原版在该数据形态下的同一支**。
//
// ============================ 🔴 与 B96 分诊的两处就地订正（铁律 5）============================
//   1. 「虚槽 83」→ **槽 23 = `PlayerItem.IsHidden()`**（推导见上，`VirtualInvokeData` 16 字节是关键）。
//   2. 「分支 1a 的实例谓词（`data + 0x1a8`，目标是 `this`）」→ **不成立**：`.c` 里那个 `+0x1a8` 是
//      **`rgctx_data` 的第 53 项**（`*(longlong*)(*(longlong*)(param_2 + 0x20) + 0xc0) + 0x1a8`，`param_2` = `MethodInfo*`，
//      `+0x20` = `klass`、`+0xc0` = `rgctx_data`），装的是**委托方法指针**；委托的 target 是 `**(klass+0xb8)`
//      （= `static_fields` 的第 0 格 = `<>c.<>9` 单例）。对应 lambda 的体已读完：
//      `_GetPossibleWarlords_b__20_0__eaa0b0.c` = `w => w.Reference`（`LoadableReference<T>.get_Reference`，**取值器**，不是谓词）。
using System;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>原版 `DraftEventBase.GetPossibleWarlords()` 尾巴那三档剔除（判据全文见本文件头）。
    /// <para>⛔ 本类**只做 ②**（本地判据齐的那三档）；① 的三支在服务端，见文件头。</para>
    /// <para>⚠️ 为什么单独一个类：`Shell/DraftModeWindow.cs` 被账按**行号**引用（`:3247` / `:4281` / `:4282` …），
    /// 往里插方法会让下游落点整体漂移（CLAUDE.md 那条「新成员追加类尾 / 别插进已有成员之间」）。
    /// 这个文件是**新文件**，对既有行号零影响；`DraftModeWindow` 侧只改**一行**的条件（不改行数）。</para></summary>
    public static class DraftWarlordTriage
    {
        /// <summary>原版档 A 的字面量：`CardInventoryOptions.NotInInventory`（`dump.cs:45676` 起那份枚举）。
        /// ⚠️ 原版写的是**等值比较 `== 10`**，不是「`>= 10`」也不是「`!= InInventory`」——
        /// 所以 `CantAddToDeck(5)` **不**被这一档剔除，`CardDef.InventoryOptions == -1`（表里查不到这张卡）
        /// 也**不**被剔除（`-1 != 10`）。逐字照抄，⛔ 别「顺手修好」。</summary>
        public const int NotInInventory = 10;

        /// <summary>原版档 A：`RawCardScript.inventoryOptions(+0x4c) == 10` ⇒ 这张卡不能当候选。
        /// <para>数据面：`cards_engine.json` 现读 1129 张里 `inventoryOptions == 10` 有 **129** 张、`== 5` 有 39 张、
        /// `== 0` 有 961 张；**56 张 `type == "hero"` 全部是 0** ⇒ 本档在当前数据下剔除 **0** 张（改前改后候选池一样大）。</para></summary>
        public static bool IsNotInInventory(CardDef c) { return c != null && c.InventoryOptions == NotInInventory; }

        /// <summary>原版档 B：`PlayerItem.IsHidden()`（虚槽 23）。真体 = `decomp_full/PlayerItem__IsHidden.c`：
        /// `FeatureConfig.IsItemHidden(ConfigManager.GetConfig&lt;FeatureConfig&gt;(), this)`，**config 为 null 时原版落到
        /// `FUN_1803f47a0()` = NRE 桩**（`.c` 尾部那句 `/* Subroutine does not return */`）。
        /// <para>🔶 自己设计（非原版）：那一格来自**远端 `FeatureConfig`**（服务器已关服 ⇒ 判据在远端），
        /// 按「拿不到 config」那一支恒取 `false`（= 不隐藏）。先例 = `Shell/DraftEventBase.cs` 的 `DisableNewMatchMaking`
        /// （同一份 `FeatureConfig`、同一条口径）· 调度台 2026-10-28 口径（P3 服务端数据 ⇒ 本地模拟 + 出声）·
        /// B97_SMALL 2026-10-29 落成常量。⛔ 别在没有判据时改成 `true`。</para></summary>
        public static bool IsHidden(CardDef c) { return false; }

        /// <summary>原版档 C：`RawCardScript.IsAlternateArt()` = `currentAlternateArt(+0x88) != null`。
        /// <para>🔴 这是**运行期选中**的那一格（原版在「换上异画」时才写），**不是** `alternateArts(+0x80)` 那份列表 ——
        /// 两件事别混。本地为什么恒 `false`（两条现读证据，不是猜）：
        /// ① `out_cpp2il_dev_ilspy` 全树 `currentAlternateArt =`（**写**）**0 命中**，只有 5 处读
        ///    （`AlternateArtCardCollectionTab.cs:265/269/520/524` · `AlternateArtPanel.cs:347` · `CardInventory.cs:168-169`）；
        /// ② 它是序列化字段，卡 SO 里从来没人写 ⇒ 恒 `null`。
        /// ⚠️ 我们的 `CardDef.HasAlternateArt(id)` 表达的是**「这张卡有没有 `+0x80` 那份列表里的某条」**，
        /// 与档 C 的语义**不同**⇒ ⛔ 别拿它顶档 C（那会把 7 张有异画的卡错误剔除）。</para></summary>
        public static bool IsAlternateArt(CardDef c) { return false; }

        /// <summary>原版 `GetPossibleWarlords()` 尾巴那三档：<see cref="IsNotInInventory"/> / <see cref="IsHidden"/> /
        /// <see cref="IsAlternateArt"/>，任一命中 ⇒ **这张卡不能当候选战将**。
        /// <para>`null` ⇒ `false`（原版 `.c` 里 `iVar1` 那一格先判 `!= 0` 才往下走；我们的池不会有 null，
        /// 但 `RollWarlords` 的既有判据是 `all[i] != null &amp;&amp; …` ⇒ 这里返回 false 与那条 `&&` 链一致，不改变语义）。</para></summary>
        public static bool IsPossibleWarlord(CardDef c)
        {
            if (c == null) return false;
            if (IsNotInInventory(c)) return false;      // 原版 `.c` 档 A（`+0x4c == 10`）
            if (IsHidden(c)) return false;              // 原版 `.c` 档 B（虚槽 23 = PlayerItem.IsHidden）
            if (IsAlternateArt(c)) return false;        // 原版 `.c` 档 C（RawCardScript.IsAlternateArt）
            return true;
        }
    }
}
