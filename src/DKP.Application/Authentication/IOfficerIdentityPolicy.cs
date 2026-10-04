namespace DKP.Application.Authentication;

public interface IOfficerIdentityPolicy
{
	bool IsOfficer(string discordId);
}
