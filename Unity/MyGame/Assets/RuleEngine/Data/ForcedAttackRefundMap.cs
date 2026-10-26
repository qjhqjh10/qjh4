// ForcedAttackRefundMap.cs — `Core/ForcedAttackRefund.cs` 的**另一半**（Unity 装载口）
//
// 🔴 本文件是 **`partial class ForcedAttackRefund` 的实现半边**（文件名叫 `…Map` 是因为
//    「`Data/` 这一层的活就是装载」，类名以 `Core/` 那半边为准 —— 它不是 MonoBehaviour，
//    不吃「类名 == 文件名」那条规矩，见 `CLAUDE.md` §二 那条 `!u!115` 的教训）。
//    照的**先例** = `Data/RelatedCardMap.cs` / `Data/TutorialDatabase.cs`。
//
// 数据：`Assets/RuleEngine/Resources/cards_engine.json` 的 **v32 `chosenAbility` 列**
//   （生成器 = `工具/gen_cards_engine.py` 的 `chosen_abilities_of`，⛔ 别手改那个 json）。
//   这里**只取** `id` + `chosenAbility` 两格（`JsonUtility` 会静默忽略其余键），
//   判定规则在 `Core/ForcedAttackRefund.Install` 里（**只此一处**，⛔ 别在这里抄第二份）。
//
// ⚠️ 为什么要**再读一遍** `cards_engine.json`（`CardDatabase` 已经读过一次）：
//   `CardDef` / `CardDatabase` 今天**没有** `chosenAbility` 这一列（v24 的 `activeAbilityLogic[].ca`
//   只覆盖主动能力支）。把它接进 `CardDef` 要动 `Core/CardDef.cs` + `Data/CardDatabase.cs` 两处
//   （本笔的白名单之外 —— 见报告落盘请求）。这里改用「同一份文件、另一个瘦 DTO」的读法，
//   代价是**首次强制攻击时**多解析一次 3.3 MB（懒加载、只一次）⇒ 若将来 `CardDef` 接了那一列，
//   本文件应整份删掉、改由 `CardDatabase.Parse` 灌 `Install`。
using System;
using UnityEngine;

namespace RuleEngine
{
    public static partial class ForcedAttackRefund
    {
        static bool _tried;

        // ---- 只取两格（其余键 `JsonUtility` 静默忽略）----
        [Serializable]
        public class CardRowDto
        {
            public string id;
            public int[] chosenAbility;
        }

        [Serializable]
        public class DocDto
        {
            public int version;
            public int count;
            public CardRowDto[] cards;
        }

        /// <summary>装载那一半。**只试一次**（失败也不重试）。
        /// 载不到 / 解析失败一律**出声**并留 `_map == null` ⇒ 调用点退回旧行为（= 一律退还）。
        /// 🔴 **写进 `_map` 用【合并】**：`Core/ForcedAttackRefund.SetForTest` 可能已经先写过
        /// 合成夹具的条目（`FixtureA1606Each` 一类），整个换表会把它们抹掉。</summary>
        static partial void LoadFromUnity()
        {
            if (_tried) return;
            _tried = true;
            var asset = Resources.Load<TextAsset>(CardDatabase.ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[强制攻击退还] `Resources/{CardDatabase.ResourcePath}.json` **没载到** ⇒ "
                               + "`ForcedAttackRefund.Of` 一律 `Unknown` ⇒ 强制攻击**一律按旧行为退还**"
                               + "（235/236 那两族会被多退还一次行动）。"
                               + "重跑：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_cards_engine.py");
                return;
            }
            DocDto dto;
            try { dto = JsonUtility.FromJson<DocDto>(asset.text); }
            catch (Exception e)
            {
                Debug.LogError($"[强制攻击退还] `Resources/{CardDatabase.ResourcePath}.json` 解析失败："
                             + $"{e.Message} ⇒ 退回旧行为（一律退还）");
                return;
            }
            if (dto == null || dto.cards == null || dto.cards.Length == 0)
            {
                Debug.LogError($"[强制攻击退还] `Resources/{CardDatabase.ResourcePath}.json` 是空的 / 形状不对"
                             + " ⇒ 退回旧行为（一律退还）");
                return;
            }
            if (dto.version < 32)
                Debug.LogWarning($"[强制攻击退还] 卡表 version={dto.version} < 32 —— **没有 `chosenAbility` 列**"
                               + " ⇒ 本表落空 ⇒ 强制攻击一律按旧行为退还（235/236 那两族会被多退还一次行动）。"
                               + "跑一下 gen_cards_engine.py 重生成。");
            EnsureMap();                       // 合并（⛔ 不 `_map = new …`，见上方那条）
            int rows = 0;
            foreach (var c in dto.cards)
            {
                if (c == null || string.IsNullOrEmpty(c.id)) continue;
                rows++;
                Install(c.id, c.chosenAbility);   // 判定规则只在 `Core/` 那半边
            }
            _installedRows = rows;
        }

        /// <summary>装载读到的卡行数（**只给自检/诊断用**；探针里恒 0）。
        /// ⚠️ getter 自己触发装载（同 `RelatedCardData.TableCards` 的写法）—— 别让读这个数的人
        /// 依赖「别的用例先跑过」那种顺序假设。</summary>
        public static int InstalledRowsForTest { get { LoadFromUnity(); return _installedRows; } }

        static int _installedRows;

        /// <summary>`Core.ForcedAttackRefund.ResetForTest` 用的钩子：把「只试一次」的闸复位
        /// （否则清表之后永远不再装载，`Of()` 会恒 `Unknown`）。</summary>
        static partial void ResetLoaderForTest()
        {
            _tried = false;
            _installedRows = 0;
        }
    }
}
