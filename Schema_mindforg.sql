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
