namespace TypeOneFoodJournal.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    public class InsulinPumpReadingController : Controller
    {
        [HttpGet(Name = "GetInsulinPumpReadings")]
        public IActionResult Get(DateTime from, DateTime to)
        {
            return View();
        }
    }
}
