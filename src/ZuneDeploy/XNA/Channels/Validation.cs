namespace ZuneDeploy.XNA.Channels;

internal static class ChannelValidation {
    internal static void ValidateFilePath(string path) {
        if (path.Length > 40) {
            throw new Exception("Max file path length is 40");
        }
        foreach (char c in path) {
            if (c >= '\u0080' || c == '/') {
                throw new Exception($"Invalid Character {c} in file path");
            }
        }
    }

    internal static void ValidateFileStream(Stream stream) {
        if (stream.Length > int.MaxValue) {
            throw new Exception("File exceeds maximum size of 2 GB");
        }
    }
}