# Changing Employee Passwords in MRIV System

## Background

The MRIV system integrates with a legacy KTDALeave database that was originally developed for SQL Server 2008. This database uses SQL Server's built-in password encryption functions (`pwdencrypt()` and `pwdcompare()`) for authentication. When migrating to SQL Server 2022, we encountered compatibility issues with these password functions.

## Issue Description

The `pwdencrypt()` function in SQL Server 2022 produces password hashes that are incompatible with those created in SQL Server 2008, even when the database compatibility level is set to 100 (SQL Server 2008). This causes authentication failures when:

1. New passwords are set using the `Pro_passwordcheck` stored procedure
2. Authentication is attempted using the `Pro_password` stored procedure

## Solution: Password Hash Copying

For development and testing purposes, we've implemented a solution that copies password hashes from accounts with working passwords to test accounts. This allows us to maintain authentication functionality without modifying the core authentication mechanisms used by other systems that share this database.

### Implementation Details

The following stored procedure allows copying a password hash from one employee account to another:

```sql
CREATE OR ALTER PROCEDURE dbo.CopyWorkingPasswordForTesting
    @SourcePayrollNo varchar(50),
    @TargetPayrollNo varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @WorkingHash varbinary(50);
    
    -- Get a hash that's known to work
    SELECT @WorkingHash = pass
    FROM ktdaleave.dbo.employee_bkp
    WHERE payrollno = @SourcePayrollNo;
    
    IF @WorkingHash IS NULL
    BEGIN
        SELECT 'Source account not found' AS Result;
        RETURN;
    END
    
    -- Copy the working hash to the target account
    UPDATE ktdaleave.dbo.employee_bkp
    SET pass = @WorkingHash
    WHERE payrollno = @TargetPayrollNo;
    
    IF @@ROWCOUNT > 0
        SELECT 'Password hash copied successfully' AS Result;
    ELSE
        SELECT 'Target account not found' AS Result;
END
GO
```

### How It Works

1. The procedure takes two parameters:
   - `@SourcePayrollNo`: The payroll number of an employee with a working password
   - `@TargetPayrollNo`: The payroll number of the employee whose password you want to change

2. It retrieves the password hash (`pass` column) from the source employee record

3. It updates the target employee record with the same password hash

4. After execution, the target employee can log in using the same password as the source employee

### Usage Example

```sql
-- Copy password hash from employee HGD01234 to employee HGD03519
EXEC dbo.CopyWorkingPasswordForTesting 'HGD01234', 'HGD03519';
```

## Security Considerations

1. **Development Use Only**: This approach is intended for development and testing environments only. It should not be used in production.

2. **Temporary Solution**: This is a temporary workaround while a more comprehensive solution is developed.

3. **Password Sharing**: Be aware that this procedure results in two employees sharing the same password. Ensure you're using test accounts or inform affected users to change their passwords afterward.

4. **Audit Trail**: There is no audit trail for password changes made using this method. Consider implementing logging if needed.

## Long-Term Solutions

For a production environment, consider implementing one of these approaches:

1. **Custom Password Hashing**: Create custom password hashing and verification functions that work consistently across SQL Server versions.

2. **Dual Authentication System**: Implement a system that can handle both old and new password formats during a transition period.

3. **Modern Authentication Layer**: Move authentication to a modern framework like ASP.NET Identity that handles password hashing securely.

## Implementation Steps for Production

1. Develop and test the chosen long-term solution in a development environment

2. Create a migration plan for existing passwords

3. Implement the solution with minimal disruption to users

4. Monitor authentication success rates after implementation

## Related Database Objects

- `Pro_password`: Stored procedure used for authentication
- `Pro_passwordcheck`: Stored procedure used for password changes
- `employee_bkp` table: Contains employee records including password hashes in the `pass` column

## Conclusion

The password hash copying approach provides a quick solution for development and testing purposes. However, a more robust solution should be implemented before moving to production to ensure proper security and maintainability of the authentication system.
