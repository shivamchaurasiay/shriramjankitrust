USE [mindforg_ramjankingo];
GO

-- 1. Roles Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        RoleId INT PRIMARY KEY IDENTITY(1,1),
        RoleName NVARCHAR(50) NOT NULL UNIQUE,
        Description NVARCHAR(200) NULL
    );
    INSERT INTO Roles (RoleName, Description) VALUES 
    ('Super Admin', 'Full system access'),
    ('Staff', 'Operational admin access'),
    ('Member', 'Donor / Registered Member access');
END
GO

-- 2. Users Table (for Admin and Member logins)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        UserId INT PRIMARY KEY IDENTITY(1,1),
        UserName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NULL,
        Mobile NVARCHAR(20) NOT NULL,
        Password NVARCHAR(255) NOT NULL,
        RoleId INT NOT NULL FOREIGN KEY REFERENCES Roles(RoleId),
        Status INT NOT NULL DEFAULT 1, -- 1: Active, 0: Inactive, -1: Suspended
        IsLogin BIT NOT NULL DEFAULT 0,
        EmailVerifiedAt DATETIME NULL,
        LastLoginAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );

    -- Seed Default Super Admin
    INSERT INTO Users (UserName, Email, Mobile, Password, RoleId, Status)
    VALUES ('Super Admin', 'admin@shriramjankitrust.com', '9999999999', 'admin123', 1, 1);
END
GO

