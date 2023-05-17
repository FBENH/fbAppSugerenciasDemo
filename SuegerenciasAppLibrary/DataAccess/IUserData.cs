namespace SuegerenciasAppLibrary.DataAccess
{
    public interface IUserData
    {
        Task CreateUsuario(UsuarioModel usuario);
        Task<UsuarioModel> GetUsuario(string id);
        Task<UsuarioModel> GetUsuarioFromAuthentication(string objectId);
        Task<List<UsuarioModel>> GetUsuariosAsync();
        Task UpdateUsuario(UsuarioModel usuario);
    }
}