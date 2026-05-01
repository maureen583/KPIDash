using System.Data;
using System.Globalization;
using Dapper;

namespace KPIDash.Data;

public class UtcDateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override DateTime Parse(object value) =>
        DateTime.Parse((string)value, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

    public override void SetValue(IDbDataParameter parameter, DateTime value) =>
        parameter.Value = DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("o");
}
