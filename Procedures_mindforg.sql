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
