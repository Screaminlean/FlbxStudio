using System.Windows.Media;

namespace FlbxStudio.Models
{
    /// <summary>
    /// Represents a brush-backed color payload for UI data binding.
    /// </summary>
    public struct DataColor
    {
        /// <summary>
        /// Gets or sets the brush value.
        /// </summary>
        public Brush Color { get; set; }
    }
}
