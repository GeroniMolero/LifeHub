using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly BusinessRules _rules;

        public ConfigController(IOptions<BusinessRules> rules)
        {
            _rules = rules.Value;
        }

        [HttpGet("limits")]
        [AllowAnonymous]
        public IActionResult GetLimits() => Ok(new
        {
            maxDocumentsPerUser               = _rules.MaxDocumentsPerUser,
            maxSpacesPerUser                  = _rules.MaxSpacesPerUser,
            maxPublishedDocumentsPerUser      = _rules.MaxPublishedDocumentsPerUser,
            maxProfileVisibleDocumentsPerUser = _rules.MaxProfileVisibleDocumentsPerUser,
            maxProfileVisibleSpacesPerUser    = _rules.MaxProfileVisibleSpacesPerUser
        });
    }
}
