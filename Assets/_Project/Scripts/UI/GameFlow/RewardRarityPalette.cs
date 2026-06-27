using System;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    /// <summary>
    /// 등급별 색상 매핑 에셋. 인스펙터에서 자유롭게 수정한다.
    /// 슬롯 아트 1장(카드/안쪽/테두리를 밝기 차이로 그린 회색 스프라이트)에 티어 색 하나를 곱한다.
    /// 색은 슬롯 Image.color로 주입된다(단색 tint).
    /// 메뉴: Assets > Create > SlotRogue > Reward Rarity Palette
    /// </summary>
    [CreateAssetMenu(
        fileName = "RewardRarityPalette",
        menuName = "SlotRogue/Reward Rarity Palette",
        order = 0)]
    public sealed class RewardRarityPalette : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public RewardRarity Rarity;

            [Tooltip("이 등급의 슬롯 색(아트 스프라이트에 곱해짐).")]
            public Color TintColor;
        }

        [SerializeField]
        private Entry[] _entries = CreateDefaults();

        public Color ColorFor(RewardRarity rarity)
        {
            if (_entries != null)
            {
                for (int index = 0; index < _entries.Length; index++)
                {
                    if (_entries[index].Rarity == rarity)
                    {
                        return _entries[index].TintColor;
                    }
                }
            }

            return Color.white;
        }

        /// <summary>인스펙터에서 비워두면 채워지는 기본 팔레트(레퍼런스 색감).</summary>
        public static Entry[] CreateDefaults()
        {
            return new[]
            {
                Make(RewardRarity.Common, new Color32(0xC9, 0xCF, 0xDB, 0xFF)),
                Make(RewardRarity.Uncommon, new Color32(0x4C, 0xD1, 0x37, 0xFF)),
                Make(RewardRarity.Rare, new Color32(0x35, 0x8C, 0xF0, 0xFF)),
                Make(RewardRarity.Epic, new Color32(0xA9, 0x4C, 0xF0, 0xFF)),
                Make(RewardRarity.Legendary, new Color32(0xF5, 0xB3, 0x2E, 0xFF)),
                Make(RewardRarity.Curse, new Color32(0xE5, 0x3A, 0x3A, 0xFF)),
            };
        }

        private static Entry Make(RewardRarity rarity, Color tint)
        {
            return new Entry { Rarity = rarity, TintColor = tint };
        }

#if UNITY_EDITOR
        // 인스펙터에서 색을 바꾸면 이 팔레트를 쓰는 슬롯의 미리보기를 즉시 갱신한다.
        private void OnValidate()
        {
            RewardSlotRarity[] slots =
                Resources.FindObjectsOfTypeAll<RewardSlotRarity>();
            for (int index = 0; index < slots.Length; index++)
            {
                RewardSlotRarity slot = slots[index];
                if (slot != null && slot.EditorPalette == this)
                {
                    slot.EditorApplyPreview();
                }
            }
        }
#endif
    }
}
