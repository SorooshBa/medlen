using System.Buffers.Binary;
using System.Text;

namespace Medlen.DurationParsing;

public static class MediaDurationReader
{
    public static bool TryRead(string path, out TimeSpan duration, out string reason)
    {
        duration = default;
        reason = "unknown error";
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 64 * 1024, FileOptions.SequentialScan);
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var success = extension switch
            {
                ".wav" => TryReadWav(stream, out duration, out reason),
                ".mp3" => TryReadMp3(stream, out duration, out reason),
                ".flac" => TryReadFlac(stream, out duration, out reason),
                ".ogg" or ".opus" => TryReadOgg(stream, out duration, out reason),
                ".mp4" or ".m4a" or ".mov" => TryReadMp4(stream, out duration, out reason),
                _ => false
            };
            if (success && duration > TimeSpan.Zero)
                return true;
            if (success)
                reason = "duration is zero";
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or OverflowException)
        {
            reason = exception.Message;
            return false;
        }
    }

    private static bool TryReadWav(Stream stream, out TimeSpan duration, out string reason)
    {
        duration = default; reason = "not a valid WAV file";
        Span<byte> header = stackalloc byte[12];
        if (!ReadExactly(stream, header) || !header[..4].SequenceEqual("RIFF"u8) || !header[8..].SequenceEqual("WAVE"u8)) return false;
        uint byteRate = 0; ulong dataSize = 0;
        var chunk = new byte[8];
        var fmt = new byte[16];
        while (stream.Position + 8 <= stream.Length)
        {
            ReadExactly(stream, chunk);
            var size = BinaryPrimitives.ReadUInt32LittleEndian(chunk[4..]);
            var id = chunk[..4];
            if (id.SequenceEqual("fmt "u8))
            {
                if (size < 16 || stream.Position + size > stream.Length) { reason = "invalid WAV format chunk"; return false; }
                ReadExactly(stream, fmt);
                byteRate = BinaryPrimitives.ReadUInt32LittleEndian(fmt[8..]);
                Skip(stream, size - 16);
            }
            else if (id.SequenceEqual("data"u8)) { dataSize = size; Skip(stream, size); }
            else Skip(stream, size);
            if ((size & 1) == 1) Skip(stream, 1);
        }
        if (byteRate == 0 || dataSize == 0) { reason = "WAV is missing format or data metadata"; return false; }
        duration = TimeSpan.FromSeconds(dataSize / (double)byteRate); return true;
    }

    private static bool TryReadFlac(Stream stream, out TimeSpan duration, out string reason)
    {
        duration = default; reason = "not a valid FLAC file";
        Span<byte> marker = stackalloc byte[4];
        if (!ReadExactly(stream, marker) || !marker.SequenceEqual("fLaC"u8)) return false;
        Span<byte> blockHeader = stackalloc byte[4];
        if (!ReadExactly(stream, blockHeader) || (blockHeader[0] & 0x7f) != 0 || BinaryPrimitives.ReadUInt32BigEndian(stackalloc byte[] { 0, blockHeader[1], blockHeader[2], blockHeader[3] }) != 34) { reason = "FLAC STREAMINFO block is missing"; return false; }
        Span<byte> info = stackalloc byte[34]; if (!ReadExactly(stream, info)) { reason = "truncated FLAC STREAMINFO block"; return false; }
        var packed = BinaryPrimitives.ReadUInt64BigEndian(info[10..18]);
        var sampleRate = packed >> 44;
        var totalSamples = packed & 0xFFFFFFFFF;
        if (sampleRate == 0 || totalSamples == 0) { reason = "FLAC has no total-sample metadata"; return false; }
        duration = TimeSpan.FromSeconds(totalSamples / (double)sampleRate); return true;
    }

    private static bool TryReadMp3(Stream stream, out TimeSpan duration, out string reason)
    {
        duration = default; reason = "no MPEG audio frame found";
        var start = SkipId3v2(stream);
        var buffer = new byte[4];
        for (long position = start; position + 4 <= stream.Length && position < start + 1024 * 1024; position++)
        {
            stream.Position = position;
            if (stream.Read(buffer) != 4 || buffer[0] != 0xff || (buffer[1] & 0xe0) != 0xe0) continue;
            if (!TryGetMp3FrameInfo(BinaryPrimitives.ReadUInt32BigEndian(buffer), out var bitrate, out var sampleRate, out var samplesPerFrame, out var frameLength)) continue;
            var xingPosition = position + (GetMp3ChannelMode(buffer[3]) == 3 ? 21 : 36) + (((buffer[1] >> 3) & 3) == 3 ? 0 : 0);
            if (TryReadXingFrameCount(stream, xingPosition, out var frames))
                duration = TimeSpan.FromSeconds(frames * (double)samplesPerFrame / sampleRate);
            else
                duration = TimeSpan.FromSeconds((stream.Length - position) * 8d / (bitrate * 1000d));
            return duration > TimeSpan.Zero;
        }
        return false;
    }

    private static bool TryReadOgg(Stream stream, out TimeSpan duration, out string reason)
    {
        duration = default; reason = "not a valid Ogg stream";
        uint sampleRate = 0; ulong lastGranule = 0; var sawPage = false;
        var header = new byte[27];
        while (stream.Position + 27 <= stream.Length)
        {
            if (!ReadExactly(stream, header) || !header[..4].SequenceEqual("OggS"u8)) { reason = "invalid Ogg page"; return false; }
            var segmentCount = header[26]; var lacing = new byte[segmentCount];
            if (stream.Read(lacing) != segmentCount) { reason = "truncated Ogg page"; return false; }
            var bodyLength = lacing.Sum(x => (int)x); var body = new byte[bodyLength];
            if (stream.Read(body) != bodyLength) { reason = "truncated Ogg page body"; return false; }
            if (sampleRate == 0)
            {
                var index = IndexOf(body, "OpusHead"u8); if (index >= 0) sampleRate = 48000;
                index = IndexOf(body, "\x01vorbis"u8);
                if (index >= 0 && index + 16 <= body.Length) sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(index + 12, 4));
            }
            var granule = BinaryPrimitives.ReadUInt64LittleEndian(header[6..14]);
            if (granule != ulong.MaxValue) lastGranule = granule;
            sawPage = true;
        }
        if (!sawPage || sampleRate == 0 || lastGranule == 0) { reason = "Ogg codec or duration metadata is missing"; return false; }
        duration = TimeSpan.FromSeconds(lastGranule / (double)sampleRate); return true;
    }

    private static bool TryReadMp4(Stream stream, out TimeSpan duration, out string reason)
    {
        duration = default; reason = "MP4 movie-header atom (mvhd) is missing";
        return FindMp4Duration(stream, 0, stream.Length, out duration);
    }

    private static bool FindMp4Duration(Stream stream, long start, long end, out TimeSpan duration)
    {
        duration = default;
        var header = new byte[16];
        for (var position = start; position + 8 <= end; )
        {
            stream.Position = position; if (!ReadExactly(stream, header.AsSpan(0, 8))) return false;
            ulong atomSize = BinaryPrimitives.ReadUInt32BigEndian(header[..4]); var type = Encoding.ASCII.GetString(header[4..8]); var headerSize = 8;
            if (atomSize == 1) { if (!ReadExactly(stream, header[8..16])) return false; atomSize = BinaryPrimitives.ReadUInt64BigEndian(header[8..16]); headerSize = 16; }
            if (atomSize == 0) atomSize = (ulong)(end - position);
            if (atomSize < (ulong)headerSize || atomSize > (ulong)(end - position)) return false;
            var contentStart = position + headerSize; var atomEnd = position + (long)atomSize;
            if (type == "mvhd" && TryParseMvhd(stream, contentStart, atomEnd, out duration)) return true;
            if (type is "moov" or "trak" or "mdia" or "udta")
                if (FindMp4Duration(stream, contentStart, atomEnd, out duration)) return true;
            position = atomEnd;
        }
        return false;
    }

    private static bool TryParseMvhd(Stream stream, long start, long end, out TimeSpan duration)
    {
        duration = default; stream.Position = start; Span<byte> version = stackalloc byte[4]; if (!ReadExactly(stream, version)) return false;
        var needed = version[0] == 1 ? 28 : 16; if (start + 4 + needed > end) return false;
        Span<byte> data = stackalloc byte[28]; if (!ReadExactly(stream, data[..needed])) return false;
        uint scale; ulong value;
        if (version[0] == 1) { scale = BinaryPrimitives.ReadUInt32BigEndian(data[16..20]); value = BinaryPrimitives.ReadUInt64BigEndian(data[20..28]); }
        else { scale = BinaryPrimitives.ReadUInt32BigEndian(data[8..12]); value = BinaryPrimitives.ReadUInt32BigEndian(data[12..16]); }
        if (scale == 0 || value == 0) return false; duration = TimeSpan.FromSeconds(value / (double)scale); return true;
    }

    private static long SkipId3v2(Stream stream)
    {
        Span<byte> id3 = stackalloc byte[10]; stream.Position = 0;
        if (!ReadExactly(stream, id3) || !id3[..3].SequenceEqual("ID3"u8)) return 0;
        var size = ((id3[6] & 0x7f) << 21) | ((id3[7] & 0x7f) << 14) | ((id3[8] & 0x7f) << 7) | (id3[9] & 0x7f);
        return 10L + size + ((id3[5] & 0x10) != 0 ? 10 : 0);
    }

    private static bool TryGetMp3FrameInfo(uint value, out int bitrate, out int sampleRate, out int samples, out int frameLength)
    {
        bitrate = sampleRate = samples = frameLength = 0;
        var version = (int)((value >> 19) & 3); var layer = (int)((value >> 17) & 3); var bitrateIndex = (int)((value >> 12) & 15); var rateIndex = (int)((value >> 10) & 3); var padding = (int)((value >> 9) & 1);
        if (version == 1 || layer != 1 || bitrateIndex is 0 or 15 || rateIndex == 3) return false;
        var rates = new[] { 44100, 48000, 32000 }; sampleRate = rates[rateIndex] / (version == 3 ? 1 : version == 2 ? 2 : 4);
        var mpeg1 = new[] { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 };
        var mpeg2 = new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160 };
        bitrate = (version == 3 ? mpeg1 : mpeg2)[bitrateIndex]; samples = version == 3 ? 1152 : 576;
        frameLength = (version == 3 ? 144 : 72) * bitrate * 1000 / sampleRate + padding;
        return bitrate > 0 && sampleRate > 0;
    }

    private static int GetMp3ChannelMode(byte fourthHeaderByte) => fourthHeaderByte >> 6;
    private static bool TryReadXingFrameCount(Stream stream, long position, out uint frames)
    {
        frames = 0; if (position < 0 || position + 12 > stream.Length) return false; stream.Position = position;
        Span<byte> data = stackalloc byte[12]; if (!ReadExactly(stream, data) || (!data[..4].SequenceEqual("Xing"u8) && !data[..4].SequenceEqual("Info"u8))) return false;
        var flags = BinaryPrimitives.ReadUInt32BigEndian(data[4..8]); if ((flags & 1) == 0) return false; frames = BinaryPrimitives.ReadUInt32BigEndian(data[8..12]); return frames > 0;
    }
    private static int IndexOf(byte[] data, ReadOnlySpan<byte> pattern) => data.AsSpan().IndexOf(pattern);
    private static bool ReadExactly(Stream stream, Span<byte> buffer) { var read = 0; while (read < buffer.Length) { var n = stream.Read(buffer[read..]); if (n == 0) return false; read += n; } return true; }
    private static void Skip(Stream stream, long count) { if (count < 0 || stream.Position + count > stream.Length) throw new InvalidDataException("unexpected end of file"); stream.Position += count; }
}
