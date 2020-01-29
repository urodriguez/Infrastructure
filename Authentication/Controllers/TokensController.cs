﻿using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Authentication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokensController : ControllerBase
    {
        [HttpPost]
        public IActionResult Create([FromBody] TokenCreateDto tokenCreateDto)
        {
            if (!CredentialsAreValid(tokenCreateDto.Account)) return Unauthorized();

            // create a claimsIdentity
            var claimsIdentity = new ClaimsIdentity(tokenCreateDto.Claims.Select(c => new Claim(c.Type, c.Value)));

            // create token to the user
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

            // describe security token
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = UciRodToken.Issuer,
                Subject = claimsIdentity,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToInt32(tokenCreateDto.Expire ?? UciRodToken.Expire)),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(System.Text.Encoding.Default.GetBytes(tokenCreateDto.Account.Secret)), SecurityAlgorithms.HmacSha256Signature)
            };

            // create JWT security token based on descriptor
            var jwtSecurityToken = tokenHandler.CreateJwtSecurityToken(securityTokenDescriptor);

            return Ok(new
            {
                UciRodToken.Issuer,
                securityTokenDescriptor.Expires,
                Token = tokenHandler.WriteToken(jwtSecurityToken) //security token as string
            });
        }

        [Route("validate")]
        [HttpPost]
        public IActionResult Validate([FromBody] TokenValidateDto tokenValidateDto)
        {
            if (!CredentialsAreValid(tokenValidateDto.Account)) return Unauthorized();

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = UciRodToken.Issuer,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                LifetimeValidator = LifetimeValidator,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.Default.GetBytes(tokenValidateDto.Account.Secret)),
                ValidateAudience = false
            };

            var identity = tokenHandler.ValidateToken(tokenValidateDto.Token, validationParameters, out var validatedToken);

            var internalClaimTypes = new[] {"nbf", "exp", "iat", "iss"};

            var claims = identity.Claims.Where(c => !internalClaimTypes.Contains(c.Type));

            return Ok(new
            {
                Claims = claims.Select(c => new
                {
                    c.Type,
                    c.Value
                })
            });
        }

        private static bool CredentialsAreValid(Account account)
        {
            return account.Id == "InventApp" && account.Secret.Equals("1nfr4structur3_1nv3nt4pp");
        }

        private bool LifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            if (expires == null) return false;

            return DateTime.UtcNow < expires;
        }
    }
}
