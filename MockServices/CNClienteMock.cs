
using EcommerceApp.Data.Data;
using EcommerceApp.Data.Models;
using EcommerceApp.Data.Negocio;

namespace EcommerceApp.Test.MockServices
{
    public class CNClienteMock : ICNCliente
    {
        private readonly ApplicationDbContext _context;

        public CNClienteMock(ApplicationDbContext context)
        {
            _context = context;
        }

        public Cliente? BuscarClientePorCorreo(string correo)
        {
            return _context.Clientes.FirstOrDefault(c => c.Correo == correo);
        }

        public Cliente? BuscarClientePorCredenciales(string correo, string claveHasheada)
        {
            return _context.Clientes.FirstOrDefault(c => c.Correo == correo && c.Clave == claveHasheada);
        }

        public Cliente? BuscarClientePorId(int idCliente)
        {
            return _context.Clientes.FirstOrDefault(c => c.IdCliente == idCliente);
        }

        public bool CambiarContra(int idCliente, string nuevaContraHasheada, out string mensaje)
        {
            mensaje = string.Empty;

            var cliente = _context.Clientes.FirstOrDefault(c => c.IdCliente == idCliente);
            if (cliente != null)
            {
                cliente.Clave = nuevaContraHasheada;
                _context.SaveChanges();
                return true;
            }

            mensaje = "Cliente no encontrado";
            return false;
        }

        public List<Cliente> Listar()
        {
            return _context.Clientes.ToList();
        }

        public int Registrar(Cliente obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            if (string.IsNullOrEmpty(obj.Correo) || string.IsNullOrEmpty(obj.Clave))
            {
                Mensaje = "Correo y clave son obligatorios.";
                return 0;
            }
            if (_context.Clientes.Any(c => c.Correo == obj.Correo))
            {
                Mensaje = $"El correo '{obj.Correo}' ya se encuentra registrado.";
                return 0;
            }
            _context.Clientes.Add(obj);
            _context.SaveChanges();
            return obj.IdCliente;
        }

        public bool RestablecerContra(int idCliente, string correo, out string mensaje)
        {
            mensaje = string.Empty;
            var cliente = _context.Clientes.FirstOrDefault(c => c.IdCliente == idCliente && c.Correo == correo);
            if (cliente == null)
            {
                mensaje = "No se encontró un cliente con este correo electrónico.";
                return false;
            }
            return true;
        }
    }
}
