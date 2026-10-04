using DKP.Application.Authentication;
using Microsoft.Extensions.Configuration;

namespace DKP.InversionOfControl.Authentication;

public sealed class OfficerIdentityPolicy(IConfiguration configuration) : IOfficerIdentityPolicy
{
	private readonly HashSet<string> officerIds = configuration
		.GetSection("Discord:OfficerUserIds")
		.Get<string[]>()?
		.Where(id => !string.IsNullOrWhiteSpace(id))
		.ToHashSet(StringComparer.Ordinal)
		?? [];

	public bool IsOfficer(string discordId) => officerIds.Contains(discordId);
}
