using PhysioAssist.Api.Modules.Intake.Entities;
using PhysioAssist.Api.Persistence;

namespace PhysioAssist.Api.Modules.Intake.Repositories;

public class PatientFormSchemaRepository(ApplicationDbContext context) : IPatientFormSchemaRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task AddAsync(PatientFormSchema schema, CancellationToken cancellationToken = default)
    {
        await _context.PatientFormSchemas.AddAsync(schema, cancellationToken);
    }

    public async Task<PatientFormSchema?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PatientFormSchemas
            .FirstOrDefaultAsync(schema => schema.Id == id, cancellationToken);
    }

    public async Task<PatientFormSchema?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PatientFormSchemas
            .FirstOrDefaultAsync(schema => schema.Id == id && schema.Status == FormSchemaStatus.Published, cancellationToken);
    }

    public async Task<PatientFormSchema?> GetDefaultForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        return await _context.PatientFormSchemas
            .FirstOrDefaultAsync(schema => schema.ClinicId == clinicId && schema.IsDefault, cancellationToken);
    }

    public async Task<IReadOnlyList<PatientFormSchema>> GetByClinicAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        return await _context.PatientFormSchemas
            .Where(schema => schema.ClinicId == clinicId)
            .OrderByDescending(schema => schema.IsDefault)
            .ThenByDescending(schema => schema.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsNameForClinicAsync(Guid clinicId, string name, Guid? excludeId, CancellationToken cancellationToken = default)
    {

        var query = _context.PatientFormSchemas
            .Where(schema => schema.ClinicId == clinicId && schema.Name == name);

        if (excludeId.HasValue)
            query = query.Where(schema => schema.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task UnsetDefaultSchemasAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        await _context.PatientFormSchemas
            .Where(schema => schema.ClinicId == clinicId && schema.IsDefault)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsDefault, false), cancellationToken);
    }

    public void Update(PatientFormSchema schema)
    {
        _context.PatientFormSchemas.Update(schema);
    }

    public void Remove(PatientFormSchema schema)
    {
        _context.PatientFormSchemas.Remove(schema);
    }

    public async Task<IReadOnlyList<PatientFormSchema>> GetCopiesByOriginalFormIdAsync(Guid originalFormId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        return await _context.PatientFormSchemas
            .Where(schema => schema.ClinicId == clinicId && schema.OriginalFormId == originalFormId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientFormSchema>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PatientFormSchemas
            .ToListAsync(cancellationToken);
    }
}
