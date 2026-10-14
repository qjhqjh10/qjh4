// TalentCardMap.cs — `Core/TalentCardData.cs` 的**另一半**（Unity 装载口）· 账 `A1873`
//
// 🔴 本文件是 **`partial class TalentCardData` 的实现半边**（文件名叫 `TalentCardMap` 是
//   因为「Data/ 这一层的活就是装载」，类名以 `Core/` 那半边为准 —— 它不是 MonoBehaviour，
//   不吃「类名 == 文件名」那条规矩，见 `CLAUDE.md` §二 那条 `!u!115` 的教训）。
//    照的**先例** = `Data/RelatedCardMap.cs`（同一形状）。
//
// 数据：`Assets/RuleEngine/Resources/talent_cards.json`
//   （生成器 = `工具/gen_talent_cards.py`，⛔ 别手改那个 json）
//   形状用**数组**（`JsonUtility` 不支持字典）。
//
// ⚠️ `Resources.Load` 认的是**全工程的 Resources 目录**（不限于本文件夹）⇒ 放在
//    `RuleEngine/Resources/` 与放在 `CardPresentation/Resources/` 对它是一样的。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleEngine
{
    public static partial class TalentCardData
    {
        /// <summary>运行时那份表的 `Resources` 路径（**没有扩展名**）。</summary>
        public const string ResourcePath = "talent_cards";

        static bool _tried;

        // ---- 运行时那份 JSON 的形状（字段名逐字照产物）----
        [Serializable] public class EntryDto { public string id; public string[] talents; }
        [Serializable] public class FileDto
        {
            public string note;
            public string source;
            public int count;
            public EntryDto[] entries;
        }

        /// <summary>装载那一半。**只试一次**（失败也不重试 —— 别每帧重来）。
        /// 载不到 / 解析失败一律**出声**并留 `_map == null`（`SpawnTalents` 退回按名字那条老路）。
        ///
        /// 🔴 **写进 `_map` 用【合并】、不整个换掉** —— `Core/TalentCardData.SetForTest` 可能已经先
        /// 往那张表里写了合成夹具的条目。两边键不会撞（夹具 id 形如 `FixtureTalentHost`），
        /// 所以「后写的赢」＝**真值优先**。⚠️ `_map == null` 那一支（载不到）**不做**：留 null
        /// 让上级照旧退回老路。</summary>
        static partial void LoadFromUnity()
        {
            if (_tried) return;
            _tried = true;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[天赋] `Resources/{ResourcePath}.json` **没载到** ⇒ "
                               + "`RuleCore.SpawnTalents` 退回「按卡面天赋名查池子」那条老路。"
                               + "重跑：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_talent_cards.py");
                return;
            }
            FileDto dto;
            try { dto = JsonUtility.FromJson<FileDto>(asset.text); }
            catch (Exception e)
            {
                Debug.LogError($"[天赋] `Resources/{ResourcePath}.json` 解析失败：{e.Message} ⇒ 退回老路");
                return;
            }
            if (dto == null || dto.entries == null || dto.entries.Length == 0)
            {
                Debug.LogError($"[天赋] `Resources/{ResourcePath}.json` 是空的 / 形状不对 ⇒ 退回老路");
                return;
            }
            if (_map == null) _map = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var e in dto.entries)
            {
                if (e == null || string.IsNullOrEmpty(e.id) || e.talents == null || e.talents.Length == 0)
                    continue;
                _map[e.id] = e.talents;
            }
        }
    }
}
