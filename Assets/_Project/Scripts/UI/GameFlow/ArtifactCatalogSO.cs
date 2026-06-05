using System.Collections.Generic;
using UnityEngine;

namespace SlotRogue.UI.GameFlow
{
    [CreateAssetMenu(menuName = "SlotRogue/Artifact/Artifact Catalog", fileName = "ArtifactCatalog")]
    public sealed class ArtifactCatalogSO : ScriptableObject
    {
        private const string ResourcePath = "ArtifactCatalog";

        [SerializeField] private List<ArtifactDefinitionSO> _artifacts = new();

        public IReadOnlyList<ArtifactDefinitionSO> All => _artifacts;

        public IReadOnlyList<ArtifactDefinitionSO> GetByCategory(ArtifactCategory category)
        {
            var result = new List<ArtifactDefinitionSO>();

            for (int i = 0; i < _artifacts.Count; i++)
            {
                if (_artifacts[i] != null && _artifacts[i].Category == category)
                {
                    result.Add(_artifacts[i]);
                }
            }

            return result;
        }

        public ArtifactDefinitionSO GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < _artifacts.Count; i++)
            {
                if (_artifacts[i] != null && _artifacts[i].ArtifactId == id)
                {
                    return _artifacts[i];
                }
            }

            return null;
        }

        public static ArtifactCatalogSO Load()
        {
            return Resources.Load<ArtifactCatalogSO>(ResourcePath);
        }
    }
}
