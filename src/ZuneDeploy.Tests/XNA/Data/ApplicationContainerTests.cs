using ZuneDeploy.XNA.Data;

namespace ZuneDeploy.Tests;


public class ApplicationContainerTests {

    [Fact]
    public void ParseValidDeployKitConfig() {
        string[] rawConfig = [
            "# (Required) The display name of the application. This appears at the top of",
            "# the DeployKit window and in the apps/games menu on the Zune.",
            "name:           Doom",
            "",
            "# (Optional) A description of the application. This appears just below the",
            "# display name in DeployKit and when the application is selected on the Zune.",
            "description:    Doom for the Zune HD",
            "",
            "# (Required) A unique identifier for the application. Deploying a new",
            "# application with the same virtual application ID (GUID) as one that is",
            "# already installed will result in the old application being overwritten. Every",
            "# application MUST have a distinct GUID, but different versions of the same",
            "# application should generally share a GUID. Do not use the pre-filled GUID",
            "# below, or copy a GUID from the configuration file for another application.",
            "# The easiest way to generate a new GUID is with the website",
            "# http://www.guidgen.com/. The curly brackets and dashes are allowed but not",
            "# required.",
            "guid:           {12439b5b-2269-4095-b00c-b73a4850d7e7}",
            "",
            "# (Optional) The deployment disposition of the application, which specifies",
            "# whether the application is actually deployed. Allowable values are:",
            "# - never       Does not deploy the application.",
            "# - always      (Default) Always deploys the application.",
            "# - automatic   Equivalent to always.",
            "# Values other than always are not typically useful other than for testing.",
            "disposition:    always",
            "",
            "# (Optional) The launch disposition of the application, which specifies whether",
            "# the application is launched after being deployed. Allowable values are:",
            "# - never       (Default) Does not launch the application.",
            "# - always      Always launches the application.",
            "# - wait        Waits for the application to exit before notifying the user",
            "#               that deployment is complete. This functionality is currently",
            "#               experimental and may be dropped in a future release.",
            "launch:         never",
            "",
            "# (Optional) A thumbnail that represents the application. This appears to the",
            "# left of the display name in DeployKit and in the apps/games menu on the Zune.",
            "thumbnail:      thumbnail.png",
            "",
            "# (Required) A path to the directory containing the application to be deployed,",
            "# relative to the DeployKit executable. Everything in this directory is",
            "# transferred to the Zune. You can find the",
            "src:            data",
            "",
            "# (Required) A path to the startup assembly of the application, relative to the",
            "# directory specified in src. This specifies the .NET executable that is",
            "# launched when the user starts the application, and is usually the only file",
            "# in the application directory with a .exe extension.",
            "exec:           Doom.exe",
            "",
            "# (Optional) The compatibility setting for the application. If the user",
            "# attempts to deploy the application in contradiction to this setting, a",
            "# warning will be displayed informing of possible incompatibility. The user may",
            "# decide to deploy the application anyway. Allowable values are:",
            "# - hd          The application is compatible with the Zune HD only.",
            "# - sd          The application is compatible with Zune models other than the",
            "#               HD only.",
            "# - any         (Default) The application is compatible with all Zune models.",
            "compatibility:  hd",
        ];

        var parsed = DeployConfiguration.FromDeployKitConfiguration(rawConfig);

        Assert.Equal("Doom", parsed.DisplayName);
        Assert.Equal("Doom for the Zune HD", parsed.Description);
        Assert.Equal(new Guid("12439b5b-2269-4095-b00c-b73a4850d7e7"), parsed.ContainerId);
        Assert.Equal("thumbnail.png", parsed.ThumbnailFileName);
        Assert.Equal("data", parsed.SourceFolderName);
        Assert.Equal("Doom.exe", parsed.EntryPoint);
    }

    [Fact]
    public void ThrowOnInvalidSyntax() {
        string[] lines = [
            "# Hello World",
            "key=value",
        ];

        Assert.Throws<ParseConfigurationException>(() => {
            DeployConfiguration.FromDeployKitConfiguration(lines);
        });
    }

    [Fact]
    public void ParseOnlyFirstColon() {
        var desc = "read more at https://example.com";

        string[] lines = {
            "name: doom",
            "guid: 12439b5b-2269-4095-b00c-b73a4850d7e7",
            "src: test",
            "exec: test",
            $"description: {desc}"
        };

        var parsed = DeployConfiguration.FromDeployKitConfiguration(lines);

        Assert.Equal(desc, parsed.Description);
    }

    [Fact]
    public void ErrorOnEmptyKey() {
        string[] lines = {
            "name: doom",
            "guid: 12439b5b-2269-4095-b00c-b73a4850d7e7",
            "src: test",
            "exec: test",
            ": lol"
        };

        Assert.Throws<ParseConfigurationException>(() => {
            DeployConfiguration.FromDeployKitConfiguration(lines);
        });
    }

    [Fact]
    public void ErrorOnEmptyValue() {
        string[] lines = {
            "name: doom",
            "guid: 12439b5b-2269-4095-b00c-b73a4850d7e7",
            "src: test",
            "exec: test",
            "lol: "
        };

        Assert.Throws<ParseConfigurationException>(() => {
            DeployConfiguration.FromDeployKitConfiguration(lines);
        });
    }


    [Fact]
    public void ErrorOnDuplicateKeys() {
        string[] lines = {
            "name: doom",
            "guid: 12439b5b-2269-4095-b00c-b73a4850d7e7",
            "src: test",
            "exec: test",
            "description: the cake",
            "description: is a lie"
        };

        Assert.Throws<ParseConfigurationException>(() => {
            DeployConfiguration.FromDeployKitConfiguration(lines);
        });
    }
}