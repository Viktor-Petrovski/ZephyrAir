using Domain.Enums;
using Domain.Models;

namespace Service.Interface;

public interface IPollutantService
{
    Task<Pollutant?> GetByCodeAsync(string code);
    Task<List<Pollutant>> GetAllAsync();
    Task<Pollutant> CreateAsync(string code, string displayName, MeasurementUnit unit);
}
