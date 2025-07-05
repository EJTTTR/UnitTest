using EcommerceApp.Data.Models;
using EcommerceApp.Data.Negocio;
using EcommerceApp.Web.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using System.Security.Claims;
using Xunit;

namespace EcommerceApp.Test
{
    public class AccesoControllerTest
    {
        private readonly Mock<ICNCliente> _mockCnCliente;
        private readonly AccesoController _controller;
        private readonly List<Cliente> _clientesDePrueba;

        public AccesoControllerTest()
        {
            // Crear el Mock del servicio de negocio
            _mockCnCliente = new Mock<ICNCliente>();
            _controller = new AccesoController(_mockCnCliente.Object);

            _clientesDePrueba = new List<Cliente>
            {
                new Cliente { IdCliente = 1, Nombre = "Mario", Apellido = "Martinez", Correo = "test1@example.com", Clave = "hashedpassword1", Restablecer = false },
                new Cliente { IdCliente = 2, Nombre = "Jose", Apellido = "Almonte", Correo = "test2@example.com", Clave = "hashedpassword2", Restablecer = true }
            };
        }

        [Fact]
        public async Task Index_PostConCredencialesCorrectas_DebeRedirigirATienda()
        {
            // Arrange
            var correo = "test1@example.com";
            var contra = "password123";
            var clienteValido = _clientesDePrueba.First(c => c.Correo == correo); 

            _mockCnCliente.Setup(s => s.BuscarClientePorCredenciales(correo, It.IsAny<string>()))
                          .Returns(clienteValido);

            // Simular el servicio de autenticacion
            var mockAuthService = new Mock<IAuthenticationService>();
            mockAuthService
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask); // Simulamos que el inicio de sesión se completa sin errores

            // Simular el proveedor de servicios
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthService.Object);

            // Simular el HttpContext y conectarlo con el proveedor de servicios
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.RequestServices).Returns(mockServiceProvider.Object);
            mockHttpContext.Setup(c => c.Session).Returns(new Mock<ISession>().Object);

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("steinsgateurl"); // Simulamos que la URL de redirección es "steinsgateurl" 

            // Asignar el HttpContext simulado al controlador
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = mockHttpContext.Object
            };

            _controller.Url = mockUrlHelper.Object;

            // Act
            var resultado = await _controller.Index(correo, contra);

            // Assert
            var redirResult = Assert.IsType<RedirectToActionResult>(resultado); // Verificamos que el resultado sea una redirección
            Assert.Equal("Index", redirResult.ActionName); // Verificamos que redirige a la acción Index
            Assert.Equal("Tienda", redirResult.ControllerName); // Verificamos que redirige al controlador Tienda
        }

        [Fact]
        public async Task Index_PostConCredencialesIncorrectas_DebeRetornarVistaConError()
        {
            // Arrange
            _mockCnCliente.Setup(s => s.BuscarClientePorCredenciales(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns((Cliente?)null);

            // Act
            var resultado = await _controller.Index("mal@usuario.com", "malacontra");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado); // Verificamos que el resultado sea una vista
            Assert.Equal("Correo o contraseña son incorrectas", viewResult.ViewData["Error"]);  // Verificamos que el mensaje de error sea el esperado
        }

        [Fact]
        public void Registrar_PostConClavesNoCoincidentes_DebeRetornarVistaConError()
        {
            // Arrange
            var cliente = new Cliente { Nombre = "Test", Apellido = "User", Correo = "test@test.com", Clave = "123", ConfirmarClave = "456" };

            // Act
            var resultado = _controller.Registrar(cliente);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado); // Verificamos que el resultado sea una vista
            Assert.Equal("Las contraseñas no coinciden. Por favor, asegúrate de que ambas contraseñas sean iguales.", viewResult.ViewData["Error"]); // Verificamos que el mensaje de error sea el esperado
            _mockCnCliente.Verify(s => s.Registrar(It.IsAny<Cliente>(), out It.Ref<string>.IsAny), Times.Never); // Verificamos que el método Registrar no se haya llamado
        }

        [Fact]
        public void Registrar_PostConRegistroExitoso_DebeLlamarServicioYRedirigir()
        {
            // Arrange
            var clienteValido = new Cliente { Nombre = "Ana", Apellido = "Gomez", Correo = "ana@test.com", Clave = "123", ConfirmarClave = "123" };
            string mensajeOut;
            _mockCnCliente.Setup(s => s.Registrar(clienteValido, out mensajeOut)).Returns(1);

            // Act
            var resultado = _controller.Registrar(clienteValido);

            // Assert
            var redirResult = Assert.IsType<RedirectToActionResult>(resultado); // Verificamos que el resultado sea una redirección
            Assert.Equal("Index", redirResult.ActionName);  // Verificamos que redirige a la acción Index
            _mockCnCliente.Verify(s => s.Registrar(clienteValido, out It.Ref<string>.IsAny), Times.Once); // Verificamos que el método Registrar se haya llamado una vez con el cliente válido
        }

        public void CambiarContra_ConContraseñaActualIncorrecta_DebeRetornarVistaConError()
        {
            // Arrange
            var idCliente = "1";
            var contraActual = "incorrecta";
            var nuevaContra = "nueva123";
            var clienteMock = _clientesDePrueba.First(c => c.IdCliente == 1);

            _mockCnCliente.Setup(s => s.BuscarClientePorId(1)).Returns(clienteMock);

            // Simular sesiones con mock y httpContext
            var mockHttpContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();

            // Cuando el codigo pida la session de httpContext doy el mock
            mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var resultado = _controller.CambiarContra(idCliente, contraActual, nuevaContra, nuevaContra);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            Assert.Equal("La contraseña actual no es correcta", viewResult.ViewData["Error"]);
        }
    }
}
