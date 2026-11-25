using System.Globalization;

namespace Encurtador.Api.Links;

/// <summary>
/// Restrição de rota que só aceita códigos no mesmo formato exigido na criação,
/// para a rota /{codigo} não capturar caminhos como /favicon.ico.
/// </summary>
public class CodigoCurtoConstraint : IRouteConstraint
{
    public const string Nome = "codigoCurto";

    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var valor)
        && ValidadorDeCodigo.EhValido(Convert.ToString(valor, CultureInfo.InvariantCulture));
}
