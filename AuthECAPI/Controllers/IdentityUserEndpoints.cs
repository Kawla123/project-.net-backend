namespace AuthECAPI.Controllers
{
    public static class IdentityUserEndpoints
    {
        public static IEndpointRouteBuilder MapIdentityUserEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/signup", CreateUser);
            app.MapPost("/signin", SignIn);
            return app;
        }
      
        [Authorize(Roles = "Admin")]
        [Authorize(Roles = "Client")]
        [Authorize(Roles = "Supplier")]
        [ApiController]
        [Route("api/[controller]")]

        //Admin
        public class AdminController : ControllerBase
        {
            [HttpGet("data")]
            public IActionResult GetAdminData()
            {
                return Ok("Données visibles uniquement par les admins.");
            }
        }

        // Client
        public class ClientController : ControllerBase
        {
            [HttpGet("data")]
            public IActionResult GetClientData()
            {
                return Ok("Données visibles uniquement par les clients.");
            }
        }

        //Suplier
        public class SupplierController : ControllerBase
        {
            [HttpGet("data")]
            public IActionResult GetSupplierData()
            {
                return Ok("Données visibles uniquement par les fournisseurs.");
            }
        }
        // Méthode pour inscrire un utilisateur et lui assigner un rôle par défaut
        private static async Task<IResult> CreateUser(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            [FromBody] UserRegistrationModel userRegistrationModel)
        {
            AppUser user = new AppUser()
            {
                UserName = userRegistrationModel.Email,
                Email = userRegistrationModel.Email,
              
            };

            var result = await userManager.CreateAsync(user, userRegistrationModel.Password);

            if (result.Succeeded)
            {
                // Assigner le rôle "Client" par défaut
                var roleExist = await roleManager.RoleExistsAsync("Client");
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole("Client"));
                }
                await userManager.AddToRoleAsync(user, "Client");

                return Results.Ok(result);
            }
            else
            {
                return Results.BadRequest(result);
            }
        }

        // Méthode pour connecter un utilisateur et générer un token JWT
        private static async Task<IResult> SignIn(
            UserManager<AppUser> userManager,
            [FromBody] LoginModel loginModel,
            IOptions<AppSettings> appSettings)
        {
            var user = await userManager.FindByEmailAsync(loginModel.Email);
            if (user != null && await userManager.CheckPasswordAsync(user, loginModel.Password))
            {
                var signInKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(appSettings.Value.JWTSecret)
                );

                var claims = new List<Claim>
                {
                    new Claim("UserID", user.Id.ToString())
                };



                // Ajouter les rôles de l'utilisateur dans le token JWT
                var userRoles = await userManager.GetRolesAsync(user);
                foreach (var role in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(10),
                    SigningCredentials = new SigningCredentials(signInKey, SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                var token = tokenHandler.WriteToken(securityToken);

                return Results.Ok(new { token });
            }
            else
            {
                return Results.BadRequest(new { message = "Username or password is incorrect." });
            }
        }
    }
}
