// CollectionTabPage.cs —— 从 `Shell/CollectionWindow.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;      // `CardDatabase` / `CardDef`（`DeckDef` 那套在 RuleEngine 里）

namespace CardPresentation
{
    /// <summary>收藏窗的一页（四页共用这一个组件，按 `_page` 分）。</summary>
    public class CollectionTabPage : WindowTabBase
    {
        CollectionWindow _win;
        Transform _root;
        int _page;

        public override WindowTabType Type
        {
            get
            {
                switch (_page)
                {
                    case 0: return WindowTabType.CollectionDecks;
                    case 1: return WindowTabType.CollectionCards;
                    case 2: return WindowTabType.CollectionCosmetics;
                    default: return WindowTabType.CollectionStyles;
                }
            }
        }

        public void SetHost(CollectionWindow win, Transform root, int page)
        {
            _win = win; _root = root; _page = page;
        }

        bool _built;

        /// <summary>🔴 **2026-10-17（第四十八会话 · `T09` 性能第 1 步）：只建一次。**
        /// <para>原来 `CollectionWindow.Build()` 把四页**一次性全建** —— 而落地页是第 0 页，
        /// 实测首开 **≈75.5 s**（Cards 页 18 格 51.4 s · 异画页 6 格 17.3 s · 卡背 3.4 s · 卡组 2.75 s）。
        /// 原版是 `RecyclableScrollRect` + 协程懒建。</para>
        /// <para>⇒ 改成「**首开只建第 0 页，其余页首次切到再建**」（`OnOpen` 每次切页都会调）。
        /// 四页的页头/抽屉各自在自己的 `Setup()` 里建，`RefreshFilterToggles` / `RefreshClearFilters`
        /// 对 null 都已有守卫 ⇒ 延迟建不会漏刷。</para></summary>
        public void SetupOnce()
        {
            if (_built) return;
            _built = true;
            Setup();
        }

        public override void OnOpen()
        {
            SetupOnce();       // 首次切到本页时才真正建内容（之后只 SetActive 切换）
        }

        public override void Setup()
        {
            if (_win == null || _root == null) return;
            switch (_page)
            {
                case 0:
                    _win.BuildDeckHeader(_root);
                    _win.BuildDeckList(_root);
                    break;
                case 1:
                    _win.BuildCardsPage(_root);
                    break;
                case 2:
                    _win.BuildCosmeticsPage(_root);        // 233 张卡背 2026-09-23 已导 ⇒ 可建了
                    break;
                default:
                    _win.BuildStylesPage(_root);           // 异画页：本地 7 张督军异画（2 种风格）
                    break;
            }
        }
    }
}
