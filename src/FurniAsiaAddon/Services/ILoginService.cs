using FurniAsiaAddon.Models;
using System.Threading.Tasks;

namespace FurniAsiaAddon.Services
{
    interface ILoginService
    {
        string SendLoginRequest();
        Task<bool> SendPatchRequestAsync(string token, Item item);
    }
}
