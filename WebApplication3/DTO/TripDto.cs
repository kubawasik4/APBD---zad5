namespace WebApplication3.DTO;

public class TripDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int MaxPeople { get; set; }
    public List<CountryDto> Countries { get; set; }
    public List<ClientDto> Clients { get; set; }
}