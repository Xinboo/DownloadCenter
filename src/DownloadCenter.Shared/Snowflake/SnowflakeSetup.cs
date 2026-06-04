using Yitter.IdGenerator;

namespace DownloadCenter.Shared.Snowflake;

public static class SnowflakeSetup
{
    public static void Init(ushort workerId = 1)
    {
        var options = new IdGeneratorOptions(workerId);
        YitIdHelper.SetIdGenerator(options);
    }

    public static long NextId() => YitIdHelper.NextId();
}
