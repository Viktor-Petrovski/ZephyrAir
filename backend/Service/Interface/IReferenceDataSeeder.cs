namespace Service.Interface;

public interface IReferenceDataSeeder
{
    /// Idempotent: safe to run on every application start.
    Task SeedAsync(CancellationToken cancellationToken = default);
}
