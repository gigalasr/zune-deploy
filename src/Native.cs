using System.Runtime.InteropServices;

namespace zune_deploy;

static internal partial class Native
{
    [LibraryImport("libzune-deploy-native.so", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int TestMethod();

}
