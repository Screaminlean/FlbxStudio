using FlbxStudio.Models;
using System;
using System.IO;
using System.Text;

namespace FlbxStudio.Services;

/// <summary>
/// Parses FLBX files into HMI vector assets and writes HMI vector assets to FLBX format.
/// </summary>
/// <remarks>Uses a binary FLBX layout with an asset catalog followed by per-state image payloads, preserving
/// asset names, state order, image format metadata, and raw image bytes.</remarks>
public class FlbxService : IFlbxService
{
    public List<HmiVectorAsset> ParseFlbx(string filePath)
    {
        var assetList = new List<HmiVectorAsset>();

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new BinaryReader(fs))
        {
            _ = reader.ReadInt16(); // version
            short sigLen = reader.ReadInt16();
            string signature = Encoding.ASCII.GetString(reader.ReadBytes(sigLen));
            short totalAssetsInFile = reader.ReadInt16();

            if (!string.Equals(signature, "flbx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Unsupported FLBX signature '{signature}'.");
            }

            var catalog = new List<(int Offset, short Index, string Name)>();
            for (int i = 0; i < totalAssetsInFile; i++)
            {
                int dataBlockOffset = reader.ReadInt32();
                short assetIndexMarker = reader.ReadInt16();
                short nameLen = reader.ReadInt16();
                string assetName = Encoding.ASCII.GetString(reader.ReadBytes(nameLen));
                catalog.Add((dataBlockOffset, assetIndexMarker, assetName));
            }

            long dataSectionStart = fs.Position;

            for (int i = 0; i < catalog.Count; i++)
            {
                var currentAsset = catalog[i];
                var asset = new HmiVectorAsset { AssetName = currentAsset.Name };

                long assetStart = ResolveAssetOffset(reader, currentAsset.Offset, dataSectionStart, fs.Length, fs.Length);
                int nextRawOffset = (i + 1 < catalog.Count) ? catalog[i + 1].Offset : (int)fs.Length;
                long assetEnd = ResolveSegmentEnd(assetStart, nextRawOffset, dataSectionStart, fs.Length);

                fs.Position = assetStart;

                while (fs.Position < assetEnd)
                {
                    if (!TryReadState(reader, assetEnd, out short stateIndex, out string format, out byte[] rawImageBytes))
                    {
                        break;
                    }

                    asset.States.Add(new HmiPictureState
                    {
                        StateIndex = stateIndex,
                        RawImageData = rawImageBytes,
                        ImageFormat = format
                    });
                }

                assetList.Add(asset);
            }

            if (catalog.Count == 1 && fs.Position < fs.Length)
            {
                ParseTrailingInlineAssets(reader, assetList, fs.Length);
            }
        }

