using Microsoft.AspNetCore.Components.Authorization;
using System.Runtime.CompilerServices;

namespace AppSugerenciasUI.Helpers;

public static class AuthenticationStateProviderHelper
{
    public static async Task<UsuarioModel> GetUsuarioFromAuth(
        this AuthenticationStateProvider provider,
        IUserData userData) 
    {
        var authState = await provider.GetAuthenticationStateAsync();
        string objectId = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("objectidentifier"))?.Value;
        return await userData.GetUsuarioFromAuthentication(objectId);
    }
}
