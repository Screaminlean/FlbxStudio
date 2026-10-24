using FlbxStudio.Models;

namespace FlbxStudio.Services;

/// <summary>
/// Provides operations for parsing FLBX files into HMI vector assets and saving HMI vector assets as FLBX files.
/// </summary>
/// <remarks>Represents the FLBX serialization boundary for loading and persisting vector asset data.</remarks>
public interface IFlbxService
{
    /// <summary>
    /// Parses an FLBX file and extracts HMI vector assets.
    /// </summary>
    /// <param name="filePath">The path to the FLBX file to parse.</param>
    /// <returns>A list of HmiVectorAsset instances parsed from the specified file.</returns>
    List<HmiVectorAsset> ParseFlbx(string filePath);

    /// <summary>
    /// Saves the specified UI vector assets to an FLBX file at the provided path.
    /// </summary>
    /// <param name="outputPath">The full path where the FLBX file is saved.</param>
    /// <param name="uiAssets">The UI vector assets to include in the FLBX file.</param>
    void SaveAsFlbx(string outputPath, List<HmiVectorAsset> uiAssets);
}