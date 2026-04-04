using FluentMigrator;

namespace PollApp.Api.Migrations;

[Migration(4)]
public class Migration_004_CreateVotesTable : Migration
{
    public override void Up()
    {
        Create.Table("Votes")
            .WithColumn("Id").AsString(36).PrimaryKey()
            .WithColumn("PollId").AsString(36).NotNullable().ForeignKey("FK_Votes_Polls", "Polls", "Id")
            .WithColumn("VoterToken").AsString(36).NotNullable()
            .WithColumn("CreatedAtUtc").AsString().NotNullable();

        Create.Index("IX_Votes_PollId_VoterToken")
            .OnTable("Votes")
            .OnColumn("PollId").Ascending()
            .OnColumn("VoterToken").Ascending()
            .WithOptions().Unique();
    }

    public override void Down() => Delete.Table("Votes");
}
