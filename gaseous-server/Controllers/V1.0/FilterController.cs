﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Authentication;
using gaseous_server.Classes;
using IGDB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace gaseous_server.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    [ApiController]
    public class FilterController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public FilterController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [MapToApiVersion("1.0")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        //[ResponseCache(CacheProfileName = "5Minute")]
        public async Task<IActionResult> FilterAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            return Ok(await Filters.Filter(user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction, user.SecurityProfile.AgeRestrictionPolicy.IncludeUnrated));
        }

        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> FilterTypesAsync()
        {
            // return enum Filters.FilterType as a list of strings
            List<string> filterTypes = Enum.GetNames(typeof(Filters.FilterType)).ToList();

            return Ok(filterTypes);
        }

        [MapToApiVersion("1.1")]
        [HttpGet("{filterType}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> FilterAsync(Filters.FilterType filterType)
        {
            var user = await _userManager.GetUserAsync(User);

            return Ok(await Filters.GetFilter(filterType, user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction, user.SecurityProfile.AgeRestrictionPolicy.IncludeUnrated));
        }
    }
}