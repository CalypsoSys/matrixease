using System;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Mvc;

namespace MatrixEase.Web
{
    [Route("/")]
    public class DefaultController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                return Ok(new
                {
                    Name = "MatrixEase API",
                    Auth = "Supabase bearer token"
                });
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error home page");
                throw;
            }
        }
    }
}
