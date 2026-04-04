using FluentMigrator;

namespace PollApp.Api.Migrations;

[Migration(5)]
public class Migration_005_CreateVoteChoicesTable : Migration
{
    public override void Up()
    {
        Create.Table("VoteChoices")
            .WithColumn("Id").AsString(36).PrimaryKey()
            .WithColumn("VoteId").AsString(36).NotNullable().ForeignKey("FK_VoteChoices_Votes", "Votes", "Id")
            .WithColumn("PollOptionId").AsString(36).NotNullable().ForeignKey("FK_VoteChoices_PollOptions", "PollOptions", "Id");
    }

    public override void Down() => Delete.Table("VoteChoices");
}
