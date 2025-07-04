using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Services
{
    public interface IUserAuthenticationService
    {
        Task<EmployeeBkp> AuthenticateAsync(string payrollNo, string password);
    }

    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly KtdaleaveContext _context;

        public UserAuthenticationService(KtdaleaveContext context)
        {
            _context = context;
        }

        public async Task<EmployeeBkp> AuthenticateAsync(string payrollNo, string password)
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
                // Return employee data in the same connection context
                return await _context.EmployeeBkps.SingleOrDefaultAsync(e => e.PayrollNo == payrollNo);
            }

            return null;
        }
    }

}
