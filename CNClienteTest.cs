using EcommerceApp.Data.Data;
using EcommerceApp.Data.Models;
using EcommerceApp.Test.MockServices;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcommerceApp.Test
{
    public class CNClienteTest
    {
        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private void datosTest(ApplicationDbContext context)
        {
            var clientes = new List<Cliente>
            {
                new Cliente
                {
                    IdCliente = 1,
                    Nombre = "Mario",
                    Apellido = "Martinez",
                    Correo = "test1@example.com",
                    Clave = "hashedpassword1",
                    Restablecer = false
                },
                new Cliente
                {
                    IdCliente = 2,
                    Nombre = "Jose",
                    Apellido = "Almonte",
                    Correo = "test2@example.com",
                    Clave = "hashedpassword2",
                    Restablecer = false
                }
            };

            context.Clientes.AddRange(clientes);
            context.SaveChanges();
        }

        [Fact]
        public void Registrar_ConDatosNuevos_DebeAgregarClienteYRetornarId()
        {
            // Arrange
            using var context = CreateContext();
            datosTest(context); 
            var cnCliente = new CNClienteMock(context);

            var nuevoCliente = new Cliente { Nombre = "Luisa", Apellido = "Lopez", Correo = "luisa@example.com", Clave = "password123" };
            string mensaje;

            // Act
            int nuevoId = cnCliente.Registrar(nuevoCliente, out mensaje);

            // Assert
            Assert.True(nuevoId > 0); // Debe retornar un ID válido
            Assert.Empty(mensaje);    // No debe haber mensaje de error
            Assert.Equal(3, context.Clientes.Count()); // Ahora debe haber 3 clientes en total
        }

        [Fact]
        public void Registrar_ConCorreoExistente_DebeFallarYRetornarMensaje()
        {
            // Arrange
            using var context = CreateContext();
            datosTest(context);
            var cnCliente = new CNClienteMock(context);

            var clienteDuplicado = new Cliente { Nombre = "jose", Apellido = "Lopez", Correo = "test1@example.com", Clave = "password123" };
            string mensaje;

            // Act
            int resultado = cnCliente.Registrar(clienteDuplicado, out mensaje);

            // Assert
            Assert.Equal($"El correo '{clienteDuplicado.Correo}' ya se encuentra registrado.", mensaje); // Debe retornar el mensaje de error específico
            Assert.Equal(0, resultado); // No debe retornar un ID
            Assert.Equal(2, context.Clientes.Count()); // El número de clientes no debe cambiar
        }

        [Fact]
        public void BuscarClientePorId_ConIdExistente_DebeRetornarClienteCorrecto()
        {
            // Arrange
            using var context = CreateContext();
            datosTest(context);
            var cnCliente = new CNClienteMock(context); 

            // Act
            var cliente = cnCliente.BuscarClientePorId(1); 

            // Assert
            Assert.NotNull(cliente); // Verifica que se haya encontrado un cliente
            Assert.Equal("Mario", cliente.Nombre); // Verifica que el cliente encontrado sea el correcto
        }

        [Fact]
        public void BuscarClientePorId_ConIdInexistente_DebeRetornarNull()
        {
            // Arrange
            using var context = CreateContext(); // Creamos un contexto solo para esta prueba
            datosTest(context); // Poblamos este contexto específico
            var cnCliente = new CNClienteMock(context);

            // Act
            var cliente = cnCliente.BuscarClientePorId(99); 

            // Assert
            Assert.Null(cliente); // Verifica que no se haya encontrado ningún cliente

        }

        [Fact]
        public void CambiarContra_ParaClienteExistente_DebeActualizarClave()
        {
            // Arrange
            using var context = CreateContext();
            datosTest(context); 
            var cnCliente = new CNClienteMock(context);
            var nuevaClave = "nuevaclave123";
            string mensaje;


            // Act
            var exito = cnCliente.CambiarContra(1, nuevaClave, out mensaje);
            var clienteActualizado = context.Clientes.Find(1);

            // Assert
            Assert.True(exito);    // Verifica que la operación fue exitosa
            Assert.Empty(mensaje);  // Verifica que no haya mensaje de error
            Assert.NotNull(clienteActualizado);// Verifica que el cliente exista
            Assert.Equal(nuevaClave, clienteActualizado.Clave);// Verifica que la clave se haya actualizado correctamente
            Assert.False(clienteActualizado.Restablecer); // Verifica que el campo Restablecer se haya puesto en false
        }

        [Fact]
        public void Listar_CuandoExistenDatos_DebeRetornarTodosLosClientes()
        {
            // Arrange
            using var context = CreateContext();
            datosTest(context);
            var cnCliente = new CNClienteMock(context);

            // Act
            var listaClientes = cnCliente.Listar();

            // Assert
            Assert.NotNull(listaClientes);// Verifica que la lista no sea nula
            Assert.Equal(2, listaClientes.Count);// Verifica que se hayan listado 2 clientes
        }

        [Fact]
        public void RestablecerContra_ConIdYCorreoCorrectos_DebeRetornarTrue()
        {
            // Arrange
            using var context = CreateContext();
            datosTest(context);
            var cnCliente = new CNClienteMock(context);

            string mensaje;

            // Act
            // Usamos datos que sabemos que existen en la base de datos de prueba
            var resultado = cnCliente.RestablecerContra(1, "test1@example.com", out mensaje);

            // Assert
            Assert.True(resultado); // Verifica que la operación fue exitosa
            Assert.Empty(mensaje); // Verifica que no haya mensaje de error
        }

        [Fact]
        public void RestablecerContra_ConCorreoIncorrecto_DebeRetornarFalseYMensaje()
        {
            // Arrange
            using var context = CreateContext();
            datosTest(context);
            var cnCliente = new CNClienteMock(context);

            string mensaje;

            // Act
            // Usamos un ID que existe pero un correo que no corresponde a ese ID
            var resultado = cnCliente.RestablecerContra(1, "correo.incorrecto@example.com", out mensaje);

            // Assert
            Assert.False(resultado); // Verifica que la operación falló
            Assert.Equal("No se encontró un cliente con este correo electrónico.", mensaje); // Verifica que el mensaje de error sea el esperado
        }
    }
}
