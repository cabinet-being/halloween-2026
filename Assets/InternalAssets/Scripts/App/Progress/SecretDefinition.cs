using UnityEngine;

namespace Halloween2026
{
    [CreateAssetMenu(menuName = "Halloween/Secret")]
    public sealed class SecretDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Permanent save ID. Keep unchanged after release; use a unique value for every secret.")]
        private string _id;

        [SerializeField]
        private string _title;

        [SerializeField, TextArea]
        private string _description;

        [SerializeField]
        private Sprite _artwork;

        public string Id => _id;
        public string Title => _title;
        public string Description => _description;
        public Sprite Artwork => _artwork;
    }
}
