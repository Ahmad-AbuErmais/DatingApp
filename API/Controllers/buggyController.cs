using API.Data;
using API.Modules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    public class buggyController : BaseApiController
    {
        private readonly DataContext _context;
        public buggyController(DataContext context)
        {
            this._context = context;
        }
       [Authorize]
        [HttpGet("auth")]
        public ActionResult<string> GetSecret()
        {
        return "Important Secret";
        }
        [HttpGet("not-found")]
        public ActionResult<AppUser> GetNotFound()
        {
        var things= _context.Users.Find(-1);
        if(things==null)
        {
            return NotFound();
        }
        return Ok(things);
        }
        [HttpGet("server-error")]
        public ActionResult<string> GetServerError()
        {
        var things= _context.Users.Find(-1);
        var thingstoString=things.ToString();
        return thingstoString;
        }
        [HttpGet("bad-request")]
        public ActionResult<string> GetBadRequest()
        {
        return BadRequest("this Was Not a Good Request");
        }
        
    }
}