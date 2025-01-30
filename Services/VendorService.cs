using Microsoft.Data.SqlClient;
using MRIV.ViewModels;

namespace MRIV.Services
{
    public class VendorService
    {
        private readonly string _connectionString;

        public VendorService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LanSupportConnection");
        }

        public async Task<List<Vendor>> GetVendorsAsync(string search = null)
        {
            var vendors = new List<Vendor>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"SELECT [VendorID], [Name], [Phone], [EMail], 
                            [Location], [IsNullRecord], [LastModified]
                            FROM [Lansupport_5_4].[dbo].[Vendors]
                            ORDER BY [VendorID] DESC";

                if (!string.IsNullOrEmpty(search))
                {
                    query += " WHERE Name LIKE @SearchTerm";
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(search))
                    {
                        cmd.Parameters.AddWithValue("@SearchTerm", $"%{search}%");
                    }

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            vendors.Add(new Vendor
                            {
                                VendorID = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Location = reader.IsDBNull(4) ? null : reader.GetString(4),
                                IsNullRecord = reader.GetBoolean(5),
                                LastModified = reader.GetDateTime(6)
                            });
                        }
                    }
                }
            }
            return vendors;
        }

    }
}
