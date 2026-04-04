using FluentMigrator;

namespace PollApp.Api.Migrations;

[Migration(3)]
public class Migration_003_CreatePollOptionsTable : Migration
{
    public override void Up()
    {
        Create.Table("PollOptions")
            .WithColumn("Id").AsString(36).PrimaryKey()
            .WithColumn("PollId").AsString(36).NotNullable().ForeignKey("FK_PollOptions_Polls", "Polls", "Id")
            .WithColumn("Text").AsString(500).NotNullable()
            .WithColumn("DisplayOrder").AsInt32().NotNullable();
    }

    public override void Down() => Delete.Table("PollOptions");
}
