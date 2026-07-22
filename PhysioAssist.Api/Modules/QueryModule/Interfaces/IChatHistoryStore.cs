using Microsoft.SemanticKernel.ChatCompletion;

namespace PhysioAssist.Api.Modules.QueryModule.Interfaces
{
    //TODO: Better use REDIS for saving chat history in memory to fix distributed system problem

    /* 
     * distributed system problem: 
     * if same user have access to the site from multiple devices and each device
     * connected to a different server .. the chat wont track or be in SYNC 
    */
    public interface IChatHistoryStore
    {
        ChatHistory Get(string conversationId);
        Result<bool> Clear(string conversationId);
    }
}
