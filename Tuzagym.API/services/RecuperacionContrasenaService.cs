
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Tuzagym.API.Services
{
    public class RecuperacionContrasenaService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, object>
            _bloqueos = new();

        private const int MaxIntentos = 5;

        public RecuperacionContrasenaService(IMemoryCache cache)
        {
            _cache = cache;
        }

        private static string Clave(string correo) =>
            "recuperacion:" + correo.Trim().ToLowerInvariant();

        private static string Hash(string codigo)
        {
            byte[] bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(codigo));

            return Convert.ToHexString(bytes);
        }

        public bool PuedeSolicitar(string correo)
        {
            return !_cache.TryGetValue(
                "espera:" + Clave(correo), out _);
        }

        public string? GenerarCodigo(string correo)
        {
            string clave = Clave(correo);
            object bloqueo = _bloqueos.GetOrAdd(clave, _ => new());

            lock (bloqueo)
            {
                if (!PuedeSolicitar(correo))
                    return null;

                string codigo = RandomNumberGenerator
                    .GetInt32(0, 1000000)
                    .ToString("D6");

                _cache.Set(
                    clave,
                    new CodigoRecuperacion
                    {
                        HashCodigo = Hash(codigo),
                        Intentos = 0
                    },
                    TimeSpan.FromMinutes(10));

                _cache.Set(
                    "espera:" + clave,
                    true,
                    TimeSpan.FromMinutes(2));

                return codigo;
            }
        }

        public bool ValidarYConsumir(string correo, string codigo)
        {
            string clave = Clave(correo);
            object bloqueo = _bloqueos.GetOrAdd(clave, _ => new());

            lock (bloqueo)
            {
                if (!_cache.TryGetValue(
                    clave, out CodigoRecuperacion? datos)
                    || datos == null)
                {
                    return false;
                }

                datos.Intentos++;

                if (datos.Intentos > MaxIntentos)
                {
                    _cache.Remove(clave);
                    return false;
                }

                bool correcto =
                    CryptographicOperations.FixedTimeEquals(
                        Convert.FromHexString(datos.HashCodigo),
                        Convert.FromHexString(Hash(codigo)));

                if (correcto || datos.Intentos >= MaxIntentos)
                    _cache.Remove(clave);

                return correcto;
            }
        }

        private sealed class CodigoRecuperacion
        {
            public string HashCodigo { get; set; } = "";
            public int Intentos { get; set; }
        }
    }
}
