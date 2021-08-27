using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SwarmClientMonitor.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DockerServiceMonitorController : ControllerBase
    {
        private static bool ReceivedReconfigure = false;
        private static bool ReceivedRemove = false;

        [HttpGet("[Action]")]
        [HttpPost("[Action]")]
        public IActionResult Reconfigure()
        {
            ReceivedReconfigure = true;
            return Ok();

        }


        [HttpGet("[Action]")]
        [HttpPost("[Action]")]
        public IActionResult Remove()
        {
            ReceivedRemove = true;
            return Ok();
        }


        [HttpGet("[Action]")]
        public IActionResult Status()
        {
            return Ok(new
            {

                ReceivedReconfigure = ReceivedReconfigure,
                ReceivedRemove = ReceivedRemove
            });

        }
    }
}