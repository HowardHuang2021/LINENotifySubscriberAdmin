using LINENotifySubscriberAdmin.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LINENotifySubscriberAdmin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriberController : ControllerBase
    {
        private readonly SubscriberContext _context;
        public SubscriberController(SubscriberContext context)
        {
            _context = context;
        }

        [HttpGet(Name = "GetSubscribers")]
        public async Task<IActionResult> GetSubscribers()
        {
            return Ok(await _context.Subscribers.ToListAsync());
        }

        //[HttpGet("{id}", Name = "GetSubscriberById")]
        //public async Task<IActionResult> GetSubscriberById(int id)
        //{
        //    var sub = await _context.Subscribers.FindAsync(id);
        //    if (sub == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(sub);
        //}

        //[HttpPost(Name = "AddSubscriber")]
        //public async Task<IActionResult> AddSubscriber([FromBody] Subscriber subscriber)
        //{
        //    await _context.Subscribers.AddAsync(subscriber);
        //    await _context.SaveChangesAsync();
        //    return CreatedAtAction(nameof(GetSubscriberById),
        //        new { id = subscriber.Id },
        //        subscriber
        //        );
        //}

        [HttpPut("{id}", Name = "UpdateSubscriber")]
        public async Task<IActionResult> UpdateSubscriber(int id, [FromBody] Subscriber subscriber)
        {
            var sub = await _context.Subscribers.FindAsync(id);
            if (sub == null)
            {
                return NotFound();
            }
            sub.Username = subscriber.Username;
            sub.AccessToken = subscriber.AccessToken;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}", Name = "DeleteSubscriber")]
        public async Task<IActionResult> DeleteSubscriber(int id)
        {
            var sub = await _context.Subscribers.FindAsync(id);
            if (sub == null)
            {
                return NotFound();
            }
            _context.Subscribers.Remove(sub);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
