using Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace gaseous_server.Classes
{
    /// <summary>
    /// Provides age enforcement for accessing resources based on the user's authentication and route parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AgeEnforcementAttribute : ActionFilterAttribute
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AgeEnforcementAttribute(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Enforces age restrictions before executing the action. If the user is not authenticated or does not have access to the resource, the request is short-circuited with an appropriate HTTP status code.
        /// </summary>
        /// <param name="context">
        /// The context for the action executing, containing information about the HTTP request, route data, and user.
        /// </param>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. Get the User from HttpContext (already populated by the Authorize attribute)
            var user = context.HttpContext.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult(); // 401 Unauthorized
                return;
            }

            var userObject = await _userManager.GetUserAsync(user);
            if (userObject == null)
            {
                context.Result = new UnauthorizedResult(); // 401 Unauthorized
                return;
            }

            // 2. Extract the route variable - if the route variable is missing, just skip the age enforcement check since the required data isn't present
            if (!context.RouteData.Values.TryGetValue("MetadataMapId", out var objectIdObj) || objectIdObj == null)
            {
                await next();
                return;
            }

            // parse the route variable to a long for further processing - fail gracefully if parsing fails
            bool objectIdParsed = long.TryParse(objectIdObj.ToString(), out var objectId);
            if (!objectIdParsed)
            {
                context.Result = new BadRequestResult(); // 400 Bad Request
                return;
            }

            // 3. Perform your business logic evaluation
            bool hasAccess = await CheckUserAccessToResourceAsync(userObject, objectId);

            if (!hasAccess)
            {
                // Note: Use ForbidResult (403) if they are logged in but just don't own this item.
                // Use UnauthorizedResult (401) if you want to fake that they aren't logged in.
                context.Result = new ForbidResult();
                return;
            }

            // If we don't set context.Result, processing naturally continues to the Controller
            await next();
        }

        /// <summary>
        /// Checks if the specified user has access to the resource identified by the objectId.
        /// </summary>
        /// <param name="user">The user whose access is being checked.</param>
        /// <param name="objectId">The identifier of the resource to check access for.</param>
        /// <returns>True if the user has access to the resource; otherwise, false.</returns>
        private static async Task<bool> CheckUserAccessToResourceAsync(ApplicationUser user, long objectId)
        {
            // quick short circuit: if the user has the highest age restriction, they automatically have access
            if (user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction == Metadata.AgeGroups.AgeRestrictionGroupings.Adult && user.SecurityProfile.AgeRestrictionPolicy.IncludeUnrated)
            {
                return true;
            }

            // get game age restriction based on objectId
            var mapData = await MetadataManagement.GetMetadataMap(objectId);
            if (mapData == null)
            {
                return false;
            }
            var ageGroup = await Metadata.AgeGroups.GetMapAgeGroupAsync(objectId);

            // check for IncludeUnrated
            if (user.SecurityProfile.AgeRestrictionPolicy.IncludeUnrated && ageGroup == null)
            {
                return true;
            }

            // check if user's age restriction allows access to this game's age group
            if (ageGroup != null && ageGroup <= user.SecurityProfile.AgeRestrictionPolicy.MaximumAgeRestriction)
            {
                return true;
            }

            return false;
        }
    }
}