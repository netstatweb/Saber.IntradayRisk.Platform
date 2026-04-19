using Microsoft.AspNetCore.SignalR;
using Saber.Risk.Core.Models;
using System.Threading.Tasks;

namespace Saber.Risk.Api.Hubs
{
    public class RiskHub : Hub
    {
        // Client method name: "ReceiveRiskUpdate"
        public async Task Subscribe(string[] tickers)
        {
            // Example: group per ticker for targeted updates
            foreach (var t in tickers)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"TICKER:{t}");
        }

        public async Task Unsubscribe(string[] tickers)
        {
            foreach (var t in tickers)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"TICKER:{t}");
        }
    }
}