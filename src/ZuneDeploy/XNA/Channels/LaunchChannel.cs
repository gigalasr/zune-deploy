using System.Globalization;
using ZuneDeploy.Transport;
using ZuneDeploy.XNA.Protocol;

namespace ZuneDeploy.XNA.Channels;

public class LaunchChannel(Client client) : Channel(client, _channelGuid) {
    public static readonly Guid _channelGuid = new Guid("A40D216D-FBD3-40d4-B852-DE77478C1475");

    /// <summary>
    /// Launch a title by its display name
    /// </summary>
    /// <param name="titleName">Name of title</param>
    /// <param name="cmdLn">Command Line Options</param>
    /// <param name="hostOptions">Host Options</param>
    /// <returns>???</returns>
    public string Launch(string titleName, string cmdLn, string hostOptions) {
        return Invoke<string>("Launch", titleName, cmdLn, hostOptions);
    }

    /// <summary>
    /// Launchy a title by its container id
    /// </summary>
    /// <param name="containerId">Id of container to launch</param>
    /// <param name="cmdLn">Command Line Options</param>
    /// <param name="hostOptions">Host Options</param>
    /// <returns></returns>
    public string Launch(Guid containerId, string cmdLn, string hostOptions) {
        return Invoke<string>("LaunchTitle", containerId.ToString("N", CultureInfo.InvariantCulture), cmdLn, hostOptions);
    }

    /// <summary>
    /// Launch a title by its container id and optionally return to xna mode when exiting application
    /// </summary>
    /// <param name="containerId">Id of container to launch</param>
    /// <param name="cmdLn">Command Line Options</param>
    /// <param name="returnToDevMode">Wheter to return to dev mode on exit</param>
    public void LaunchInMode(Guid containerId, string cmdLn, bool returnToDevMode) {
        Invoke("LaunchTestMode", containerId.ToString("N", CultureInfo.InvariantCulture), cmdLn, returnToDevMode);
    }

    /// <summary>
    /// Check if a title is currently running
    /// </summary>
    /// <returns>True if a title is currently running</returns>
    public bool IsTitleRunning() {
        return Invoke<bool>("IsTitleRunning");
    }

    /// <summary>
    /// Get the title name and executable name of the currently running title
    /// </summary>
    /// <returns>Title name and executable name</returns>
    public (string? titleName, string? executableName) GetRunningTitleInfo() {
        var response = Invoke<string>("GetRunningTitleInfo");
        var items = response.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (items.Length == 2) {
            return (items[0], items[1]);
        }

        return (null, null);
    }

    /// <summary>
    /// Get a list of available services
    /// </summary>
    /// <returns>List of available services</returns>
    public string[] EnumerateAvailableServices() {
        var response = Invoke<string>("EnumerateAvailableServices");
        return response.Split('|', StringSplitOptions.RemoveEmptyEntries);
    }
}