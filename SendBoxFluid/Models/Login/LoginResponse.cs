namespace SendBoxFluid.Models.Login;

public class LoginResponse
{
    public string SessionId { get; set; }
    public int SessionTimeout { get; set; }
    public string Version { get; set; }
}
