using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Services
{
    public interface IUserAuthenticationService
    {
        Task<bool> AuthenticateAsync(string payrollNo, string password);
    }
    // Authentication service implementation
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly KtdaleaveContext _context;

        public UserAuthenticationService(KtdaleaveContext context)
        {
            _context = context;
        }

        public async Task<bool> AuthenticateAsync(string payrollNo, string password)
        {
            var parameters = new[]
            {
            new SqlParameter("@username", payrollNo),
            new SqlParameter("@pass_word", password)
        };

            var result = await _context.PasswordCheckResults
                .FromSqlRaw("EXEC Pro_password @username, @pass_word", parameters)
                .ToListAsync();

            if (result.Count > 0 && result.First().Passer == password)
            {
                return true;
            }

            return false;
        }
    }

}
