namespace JwtTask.Models
{
    public class RevokeTokenRequest
    {
        public int Id { get; set; }
        public string Jti { get; set; }
        public DateTime RevocationDate { get; set; }
    }
}
