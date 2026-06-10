using ZuneDeploy.Transport;
using ZuneDeploy.XNA.Data;
using ZuneDeploy.XNA.Protocol;

namespace ZuneDeploy.XNA.Channels;

public class GameDeployChannel(Client client) : Channel(client, _channelGuid) {
    private static readonly Guid _channelGuid = new("AA3C2881-4EB9-4af6-8137-635C2E64CE4A");

    public bool OpenContainer(ApplicationContainer container) {
        return OpenGameContainer(container.ContainerId, container.DisplayName);
    }

    /// <summary>
    /// Opens a Game Container
    /// </summary>
    /// <param name="containerId">Id of container to open</param>
    /// <param name="titleName">Name of the container. Will be capped to 127 characters</param>
    /// <returns>True if the container was already open</returns>
    public bool OpenGameContainer(Guid containerId, string titleName) {
        if (titleName.Length > 127) {
            titleName = titleName[..127];
        }
        return Invoke<bool>("OpenGameContainer", containerId, titleName);
    }

    /// <summary>
    /// ?
    /// </summary>
    /// <param name="containerId"></param>
    /// <param name="titleName"></param>
    /// <param name="requestedContainerCookie"></param>
    /// <returns>Opened Container Cookie</returns>
    public long OpenSpecificGameContainer(Guid containerId, string titleName, long requestedContainerCookie) {
        if (titleName.Length > 127) {
            titleName = titleName[..127];
        }
        return Invoke<long>("OpenSpecificGameContainer", containerId, titleName, requestedContainerCookie);
    }

    /// <summary>
    /// Closes the currently open game container
    /// </summary>
    public void CloseGameContainer() {
        Invoke("CloseGameContainer");
    }

    /// <summary>
    /// Deletes a game container
    /// </summary>
    /// <param name="containerId">Id of container to delete</param>
    public void DeleteGameContainer(Guid containerId) {
        Invoke("DeleteGameContainer");
    }

    /// <summary>
    /// Upload the specified file to the currently opened game container
    /// </summary>
    /// <param name="pathInContainer">File path in the container on the Zune</param>
    /// <param name="fileContent">File to upload</param>
    /// <exception cref="Exception">Thrown if file is bigger than 2 GB</exception>
    public void PutFileInContainer(string pathInContainer, Stream fileContent) {
        ChannelValidation.ValidateFileStream(fileContent);
        ChannelValidation.ValidateFilePath(pathInContainer);
        Invoke("PutFileInContainer", pathInContainer, fileContent);
    }

    /// <summary>
    /// Remove the specified file from the currently opened game container
    /// </summary>
    /// <param name="pathInContainer">File path in the container on the Zune</param>
    /// <returns>True if file was removed</returns>
    public bool RemoveFileFromContainer(string pathInContainer) {
        ChannelValidation.ValidateFilePath(pathInContainer);
        return Invoke<bool>("RemoveFileFromContainer", pathInContainer);
    }

    public object PutGameProperties(Guid containerId, string name, string description, string copyright, string startupAssembly, int xnaFrameworkVersion) {
        if (name.Length > 127) {
            name = name.Substring(0, 127);
        }
        return Invoke("PutGameProperties", containerId, name, description, copyright, startupAssembly, xnaFrameworkVersion);
    }

    public object PutGamePropertiesEx(ApplicationContainer container) {
        return PutGamePropertiesEx(container.ContainerId, container.DisplayName, container.Description ?? "", container.EntryPoint, container.RuntimeToken);
    }

    public object PutGamePropertiesEx(Guid containerId, string name, string description, string startupAssembly, string runtimeProfile) {
        if (name.Length > 127) {
            name = name.Substring(0, 127);
        }
        return Invoke("PutGamePropertiesEx", containerId, name, description, startupAssembly, runtimeProfile);
    }

    /// <summary>
    /// Sets the Thumbnail for the currently open container
    /// </summary>
    /// <param name="thumbnailContent">Thumbnail to upload</param>
    /// <exception cref="Exception">Thrown if Thumbnail is bigger than 16.384 bytes</exception>
    public void PutThumbnailInContainer(Stream thumbnailContent) {
        if (thumbnailContent.Length > 16384) {
            throw new Exception("Thubnail too big");
        }
        Invoke("PutThumbnailInContainer", thumbnailContent);
    }

    /// <summary>
    /// Remove the Thumbnail from the currently open container
    /// </summary>
    public void ClearThumbnail() {
        Invoke("ClearContainThumbnail");
    }
}
