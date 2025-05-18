using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using System.Linq.Expressions;
using static System.Collections.Specialized.BitVector32;

namespace MRIV.Services
{
    public interface IEmployeeService  // Changed from class to interface
    {
        Task<EmployeeBkp> GetApproverByContextAsync(
           string contextType,
           int contextStationId,
           int contextDepartmentId,
           EmployeeBkp loggedInUser,
           string dispatchType = null,
           string dispatchPayrollNo = null);

        Task<EmployeeBkp> GetEmployeeByRoleAndLocationAsync(
        string role,
        string location,
        int departmentId,
        bool isIssueContext,
        EmployeeBkp requestingEmployee,
        Dictionary<string, string> parameters = null);

        Task<(EmployeeBkp loggedInUserEmployee, Department loggedInUserDepartment, Station loggedInUserStation)> GetEmployeeAndDepartmentAsync(string payrollNo);

        Task<(EmployeeBkp employeeDetail, Department departmentDetail, Station stationDetail)> GetEmployeeDepartmentStationDetailAsync(string payrollNo);
        EmployeeBkp GetSupervisor(EmployeeBkp employee);
        Task<EmployeeBkp> GetFactoryEmployeeAsync(string stationName); // Changed to Task<EmployeeBkp>
        Task<EmployeeBkp> GetRegionEmployee(); // Changed to Task<EmployeeBkp>
        Task<EmployeeBkp> GetHoEmployeeAsync(string departmentName);

        Task<EmployeeBkp> GetEmployeeByPayrollAsync(string payrollNo);
        Task<List<EmployeeBkp>> GetEmployeesByDepartmentAsync(int departmentId);
        Task<List<EmployeeBkp>> GetEmployeesByDepartmentNameAsync(string deliveryStation);
        Task<List<EmployeeBkp>> GetFactoryEmployeesByStationAsync(string station);
        Task<List<EmployeeBkp>> GetSupervisorsByDepartmentAsync(int departmentId);
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly KtdaleaveContext _context;

        public EmployeeService(KtdaleaveContext context)
        {
            _context = context;
        }

