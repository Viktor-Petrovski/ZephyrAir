using Domain.Enums;
using Domain.Models;
using Repository.Interface;
using Service.Interface;

namespace Service.Implementation;

public class PollutantService(IRepository<Pollutant> repository) : IPollutantService
{
    public async Task<Pollutant?> GetByCodeAsync(string code)
    {
        var matches = await repository.GetAllAsync(p => p, p => p.Code == code);
        return matches.FirstOrDefault();
    }

    public Task<List<Pollutant>> GetAllAsync()
        => repository.GetAllAsync(p => p, orderBy: q => q.OrderBy(p => p.Code));

    public async Task<Pollutant> CreateAsync(string code, string displayName, MeasurementUnit unit)
    {
        var pollutant = new Pollutant
        {
            Code = code,
            DisplayName = displayName,
            Unit = unit
        };

        await repository.InsertAsync(pollutant);
        return pollutant;
    }
}
