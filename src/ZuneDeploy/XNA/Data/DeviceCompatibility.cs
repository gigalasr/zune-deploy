using System.Text.Json.Serialization;

namespace ZuneDeploy.XNA.Data;

[JsonConverter(typeof(JsonStringEnumConverter<DeviceCompatibility>))]
public enum DeviceCompatibility {
    ZUNE_HD_ONLY,
    ZUNE_SD_ONLY,
    ANY
}
