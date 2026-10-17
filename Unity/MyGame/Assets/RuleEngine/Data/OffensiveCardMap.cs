// OffensiveCardMap.cs — `Core/OffensiveCardData.cs` 的**另一半**（Unity 装载口）· 账 `A1547`
//
// 🔴 本文件是 **`partial class OffensiveCardData` 的实现半边**（文件名叫 `OffensiveCardMap`
//   是因为「Data/ 这一层的活就是装载」，类名以 `Core/` 那半边为准 —— 它不是 MonoBehaviour，
//   不吃「类名 == 文件名」那条规矩）。照的先例 = `Data/TalentCardMap.cs` / `Data/RelatedCardMap.cs`。
//
// 数据：`Assets/RuleEngine/Resources/offensive_card_abilities.json`
//   来源 = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（2022 快照）。
//   形状用**数组**（`JsonUtility` 不支持字典）。
//
// ⚠️ `Resources.Load` 认的是**全工程的 Resources 目录** ⇒ 放 `RuleEngine/Resources/` 就行。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleEngine
{
    public static partial class OffensiveCardData
    {
        static bool _tried;

        // ---- 运行时那份 JSON 的形状（字段名逐字照产物）----
        [Serializable] public class FileDto
        {
            public string note;
            public string source;
            public OffensiveCardEntry[] cards;
        }

        /// <summary>装载那一半。**只试一次**（失败也不重试 —— 别每帧重来）。
        /// 载不到 / 解析失败一律**出声**并留 `_map == null`（调用方照常「什么都不做」）。</summary>
        static partial void LoadFromUnity()
        {
            if (_tried) return;
            _tried = true;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[进攻卡] `Resources/{ResourcePath}.json` **没载到** ⇒ "
                               + "`AbilityTrigger.PlayedThirdCardInTurn`（`A1547`）这一档不会触发。"
                               + "该文件在 `Assets/RuleEngine/Resources/`，来源见文件里的 `note`");
                return;
            }
            FileDto dto;
            try { dto = JsonUtility.FromJson<FileDto>(asset.text); }
            catch (Exception e)
            {
                Debug.LogError($"[进攻卡] `Resources/{ResourcePath}.json` 解析失败：{e.Message}");
                return;
            }
            if (dto == null || dto.cards == null || dto.cards.Length == 0)
            {
                Debug.LogError($"[进攻卡] `Resources/{ResourcePath}.json` 是空的 / 形状不对");
                return;
            }
            if (_map == null) _map = new Dictionary<string, OffensiveCardEntry>(StringComparer.Ordinal);
            foreach (var c in dto.cards)
            {
                if (c == null || string.IsNullOrEmpty(c.envSO)) continue;
                _map[c.envSO] = c;
            }
        }
    }
}