        return assetList;
    }

    private static long ResolveAssetOffset(BinaryReader reader, int rawOffset, long dataSectionStart, long fileLength, long segmentEnd)
    {
        var candidates = new List<long>
        {
            rawOffset,
            dataSectionStart + rawOffset,
            rawOffset + 5,
            dataSectionStart + rawOffset + 5
        };

        foreach (long candidate in candidates)
        {
            if (LooksLikeStateAt(reader, candidate, segmentEnd, fileLength))
            {
                return candidate;
            }
        }

        long scanStart = Math.Max(0, dataSectionStart);
        long scanEnd = Math.Min(fileLength - 25, segmentEnd - 25);

        for (long pos = scanStart; pos <= scanEnd; pos++)
        {
            if (LooksLikeStateAt(reader, pos, segmentEnd, fileLength))
            {
                return pos;
            }
        }

        if (rawOffset < 0)
        {
            return 0;
        }

        if (rawOffset > fileLength)
        {
            return fileLength;
        }

        return rawOffset;
    }

    private static long ResolveSegmentEnd(long assetStart, int nextRawOffset, long dataSectionStart, long fileLength)
    {
        var candidates = new List<long>
        {
            nextRawOffset,
            dataSectionStart + nextRawOffset,
            nextRawOffset + 5,
            dataSectionStart + nextRawOffset + 5,
            fileLength
        };

        long best = fileLength;
        foreach (long candidate in candidates)
        {
            if (candidate > assetStart && candidate <= fileLength && candidate < best)
            {
                best = candidate;
            }
        }

        return best;
    }

    private static void ParseTrailingInlineAssets(BinaryReader reader, List<HmiVectorAsset> assetList, long fileLength)
    {
        while (reader.BaseStream.Position < fileLength &&
               TryReadInlineAssetHeader(reader, out string assetName, out int stateCount))
        {
            var asset = new HmiVectorAsset { AssetName = assetName };
            int targetStates = stateCount > 0 ? stateCount : 1;

            for (int i = 0; i < targetStates; i++)
            {
                if (!TryReadState(reader, fileLength, out short stateIndex, out string format, out byte[] rawImageBytes))
                {
                    break;
                }

                asset.States.Add(new HmiPictureState
                {
                    StateIndex = stateIndex,
                    RawImageData = rawImageBytes,
                    ImageFormat = format
                });
            }

            if (asset.States.Count == 0)
            {
                break;
            }

            assetList.Add(asset);
        }
    }

    private static bool TryReadInlineAssetHeader(BinaryReader reader, out string assetName, out int stateCount)
    {
        assetName = string.Empty;
        stateCount = 0;

        var fs = reader.BaseStream;
        long start = fs.Position;

        if (fs.Length - fs.Position < 8)
        {
            return false;
        }

        short marker = reader.ReadInt16();
        short nameLen = reader.ReadInt16();

        if (marker < 0 || nameLen <= 0 || nameLen > 512 || fs.Length - fs.Position < nameLen)
        {
            fs.Position = start;
            return false;
        }

        string name = Encoding.ASCII.GetString(reader.ReadBytes(nameLen));
        if (!IsLikelyAssetName(name))
        {
            fs.Position = start;
            return false;
        }

        if (fs.Length - fs.Position >= 4)
        {
            int value = reader.ReadInt32();
            if (value >= 0 && value <= 1024)
            {
                stateCount = value;
            }
            else
            {
                fs.Position -= 4;
            }
        }

        assetName = name;
        return true;
    }

    private static bool TryReadState(BinaryReader reader, long segmentEnd, out short stateIndex, out string format, out byte[] rawImageBytes)
    {
        stateIndex = 0;
        format = string.Empty;
        rawImageBytes = Array.Empty<byte>();

        var fs = reader.BaseStream;
        long initialPosition = fs.Position;

        try
        {
            if (segmentEnd - fs.Position < 25)
            {
                return false;
            }

            stateIndex = reader.ReadInt16();
            short formatLen = reader.ReadInt16();

            if (formatLen <= 0 || formatLen > 32 || segmentEnd - fs.Position < formatLen + 20)
            {
                fs.Position = initialPosition;
                return false;
            }

            var formatBytes = reader.ReadBytes(formatLen);
            format = Encoding.ASCII.GetString(formatBytes);

            if (!IsLikelyFormatToken(format))
            {
                fs.Position = initialPosition;
                return false;
            }

            _ = reader.ReadInt32();
            long positionAfterFlag = fs.Position;

            if (TryReadStatePayload(reader, segmentEnd, 8, out rawImageBytes) ||
                TryReadStatePayload(reader, segmentEnd, 4, out rawImageBytes))
            {
                return true;
            }

            fs.Position = positionAfterFlag;
            fs.Position = initialPosition;
            return false;
        }
        catch
        {
            fs.Position = initialPosition;
            return false;
        }
    }

    private static bool TryReadStatePayload(BinaryReader reader, long segmentEnd, int reservedBytes, out byte[] rawImageBytes)
    {
        rawImageBytes = Array.Empty<byte>();

        var fs = reader.BaseStream;
        long start = fs.Position;

        long bytesNeeded = reservedBytes + 4 + 4;
        if (segmentEnd - fs.Position < bytesNeeded)
        {
            return false;
        }

        _ = reader.ReadBytes(reservedBytes);
        _ = reader.ReadBytes(4);
        int imageByteLength = reader.ReadInt32();

        if (imageByteLength < 0 || imageByteLength > segmentEnd - fs.Position)
        {
            fs.Position = start;
            return false;
        }

        rawImageBytes = reader.ReadBytes(imageByteLength);
        if (rawImageBytes.Length != imageByteLength)
        {
            fs.Position = start;
            return false;
        }

        return true;
    }

    private static bool LooksLikeStateAt(BinaryReader reader, long offset, long segmentEnd, long fileLength)
    {
        if (offset < 0 || offset >= fileLength)
        {
            return false;
        }

        long savedPosition = reader.BaseStream.Position;
        reader.BaseStream.Position = offset;

        bool ok = TryReadState(reader, segmentEnd, out _, out _, out _);

        reader.BaseStream.Position = savedPosition;
        return ok;
    }

    private static bool IsLikelyFormatToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 8)
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!char.IsLetterOrDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsLikelyAssetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 512)
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ' '))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Writes the provided UI vector assets to an FLBX binary file.
    /// </summary>
    /// <remarks>The output uses ASCII-encoded identifiers and stores each state as PNG payload data in the
    /// FLBX structure.</remarks>
    /// <param name="outputPath">Path to the FLBX file to create or overwrite.</param>
    /// <param name="uiAssets">Assets to serialize, including asset names and state image data.</param>
    public void SaveAsFlbx(string outputPath, List<HmiVectorAsset> uiAssets)
    {
        using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            writer.Write((short)3);
            writer.Write((short)4);
            writer.Write(Encoding.ASCII.GetBytes("flbx"));
            writer.Write((short)uiAssets.Count);

            int currentDataOffset = 10;
            foreach (var asset in uiAssets)
            {
                currentDataOffset += 4 + 2 + 2 + Encoding.ASCII.GetByteCount(asset.AssetName);
            }

            for (short i = 0; i < uiAssets.Count; i++)
            {
                var asset = uiAssets[i];
                writer.Write(currentDataOffset);
                writer.Write(i);

                byte[] nameBytes = Encoding.ASCII.GetBytes(asset.AssetName);
                writer.Write((short)nameBytes.Length);
                writer.Write(nameBytes);

                foreach (var state in asset.States)
                {
                    currentDataOffset += 27 + state.RawImageData.Length;
                }
            }

            foreach (var asset in uiAssets)
            {
                for (short stateIdx = 0; stateIdx < asset.States.Count; stateIdx++)
                {
                    var state = asset.States[stateIdx];
                    writer.Write(stateIdx);
                    writer.Write((short)3);
                    writer.Write(Encoding.ASCII.GetBytes("PNG"));
                    writer.Write(asset.States.Count);
                    writer.Write((long)0);
                    writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0x00 });
                    writer.Write(state.RawImageData.Length);
                    writer.Write(state.RawImageData);
                }
            }
        }
    }
}