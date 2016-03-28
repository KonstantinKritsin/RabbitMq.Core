namespace ConsumerNet35.Contracts
{
	public interface ICMSService
	{
		BlockAgentInfo[] GetBCHAgents(AgencyFilter filter);
		PartnerData GetAgentDetailsByCode(string code);
	}
}