// RelatedCardMap.cs — `Core/RelatedCardData.cs` 的**另一半**（Unity 装载口）
//
// 🔴 本文件是 **`partial class RelatedCardData` 的实现半边**（文件名叫 `RelatedCardMap` 是
//   因为「Data/ 这一层的活就是装载」，类名以 `Core/` 那半边为准 —— 它不是 MonoBehaviour，
//   不吃「类名 == 文件名」那条规矩，见 `CLAUDE.md` §二 那条 `!u!115` 的教训）。
//    照的**先例** = `Data/TutorialDatabase.cs`（声明在 `Core/TutorialScript.cs`，
//    装载在 `Data/`；探针只编 `Core/*.cs` ⇒ 那一半被当成「没有实现」抹掉）。
//
// 数据：`Assets/CardPresentation/Resources/related_cards.json`
//   （生成器 = `工具/gen_related_cards.py`，⛔ 别手改那个 json）
//   形状用**数组**（`JsonUtility` 不支持字典）。
//
// 判据 / 缺口 / 对不上的那些 → `资料/普查产出_第二十八会话/W3_相关卡接真值.md`
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleEngine
{
    public static partial class RelatedCardData
    {
        /// <summary>运行时那份表的 `Resources` 路径（**没有扩展名**）。</summary>
        public const string ResourcePath = "related_cards";

        static bool _tried;

        // ---- 运行时那份 JSON 的形状（字段名逐字照产物）----
        [Serializable] public class EntryDto { public string id; public string[] related; }
        [Serializable] public class FileDto
        {
            public string note;
            public string source;
            public string provenance;
            public string invariant;
            public int count;
            public int edges;
            public int skipped;
            public EntryDto[] entries;
        }

        /// <summary>装载那一半。**只试一次**（失败也不重试 —— 别每帧重来）。
        /// 载不到 / 解析失败一律**出声**并留 `_map == null`（两个消费者各自退回老路）。
        ///
        /// 🔴 **2026-10-14（`A1850`）：写进 `_map` 用【合并】、不再整个换掉** ——
        ///     `Core/RelatedCardData.SetForTest` 可能已经先往那张表里写了合成夹具的条目
        ///     （自检夹具要过 `ShouldTriggerCompanion` 的严格门），换成新表会把它们抹掉。
        ///     两边键不会撞（夹具 id 形如 `FixtureHost`），所以「后写的赢」＝**真值优先**。
        ///     ⚠️ `_map == null` 那一支（载不到）**不做**：留 null 让上级照旧退回老路。</summary>
        static partial void LoadFromUnity()
        {
            if (_tried) return;
            _tried = true;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[相关卡] `Resources/{ResourcePath}.json` **没载到** ⇒ "
                               + "两个消费者都退回老路（详情窗走替身三支；伴生部队按卡面名字查池子）。"
                               + "重跑：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_related_cards.py");
                return;
            }
            FileDto dto;
            try { dto = JsonUtility.FromJson<FileDto>(asset.text); }
            catch (Exception e)
            {
                Debug.LogError($"[相关卡] `Resources/{ResourcePath}.json` 解析失败：{e.Message} ⇒ 退回老路");
                return;
            }
            if (dto == null || dto.entries == null || dto.entries.Length == 0)
            {
                Debug.LogError($"[相关卡] `Resources/{ResourcePath}.json` 是空的 / 形状不对 ⇒ 退回老路");
                return;
            }
            if (_map == null) _map = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var e in dto.entries)
            {
                if (e == null || string.IsNullOrEmpty(e.id) || e.related == null || e.related.Length == 0)
                    continue;
                _map[e.id] = e.related;
            }
        }
    }
}
