using TMPro;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 평소(상점 닫힘) 재화(별조각) HUD. 상점이 열리면 숨긴다
    /// (상점 열림 재화는 RunBattlePlayerHudView의 Star Text가 담당).
    /// RunBattleScreenView가 전투 상태로 Render를 호출한다.
    /// </summary>
    public sealed class RunBattleCurrencyView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _text;

        public void Render(int runCoins, bool hidden)
        {
            if (_panel != null)
            {
                _panel.SetActive(!hidden);
            }
            else
            {
                gameObject.SetActive(!hidden);
            }

            if (hidden || _text == null)
            {
                return;
            }

            _text.text = Mathf.Max(0, runCoins).ToString();
        }
    }
}