-- 3. Members Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Members')
BEGIN
    CREATE TABLE Members (
        MemberId INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NULL FOREIGN KEY REFERENCES Users(UserId),
        MemberCode NVARCHAR(50) NOT NULL UNIQUE,
        FullName NVARCHAR(150) NOT NULL,
        Email NVARCHAR(150) NULL,
        Mobile NVARCHAR(20) NOT NULL,
        Address NVARCHAR(255) NULL,
        City NVARCHAR(100) NULL,
        State NVARCHAR(100) NULL,
        PinCode NVARCHAR(20) NULL,
        DateOfBirth DATE NULL,
        MemberType NVARCHAR(50) DEFAULT 'Regular', -- Regular, Life, Patron, Volunteer
        Status INT NOT NULL DEFAULT 1, -- 1: Active, 0: Inactive
        ProfilePhoto NVARCHAR(255) NULL,
        JoinedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_Members_Mobile ON Members(Mobile);
    CREATE INDEX IX_Members_Email ON Members(Email);
END
GO

-- 4. Campaigns / Causes Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Campaigns')
BEGIN
    CREATE TABLE Campaigns (
        CampaignId INT PRIMARY KEY IDENTITY(1,1),
        Title NVARCHAR(200) NOT NULL,
        Slug NVARCHAR(220) NULL,
        Category NVARCHAR(100) NULL,
        ShortDescription NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        TargetAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        RaisedAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        StartDate DATE NULL,
        EndDate DATE NULL,
        ImagePath NVARCHAR(255) NULL,
        IsFeatured BIT NOT NULL DEFAULT 0,
        Status INT NOT NULL DEFAULT 1, -- 1: Active, 0: Inactive
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 5. Projects Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Projects')
BEGIN
    CREATE TABLE Projects (
        ProjectId INT PRIMARY KEY IDENTITY(1,1),
        Title NVARCHAR(200) NOT NULL,
        Slug NVARCHAR(220) NULL,
        Category NVARCHAR(100) NULL,
        Description NVARCHAR(MAX) NULL,
        ImagePath NVARCHAR(255) NULL,
        Status INT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 6. Donations Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Donations')
BEGIN
    CREATE TABLE Donations (
        DonationId INT PRIMARY KEY IDENTITY(1,1),
        DonationCode NVARCHAR(50) NOT NULL UNIQUE,
        ReceiptNumber NVARCHAR(50) NULL UNIQUE,
        MemberId INT NULL FOREIGN KEY REFERENCES Members(MemberId),
        CampaignId INT NULL FOREIGN KEY REFERENCES Campaigns(CampaignId),
        DonorName NVARCHAR(150) NOT NULL,
        DonorEmail NVARCHAR(150) NULL,
        DonorPhone NVARCHAR(20) NOT NULL,
        DonorAddress NVARCHAR(255) NULL,
        DonorPan NVARCHAR(20) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(10) NOT NULL DEFAULT 'INR',
        PaymentMode NVARCHAR(50) NOT NULL DEFAULT 'Online', -- Online, Cash, Cheque, Bank Transfer, UPI
        PaymentStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Successful, Failed, Refunded
        PaymentGatewayReference NVARCHAR(100) NULL,
        TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
        PaidAt DATETIME NULL,
        Notes NVARCHAR(500) NULL,
        CreatedBy NVARCHAR(100) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_Donations_DonorPhone ON Donations(DonorPhone);
    CREATE INDEX IX_Donations_Status ON Donations(PaymentStatus);
END
GO

-- 7. Payments Table (Gateway tracking)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments')
BEGIN
    CREATE TABLE Payments (
        PaymentId INT PRIMARY KEY IDENTITY(1,1),
        DonationId INT NOT NULL FOREIGN KEY REFERENCES Donations(DonationId),
        GatewayName NVARCHAR(50) NOT NULL, -- Razorpay, Cashfree, Manual
        GatewayOrderId NVARCHAR(100) NULL,
        GatewayPaymentId NVARCHAR(100) NULL,
        GatewaySignature NVARCHAR(255) NULL,
        Status NVARCHAR(50) NOT NULL,
        VerifiedAt DATETIME NULL,
        RawResponse NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 8. Receipts Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Receipts')
BEGIN
    CREATE TABLE Receipts (
        ReceiptId INT PRIMARY KEY IDENTITY(1,1),
        DonationId INT NOT NULL FOREIGN KEY REFERENCES Donations(DonationId),
        ReceiptNumber NVARCHAR(50) NOT NULL UNIQUE,
        DocumentPath NVARCHAR(255) NULL,
        IssuedAt DATETIME NOT NULL DEFAULT GETDATE(),
        Version INT NOT NULL DEFAULT 1,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Issued'
    );
END
GO

-- 9. Blogs / News Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Blogs')
BEGIN
    CREATE TABLE Blogs (
        BlogId INT PRIMARY KEY IDENTITY(1,1),
        Title NVARCHAR(255) NOT NULL,
        Category NVARCHAR(100) NULL,
        Author NVARCHAR(100) NULL DEFAULT 'Admin',
        ShortDescription NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        ImagePath NVARCHAR(255) NULL,
        Status INT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 10. Blog Comments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BlogComments')
BEGIN
    CREATE TABLE BlogComments (
        CommentId INT PRIMARY KEY IDENTITY(1,1),
        BlogId INT NOT NULL FOREIGN KEY REFERENCES Blogs(BlogId) ON DELETE CASCADE,
        UserName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NULL,
        Comment NVARCHAR(MAX) NOT NULL,
        IsApproved BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 11. Gallery Master & Details (Matching AuraWeb structure)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GalleryMaster')
BEGIN
    CREATE TABLE GalleryMaster (
        GalleryId INT PRIMARY KEY IDENTITY(1,1),
        GalleryName NVARCHAR(150) NOT NULL,
        Status INT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GalleryDetails')
BEGIN
    CREATE TABLE GalleryDetails (
        GalleryDetailsId INT PRIMARY KEY IDENTITY(1,1),
        GalleryId INT NOT NULL FOREIGN KEY REFERENCES GalleryMaster(GalleryId) ON DELETE CASCADE,
        ImageTitle NVARCHAR(150) NULL,
        ImagePath NVARCHAR(255) NOT NULL,
        Status INT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 12. Team Members Table (Matching AuraWeb structure)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OurTeamDetails')
BEGIN
    CREATE TABLE OurTeamDetails (
        TeamId INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(100) NOT NULL,
        Designation NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        ImagePath NVARCHAR(255) NULL,
        FacebookUrl NVARCHAR(255) NULL,
        TwitterUrl NVARCHAR(255) NULL,
        LinkedInUrl NVARCHAR(255) NULL,
        Status INT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 13. Contact Inquiries & Volunteers Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContactInquiries')
BEGIN
    CREATE TABLE ContactInquiries (
        InquiryId INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NOT NULL,
        Phone NVARCHAR(20) NULL,
        Subject NVARCHAR(200) NULL,
        Message NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'New', -- New, Responded, Closed
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VolunteerRegistrations')
BEGIN
    CREATE TABLE VolunteerRegistrations (
        VolunteerId INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NOT NULL,
        Phone NVARCHAR(20) NOT NULL,
        Address NVARCHAR(255) NULL,
        Occupation NVARCHAR(100) NULL,
        AreaOfInterest NVARCHAR(100) NULL,
        Message NVARCHAR(MAX) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 14. Member Imports & Errors Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MemberImports')
BEGIN
    CREATE TABLE MemberImports (
        ImportId INT PRIMARY KEY IDENTITY(1,1),
        FileName NVARCHAR(255) NOT NULL,
        UploadedBy NVARCHAR(100) NOT NULL,
        TotalRows INT NOT NULL DEFAULT 0,
        SuccessRows INT NOT NULL DEFAULT 0,
        FailedRows INT NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Completed',
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MemberImportErrors')
BEGIN
    CREATE TABLE MemberImportErrors (
        ErrorId INT PRIMARY KEY IDENTITY(1,1),
        ImportId INT NOT NULL FOREIGN KEY REFERENCES MemberImports(ImportId) ON DELETE CASCADE,
        RowNumber INT NOT NULL,
        RowData NVARCHAR(MAX) NULL,
        ErrorMessage NVARCHAR(500) NOT NULL
    );
END
GO

-- 15. Settings Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Settings')
BEGIN
    CREATE TABLE Settings (
        SettingId INT PRIMARY KEY IDENTITY(1,1),
        SettingKey NVARCHAR(100) NOT NULL UNIQUE,
        SettingValue NVARCHAR(MAX) NULL,
        Description NVARCHAR(255) NULL
    );

    INSERT INTO Settings (SettingKey, SettingValue, Description) VALUES
    ('OrgName', 'Shriram Janki Trust', 'Legal organization name'),
    ('OrgTagline', 'Committed to Education, Healthcare, and Community Development', 'Organization Tagline'),
    ('OrgEmail', 'info@shriramjankitrust.com', 'Official Email Address'),
    ('OrgPhone', '+91 98765 43210', 'Official Phone Number'),
    ('OrgAddress', 'Ayodhya, Uttar Pradesh, India', 'Official Address'),
    ('ReceiptPrefix', 'SJT', 'Prefix for donation receipts'),
    ('ReceiptNextNumber', '1001', 'Next sequence number for receipts'),
    ('MemberPrefix', 'MEM', 'Prefix for member ID generation'),
    ('MemberNextNumber', '101', 'Next sequence number for members'),
    ('Currency', 'INR', 'Default donation currency'),
    ('RazorpayKeyId', 'rzp_test_sample', 'Razorpay Key ID'),
    ('RazorpayKeySecret', 'sample_secret_key', 'Razorpay Key Secret'),
    ('EnableLiveGateway', '0', '0 for demo/test mode, 1 for live gateway');
END
GO

-- 16. Audit Logs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        LogId INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NULL,
        Action NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(50) NULL,
        Details NVARCHAR(MAX) NULL,
        IpAddress NVARCHAR(50) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 17. Seed initial campaigns and projects if empty
IF NOT EXISTS (SELECT 1 FROM Campaigns)
BEGIN
    INSERT INTO Campaigns (Title, Slug, Category, ShortDescription, Description, TargetAmount, RaisedAmount, StartDate, EndDate, ImagePath, IsFeatured, Status)
    VALUES 
    ('Child Education & School Kits Support', 'child-education-support', 'Education', 'Providing notebooks, uniform and schooling support to underprivileged children.', 'Every child deserves an opportunity to learn. We support 500+ students in rural areas with school kits, tuition support and digital education access.', 250000.00, 142500.00, '2026-01-01', '2026-12-31', '/assets/img/causes-img-1.jpg', 1, 1),
    ('Rural Free Health Camps & Medicine Distribution', 'rural-health-camps', 'Healthcare', 'Conducting monthly free health check-up camps and providing vital medicines to elders and families.', 'Our medical relief camps reach remote villages offering free doctor consultations, diagnostic tests and life-saving medicines.', 500000.00, 318000.00, '2026-01-01', '2026-12-31', '/assets/img/causes-img-2.jpg', 1, 1),
    ('Daily Food Relief & Annakshetra', 'food-relief-program', 'Community', 'Serving warm nutritious meals daily to sadhus, pilgrims and families in distress.', 'Shriram Janki Trust runs daily Annakshetra serving hygienic, nutritious food with love and devotion.', 300000.00, 265000.00, '2026-01-01', '2026-12-31', '/assets/img/causes-img-3.jpg', 1, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM Projects)
BEGIN
    INSERT INTO Projects (Title, Slug, Category, Description, ImagePath, Status)
    VALUES
    ('Digital Classroom Initiative', 'digital-classroom', 'Education', 'Setting up smart classrooms with computers in community centers.', '/assets/img/project-1.jpg', 1),
    ('Clean Drinking Water Station', 'clean-drinking-water', 'Sanitation', 'Installing RO filtration plants in rural public schools and village squares.', '/assets/img/project-2.jpg', 1),
    ('Women Skill Development Center', 'women-skill-development', 'Livelihood', 'Vocational sewing and handicraft training for women self-reliance.', '/assets/img/project-3.jpg', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM GalleryMaster)
BEGIN
    INSERT INTO GalleryMaster (GalleryName, Status) VALUES ('Community Service & Camps', 1);
    INSERT INTO GalleryMaster (GalleryName, Status) VALUES ('Events & Celebrations', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM OurTeamDetails)
BEGIN
    INSERT INTO OurTeamDetails (Name, Designation, Description, ImagePath, Status)
    VALUES
    ('Acharya Rajesh Sharma', 'Founder & Managing Trustee', 'Dedicated social reformer and spiritual mentor guiding social welfare projects.', '/assets/img/team-1.jpg', 1),
    ('Dr. Sneha Verma', 'Healthcare Director', 'Renowned general physician supervising rural health camps and medical outreach.', '/assets/img/team-2.jpg', 1),
    ('Vikramaditya Singh', 'Operations Head', 'Leading volunteer teams, logistics and relief execution on the ground.', '/assets/img/team-3.jpg', 1);
END
GO

USE [mindforg_ramjankingo];
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasswordResetOtps')
BEGIN
    CREATE TABLE PasswordResetOtps (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(150) NOT NULL,
        OtpCode NVARCHAR(10) NOT NULL,
        ExpiresAt DATETIME NOT NULL,
        IsUsed BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO
USE [mindforg_ramjankingo];
GO

-- 1. ManageAdminLogin Procedure
CREATE OR ALTER PROCEDURE ManageAdminLogin
    @Action NVARCHAR(10) = NULL,
    @UserId INT = NULL,
    @UserName NVARCHAR(100) = NULL,
    @Email NVARCHAR(150) = NULL,
    @Mobile NVARCHAR(150) = NULL,
    @Password NVARCHAR(255) = NULL,
    @RoleId INT = NULL,
    @IsLogin BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = '1' -- Insert new user
    BEGIN
        INSERT INTO Users (UserName, Email, Mobile, Password, RoleId, Status, CreatedAt, UpdatedAt)
        VALUES (@UserName, @Email, @Mobile, @Password, ISNULL(@RoleId, 2), 1, GETDATE(), GETDATE());
        SELECT SCOPE_IDENTITY() AS UserId, 1 AS RowsAffected;
    END
    ELSE IF @Action = '2' -- Update User Profile
    BEGIN
        UPDATE Users
        SET UserName = ISNULL(@UserName, UserName),
            Email = ISNULL(@Email, Email),
            Mobile = ISNULL(@Mobile, Mobile),
            UpdatedAt = GETDATE()
        WHERE UserId = @UserId;
        SELECT @UserId AS UserId, @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '3' -- Toggle Status
    BEGIN
        UPDATE Users
        SET Status = CASE WHEN Status = 1 THEN 0 ELSE 1 END,
            UpdatedAt = GETDATE()
        WHERE UserId = @UserId;
        SELECT @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '4' -- Get by Mobile or Email (for login)
    BEGIN
        SELECT u.UserId, u.UserName, u.Email, u.Mobile, u.Password, u.RoleId, r.RoleName, u.Status, u.IsLogin, u.LastLoginAt
        FROM Users u
        JOIN Roles r ON u.RoleId = r.RoleId
        WHERE (u.Mobile = @Mobile OR u.Email = @Mobile) AND u.Status = 1;
    END
    ELSE IF @Action = '5' -- Mark login
    BEGIN
        UPDATE Users
        SET IsLogin = 1, LastLoginAt = GETDATE()
        WHERE UserId = @UserId;
        SELECT @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '6' -- Mark logout
    BEGIN
        UPDATE Users
        SET IsLogin = 0
        WHERE UserId = @UserId;
        SELECT @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '7' -- Get by UserId
    BEGIN
        SELECT u.UserId, u.UserName, u.Email, u.Mobile, u.Password, u.RoleId, r.RoleName, u.Status, u.IsLogin, u.LastLoginAt
        FROM Users u
        JOIN Roles r ON u.RoleId = r.RoleId
        WHERE u.UserId = @UserId;
    END
    ELSE IF @Action = '8' -- Get all users
    BEGIN
        SELECT u.UserId, u.UserName, u.Email, u.Mobile, u.RoleId, r.RoleName, u.Status, u.LastLoginAt, u.CreatedAt
        FROM Users u
        JOIN Roles r ON u.RoleId = r.RoleId
        ORDER BY u.UserId DESC;
    END
END
GO

-- 2. USP_ResetPassword Procedure
CREATE OR ALTER PROCEDURE USP_ResetPassword
    @oldPassword NVARCHAR(255),
    @newPassword NVARCHAR(255),
    @employeeCode NVARCHAR(100) -- Mobile or UserId
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Uid INT = NULL;

    IF ISNUMERIC(@employeeCode) = 1
        SELECT @Uid = UserId FROM Users WHERE (UserId = CAST(@employeeCode AS INT) OR Mobile = @employeeCode) AND Password = @oldPassword;
    ELSE
        SELECT @Uid = UserId FROM Users WHERE (Email = @employeeCode OR Mobile = @employeeCode) AND Password = @oldPassword;

    IF @Uid IS NOT NULL
    BEGIN
        UPDATE Users SET Password = @newPassword, UpdatedAt = GETDATE() WHERE UserId = @Uid;
        SELECT 1 AS Status, 'Password changed successfully.' AS Message;
    END
    ELSE
    BEGIN
        SELECT 0 AS Status, 'Current password does not match.' AS Message;
    END
END
GO

-- 3. ManageMembers Procedure
CREATE OR ALTER PROCEDURE ManageMembers
    @Action NVARCHAR(10),
    @MemberId INT = NULL,
    @UserId INT = NULL,
    @MemberCode NVARCHAR(50) = NULL,
    @FullName NVARCHAR(150) = NULL,
    @Email NVARCHAR(150) = NULL,
    @Mobile NVARCHAR(20) = NULL,
    @Address NVARCHAR(255) = NULL,
    @City NVARCHAR(100) = NULL,
    @State NVARCHAR(100) = NULL,
    @PinCode NVARCHAR(20) = NULL,
    @DateOfBirth DATE = NULL,
    @MemberType NVARCHAR(50) = 'Regular',
    @Status INT = 1,
    @ProfilePhoto NVARCHAR(255) = NULL,
    @Search NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = '1' -- Insert Member
    BEGIN
        -- Generate MemberCode if not provided
        IF @MemberCode IS NULL OR LTRIM(RTRIM(@MemberCode)) = ''
        BEGIN
            DECLARE @NextSeq INT = 101;
            SELECT @NextSeq = CAST(SettingValue AS INT) FROM Settings WHERE SettingKey = 'MemberNextNumber';
            SET @MemberCode = 'MEM' + CAST(@NextSeq AS NVARCHAR(20));
            UPDATE Settings SET SettingValue = CAST(@NextSeq + 1 AS NVARCHAR(20)) WHERE SettingKey = 'MemberNextNumber';
        END

        -- Create User login account for member if not existing
        DECLARE @NewUserId INT = NULL;
        SELECT @NewUserId = UserId FROM Users WHERE Mobile = @Mobile;

        IF @NewUserId IS NULL
        BEGIN
            INSERT INTO Users (UserName, Email, Mobile, Password, RoleId, Status)
            VALUES (@FullName, @Email, @Mobile, 'member123', 3, 1);
            SET @NewUserId = SCOPE_IDENTITY();
        END

        INSERT INTO Members (UserId, MemberCode, FullName, Email, Mobile, Address, City, State, PinCode, DateOfBirth, MemberType, Status, ProfilePhoto, JoinedAt)
        VALUES (@NewUserId, @MemberCode, @FullName, @Email, @Mobile, @Address, @City, @State, @PinCode, @DateOfBirth, @MemberType, @Status, @ProfilePhoto, GETDATE());

        DECLARE @NewMemberId INT = SCOPE_IDENTITY();
        SELECT @NewMemberId AS MemberId, @MemberCode AS MemberCode, 1 AS RowsAffected;
    END
    ELSE IF @Action = '2' -- Update Member
    BEGIN
        UPDATE Members
        SET FullName = ISNULL(@FullName, FullName),
            Email = ISNULL(@Email, Email),
            Mobile = ISNULL(@Mobile, Mobile),
            Address = ISNULL(@Address, Address),
            City = ISNULL(@City, City),
            State = ISNULL(@State, State),
            PinCode = ISNULL(@PinCode, PinCode),
            DateOfBirth = ISNULL(@DateOfBirth, DateOfBirth),
            MemberType = ISNULL(@MemberType, MemberType),
            Status = ISNULL(@Status, Status),
            ProfilePhoto = ISNULL(@ProfilePhoto, ProfilePhoto),
            UpdatedAt = GETDATE()
        WHERE MemberId = @MemberId;

        -- Also update user record if linked
        SELECT @UserId = UserId FROM Members WHERE MemberId = @MemberId;
        IF @UserId IS NOT NULL
        BEGIN
            UPDATE Users
            SET UserName = ISNULL(@FullName, UserName),
                Email = ISNULL(@Email, Email),
                Mobile = ISNULL(@Mobile, Mobile),
                UpdatedAt = GETDATE()
            WHERE UserId = @UserId;
        END

        SELECT @MemberId AS MemberId, @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '3' -- Toggle Status
    BEGIN
        UPDATE Members
        SET Status = CASE WHEN Status = 1 THEN 0 ELSE 1 END,
            UpdatedAt = GETDATE()
        WHERE MemberId = @MemberId;

        SELECT @UserId = UserId FROM Members WHERE MemberId = @MemberId;
        IF @UserId IS NOT NULL
        BEGIN
            UPDATE Users SET Status = (SELECT Status FROM Members WHERE MemberId = @MemberId) WHERE UserId = @UserId;
        END

        SELECT @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '4' -- List Members
    BEGIN
        SELECT m.MemberId, m.UserId, m.MemberCode, m.FullName, m.Email, m.Mobile, m.Address, m.City, m.State, m.PinCode,
               m.DateOfBirth, m.MemberType, m.Status, m.ProfilePhoto, m.JoinedAt,
               ISNULL(SUM(d.Amount), 0) AS TotalDonated,
               COUNT(d.DonationId) AS TotalDonationsCount
        FROM Members m
        LEFT JOIN Donations d ON m.MemberId = d.MemberId AND d.PaymentStatus = 'Successful'
        WHERE (@Search IS NULL OR @Search = '' 
               OR m.FullName LIKE '%' + @Search + '%' 
               OR m.MemberCode LIKE '%' + @Search + '%' 
               OR m.Mobile LIKE '%' + @Search + '%'
               OR m.Email LIKE '%' + @Search + '%')
        GROUP BY m.MemberId, m.UserId, m.MemberCode, m.FullName, m.Email, m.Mobile, m.Address, m.City, m.State, m.PinCode,
                 m.DateOfBirth, m.MemberType, m.Status, m.ProfilePhoto, m.JoinedAt
        ORDER BY m.MemberId DESC;
    END
    ELSE IF @Action = '5' -- Get by MemberId
    BEGIN
        SELECT m.MemberId, m.UserId, m.MemberCode, m.FullName, m.Email, m.Mobile, m.Address, m.City, m.State, m.PinCode,
               m.DateOfBirth, m.MemberType, m.Status, m.ProfilePhoto, m.JoinedAt,
               ISNULL(SUM(d.Amount), 0) AS TotalDonated,
               COUNT(d.DonationId) AS TotalDonationsCount
        FROM Members m
        LEFT JOIN Donations d ON m.MemberId = d.MemberId AND d.PaymentStatus = 'Successful'
        WHERE m.MemberId = @MemberId
        GROUP BY m.MemberId, m.UserId, m.MemberCode, m.FullName, m.Email, m.Mobile, m.Address, m.City, m.State, m.PinCode,
                 m.DateOfBirth, m.MemberType, m.Status, m.ProfilePhoto, m.JoinedAt;
    END
    ELSE IF @Action = '6' -- Get by UserId
    BEGIN
        SELECT m.MemberId, m.UserId, m.MemberCode, m.FullName, m.Email, m.Mobile, m.Address, m.City, m.State, m.PinCode,
               m.DateOfBirth, m.MemberType, m.Status, m.ProfilePhoto, m.JoinedAt,
               ISNULL(SUM(d.Amount), 0) AS TotalDonated,
               COUNT(d.DonationId) AS TotalDonationsCount
        FROM Members m
        LEFT JOIN Donations d ON m.MemberId = d.MemberId AND d.PaymentStatus = 'Successful'
        WHERE m.UserId = @UserId
        GROUP BY m.MemberId, m.UserId, m.MemberCode, m.FullName, m.Email, m.Mobile, m.Address, m.City, m.State, m.PinCode,
                 m.DateOfBirth, m.MemberType, m.Status, m.ProfilePhoto, m.JoinedAt;
    END
    ELSE IF @Action = '7' -- Check Duplicate
    BEGIN
        SELECT MemberId, MemberCode, FullName, Mobile, Email 
        FROM Members 
        WHERE Mobile = @Mobile OR (@Email IS NOT NULL AND @Email <> '' AND Email = @Email) OR (@MemberCode IS NOT NULL AND MemberCode = @MemberCode);
    END
END
GO

-- 4. ManageDonations Procedure
CREATE OR ALTER PROCEDURE ManageDonations
    @Action NVARCHAR(10),
    @DonationId INT = NULL,
    @DonationCode NVARCHAR(50) = NULL,
    @ReceiptNumber NVARCHAR(50) = NULL,
    @MemberId INT = NULL,
    @CampaignId INT = NULL,
    @DonorName NVARCHAR(150) = NULL,
    @DonorEmail NVARCHAR(150) = NULL,
    @DonorPhone NVARCHAR(20) = NULL,
    @DonorAddress NVARCHAR(255) = NULL,
    @DonorPan NVARCHAR(20) = NULL,
    @Amount DECIMAL(18,2) = NULL,
    @PaymentMode NVARCHAR(50) = 'Online',
    @PaymentStatus NVARCHAR(50) = 'Pending',
    @PaymentGatewayReference NVARCHAR(100) = NULL,
    @Notes NVARCHAR(500) = NULL,
    @CreatedBy NVARCHAR(100) = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @Search NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = '1' -- Create Online Donation Pending
    BEGIN
        IF @DonationCode IS NULL OR LTRIM(RTRIM(@DonationCode)) = ''
            SET @DonationCode = 'DON' + CONVERT(NVARCHAR(8), GETDATE(), 112) + RIGHT(REPLACE(CAST(NEWID() AS NVARCHAR(36)),'-',''), 6);

        -- If donor phone matches member, auto link
        IF @MemberId IS NULL AND @DonorPhone IS NOT NULL
        BEGIN
            SELECT @MemberId = MemberId FROM Members WHERE Mobile = @DonorPhone;
        END

        INSERT INTO Donations (DonationCode, MemberId, CampaignId, DonorName, DonorEmail, DonorPhone, DonorAddress, DonorPan, Amount, PaymentMode, PaymentStatus, CreatedBy, CreatedAt)
        VALUES (@DonationCode, @MemberId, @CampaignId, @DonorName, @DonorEmail, @DonorPhone, @DonorAddress, @DonorPan, @Amount, @PaymentMode, 'Pending', @CreatedBy, GETDATE());

        DECLARE @NewDonId INT = SCOPE_IDENTITY();
        SELECT @NewDonId AS DonationId, @DonationCode AS DonationCode, 1 AS RowsAffected;
    END
    ELSE IF @Action = '2' -- Mark Successful & Issue Receipt
    BEGIN
        -- Generate Receipt Number if not present
        IF @ReceiptNumber IS NULL
        BEGIN
            DECLARE @Prefix NVARCHAR(20) = 'SJT';
            DECLARE @NextRec INT = 1001;
            SELECT @Prefix = SettingValue FROM Settings WHERE SettingKey = 'ReceiptPrefix';
            SELECT @NextRec = CAST(SettingValue AS INT) FROM Settings WHERE SettingKey = 'ReceiptNextNumber';
            
            -- Ensure receipt number is unique across both Donations and Receipts tables
            WHILE EXISTS (SELECT 1 FROM Donations WHERE ReceiptNumber = @Prefix + '-' + CAST(YEAR(GETDATE()) AS NVARCHAR(4)) + '-' + CAST(@NextRec AS NVARCHAR(20)))
                  OR EXISTS (SELECT 1 FROM Receipts WHERE ReceiptNumber = @Prefix + '-' + CAST(YEAR(GETDATE()) AS NVARCHAR(4)) + '-' + CAST(@NextRec AS NVARCHAR(20)))
            BEGIN
                SET @NextRec = @NextRec + 1;
            END

            SET @ReceiptNumber = @Prefix + '-' + CAST(YEAR(GETDATE()) AS NVARCHAR(4)) + '-' + CAST(@NextRec AS NVARCHAR(20));
            UPDATE Settings SET SettingValue = CAST(@NextRec + 1 AS NVARCHAR(20)) WHERE SettingKey = 'ReceiptNextNumber';
        END

        UPDATE Donations
        SET PaymentStatus = 'Successful',
            ReceiptNumber = @ReceiptNumber,
            PaymentGatewayReference = ISNULL(@PaymentGatewayReference, PaymentGatewayReference),
            PaidAt = GETDATE(),
            UpdatedAt = GETDATE()
        WHERE DonationId = @DonationId OR DonationCode = @DonationCode;

        -- Update Campaign Raised Amount if linked
        DECLARE @LinkedCampId INT;
        DECLARE @DonAmount DECIMAL(18,2);
        SELECT @LinkedCampId = CampaignId, @DonAmount = Amount, @DonationId = DonationId FROM Donations WHERE DonationId = @DonationId OR DonationCode = @DonationCode;

        IF @LinkedCampId IS NOT NULL
        BEGIN
            UPDATE Campaigns SET RaisedAmount = RaisedAmount + @DonAmount WHERE CampaignId = @LinkedCampId;
        END

        -- Insert Receipt record
        IF NOT EXISTS (SELECT 1 FROM Receipts WHERE DonationId = @DonationId)
        BEGIN
            INSERT INTO Receipts (DonationId, ReceiptNumber, IssuedAt, Status)
            VALUES (@DonationId, @ReceiptNumber, GETDATE(), 'Issued');
        END

        SELECT @DonationId AS DonationId, @ReceiptNumber AS ReceiptNumber, 1 AS RowsAffected;
    END
    ELSE IF @Action = '3' -- Record Offline Donation (Cash / Cheque / Bank Transfer)
    BEGIN
        IF @DonationCode IS NULL OR LTRIM(RTRIM(@DonationCode)) = ''
            SET @DonationCode = 'OFF' + CONVERT(NVARCHAR(8), GETDATE(), 112) + RIGHT(REPLACE(CAST(NEWID() AS NVARCHAR(36)),'-',''), 6);

        DECLARE @PrefixOff NVARCHAR(20) = 'SJT';
        DECLARE @NextRecOff INT = 1001;
        SELECT @PrefixOff = SettingValue FROM Settings WHERE SettingKey = 'ReceiptPrefix';
        SELECT @NextRecOff = CAST(SettingValue AS INT) FROM Settings WHERE SettingKey = 'ReceiptNextNumber';
        
        -- Ensure receipt number is unique across both Donations and Receipts tables
        WHILE EXISTS (SELECT 1 FROM Donations WHERE ReceiptNumber = @PrefixOff + '-' + CAST(YEAR(GETDATE()) AS NVARCHAR(4)) + '-' + CAST(@NextRecOff AS NVARCHAR(20)))
              OR EXISTS (SELECT 1 FROM Receipts WHERE ReceiptNumber = @PrefixOff + '-' + CAST(YEAR(GETDATE()) AS NVARCHAR(4)) + '-' + CAST(@NextRecOff AS NVARCHAR(20)))
        BEGIN
            SET @NextRecOff = @NextRecOff + 1;
        END

        SET @ReceiptNumber = @PrefixOff + '-' + CAST(YEAR(GETDATE()) AS NVARCHAR(4)) + '-' + CAST(@NextRecOff AS NVARCHAR(20));
        UPDATE Settings SET SettingValue = CAST(@NextRecOff + 1 AS NVARCHAR(20)) WHERE SettingKey = 'ReceiptNextNumber';

        IF @MemberId IS NULL AND @DonorPhone IS NOT NULL
        BEGIN
            SELECT @MemberId = MemberId FROM Members WHERE Mobile = @DonorPhone;
        END

        INSERT INTO Donations (DonationCode, ReceiptNumber, MemberId, CampaignId, DonorName, DonorEmail, DonorPhone, DonorAddress, DonorPan, Amount, PaymentMode, PaymentStatus, PaymentGatewayReference, Notes, CreatedBy, PaidAt, CreatedAt)
        VALUES (@DonationCode, @ReceiptNumber, @MemberId, @CampaignId, @DonorName, @DonorEmail, @DonorPhone, @DonorAddress, @DonorPan, @Amount, @PaymentMode, 'Successful', @PaymentGatewayReference, @Notes, @CreatedBy, GETDATE(), GETDATE());

        DECLARE @OffDonId INT = SCOPE_IDENTITY();

        IF @CampaignId IS NOT NULL
        BEGIN
            UPDATE Campaigns SET RaisedAmount = RaisedAmount + @Amount WHERE CampaignId = @CampaignId;
        END

        INSERT INTO Receipts (DonationId, ReceiptNumber, IssuedAt, Status)
        VALUES (@OffDonId, @ReceiptNumber, GETDATE(), 'Issued');

        SELECT @OffDonId AS DonationId, @ReceiptNumber AS ReceiptNumber, 1 AS RowsAffected;
    END
    ELSE IF @Action = '4' -- List Donations with filter
    BEGIN
        SELECT d.DonationId, d.DonationCode, d.ReceiptNumber, d.MemberId, m.MemberCode, d.CampaignId, c.Title AS CampaignTitle,
               d.DonorName, d.DonorEmail, d.DonorPhone, d.DonorAddress, d.DonorPan, d.Amount, d.Currency,
               d.PaymentMode, d.PaymentStatus, d.PaymentGatewayReference, d.TransactionDate, d.PaidAt, d.Notes, d.CreatedBy, d.CreatedAt
        FROM Donations d
        LEFT JOIN Members m ON d.MemberId = m.MemberId
        LEFT JOIN Campaigns c ON d.CampaignId = c.CampaignId
        WHERE (@StartDate IS NULL OR CAST(d.CreatedAt AS DATE) >= @StartDate)
          AND (@EndDate IS NULL OR CAST(d.CreatedAt AS DATE) <= @EndDate)
          AND (@PaymentStatus IS NULL OR @PaymentStatus = '' OR d.PaymentStatus = @PaymentStatus)
          AND (@CampaignId IS NULL OR d.CampaignId = @CampaignId)
          AND (@Search IS NULL OR @Search = ''
               OR d.DonorName LIKE '%' + @Search + '%'
               OR d.DonorPhone LIKE '%' + @Search + '%'
               OR d.DonationCode LIKE '%' + @Search + '%'
               OR d.ReceiptNumber LIKE '%' + @Search + '%')
        ORDER BY d.DonationId DESC;
    END
    ELSE IF @Action = '5' -- Get by DonationId or Code
    BEGIN
        SELECT d.DonationId, d.DonationCode, d.ReceiptNumber, d.MemberId, m.MemberCode, d.CampaignId, c.Title AS CampaignTitle,
               d.DonorName, d.DonorEmail, d.DonorPhone, d.DonorAddress, d.DonorPan, d.Amount, d.Currency,
               d.PaymentMode, d.PaymentStatus, d.PaymentGatewayReference, d.TransactionDate, d.PaidAt, d.Notes, d.CreatedBy, d.CreatedAt
        FROM Donations d
        LEFT JOIN Members m ON d.MemberId = m.MemberId
        LEFT JOIN Campaigns c ON d.CampaignId = c.CampaignId
        WHERE d.DonationId = @DonationId OR d.DonationCode = @DonationCode;
    END
    ELSE IF @Action = '6' -- Get by MemberId
    BEGIN
        SELECT d.DonationId, d.DonationCode, d.ReceiptNumber, d.MemberId, d.CampaignId, c.Title AS CampaignTitle,
               d.DonorName, d.DonorEmail, d.DonorPhone, d.Amount, d.Currency,
               d.PaymentMode, d.PaymentStatus, d.PaymentGatewayReference, d.PaidAt, d.CreatedAt
        FROM Donations d
        LEFT JOIN Campaigns c ON d.CampaignId = c.CampaignId
        WHERE d.MemberId = @MemberId
        ORDER BY d.DonationId DESC;
    END
    ELSE IF @Action = '7' -- Refund
    BEGIN
        UPDATE Donations
        SET PaymentStatus = 'Refunded',
            Notes = ISNULL(Notes, '') + ' [Refunded: ' + ISNULL(@Notes, '') + ']',
            UpdatedAt = GETDATE()
        WHERE DonationId = @DonationId;

        SELECT @@ROWCOUNT AS RowsAffected;
    END
END
GO

-- 5. ManageCampaigns Procedure
CREATE OR ALTER PROCEDURE ManageCampaigns
    @Action NVARCHAR(10),
    @CampaignId INT = NULL,
    @Title NVARCHAR(200) = NULL,
    @Category NVARCHAR(100) = NULL,
    @ShortDescription NVARCHAR(500) = NULL,
    @Description NVARCHAR(MAX) = NULL,
    @TargetAmount DECIMAL(18,2) = 0,
    @RaisedAmount DECIMAL(18,2) = 0,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @ImagePath NVARCHAR(255) = NULL,
    @IsFeatured BIT = 0,
    @Status INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = '1' -- Insert
    BEGIN
        INSERT INTO Campaigns (Title, Category, ShortDescription, Description, TargetAmount, RaisedAmount, StartDate, EndDate, ImagePath, IsFeatured, Status, CreatedAt)
        VALUES (@Title, @Category, @ShortDescription, @Description, @TargetAmount, @RaisedAmount, @StartDate, @EndDate, @ImagePath, @IsFeatured, @Status, GETDATE());
        SELECT SCOPE_IDENTITY() AS CampaignId, 1 AS RowsAffected;
    END
    ELSE IF @Action = '2' -- Update
    BEGIN
        UPDATE Campaigns
        SET Title = ISNULL(@Title, Title),
            Category = ISNULL(@Category, Category),
            ShortDescription = ISNULL(@ShortDescription, ShortDescription),
            Description = ISNULL(@Description, Description),
            TargetAmount = ISNULL(@TargetAmount, TargetAmount),
            RaisedAmount = ISNULL(@RaisedAmount, RaisedAmount),
            StartDate = ISNULL(@StartDate, StartDate),
            EndDate = ISNULL(@EndDate, EndDate),
            ImagePath = ISNULL(@ImagePath, ImagePath),
            IsFeatured = ISNULL(@IsFeatured, IsFeatured),
            Status = ISNULL(@Status, Status)
        WHERE CampaignId = @CampaignId;
        SELECT @CampaignId AS CampaignId, @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '3' -- Delete
    BEGIN
        DELETE FROM Campaigns WHERE CampaignId = @CampaignId;
        SELECT @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '4' -- List all
    BEGIN
        SELECT CampaignId, Title, Category, ShortDescription, Description, TargetAmount, RaisedAmount, StartDate, EndDate, ImagePath, IsFeatured, Status, CreatedAt
        FROM Campaigns
        ORDER BY CampaignId DESC;
    END
    ELSE IF @Action = '5' -- Get by Id
    BEGIN
        SELECT CampaignId, Title, Category, ShortDescription, Description, TargetAmount, RaisedAmount, StartDate, EndDate, ImagePath, IsFeatured, Status, CreatedAt
        FROM Campaigns
        WHERE CampaignId = @CampaignId;
    END
    ELSE IF @Action = '6' -- Frontend Active
    BEGIN
        SELECT CampaignId, Title, Category, ShortDescription, Description, TargetAmount, RaisedAmount, StartDate, EndDate, ImagePath, IsFeatured, Status
        FROM Campaigns
        WHERE Status = 1
        ORDER BY IsFeatured DESC, CampaignId DESC;
    END
END
GO

-- 6. ManageBlogs Procedure
CREATE OR ALTER PROCEDURE ManageBlogs
    @Action NVARCHAR(10),
    @BlogId INT = NULL,
    @Title NVARCHAR(255) = NULL,
    @Category NVARCHAR(100) = NULL,
    @Author NVARCHAR(100) = 'Admin',
    @ShortDescription NVARCHAR(500) = NULL,
    @Description NVARCHAR(MAX) = NULL,
    @ImagePath NVARCHAR(255) = NULL,
    @Status INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = '1' -- Insert
    BEGIN
        INSERT INTO Blogs (Title, Category, Author, ShortDescription, Description, ImagePath, Status, CreatedAt)
        VALUES (@Title, @Category, @Author, @ShortDescription, @Description, @ImagePath, @Status, GETDATE());
        SELECT SCOPE_IDENTITY() AS BlogId, 1 AS RowsAffected;
    END
    ELSE IF @Action = '2' -- Update
    BEGIN
        UPDATE Blogs
        SET Title = ISNULL(@Title, Title),
            Category = ISNULL(@Category, Category),
            Author = ISNULL(@Author, Author),
            ShortDescription = ISNULL(@ShortDescription, ShortDescription),
            Description = ISNULL(@Description, Description),
            ImagePath = ISNULL(@ImagePath, ImagePath),
            Status = ISNULL(@Status, Status)
        WHERE BlogId = @BlogId;
        SELECT @BlogId AS BlogId, @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '3' -- Delete
    BEGIN
        DELETE FROM Blogs WHERE BlogId = @BlogId;
        SELECT @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '4' -- List all
    BEGIN
        SELECT BlogId, Title, Category, Author, ShortDescription, Description, ImagePath, Status, CreatedAt
        FROM Blogs
        ORDER BY BlogId DESC;
    END
    ELSE IF @Action = '5' -- Get by Id
    BEGIN
        SELECT BlogId, Title, Category, Author, ShortDescription, Description, ImagePath, Status, CreatedAt
        FROM Blogs
        WHERE BlogId = @BlogId;
    END
END
GO

-- 7. ManageOurTeam Procedure
CREATE OR ALTER PROCEDURE ManageOurTeam
    @Action NVARCHAR(10),
    @TeamId INT = NULL,
    @Name NVARCHAR(100) = NULL,
    @Designation NVARCHAR(100) = NULL,
    @Description NVARCHAR(500) = NULL,
    @ImagePath NVARCHAR(255) = NULL,
    @Status INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = '1'
    BEGIN
        INSERT INTO OurTeamDetails (Name, Designation, Description, ImagePath, Status, CreatedAt)
        VALUES (@Name, @Designation, @Description, @ImagePath, @Status, GETDATE());
        SELECT SCOPE_IDENTITY() AS TeamId, 1 AS RowsAffected;
    END
    ELSE IF @Action = '2'
    BEGIN
        UPDATE OurTeamDetails
        SET Name = ISNULL(@Name, Name),
            Designation = ISNULL(@Designation, Designation),
            Description = ISNULL(@Description, Description),
            ImagePath = ISNULL(@ImagePath, ImagePath),
            Status = ISNULL(@Status, Status)
        WHERE TeamId = @TeamId;
        SELECT @TeamId AS TeamId, @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '3'
    BEGIN
        DELETE FROM OurTeamDetails WHERE TeamId = @TeamId;
        SELECT @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '4'
    BEGIN
        SELECT TeamId, Name, Designation, Description, ImagePath, Status, CreatedAt FROM OurTeamDetails ORDER BY TeamId DESC;
    END
    ELSE IF @Action = '5'
    BEGIN
        SELECT TeamId, Name, Designation, Description, ImagePath, Status, CreatedAt FROM OurTeamDetails WHERE TeamId = @TeamId;
    END
END
GO

-- 8. ManageGallery Procedure
CREATE OR ALTER PROCEDURE ManageGallery
    @Action NVARCHAR(10),
    @GalleryId INT = NULL,
    @GalleryName NVARCHAR(150) = NULL,
    @GalleryDetailsId INT = NULL,
    @ImageTitle NVARCHAR(150) = NULL,
    @ImagePath NVARCHAR(255) = NULL,
    @Status INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = '1' -- Insert Gallery Master
    BEGIN
        INSERT INTO GalleryMaster (GalleryName, Status, CreatedAt) VALUES (@GalleryName, @Status, GETDATE());
        SELECT SCOPE_IDENTITY() AS GalleryId, 1 AS RowsAffected;
    END
    ELSE IF @Action = '2' -- Update Gallery Master
    BEGIN
        UPDATE GalleryMaster SET GalleryName = @GalleryName, Status = @Status WHERE GalleryId = @GalleryId;
        SELECT @GalleryId AS GalleryId, @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '3' -- Delete Gallery Master
    BEGIN
        DELETE FROM GalleryMaster WHERE GalleryId = @GalleryId;
        SELECT @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '4' -- List Gallery Masters
    BEGIN
        SELECT GalleryId, GalleryName, Status, CreatedAt FROM GalleryMaster ORDER BY GalleryId DESC;
    END
    ELSE IF @Action = '5' -- Insert Gallery Details (Photo)
    BEGIN
        INSERT INTO GalleryDetails (GalleryId, ImageTitle, ImagePath, Status, CreatedAt)
        VALUES (@GalleryId, @ImageTitle, @ImagePath, @Status, GETDATE());
        SELECT SCOPE_IDENTITY() AS GalleryDetailsId, 1 AS RowsAffected;
    END
    ELSE IF @Action = '6' -- Delete Gallery Details (Photo)
    BEGIN
        DELETE FROM GalleryDetails WHERE GalleryDetailsId = @GalleryDetailsId;
        SELECT @@ROWCOUNT AS RowsAffected;
    END
    ELSE IF @Action = '7' -- List Gallery Details
    BEGIN
        SELECT gd.GalleryDetailsId, gd.GalleryId, gm.GalleryName, gd.ImageTitle, gd.ImagePath, gd.Status, gd.CreatedAt
        FROM GalleryDetails gd
        JOIN GalleryMaster gm ON gd.GalleryId = gm.GalleryId
        WHERE (@GalleryId IS NULL OR gd.GalleryId = @GalleryId)
        ORDER BY gd.GalleryDetailsId DESC;
    END
END
GO

-- 9. GetDashboardStats Procedure
CREATE OR ALTER PROCEDURE GetDashboardStats
AS
BEGIN
    SET NOCOUNT ON;

    -- Metrics Result Set
    SELECT 
        (SELECT COUNT(1) FROM Members) AS TotalMembers,
        (SELECT COUNT(1) FROM Members WHERE Status = 1) AS ActiveMembers,
        (SELECT ISNULL(SUM(Amount), 0) FROM Donations WHERE PaymentStatus = 'Successful') AS TotalDonationAmount,
        (SELECT COUNT(1) FROM Donations WHERE PaymentStatus = 'Successful') AS TotalDonationCount,
        (SELECT ISNULL(SUM(Amount), 0) FROM Donations WHERE PaymentStatus = 'Successful' AND MONTH(PaidAt) = MONTH(GETDATE()) AND YEAR(PaidAt) = YEAR(GETDATE())) AS CurrentMonthAmount,
        (SELECT COUNT(1) FROM Donations WHERE PaymentStatus = 'Pending') AS PendingDonationsCount,
        (SELECT COUNT(1) FROM Campaigns WHERE Status = 1) AS ActiveCampaigns,
        (SELECT COUNT(1) FROM ContactInquiries WHERE Status = 'New') AS NewInquiries;

    -- Recent Donations
    SELECT TOP 10 d.DonationId, d.DonationCode, d.ReceiptNumber, d.DonorName, d.DonorPhone, d.Amount, d.PaymentMode, d.PaymentStatus, d.CreatedAt
    FROM Donations d
    ORDER BY d.DonationId DESC;

    -- Recent Members
    SELECT TOP 10 MemberId, MemberCode, FullName, Mobile, Email, MemberType, Status, JoinedAt
    FROM Members
    ORDER BY MemberId DESC;

    -- Campaign Wise Summary
    SELECT c.CampaignId, c.Title, c.TargetAmount, c.RaisedAmount, COUNT(d.DonationId) AS TotalDonations
    FROM Campaigns c
    LEFT JOIN Donations d ON c.CampaignId = d.CampaignId AND d.PaymentStatus = 'Successful'
    GROUP BY c.CampaignId, c.Title, c.TargetAmount, c.RaisedAmount
    ORDER BY c.RaisedAmount DESC;
END
GO
USE [mindforg_ramjankingo];
GO

-- 1. Insert Members & Linked User Accounts
IF NOT EXISTS (SELECT 1 FROM Members WHERE Mobile = '9876543210')
BEGIN
    -- Member 1: Ramesh Kumar
    INSERT INTO Users (UserName, Email, Mobile, Password, RoleId, Status, CreatedAt)
    VALUES ('Ramesh Kumar Sharma', 'ramesh.sharma@example.com', '9876543210', 'member123', 3, 1, DATEADD(DAY, -45, GETDATE()));
    DECLARE @U1 INT = SCOPE_IDENTITY();
    INSERT INTO Members (UserId, MemberCode, FullName, Email, Mobile, Address, City, State, PinCode, DateOfBirth, MemberType, Status, ProfilePhoto, JoinedAt)
    VALUES (@U1, 'MEM101', 'Ramesh Kumar Sharma', 'ramesh.sharma@example.com', '9876543210', '12 Civil Lines', 'Ayodhya', 'Uttar Pradesh', '224123', '1985-06-15', 'Patron', 1, '/assets/img/member-1.jpg', DATEADD(DAY, -45, GETDATE()));
END

IF NOT EXISTS (SELECT 1 FROM Members WHERE Mobile = '9812345678')
BEGIN
    -- Member 2: Sunita Devi
    INSERT INTO Users (UserName, Email, Mobile, Password, RoleId, Status, CreatedAt)
    VALUES ('Sunita Devi Gupta', 'sunita.gupta@example.com', '9812345678', 'member123', 3, 1, DATEADD(DAY, -30, GETDATE()));
    DECLARE @U2 INT = SCOPE_IDENTITY();
    INSERT INTO Members (UserId, MemberCode, FullName, Email, Mobile, Address, City, State, PinCode, DateOfBirth, MemberType, Status, ProfilePhoto, JoinedAt)
    VALUES (@U2, 'MEM102', 'Sunita Devi Gupta', 'sunita.gupta@example.com', '9812345678', '45 Ram Nagar', 'Lucknow', 'Uttar Pradesh', '226001', '1990-08-20', 'Life', 1, '/assets/img/member-2.jpg', DATEADD(DAY, -30, GETDATE()));
END

IF NOT EXISTS (SELECT 1 FROM Members WHERE Mobile = '9765432109')
BEGIN
    -- Member 3: Anil Verma
    INSERT INTO Users (UserName, Email, Mobile, Password, RoleId, Status, CreatedAt)
    VALUES ('Anil Verma', 'anil.verma@example.com', '9765432109', 'member123', 3, 1, DATEADD(DAY, -15, GETDATE()));
    DECLARE @U3 INT = SCOPE_IDENTITY();
    INSERT INTO Members (UserId, MemberCode, FullName, Email, Mobile, Address, City, State, PinCode, DateOfBirth, MemberType, Status, ProfilePhoto, JoinedAt)
    VALUES (@U3, 'MEM103', 'Anil Verma', 'anil.verma@example.com', '9765432109', 'Sector 14, Indira Nagar', 'Varanasi', 'Uttar Pradesh', '221005', '1982-11-10', 'Regular', 1, '/assets/img/member-3.jpg', DATEADD(DAY, -15, GETDATE()));
END

IF NOT EXISTS (SELECT 1 FROM Members WHERE Mobile = '9955443322')
BEGIN
    -- Member 4: Meera Tiwari
    INSERT INTO Users (UserName, Email, Mobile, Password, RoleId, Status, CreatedAt)
    VALUES ('Meera Tiwari', 'meera.tiwari@example.com', '9955443322', 'member123', 3, 1, DATEADD(DAY, -5, GETDATE()));
    DECLARE @U4 INT = SCOPE_IDENTITY();
    INSERT INTO Members (UserId, MemberCode, FullName, Email, Mobile, Address, City, State, PinCode, DateOfBirth, MemberType, Status, ProfilePhoto, JoinedAt)
    VALUES (@U4, 'MEM104', 'Meera Tiwari', 'meera.tiwari@example.com', '9955443322', 'Hanuman Garhi Road', 'Ayodhya', 'Uttar Pradesh', '224123', '1995-02-28', 'Volunteer', 1, '/assets/img/member-4.jpg', DATEADD(DAY, -5, GETDATE()));
END

-- 2. Update / Insert Campaigns with Real Images & Progress
DELETE FROM Campaigns;
INSERT INTO Campaigns (Title, Slug, Category, ShortDescription, Description, TargetAmount, RaisedAmount, StartDate, EndDate, ImagePath, IsFeatured, Status)
VALUES
('Daily Annakshetra & Warm Meals Seva', 'daily-food-relief', 'Foods', 'Providing fresh nutritious meals every single day to sadhus, pilgrims and families in distress in Ayodhya.', 'Shriram Janki Trust conducts daily Bhandara and Annakshetra serving thousands of pilgrims, needy families, and wandering ascetics. Your support ensures no one sleeps hungry.', 300000.00, 285000.00, '2026-01-01', '2026-12-31', '/assets/img/donation-1.jpg', 1, 1),
('Child Education & School Kits Support', 'child-education-support', 'Education', 'Supplying textbooks, uniforms, bags and basic tuition support for underprivileged village children.', 'Every child deserves an equal opportunity to learn and grow. We support primary schooling across 12 villages, setting up community learning centers.', 250000.00, 137500.00, '2026-01-01', '2026-12-31', '/assets/img/donation-2.jpg', 1, 1),
('Rural Free Health Camps & Medicine Seva', 'rural-free-health-camps', 'Healthcare', 'Conducting monthly free medical diagnosis, cataract checkups and life-saving medicines distribution.', 'Remote rural populations often lack basic medical facilities. Our dedicated team of visiting doctors provides free health checkups, tests and generic medicines.', 500000.00, 325000.00, '2026-01-01', '2026-12-31', '/assets/img/donation-3.jpg', 1, 1),
('Clean Drinking Water & RO Plants in Rural Schools', 'clean-drinking-water', 'Sanitation', 'Installing commercial solar-powered RO water filtration plants in primary government schools.', 'Ensuring clean and fluorosis-free drinking water for rural students and village elders to prevent water-borne infections.', 200000.00, 160000.00, '2026-01-01', '2026-12-31', '/assets/img/donation-4.jpg', 1, 1);

-- 3. Insert Projects
DELETE FROM Projects;
INSERT INTO Projects (Title, Slug, Category, Description, ImagePath, Status)
VALUES
('Digital Smart Classes in Community Centers', 'smart-classes', 'Education', 'Equipping rural study centers with tablets, computers, and interactive digital curriculum.', '/assets/img/project-1.jpg', 1),
('Mobile Medical Van Outreach', 'mobile-medical-van', 'Healthcare', 'Equipped diagnostic van visiting remote hamlet clusters twice a week.', '/assets/img/project-2.jpg', 1),
('Women Stitching & Vocational Center', 'women-empowerment', 'Livelihood', 'Empowering 100+ rural women with sewing and handicraft training for self-reliance.', '/assets/img/project-3.jpg', 1),
('Elderly Care & Winter Blanket Distribution', 'winter-relief', 'Relief', 'Distributing warm blankets and winter supplies to shelterless elders.', '/assets/img/project-4.jpg', 1);

-- 4. Insert Team Members
DELETE FROM OurTeamDetails;
INSERT INTO OurTeamDetails (Name, Designation, Description, ImagePath, Status)
VALUES
('Acharya Rajesh Sharma', 'Founder & Managing Trustee', 'Spiritual guide and dedicated social reformer leading welfare initiatives since 2010.', '/assets/img/member-1.jpg', 1),
('Dr. Sneha Verma', 'Chief Medical Officer', 'MD Physician guiding free health camps and rural wellness screening initiatives.', '/assets/img/member-2.jpg', 1),
('Vikramaditya Singh', 'Head of Field Operations', 'Coordinates ground logistics, distribution teams, and volunteer mobilization.', '/assets/img/member-3.jpg', 1),
('Pooja Agrawal', 'Education & Child Welfare Lead', 'Experienced educator designing village smart class curriculums and tutoring.', '/assets/img/member-4.jpg', 1);

-- 5. Insert Gallery Albums & Photos
DELETE FROM GalleryDetails;
DELETE FROM GalleryMaster;

INSERT INTO GalleryMaster (GalleryName, Status) VALUES ('Community Service & Bhandara', 1);
DECLARE @G1 INT = SCOPE_IDENTITY();
INSERT INTO GalleryMaster (GalleryName, Status) VALUES ('Healthcare & Medical Camps', 1);
DECLARE @G2 INT = SCOPE_IDENTITY();
INSERT INTO GalleryMaster (GalleryName, Status) VALUES ('Child Education Distribution', 1);
DECLARE @G3 INT = SCOPE_IDENTITY();

INSERT INTO GalleryDetails (GalleryId, ImageTitle, ImagePath, Status) VALUES
(@G1, 'Daily Annakshetra Seva in Ayodhya', '/assets/img/gallery-item-1.png', 1),
(@G1, 'Warm Meals Distribution to Sadhus & Pilgrims', '/assets/img/gallery-item-2.png', 1),
(@G2, 'Free Eye & Health Screening Camp', '/assets/img/gallery-item-3.png', 1),
(@G2, 'Free Medicine Distribution at Village Camp', '/assets/img/gallery-item-4.png', 1),
(@G3, 'School Bag and Book Kit Distribution', '/assets/img/gallery-item-5.png', 1),
(@G3, 'Smart Digital Classroom Workshop', '/assets/img/gallery-item-6.png', 1);

-- 6. Insert Blogs & News
DELETE FROM Blogs;
INSERT INTO Blogs (Title, Category, Author, ShortDescription, Description, ImagePath, Status, CreatedAt)
VALUES
('Shriram Janki Trust Organizes Free Eye Surgery & Health Camp', 'Healthcare', 'Admin', 'Over 450 rural villagers received free checkups and 65 successful cataract procedures were performed.', 'The camp was organized with leading ophthalmologists and general practitioners providing comprehensive consultations, prescription glasses, and free post-surgery medications.', '/assets/img/blog-1.jpg', 1, DATEADD(DAY, -10, GETDATE())),
('Annakshetra Completes 50,000 Free Meals Milestone in Ayodhya', 'Food Relief', 'Admin', 'With the blessings of donors and volunteers, our daily food seva reaches historic milestone.', 'Every morning and evening, the Annakshetra welcomes hundreds of seekers, pilgrims, and vulnerable locals for freshly cooked, hygienic, and nutritious meals.', '/assets/img/blog-2.jpg', 1, DATEADD(DAY, -20, GETDATE())),
('10 New Village Smart Learning Centers Launched', 'Education', 'Admin', 'Equipping underprivileged students with basic computer literacy and multimedia education.', 'Bridging the rural digital divide by providing solar-powered computer labs and certified volunteer tutors in primary schools.', '/assets/img/blog-3.jpg', 1, DATEADD(DAY, -30, GETDATE()));

-- 7. Insert Confirmed Donations & Receipts
DELETE FROM Receipts;
DELETE FROM Donations;

DECLARE @M1 INT, @M2 INT, @M3 INT;
SELECT @M1 = MemberId FROM Members WHERE MemberCode = 'MEM101';
SELECT @M2 = MemberId FROM Members WHERE MemberCode = 'MEM102';
SELECT @M3 = MemberId FROM Members WHERE MemberCode = 'MEM103';

DECLARE @C1 INT, @C2 INT, @C3 INT;
SELECT @C1 = CampaignId FROM Campaigns WHERE Category = 'Foods';
SELECT @C2 = CampaignId FROM Campaigns WHERE Category = 'Education';
SELECT @C3 = CampaignId FROM Campaigns WHERE Category = 'Healthcare';

-- Donation 1 (Ramesh Kumar - Successful)
INSERT INTO Donations (DonationCode, ReceiptNumber, MemberId, CampaignId, DonorName, DonorEmail, DonorPhone, DonorAddress, DonorPan, Amount, PaymentMode, PaymentStatus, PaymentGatewayReference, PaidAt, CreatedAt)
VALUES ('DON20261101', 'SJT-2026-1001', @M1, @C1, 'Ramesh Kumar Sharma', 'ramesh.sharma@example.com', '9876543210', '12 Civil Lines, Ayodhya', 'ABCPR1234F', 21000.00, 'UPI', 'Successful', 'UPI/20261101/998877', DATEADD(DAY, -14, GETDATE()), DATEADD(DAY, -14, GETDATE()));
DECLARE @D1 INT = SCOPE_IDENTITY();
INSERT INTO Receipts (DonationId, ReceiptNumber, IssuedAt, Status) VALUES (@D1, 'SJT-2026-1001', DATEADD(DAY, -14, GETDATE()), 'Issued');

-- Donation 2 (Sunita Devi - Successful)
INSERT INTO Donations (DonationCode, ReceiptNumber, MemberId, CampaignId, DonorName, DonorEmail, DonorPhone, DonorAddress, DonorPan, Amount, PaymentMode, PaymentStatus, PaymentGatewayReference, PaidAt, CreatedAt)
VALUES ('DON20261105', 'SJT-2026-1002', @M2, @C2, 'Sunita Devi Gupta', 'sunita.gupta@example.com', '9812345678', '45 Ram Nagar, Lucknow', 'BGFPG5678K', 11000.00, 'Online', 'Successful', 'TXN20261105887766', DATEADD(DAY, -9, GETDATE()), DATEADD(DAY, -9, GETDATE()));
DECLARE @D2 INT = SCOPE_IDENTITY();
INSERT INTO Receipts (DonationId, ReceiptNumber, IssuedAt, Status) VALUES (@D2, 'SJT-2026-1002', DATEADD(DAY, -9, GETDATE()), 'Issued');

-- Donation 3 (Anil Verma - Successful)
INSERT INTO Donations (DonationCode, ReceiptNumber, MemberId, CampaignId, DonorName, DonorEmail, DonorPhone, DonorAddress, DonorPan, Amount, PaymentMode, PaymentStatus, PaymentGatewayReference, PaidAt, CreatedAt)
VALUES ('DON20261108', 'SJT-2026-1003', @M3, @C3, 'Anil Verma', 'anil.verma@example.com', '9765432109', 'Sector 14, Indira Nagar, Varanasi', 'CDLPV9988M', 5000.00, 'Bank Transfer', 'Successful', 'NEFT/UBIN001122', DATEADD(DAY, -6, GETDATE()), DATEADD(DAY, -6, GETDATE()));
DECLARE @D3 INT = SCOPE_IDENTITY();
INSERT INTO Receipts (DonationId, ReceiptNumber, IssuedAt, Status) VALUES (@D3, 'SJT-2026-1003', DATEADD(DAY, -6, GETDATE()), 'Issued');

-- Donation 4 (Guest Donor - Offline Cash)
INSERT INTO Donations (DonationCode, ReceiptNumber, MemberId, CampaignId, DonorName, DonorEmail, DonorPhone, DonorAddress, DonorPan, Amount, PaymentMode, PaymentStatus, PaymentGatewayReference, Notes, PaidAt, CreatedAt)
VALUES ('OFF20261112', 'SJT-2026-1004', NULL, @C1, 'Rajeshwari Prasad Singhal', 'rajeshwari.singhal@gmail.com', '9899887766', 'Kanpur, Uttar Pradesh', 'AAAPS7766R', 51000.00, 'Cash', 'Successful', 'CASH/OFF/1004', 'Contributed for Annakshetra Mahaprasad seva', DATEADD(DAY, -2, GETDATE()), DATEADD(DAY, -2, GETDATE()));
DECLARE @D4 INT = SCOPE_IDENTITY();
INSERT INTO Receipts (DonationId, ReceiptNumber, IssuedAt, Status) VALUES (@D4, 'SJT-2026-1004', DATEADD(DAY, -2, GETDATE()), 'Issued');

-- Update Settings next number
UPDATE Settings SET SettingValue = '1005' WHERE SettingKey = 'ReceiptNextNumber';
UPDATE Settings SET SettingValue = '105' WHERE SettingKey = 'MemberNextNumber';

-- 8. Insert Contact Inquiries
DELETE FROM ContactInquiries;
INSERT INTO ContactInquiries (Name, Email, Phone, Subject, Message, Status, CreatedAt)
VALUES
('Vijay Narayan Trivedi', 'vijay.trivedi@outlook.com', '9871122334', 'Organizing Eye Camp in Village', 'Pranam, our village panchayat in Gonda district wishes to host a free eye cataract camp in collaboration with Shriram Janki Trust.', 'New', DATEADD(DAY, -3, GETDATE())),
('Dr. Alok Srivastava', 'dralok.s@gmail.com', '9810998877', 'Medical Supplies Donation', 'We have a consignment of pediatric vitamins, syrups and antiseptic supplies to donate to your free medical van initiative.', 'New', DATEADD(DAY, -1, GETDATE()));

-- 9. Insert Volunteer Registrations
DELETE FROM VolunteerRegistrations;
INSERT INTO VolunteerRegistrations (Name, Email, Phone, Address, Occupation, AreaOfInterest, Message, Status, CreatedAt)
VALUES
('Gaurav Mishra', 'gaurav.mishra@gmail.com', '9988776655', 'Naka, Ayodhya', 'Software Engineer', 'Digital / Social Media', 'Available on weekends to manage social media outreach, website content and student digital classes.', 'Pending', DATEADD(DAY, -4, GETDATE())),
('Shalini Pandey', 'shalini.pandey@yahoo.com', '9877665544', 'Faizabad Road, Ayodhya', 'High School Teacher', 'Education Support', 'I would love to teach Hindi and English to primary school students at the village community center on Sundays.', 'Pending', DATEADD(DAY, -2, GETDATE()));

PRINT 'All rich dynamic records inserted successfully!';
GO

USE [mindforg_ramjankingo];
GO

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@shriramjankitrust.com')
BEGIN
    INSERT INTO Users (UserName, Email, Mobile, Password, RoleId, Status, CreatedAt)
    VALUES ('Super Admin', 'admin@shriramjankitrust.com', '9999999999', 'Admin@123', 1, 1, GETDATE());
END
ELSE
BEGIN
    UPDATE Users SET Password = 'Admin@123', Mobile = '9999999999', Status = 1 WHERE Email = 'admin@shriramjankitrust.com';
END
GO
