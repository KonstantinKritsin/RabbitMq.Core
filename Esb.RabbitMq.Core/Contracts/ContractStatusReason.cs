namespace Esb.RabbitMq.Core.Contracts
{
	public enum ContractStatusReason
	{
		Active = 0,
		NotAuthorized = 1,
		Paused = 2,
		Terminated = 3,
		Canceled = 4
	}
}