namespace Esb.RabbitMq.Core.Contracts
{
	public interface ICMSService
	{
		BlockAgentInfo[] GetBCHAgents(AgencyFilter filter);
		PartnerData GetAgentDetailsByCode(string code);
	}
}