        /// GET APPROVER
        /// 
        public async Task<EmployeeBkp> GetApproverByContextAsync(
            string contextType,
            int contextStationId,
            int contextDepartmentId,
            EmployeeBkp loggedInUser,
            string dispatchType = null,
            string dispatchPayrollNo = null)
        {
            // IsApprover query - reusable predicate
            Expression<Func<EmployeeBkp, bool>> isApprover = e =>
                !string.IsNullOrEmpty(e.PayrollNo) && e.EmpisCurrActive == 0;

            // CASE 1: ISSUE CONTEXT
            if (contextType == "Issue")
            {
                // Primary: Try immediate supervisor of logged-in user
                if (!string.IsNullOrEmpty(loggedInUser?.Supervisor))
                {
                    var supervisor = await _context.EmployeeBkps
                        .Where(e => e.PayrollNo == loggedInUser.Supervisor)
                        .Where(isApprover)
                        .FirstOrDefaultAsync();

                    if (supervisor != null)
                        return supervisor;
                }

                // Fallback 1: Station-based supervisor (Non-HQ)
                if (contextStationId != 0)
                {
                    // Try to find field supervisor at the station
                    var fieldSupervisor = await _context.EmployeeBkps
                        .Where(e => e.Station == contextStationId.ToString())
                        .Where(e => e.Role == "fieldsupervisor")
                        .Where(isApprover)
                        .FirstOrDefaultAsync();

                    if (fieldSupervisor != null)
                        return fieldSupervisor;

                    // Try to find field user at the station
                    var fieldUser = await _context.EmployeeBkps
                        .Where(e => e.Station == contextStationId.ToString())
                        .Where(e => e.Role == "fielduser")
                        .Where(isApprover)
                        .FirstOrDefaultAsync();

                    if (fieldUser != null)
                        return fieldUser;
                }

                // Fallback 2: Department-based supervisor (HQ)
                if (contextStationId == 0)
                {
                    var hqSupervisor = await _context.EmployeeBkps
                        .Where(e => e.Station == "HQ" || e.Station == "0")
                        .Where(e => e.Department == contextDepartmentId.ToString())
                        .Where(e => e.Role == "supervisor")
                        .Where(isApprover)
                        .FirstOrDefaultAsync();

                    if (hqSupervisor != null)
                        return hqSupervisor;
                }

                // Final Fallback: Any supervisor in the department
                var departmentSupervisor = await _context.EmployeeBkps
                    .Where(e => e.Department == contextDepartmentId.ToString())
                    .Where(e => e.Role.Contains("supervisor"))
                    .Where(isApprover)
                    .FirstOrDefaultAsync();

                if (departmentSupervisor != null)
                    return departmentSupervisor;
            }

            // CASE 2: DISPATCH CONTEXT
            else if (contextType == "Dispatch")
            {
                // Vendor Dispatch
                if (dispatchType == "vendor")
                {
                    // Return a special value that will be handled by the caller
                    // Since this isn't a real employee, we'll return null and let the caller handle it
                    return null; // Caller will set "ICTdispatch"
                }

                // Admin Dispatch
                else if (dispatchType == "admin" && !string.IsNullOrEmpty(dispatchPayrollNo))
                {
                    var adminDispatcher = await _context.EmployeeBkps
                        .Where(e => e.PayrollNo == dispatchPayrollNo)
                        .Where(isApprover)
                        .FirstOrDefaultAsync();

                    if (adminDispatcher != null)
                        return adminDispatcher;
                }
            }

            // CASE 3: DELIVERY CONTEXT
            else if (contextType == "Delivery")
            {
                // Primary: Station and Department Match
                var departmentEmployee = await _context.EmployeeBkps
                    .Where(e => e.Station == (contextStationId == 0 ? "HQ" : contextStationId.ToString()))
                    .Where(e => e.Department == contextDepartmentId.ToString())
                    .Where(isApprover)
                    .FirstOrDefaultAsync();

                if (departmentEmployee != null)
                    return departmentEmployee;

                // Fallback 1: Station-based employee (Non-HQ)
                if (contextStationId != 0)
                {
                    // Try field supervisor
                    var fieldSupervisor = await _context.EmployeeBkps
                        .Where(e => e.Station == contextStationId.ToString())
                        .Where(e => e.Role == "fieldsupervisor")
                        .Where(isApprover)
                        .FirstOrDefaultAsync();

                    if (fieldSupervisor != null)
                        return fieldSupervisor;

                    // Try field user
                    var fieldUser = await _context.EmployeeBkps
                        .Where(e => e.Station == contextStationId.ToString())
                        .Where(e => e.Role == "fielduser")
                        .Where(isApprover)
                        .FirstOrDefaultAsync();

                    if (fieldUser != null)
                        return fieldUser;

                    // Fallback 2: IT Department at station
                    var itEmployee = await _context.EmployeeBkps
                        .Where(e => e.Station == contextStationId.ToString())
                        .Where(e => e.Department == "114") // IT Department
                        .Where(isApprover)
                        .FirstOrDefaultAsync();

                    if (itEmployee != null)
                        return itEmployee;
                }

                // Fallback 3: HQ Department Employee
                if (contextStationId == 0)
                {
                    var hqEmployee = await _context.EmployeeBkps
                        .Where(e => e.Station == "HQ" || e.Station == "0")
                        .Where(e => e.Department == contextDepartmentId.ToString())
                        .Where(isApprover)
                        .FirstOrDefaultAsync();

                    if (hqEmployee != null)
                        return hqEmployee;
                }
            }

            // If no approver found after all attempts, return null
            return null;
        }
        public async Task<EmployeeBkp> GetEmployeeByRoleAndLocationAsync(
            string role,
            string location,
            int departmentId,
            bool isIssueContext,
            EmployeeBkp requestingEmployee,
            Dictionary<string, string> parameters = null)
        {
            // Default to empty dictionary if null
            parameters = parameters ?? new Dictionary<string, string>();

            // If no role provided, return null
            if (string.IsNullOrEmpty(role))
                return null;

            role = role.ToLower();

            // Special case for supervisor - first check direct supervisor
            if (role == "supervisor" || role == "hod")
            {
                // Try to get the supervisor or HOD of the requesting employee
                var supervisor = GetSupervisor(requestingEmployee);
                if (supervisor != null)
                    return supervisor;

                // If no direct supervisor, try department supervisors
                var departmentSupervisors = await GetSupervisorsByDepartmentAsync(departmentId);
                return departmentSupervisors.FirstOrDefault();
            }

            // Special case for admin dispatch
            if (role == "dispatchadmin" && parameters.TryGetValue("payrollNo", out var payrollNo))
            {
                return await GetEmployeeByPayrollAsync(payrollNo);
            }

            // For role-based searches in departments (HQ context)
            if (location.Contains("Department", StringComparison.OrdinalIgnoreCase) ||
                isIssueContext && location.ToLower().Contains("hq"))
            {
                // For HQ employees, search by department and role
                var departmentEmployees = await GetEmployeesByDepartmentNameAsync(location);

                // Filter by role if provided
                return departmentEmployees.FirstOrDefault(e => string.Equals(e.Role, role, StringComparison.OrdinalIgnoreCase));
            }

            // For factory or field locations
            if (role.Contains("field") || (!isIssueContext && location.ToLower().Contains("factory")))
            {
                switch (role.ToLower())
                {
                    case "fieldsupervisor":
                        return await GetRegionEmployee(); // For regional supervisor

                    case "fielduser":
                        return await GetFactoryEmployeeAsync(location); // For factory employee

                    default:
                        // Get all employees at the station, then filter by role
                        var stationEmployees = await GetFactoryEmployeesByStationAsync(location);
                        return stationEmployees.FirstOrDefault(e => string.Equals(e.Role, role, StringComparison.OrdinalIgnoreCase));
                }
            }

            // For regional locations
            if (role.Contains("region") || (!isIssueContext && location.ToLower().Contains("region")))
            {
                return await GetRegionEmployee();
            }

            // Generic role-based lookup as fallback
            // Try to find in department first
            var employees = await GetEmployeesByDepartmentAsync(departmentId);
            var roleMatch = employees.FirstOrDefault(e => string.Equals(e.Role, role, StringComparison.OrdinalIgnoreCase));
            if (roleMatch != null)
                return roleMatch;

            // If not found in department, try by station name
            if (!string.IsNullOrEmpty(location))
            {
                var stationEmployees = await GetFactoryEmployeesByStationAsync(location);
                return stationEmployees.FirstOrDefault(e => string.Equals(e.Role, role, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        public async Task<(EmployeeBkp loggedInUserEmployee, Department loggedInUserDepartment, Station loggedInUserStation)> GetEmployeeAndDepartmentAsync(string payrollNo)
        {
            var loggedInUserEmployee = await _context.EmployeeBkps
                .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);

            if (loggedInUserEmployee == null)
                return (null, null, null);

            var loggedInUserDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == loggedInUserEmployee.Department);  // Added Async

            Station loggedInUserStation;
            if (loggedInUserEmployee.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
            {
                // Explicitly create an HQ station
                loggedInUserStation = new Station { StationId = 0, StationName = "HQ" };
            }
            else if (int.TryParse(loggedInUserEmployee.Station, out int stationId))
            {
                loggedInUserStation = await _context.Stations
                    .FirstOrDefaultAsync(d => d.StationId == stationId);
            }
            else
            {
                // Handle invalid station data gracefully
                loggedInUserStation = new Station { StationId = -1, StationName = "Unknown" };
            }

            return (loggedInUserEmployee, loggedInUserDepartment, loggedInUserStation);
        }

        public async Task<(EmployeeBkp employeeDetail, Department departmentDetail, Station stationDetail)> GetEmployeeDepartmentStationDetailAsync(string payrollNo)
        {
            var employeeDetail = await _context.EmployeeBkps
                .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);

            if (employeeDetail == null)
                return (null, null, null);

            var departmentDetail = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == employeeDetail.Department);  // Added Async

            Station stationDetail;
            if (employeeDetail.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
            {
                // Explicitly create an HQ station
                stationDetail = new Station { StationId = 0, StationName = "HQ" };
            }
            else if (int.TryParse(employeeDetail.Station, out int stationId))
            {
                stationDetail = await _context.Stations
                    .FirstOrDefaultAsync(d => d.StationId == stationId);
            }
            else
            {
                // Handle invalid station data gracefully
                stationDetail = new Station { StationId = -1, StationName = "Unknown" };
            }

            return (employeeDetail, departmentDetail, stationDetail);
        }

        public EmployeeBkp GetSupervisor(EmployeeBkp loggedInUserEmployee)
        {
            // Get supervisor from employee's HOD or Supervisor property
            return _context.EmployeeBkps
                .FirstOrDefault(e => e.PayrollNo == loggedInUserEmployee.Supervisor || e.PayrollNo == loggedInUserEmployee.Hod);
        }

        public async Task<EmployeeBkp> GetFactoryEmployeeAsync(string locationName)
        {
            if (string.IsNullOrEmpty(locationName)) return null;

            EmployeeBkp employee = null;
            var station = await _context.Stations
                   .FirstOrDefaultAsync(d => d.StationName == locationName);

            // 1. First attempt: Find employee by station and designation containing "ADMINISTR" (case-insensitive)
            if (station != null)
            {
                string formattedStationId = station.StationId.ToString("D3");
                employee = await _context.EmployeeBkps
                    .Where(e => e.Station == formattedStationId &&
                               e.Designation.ToLower().Contains("field systems administr") && // Partial match for "ADMINISTRATOR" or "ADMINISTRA"
                               e.EmpisCurrActive == 0)
                    .FirstOrDefaultAsync();
            }

            // 2. Fallback: If no employee found, search by designation alone (case-insensitive)
            if (employee == null)
            {
                employee = await _context.EmployeeBkps
                    .Where(e => e.Designation.ToLower().Contains("field systems administr") && 
                                e.Station !="HQ" &&
                                e.EmpisCurrActive == 0)
                    .FirstOrDefaultAsync();
            }

            return employee;
        }

        public async Task<EmployeeBkp> GetRegionEmployee()
        {
            return await _context.EmployeeBkps
                .Where(e => e.Designation.Contains("REGIONAL ICT"))
                .FirstOrDefaultAsync(e=> e.EmpisCurrActive == 0); // Add Async
        }

        public async Task<EmployeeBkp> GetHoEmployeeAsync(string locationName)
        {
            if (string.IsNullOrEmpty(locationName)) return null;
            var department = await _context.Departments
       .FirstOrDefaultAsync(d => d.DepartmentName == locationName);

            if (department != null)
            {
                return await _context.EmployeeBkps
                    .FirstOrDefaultAsync(e => e.Department == department.DepartmentId &&
                e.EmpisCurrActive == 0);
            }
            return null;
        }
        public async Task<EmployeeBkp> GetEmployeeByPayrollAsync(string payrollNo)
        {
            // Implement logic to fetch employee by payroll number
            return await _context.EmployeeBkps.FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
        }

        public async Task<List<EmployeeBkp>> GetEmployeesByDepartmentAsync(int departmentId)
        {

            return await _context.EmployeeBkps
                .Where(e => e.Department == departmentId.ToString() && e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
        }
        public async Task<List<EmployeeBkp>> GetEmployeesByDepartmentNameAsync(string deliveryStation)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentName == deliveryStation);

            if (department == null)
                return new List<EmployeeBkp>(); // Return empty list if department not found

            return await _context.EmployeeBkps
                .Where(e => e.Department == department.DepartmentId && e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
        }
        public async Task<List<EmployeeBkp>> GetSupervisorsByDepartmentAsync(int departmentId)
        {
            var roles = new[] { "Admin", "Hod", "supervisor" };
            return await _context.EmployeeBkps
                .Where(e => e.Department == departmentId.ToString() && roles.Contains(e.Role)
                && e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
        }
        public async Task<List<EmployeeBkp>> GetFactoryEmployeesByStationAsync(string station)
        {
            var deliveryStation = await _context.Stations
                  .FirstOrDefaultAsync(d => d.StationName == station);
            if (deliveryStation != null)
            {
                string formattedStationId = deliveryStation.StationId.ToString("D3");
                return await _context.EmployeeBkps
                .Where(e => e.Station == formattedStationId && e.EmpisCurrActive == 0)
                .OrderBy(e => e.Scale)
                .ToListAsync();
            }
            return null;
        }


    }
}