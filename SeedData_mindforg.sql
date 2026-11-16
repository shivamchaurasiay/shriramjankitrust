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
