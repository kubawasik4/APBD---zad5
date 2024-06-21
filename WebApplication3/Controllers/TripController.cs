using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Context;
using WebApplication3.DTO;
using WebApplication3.Models;

namespace WebApplication3.Controllers;
[Route("api/[controller]")]
[ApiController]
public class TripController : ControllerBase
{
    private readonly MasterContext _context;

    public TripController(MasterContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripDto>>> GetTrips()
    {
        var trips = await _context.Trips
            .Include(t => t.IdCountries)
            .Include(t => t.ClientTrips)
            .ThenInclude(ct => ct.IdClientNavigation)
            .OrderByDescending(t => t.DateFrom)
            .Select(t => new TripDto
            {
                Name = t.Name,
                Description = t.Description,
                DateFrom = t.DateFrom,
                DateTo = t.DateTo,
                MaxPeople = t.MaxPeople,
                Countries = t.IdCountries.Select(c => new CountryDto { Name = c.Name }).ToList(),
                Clients = t.ClientTrips.Select(ct => new ClientDto 
                { 
                    FirstName = ct.IdClientNavigation.FirstName,
                    LastName = ct.IdClientNavigation.LastName 
                }).ToList()
            })
            .ToListAsync();

        return Ok(trips);
    }
    
    [HttpDelete("{idClient}")]
    public async Task<ActionResult> removeClient(int id)
    {
        var client = await _context.Clients.Include(e => e.ClientTrips)
            .FirstOrDefaultAsync(c => c.IdClient == id);//jesli cos jest to 1 element a jesli nic to null
        var client2 = await _context.Clients.FirstOrDefaultAsync(e => e.IdClient == id);
        
        if (client2 == null )
        {
            return NotFound();
        }

        if (client2.ClientTrips.Any()) //czy zawiera jakiekolwek elementy
        {
            return BadRequest("ma wycioeczki");
        }

        _context.Clients.Remove(client2);
        await _context.SaveChangesAsync();
        return NoContent();

    }

    [HttpPost("{idTrip}/clients")]
    public async Task<ActionResult> assignClientToTrip(int idTrip, [FromBody] ClientAssignmentDto assignmentDto)
    {
        var trip = await _context.Trips.FindAsync(idTrip); //zwraca encje z kluczem lub null
        if (trip == null)
        {
            return NotFound();
        }

        var client = await _context.Clients.FirstOrDefaultAsync(e => e.Pesel == assignmentDto.PESEL);
        if (client == null)
        {
            client = new Client
            {
                
                FirstName = assignmentDto.Name,
                Pesel = assignmentDto.PESEL

            };
            _context.Clients.Add(client);
        }

        var clientTrip =
            await _context.ClientTrips.FirstOrDefaultAsync(e => e.IdClient == client.IdClient && e.IdTrip == idTrip);

        if (clientTrip != null)
        {
            return BadRequest();
        }

        clientTrip = new ClientTrip
        {
            IdClient = client.IdClient,
            IdTrip = idTrip,
            RegisteredAt = DateTime.Now
        };
        _context.ClientTrips.Add(clientTrip);
        await _context.SaveChangesAsync();
        
        return Ok();
    }
    

}

[HttpGet("{id}")]
public async Task<IActionResult> GetPatientDetails(int id)
{
    var patient = await _context.Patients
        .Include(p => p.Prescriptions)
        .ThenInclude(pr => pr.Doctor)
        .Include(p => p.Prescriptions)
        .ThenInclude(pr => pr.PrescriptionMedicaments)
        .ThenInclude(pm => pm.Medicament)
        .Where(p => p.Id == id)
        .Select(p => new
        {
            p.Id,
            p.FirstName,
            p.LastName,
            Prescriptions = p.Prescriptions.Select(pr => new
            {
                pr.Id,
                pr.Date,
                pr.DueDate,
                Doctor = new { pr.Doctor.Id, pr.Doctor.FirstName, pr.Doctor.LastName },
                Medicaments = pr.PrescriptionMedicaments.Select(pm => new
                {
                    pm.Medicament.Id,
                    pm.Medicament.Name,
                    pm.Dose,
                    pm.Description
                })
            }).OrderBy(pr => pr.DueDate)
        })
        .FirstOrDefaultAsync();

    if (patient == null)
    {
        return NotFound();
    }

    return Ok(patient);
}
}