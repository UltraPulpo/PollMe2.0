using FluentMigrator;

namespace PollApp.Api.Migrations;

[Migration(1)]
public class Migration_001_CreateCreatorsTable : Migration
{
    public override void Up()
    {
        Create.Table("Creators")
            .WithColumn("Id").AsString(36).PrimaryKey()
            .WithColumn("SecretToken").AsString(36).NotNullable()
            .WithColumn("DisplayName").AsString(200).Nullable()
            .WithColumn("CreatedAtUtc").AsString().NotNullable();

        Create.Index("IX_Creators_SecretToken")
            .OnTable("Creators")
            .OnColumn("SecretToken").Unique();
    }

    public override void Down() => Delete.Table("Creators");
}
