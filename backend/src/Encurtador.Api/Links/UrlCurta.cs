namespace Encurtador.Api.Links;

public static class UrlCurta
{
    /// <summary>Endereço público do link curto, montado a partir do endereço em que a API foi chamada.</summary>
    public static string Montar(HttpRequest http, string codigo) =>
        $"{http.Scheme}://{http.Host}{http.PathBase}/{codigo}";
}
