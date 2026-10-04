using DKP.Application.SoftReserves;
using Microsoft.Extensions.Configuration;

namespace DKP.Infrastructure.Persistence;

public sealed class ConfigurationSoftReserveSettings(IConfiguration configuration) : ISoftReserveSettings
{
	public int DkpCost => int.TryParse(configuration["SoftReserve:DkpCost"], out var cost) ? cost : 10;
	public int MaxReserves => int.TryParse(configuration["SoftReserve:MaxReserves"], out var max) ? max : 2;
}
