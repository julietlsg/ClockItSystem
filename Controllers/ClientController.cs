using ClockItSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers
{
    public class ClientController : Controller
    {

        private readonly IClientService _clientService;
        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }

        public async Task<IActionResult> Index()
        {
            var clients = await _clientService.GetAllAsync();

            return View(clients);
        }
    }
}
