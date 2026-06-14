using ZuneDeploy.Transport;
using ZuneDeploy.XNA.Data;
using ZuneDeploy.XNA.Protocol;

namespace ZuneDeploy.XNA.Channels;

/// <summary>
/// This Channel is used to deploy .net / XNA runtimes to the Zune into a container that
/// can be referenced by game titles.
/// </summary>
/// <remarks>
/// The runtime token Zune.v4.0.Beta should be used to deploy the latest available version of the XNA runtime
/// </remarks>
/// <param name="client">Device to connect to</param>
public class RuntimeDeployChannel(Client client) : Channel(client, _channelGuid), IFileDeployChannel {
    public static readonly Guid _channelGuid = new("30D0E81E-D272-4735-ABD3-918ADAD29FD3");

    /// <summary>
    /// Opens a runtime container
    /// </summary>
    /// <param name="runtimeToken">Token, identifiying the runtime</param>
    /// <param name="exactVersion">Exact version of the runtime</param>
    public void OpenRuntimeContainer(string runtimeToken, long exactVersion) {
        Invoke("OpenRuntimeContainer", NormalizeRuntimeToken(runtimeToken), (int)exactVersion);
    }

    /// <summary>
    /// Close the currently open runtime container
    /// </summary>
    public void CloseRuntimeContainer() {
        Invoke("CloseRuntimeContainer");
    }

    /// <summary>
    /// Upload a file to the runtime container
    /// </summary>
    /// <param name="filePath">Path in the remote runtime container</param>
    /// <param name="fileContent">Data to upload</param>
    public void PutFileInContainer(string filePath, Stream fileContent) {
        ChannelValidation.ValidateFilePath(filePath);
        ChannelValidation.ValidateFileStream(fileContent);
        Invoke("PutFileInContainer", filePath, fileContent);
    }

    /// <summary>
    /// Check if a given runtime is available
    /// </summary>
    /// <param name="runtimeToken">Token, identifiying the runtime</param>
    /// <param name="exactVersion">Exact version of the runtime</param>
    /// <returns>true if runtime is available</returns>
    public bool IsRuntimeAvailable(string runtimeToken, long exactVersion) {
        return Invoke<bool>("IsRuntimeAvailable", NormalizeRuntimeToken(runtimeToken), (int)exactVersion);
    }

    /// <summary>
    /// Check if a given runtime is available
    /// </summary>
    /// <param name="container">Container to check for availability</param>
    /// <returns>true if runtime is available</returns>
    public bool IsRuntimeAvailable(RuntimeContainer container) {
        return Invoke<bool>("IsRuntimeAvailable", NormalizeRuntimeToken(container.RuntimeToken), (int)container.Version);
    }

    // The original driver does this
    // and the Zune seems to dislike any other runtime token.
    private static string NormalizeRuntimeToken(string runtimeToken) {
        if (runtimeToken == "Zune.v3.1") {
            return "Zune.v4.0.Beta";
        }
        return runtimeToken;
    }
}
