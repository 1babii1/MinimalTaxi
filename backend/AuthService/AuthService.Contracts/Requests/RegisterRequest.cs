namespace AuthService.Contracts.Requests;

public sealed class RegisterRequest
{
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public string Role { get; set; } = "Passenger";
	public string? Name { get; set; }
	public string? Phone { get; set; }
	public RegisterAddressRequest? Address { get; set; }
	public RegisterCarInfoRequest? CarInfo { get; set; }
}

public sealed class RegisterAddressRequest
{
	public string? City { get; set; }
	public string? Street { get; set; }
	public string? House { get; set; }
	public string? Apartment { get; set; }
}

public sealed class RegisterCarInfoRequest
{
	public string? Brand { get; set; }
	public string? Model { get; set; }
	public string? Color { get; set; }
	public string? PlateNumber { get; set; }
}
