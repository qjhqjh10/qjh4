// DeckStore.cs — 卡组存档（读写玩家自己组的卡组）
//
// ⚠️ 本文件用 UnityEngine（`Application.persistentDataPath` + `JsonUtility`）。
//    `Core/` 下的东西一律不碰 Unity，规则/模型在 `Core/DeckRules.cs` 里。
//
// 存在哪：`Application.persistentDataPath/WarpforgeDecks.json`
//   · Windows 编辑器/独立版都在 `%USERPROFILE%\AppData\LocalLow\<公司名>\<产品名>\`
//   · 不进工程、不进仓库、卸载才没 —— 原版的卡组是存在**服务器**上的
//     （`CardDeck.syncedToServer` / `deckId`），单机只能落到本地，这一点和原版不同。
//
// 为什么自己读写文件而不是 `PlayerPrefs`：卡组是一份**有结构、会变长**的数据，
// PlayerPrefs 是给零散键值用的，塞 JSON 进去在 Windows 上还会写注册表。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RuleEngine
{
    public static class DeckStore
    {
        public const string FileName = "WarpforgeDecks.json";

        /// <summary>自检用的路径改写口。设了就用它，不设就用玩家目录 —— 自检不该动玩家的真存档。</summary>
        public static string OverridePath;

        /// <summary>存档文件的完整路径。UI 上要显示给玩家看的时候用它。</summary>
        public static string Path
        {
            get
            {
                return OverridePath ?? System.IO.Path.Combine(Application.persistentDataPath, FileName);
            }
        }

        [Serializable]
        class Dto
        {
            public int version;
            public List<PlayerDeck> decks;
        }

        /// <summary>读全部卡组。文件不存在/坏了都返回空表 —— 不抛异常，让调用方能自己决定怎么提示。</summary>
        public static List<PlayerDeck> LoadAll(out string note)
        {
            note = null;
            var path = Path;
            if (!File.Exists(path))
            {
                note = "还没有存档";
                return new List<PlayerDeck>();
            }
            try
            {
                var text = File.ReadAllText(path);
                var dto = JsonUtility.FromJson<Dto>(text);
                if (dto == null || dto.decks == null)
                {
                    note = "存档解析失败（内容为空）";
                    return new List<PlayerDeck>();
                }
                // 归一化：JsonUtility 会把缺字段的字符串写成 null，UI 不想到处判空
                foreach (var d in dto.decks)
                {
                    if (d == null) continue;
                    d.Name = d.Name ?? "未命名";
                    d.WarlordId = d.WarlordId ?? "";
                    d.DefensiveId = d.DefensiveId ?? "";
                    if (d.CardIds == null) d.CardIds = new List<string>();
                }
                return dto.decks;
            }
            catch (Exception e)
            {
                note = "存档读取失败：" + e.Message;
                return new List<PlayerDeck>();
            }
        }

        public static List<PlayerDeck> LoadAll() { string _; return LoadAll(out _); }

        /// <summary>写全部卡组。返回 true = 写成功；失败时 `error` 里是人话。</summary>
        public static bool SaveAll(List<PlayerDeck> decks, out string error)
        {
            error = null;
            try
            {
                var dto = new Dto { version = 1, decks = decks ?? new List<PlayerDeck>() };
                var json = JsonUtility.ToJson(dto, true);
                // 先写临时文件再替换 —— 写一半崩了不至于把旧存档毁掉
                var tmp = Path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(Path)) File.Delete(Path);
                File.Move(tmp, Path);
                return true;
            }
            catch (Exception e)
            {
                error = "存档写入失败：" + e.Message;
                return false;
            }
        }

        public static bool SaveAll(List<PlayerDeck> decks) { string _; return SaveAll(decks, out _); }

        /// <summary>删掉存档文件（自检用 —— 真实玩家路径上没有这个入口）。</summary>
        public static void DeleteFile()
        {
            try { if (File.Exists(Path)) File.Delete(Path); } catch { /* 自检里失败无所谓 */ }
        }
    }
}
