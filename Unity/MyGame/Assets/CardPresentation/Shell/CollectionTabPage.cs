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
