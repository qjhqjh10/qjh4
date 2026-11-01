// CardDatabaseProbeHook.cs — 探针侧对 `CardDatabase` **两个私有映射函数**的公开转发
//   写手 `W-25` · 2026-11-01 · 活状态表 `B-257`（同族：`A1911` / `A1959` / `B8_AV`）
//
// 🔴 为什么需要它：
//   `Program.LoadPool` 是 `System.Text.Json` **手读**卡表，不走 `Data/CardDatabase.cs`
//   ⇒ 引擎 `CardDatabase.cs:1762` 那两句（`AttachTriggerAbilityIndices(MapTriggerAbilityIndices(...))`
//   ＋ `AttachTriggerOpsByNumber(MapTriggerOpsByNumber(def))`）在探针里**从来没被执行过**。
//   后果：探针池里每张卡 `HasTriggerOpsByNumber == false` ⇒ `CardDef.TriggerOpsByNumber(int)`
//   会**静默落进「卡面关键词 == 触发词名」退回档**（`Core/CardDefPartial_B96CA.cs:102` 的第二级）
//   ⇒ 任何拿 `Program.PoolList` 量 `TriggerOpsByNumber` 的读数（`play` 那条链上的 `EffectResolver`
//   全都在用它）**看着像产品口径、其实不是同一条路**。
//
// 🔑 为什么是「partial 转发」而不是反射 / 抄一份：
//   ① **C# 语言规则**：`partial class` 的所有 part 是**同一个类**，`private` 成员**跨 part 可见**；
//      而本文件与 `Assets/RuleEngine/Data/CardDatabase*.cs` 被编进**同一个程序集**
//      （`ruleprobe.csproj` 显式列了两者）⇒ 这里能直接调那两个 private mapper。
//   ② **不许抄一份**：这两个映射里有「行级坏值丢弃 / `(ab,lg)` 撞键去重 / 按 `ab` 归位 /
//      同一 ability 的多条 logic 行只算一次 / 三态『不挂·挂空表·挂非空』」 —— 抄进探针 =
//      两处写同一条规则（迟早不一致，**正是本账要治的病**）。
//   ③ **不用反射**：反射在方法改名时**静默落空**（= 又退回退回档，缺陷原样复发）；
//      partial 转发是**编译期**绑定 —— 引擎一改名，`ruleprobe.sh` 当场编不过。
//      （`Assets/**` 不在本写手白名单 ⇒ ⛔ 不许把那两个方法改成 `public`。）
//
// ⚠️ 本文件**不在 `Assets/` 下** ⇒ 不会进 Unity 工程，只在 `ruleprobe.csproj` 的显式编译清单里。
// ⛔ 别把这两个转发扩散成「探针能调引擎一切私有方法」的口子 —— 只服务 `LoadPool` 这一处口径对齐。
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class CardDatabase
    {
        /// <summary>转发到私有 `MapTriggerAbilityIndices(CardDto, int)`（`Data/CardDatabase.cs:4813`）——
        /// `triggerAbilityIndices` 列 → `CardDef.TriggerAbilityIndexSpec[]`。
        /// 三态同引擎：`null` = 缺列（未知）· `[]` = 明确没有 · 非空 = 有行。</summary>
        public static CardDef.TriggerAbilityIndexSpec[] ProbeMapTriggerAbilityIndices(CardDto card, int version)
            => MapTriggerAbilityIndices(card, version);

        /// <summary>转发到私有 `MapTriggerOpsByNumber(CardDef)`（`Data/CardDatabasePartial_B98H7.cs:134`）——
        /// `{触发数值 → 正文 op}` 表。`null` = **不挂**（列未知 / 一条都归不了位）⇒ 消费口保留退回档；
        /// 空表 = 挂上（这张卡的触发数值都没有可跑的正文）。
        /// ⚠️ **必须排在 `AttachTriggerAbilityIndices` 之后**（它读那一列）。</summary>
        public static Dictionary<int, List<EffectOp>> ProbeMapTriggerOpsByNumber(CardDef def)
            => MapTriggerOpsByNumber(def);
    }
}
