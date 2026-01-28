using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.AuthDTO;
using TripTogether.Application.DTOs.UserDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;

        public AuthService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<AuthService> loggerService, IClaimsService claimsService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _loggerService = loggerService;
            _claimsService = claimsService;

        }

        /// <summary>
        ///     Register a new user.
        /// </summary>
        /// <param name="registrationDto"></param>
        /// <returns></returns>
        public async Task<UserDto?> RegisterUserAsync(UserRegistrationDto registrationDto)
        {
            _loggerService.LogInformation($"Start registration for {registrationDto.Email}");

            if (await UserExistsAsync(registrationDto.Email))
            {
                _loggerService.LogWarning($"Email {registrationDto.Email} already registered.");
                throw ErrorHelper.Conflict("Email have been used.");
            }

            var hashedPassword = new PasswordHasher().HashPassword(registrationDto.Password);

            var user = new User
            {
                Email = registrationDto.Email,
                Username = registrationDto.Username,
                PasswordHash = hashedPassword,
                Gender = registrationDto.Gender ?? false,
                IsEmailVerified = false
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation($"User {user.Email} created successfully.");

            await GenerateAndSendOtpAsync(user, OtpPurpose.Register);

            _loggerService.LogInformation($"OTP sent to {user.Email} for verification.");

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Gender = user.Gender,
                PaymentQrCodeUrl = user.PaymentQrCodeUrl,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt
            };
        }

        /// <summary>
        ///     Login a user and return JWT access and refresh token.
        /// </summary>
        /// <param name="loginDto"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration)
        {
            _loggerService.LogInformation($"Login attempt for {loginDto.Email}");

            // Get user from DB
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email && !u.IsDeleted);

            if (user == null)
                throw ErrorHelper.NotFound("Account does not exist.");

            if (!new PasswordHasher().VerifyPassword(loginDto.Password!, user.PasswordHash))
                throw ErrorHelper.Unauthorized("Password is incorrect.");

            if (user.IsDeleted)
                throw ErrorHelper.Forbidden("Your account has been disabled. Please contact support for more information.");

            if (!user.IsEmailVerified)
                throw ErrorHelper.Forbidden("Account have not verified yet.");

            _loggerService.LogInformation($"User {loginDto.Email} authenticated successfully.");

            // Generate JWT token and refresh token
            var accessToken = JwtUtils.GenerateJwtToken(
                user.Id,
                user.Email,
                "User",
                configuration,
                TimeSpan.FromMinutes(30)
            );

            var refreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation($"Tokens generated and user cache updated for {user.Email}");

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            };
        }

        /// <summary>
        ///     Logout a user by removing their refresh token from the database.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LogoutAsync()
        {
            _loggerService.LogInformation("Logout process started.");

            var userId = _claimsService.GetCurrentUserId;
            _loggerService.LogInformation($"Logout process initiated for user ID: {userId}");

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
                throw ErrorHelper.NotFound("Account does not exist.");

            if (user.IsDeleted)
                throw ErrorHelper.Forbidden("Account has been disabled or banned.");

            // Đã logout rồi thì không cần xóa token nữa
            if (string.IsNullOrEmpty(user.RefreshToken))
                throw ErrorHelper.BadRequest("User previously logged out.");

            // Xóa token trong DB
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.LogInformation($"Logout successful for user ID: {userId}.");
            return true;
        }

        /// <summary>
        ///     Refresh the access token using the refresh token. 🐧
        /// </summary>
        /// <param name="refreshTokenDto"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public async Task<LoginResponseDto?> RefreshTokenAsync(TokenRefreshRequestDto refreshTokenDto, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
                throw ErrorHelper.BadRequest("Missing tokens");

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenDto.RefreshToken && !u.IsDeleted);

            if (user == null)
                throw ErrorHelper.NotFound("Account does not exist.");

            if (string.IsNullOrEmpty(user.RefreshToken))
                throw ErrorHelper.BadRequest("User previously logged out.");

            // Kiểm tra Refresh Token có còn hiệu lực hay không
            if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
                throw ErrorHelper.Conflict("Refresh token has expired.");

            // Tạo mới access và refresh token
            var newAccessToken = JwtUtils.GenerateJwtToken(
                user.Id,
                user.Email,
                "User",
                configuration,
                TimeSpan.FromHours(1)
            );

            var newRefreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        /// <summary>
        ///     Verify account
        /// </summary>
        /// <param name="email"></param>
        /// <param name="otp"></param>
        /// <returns></returns>
        public async Task<bool> VerifyEmailOtpAsync(string email, string otp)
        {
            _loggerService.LogInformation($"Verifying OTP for {email}");

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) throw ErrorHelper.NotFound("Account does not exist.");

            if (user.IsEmailVerified) return false;
            if (!await VerifyOtpAsync(email, otp, OtpPurpose.Register))
                return false;

            // Activate user account
            user.IsEmailVerified = true;
            _loggerService.LogInformation($"OTP verified for {email}, activating account.");

            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendRegistrationSuccessEmailAsync(new EmailRequestDto
            {
                To = user.Email,
                UserName = user.Username
            });

            _loggerService.LogInformation($"User {email} verified and activated.");
            return true;
        }

        /// <summary>
        ///     Check resend lại OTP là gì và gọi đúng hàm resend OTP
        /// </summary>
        /// <param name="email"></param>
        /// <param name="otpPurpose"></param>
        /// <returns></returns>
        public async Task<bool> ResendOtpAsync(string email, OtpPurpose otpPurpose)
        {
            return otpPurpose switch
            {
                OtpPurpose.Register => await SendRegisterOtpAsync(email),
                OtpPurpose.ForgotPassword => await SendForgotPasswordOtpAsync(email),
                _ => throw ErrorHelper.BadRequest("Invalid OTP type.")
            };
        }

        /// <summary>
        ///     Reset mật khẩu cho user.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="otp"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public async Task<bool> ResetPasswordAsync(string email, string otp, string newPassword)
        {
            _loggerService.LogInformation($"Password reset requested for {email}");

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
            if (user == null) return false;
            if (!user.IsEmailVerified) return false;
            if (!await VerifyOtpAsync(email, otp, OtpPurpose.ForgotPassword)) return false;

            // Hash và cập nhật mật khẩu
            var hashedPassword = new PasswordHasher().HashPassword(newPassword);
            if (hashedPassword == null)
            {
                _loggerService.LogWarning($"Failed to hash password for {email}");
                return false;
            }

            user.PasswordHash = hashedPassword;
            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendPasswordChangeSuccessAsync(new EmailRequestDto
            {
                To = user.Email,
                UserName = user.Username
            });

            _loggerService.LogInformation($"Password reset successful for {email}.");
            return true;
        }

        //========================= PRIVATE HELPER METHODS ============================

        private async Task<bool> UserExistsAsync(string email)
        {
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            return existingUser != null;
        }

        private async Task GenerateAndSendOtpAsync(User user, OtpPurpose purpose)
        {
            var otpToken = OtpGenerator.GenerateToken(6, TimeSpan.FromMinutes(10));
            var otp = new OtpStorage
            {
                Target = user.Email,
                OtpCode = otpToken.Code,
                ExpiredAt = otpToken.ExpiresAtUtc,
                IsUsed = false,
                Purpose = purpose
            };

            await _unitOfWork.OtpStorages.AddAsync(otp);
            await _unitOfWork.SaveChangesAsync();

            // Send the correct email based on OTP purpose
            if (purpose == OtpPurpose.Register)
            {
                await _emailService.SendOtpVerificationEmailAsync(new EmailRequestDto
                {
                    To = user.Email,
                    Otp = otpToken.Code,
                    UserName = user.Username
                });
                _loggerService.LogInformation($"Registration OTP sent to {user.Email}");
            }
            else if (purpose == OtpPurpose.ForgotPassword)
            {
                await _emailService.SendForgotPasswordOtpEmailAsync(new EmailRequestDto
                {
                    To = user.Email,
                    Otp = otpToken.Code,
                    UserName = user.Username
                });
                _loggerService.LogInformation($"Forgot password OTP sent to {user.Email}");
            }
        }

        private async Task<bool> SendRegisterOtpAsync(string email)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw ErrorHelper.NotFound("Email does not exist in the system.");

            if (user.IsDeleted)
                throw ErrorHelper.Forbidden("Account has been disabled or banned.");

            if (user.IsEmailVerified)
                throw ErrorHelper.Conflict("Verified account, no need to resend OTP.");

            await GenerateAndSendOtpAsync(user, OtpPurpose.Register);

            return true;
        }

        private async Task<bool> SendForgotPasswordOtpAsync(string email)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw ErrorHelper.NotFound("Email does not exist in the system.");

            if (user.IsDeleted)
                throw ErrorHelper.Forbidden("Account has been disabled or banned.");

            await GenerateAndSendOtpAsync(user, OtpPurpose.ForgotPassword);

            return true;
        }

        private async Task<bool> VerifyOtpAsync(string email, string otp, OtpPurpose purpose)
        {
            // Check trong db có tồn tại OTP chưa
            var otpRecord = await _unitOfWork.OtpStorages.FirstOrDefaultAsync(o =>
                o.Target == email && o.OtpCode == otp && o.Purpose == purpose && !o.IsUsed);

            // Nếu ko có OTP hoặc expired thì trả log
            if (otpRecord == null || otpRecord.ExpiredAt < DateTime.UtcNow)
            {
                _loggerService.LogWarning($"[VerifyOtpAsync] OTP not found or expired for {email} (purpose: {purpose})");
                return false;
            }

            otpRecord.IsUsed = true;
            await _unitOfWork.OtpStorages.Update(otpRecord);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.LogInformation($"[VerifyOtpAsync] OTP for {email} (purpose: {purpose}) verified and marked as used in DB.");
            return true;
        }
    }
}
