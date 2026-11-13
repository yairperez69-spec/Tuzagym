
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Tuzagym.Models;
using Tuzagym.Services;
using Tuzagym.Views;


namespace Tuzagym.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;

        private string _correo = string.Empty;
        public string Correo
        {
            get => _correo;
            set
            {
                if (_correo == value) return;
                _correo = value;
                OnPropertyChanged();
            }
        }

        private string _contrasena = string.Empty;
        public string Contrasena
        {
            get => _contrasena;
            set
            {
                if (_contrasena == value) return;
                _contrasena = value;
                OnPropertyChanged();
            }
        }

        private string _rolSeleccionado = string.Empty;
        public string RolSeleccionado
        {
            get => _rolSeleccionado;
            set
            {
                if (_rolSeleccionado == value) return;
                _rolSeleccionado = value;
                OnPropertyChanged();
            }
        }

        private bool _cargando;
        public bool Cargando
        {
            get => _cargando;
            set
            {
                if (_cargando == value) return;

                _cargando = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NoCargando));
            }
        }

        public bool NoCargando => !Cargando;

        public ICommand LoginCommand { get; }


        public LoginViewModel(ApiService apiService)
        {
            _apiService = apiService
                ?? throw new ArgumentNullException(nameof(apiService));

            LoginCommand = new Command(
                async () => await EjecutarLoginAsync());
        }


        private async Task EjecutarLoginAsync()
        {
            if (Cargando)
                return;

            if (string.IsNullOrWhiteSpace(Correo) ||
                string.IsNullOrWhiteSpace(Contrasena) ||
                string.IsNullOrWhiteSpace(RolSeleccionado))
            {
                await MostrarAlertaAsync(
                    "Atención",
                    "Ingresa tu correo, contraseña y selecciona tu rol.");

                return;
            }

            Cargando = true;

            try
            {
                // 1. CONSULTAR LA API
                LoginResult resultado =
                    await _apiService.LoginAsync(
                        Correo.Trim(),
                        Contrasena);

                if (!resultado.Exito)
                {
                    await MostrarAlertaAsync(
                        "Error de inicio de sesión",
                        resultado.Mensaje ??
                        "No se pudo iniciar sesión.");

                    return;
                }

                // 2. VALIDAR USUARIO
                if (resultado.Usuario == null ||
                    resultado.IdUsuario <= 0)
                {
                    await MostrarAlertaAsync(
                        "Error del servidor",
                        "La API no devolvió un usuario válido.");

                    return;
                }

                // 3. VALIDAR ROL
                string rolRealBD =
                    resultado.Rol?.Trim() ?? string.Empty;

                string rolSeleccionado =
                    RolSeleccionado.Trim();

                if (string.IsNullOrWhiteSpace(rolRealBD))
                {
                    await MostrarAlertaAsync(
                        "Error del servidor",
                        "La API no devolvió el rol del usuario.");

                    return;
                }

                if (!rolRealBD.Equals(
                    rolSeleccionado,
                    StringComparison.OrdinalIgnoreCase))
                {
                    await MostrarAlertaAsync(
                        "Acceso denegado",
                        $"Tu cuenta tiene el rol '{rolRealBD}'. " +
                        $"No puedes ingresar como '{rolSeleccionado}'.");

                    return;
                }

                // 4. VALIDAR ESTADO DE LA CUENTA
                if (!string.Equals(
                    resultado.Usuario.Estado,
                    "Activo",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await MostrarAlertaAsync(
                        "Cuenta inactiva",
                        "Tu cuenta no está activa.");

                    return;
                }

                // 5. VALIDAR JWT DEL ADMINISTRADOR
                if (rolRealBD.Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(resultado.Token))
                    {
                        await MostrarAlertaAsync(
                            "Token JWT no recibido",
                            "La API validó tus credenciales, " +
                            "pero no devolvió un token JWT. " +
                            "Debemos revisar AuthController.cs " +
                            "en Tuzagym.API.");

                        return;
                    }
                }

                // 6. CONFIGURAR TOKEN SI EXISTE
                if (!string.IsNullOrWhiteSpace(resultado.Token))
                {
                    _apiService.ConfigurarToken(resultado.Token);
                }

                // 7. NAVEGAR SEGÚN EL ROL

                // 7. NAVEGAR SEGÚN EL ROL
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (Application.Current == null)
                    {
                        throw new InvalidOperationException(
                            "No se encontró la aplicación activa.");
                    }

                    Page paginaDestino;

                    if (rolRealBD.Equals(
                        "Socio",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        paginaDestino =
                            new SocioHomePage(resultado);
                    }
                    else if (rolRealBD.Equals(
                        "Entrenador",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        paginaDestino =
                            new EntrenadorHomePage(resultado);
                    }
                    else if (rolRealBD.Equals(
                            "Recepcionista",
                          StringComparison.OrdinalIgnoreCase))
                    {
                        paginaDestino = new RecepcionHomePage();
                    }
                    else if (rolRealBD.Equals(
                        "Administrador",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        paginaDestino =
                            new AdminHomePage(resultado);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Rol no reconocido: {rolRealBD}");
                    }

                    // ABRIR LA PANTALLA CORRESPONDIENTE
                    Application.Current.MainPage =
                        new NavigationPage(paginaDestino);
                });

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ERROR LOGIN TUZAGYM: {ex}");

                await MostrarAlertaAsync(
                    "Error de inicio de sesión",
                    "No se pudo completar el inicio de sesión: " +
                    ex.Message);
            }
            finally
            {
                Cargando = false;
            }
        }

        private Task MostrarAlertaAsync(
            string titulo,
            string mensaje)
        {
            return MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage
                            .DisplayAlert(
                                titulo,
                                mensaje,
                                "Aceptar");
                    }
                });
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
