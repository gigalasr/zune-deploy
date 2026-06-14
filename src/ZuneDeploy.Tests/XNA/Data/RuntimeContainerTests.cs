using ZuneDeploy.XNA.Data;

namespace ZuneDeploy.Tests.XNA.Data;


public class RuntimeContainerTests {
    [Fact]
    public void LoadConfigFromJson() {
        string rawConfig = @"{
          ""Version"": 822262628,
          ""RuntimeToken"": ""Zune.v4.0.Beta"",
          ""Compatibility"": ""ZUNE_HD_ONLY""
        }";

        var config = RuntimeContainerConfig.FromJsonText(rawConfig);

        Assert.Equal(0x3102bb64, config.Version);
        Assert.Equal("Zune.v4.0.Beta", config.RuntimeToken);
        Assert.Equal(DeviceCompatibility.ZUNE_HD_ONLY, config.Compatibility);
    }
}
