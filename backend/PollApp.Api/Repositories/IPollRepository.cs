using PollApp.Api.Entities;

namespace PollApp.Api.Repositories;

public interface IPollRepository
{
    Task CreateAsync(Poll poll, List<PollOption> options);
    Task<Poll?> GetByIdAsync(Guid id);
    Task<(Poll Poll, List<PollOption> Options)?> GetWithOptionsAsync(Guid id);
    Task<List<Poll>> GetByCreatorIdAsync(Guid creatorId);
    Task UpdateIsActiveAsync(Guid id, bool isActive);
    Task DeleteAsync(Guid id);
}
