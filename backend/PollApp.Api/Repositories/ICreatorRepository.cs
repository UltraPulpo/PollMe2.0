using PollApp.Api.Entities;

namespace PollApp.Api.Repositories;

public interface ICreatorRepository
{
    Task CreateAsync(Creator creator);
    Task<Creator?> GetBySecretTokenAsync(Guid secretToken);
}
