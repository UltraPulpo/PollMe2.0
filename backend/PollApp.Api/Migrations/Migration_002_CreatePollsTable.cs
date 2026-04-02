using FluentMigrator;

namespace PollApp.Api.Migrations;

[Migration(2)]
public class Migration_002_CreatePollsTable : Migration
{
    public override void Up()
    {
        Create.Table("Polls")
            .WithColumn("Id").AsString(36).PrimaryKey()
            .WithColumn("CreatorId").AsString(36).NotNullable().ForeignKey("FK_Polls_Creators", "Creators", "Id")
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Description").AsString(2000).Nullable()
            .WithColumn("PollType").AsInt32().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAtUtc").AsString().NotNullable();
    }

    public override void Down() => Delete.Table("Polls");
}
