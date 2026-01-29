using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ETicaret.TagHelpers
{
    [HtmlTargetElement("td", Attributes = "user-role")]
    public class UserRoleTagHelper : TagHelper
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        
        [HtmlAttributeName("user-name")]
        public string? UserName { get; set; }
        
        public UserRoleTagHelper(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var user = await _userManager.FindByNameAsync(UserName ?? "");
            
            // Root container for roles
            TagBuilder container = new TagBuilder("div");
            container.AddCssClass("adminuser-roles-wrapper");
            
            // Create role list with modern styling
            TagBuilder roleList = new TagBuilder("div");
            roleList.AddCssClass("adminuser-role-list");
            
            var roles = _roleManager.Roles.ToList().Select(r => r.Name);
            bool hasRoles = false;
            
            foreach (var role in roles)
            {
                if (user == null || role == null)
                {
                 continue;
                }
                var result = await _userManager.IsInRoleAsync(user, role);
                
                TagBuilder roleItem = new TagBuilder("div");
                roleItem.AddCssClass($"adminuser-role-item {(result ? "active" : "inactive")}");
                
                roleItem.InnerHtml.Append($"{role}");
                
                TagBuilder icon = new TagBuilder("i");
                icon.AddCssClass(result ? "fa-solid fa-circle-check" : "fa-solid fa-circle-xmark");
                
                roleItem.InnerHtml.AppendHtml(icon);
                roleList.InnerHtml.AppendHtml(roleItem);
                
                if (result) hasRoles = true;
            }
            
            // Add a message if no roles are assigned
            if (!hasRoles) {
                TagBuilder noRoles = new TagBuilder("div");
                noRoles.AddCssClass("adminuser-role-item inactive");
                noRoles.InnerHtml.Append("Rol atanmamış");
                roleList.InnerHtml.AppendHtml(noRoles);
            }
            
            // Add a manage button
            TagBuilder manageButton = new TagBuilder("button");
            manageButton.AddCssClass("adminuser-role-manage");
            
            TagBuilder manageIcon = new TagBuilder("i");
            manageIcon.AddCssClass("fa-solid fa-gear");
            
            manageButton.InnerHtml.AppendHtml(manageIcon);
            manageButton.InnerHtml.Append("Rolleri Yönet");
            
            container.InnerHtml.AppendHtml(roleList);
            container.InnerHtml.AppendHtml(manageButton);
            
            output.Content.AppendHtml(container);
        }
    }
}