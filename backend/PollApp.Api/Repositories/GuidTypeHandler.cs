using Dapper;

namespace PollApp.Api.Repositories;

/// <summary>
/// Dapper type handler to convert between C# Guid and SQLite TEXT storage.
/// SQLite has no native GUID type — we store GUIDs as 36-character strings.
/// </summary>
public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(System.Data.IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToString();
    }

    public override Guid Parse(object value)
    {
        return Guid.Parse(value.ToString()!);
    }
}
