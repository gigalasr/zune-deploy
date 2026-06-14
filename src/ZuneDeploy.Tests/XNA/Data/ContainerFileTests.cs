using ZuneDeploy.XNA.Data;

namespace ZuneDeploy.Tests.XNA.Data;

public class ContainerFileTests {
    [Fact]
    public void NormalizePathUnix() {
        var root = "/some/dir";
        var path = "/some/dir/very/cool/file.md";

        var file = new ContainerFile(root, path);

        Assert.Equal("very\\cool\\file.md", file.PathInContainer);
    }
}
