using Drones.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Services;


namespace Drones
{
    /// <summary>
    /// Descripción breve de WebService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la línea siguiente. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService : System.Web.Services.WebService
    {
        //Preguntas de Seguridad del registro
        [WebMethod]
        public List<PreguntaSeguridad> ObtenerPreguntasSeguridad()
        {
            string connectionString = LibroV.connectionString;
            List<PreguntaSeguridad> preguntas = new List<PreguntaSeguridad>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand("MostrarPreguntasSeguridad", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                preguntas.Add(new PreguntaSeguridad
                                {
                                    PreguntaID = Convert.ToInt32(reader["PreguntaID"]),
                                    Pregunta = reader["Pregunta"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las preguntas de seguridad: " + ex.Message);
            }

            return preguntas;
        }

        //Registrar el usuario

        [WebMethod]
        public void CargarDatos(Usuario usuario, string provincia, string canton, string distrito)
        {
            try
            {
                // Verificar la existencia del registro antes de realizar el registro
                string connectionString = LibroV.connectionString;

                // Llamada al método para registrar auditoría
                Auditoria.RegistrarAuditoria(usuario.Identificacion, "registro", connectionString);

                // Llamada al procedimiento almacenado para realizar el registro
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand("InsertRegistro", connection))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        cmd.Parameters.AddWithValue("@Identificacion", usuario.Identificacion);
                        cmd.Parameters.AddWithValue("@Nombre", usuario.Nombre);
                        cmd.Parameters.AddWithValue("@Apellido", usuario.Apellido);
                        cmd.Parameters.AddWithValue("@Residencia", usuario.Residencia);
                        cmd.Parameters.AddWithValue("@Telefono", usuario.Telefono);
                        cmd.Parameters.AddWithValue("@Correo", usuario.Correo);
                        cmd.Parameters.AddWithValue("@Contraseña", usuario.Contraseña);
                        cmd.Parameters.AddWithValue("@Pregunta1", usuario.Pregunta1);
                        cmd.Parameters.AddWithValue("@Respuesta1", usuario.Respuesta1);
                        cmd.Parameters.AddWithValue("@Pregunta2", usuario.Pregunta2);
                        cmd.Parameters.AddWithValue("@Respuesta2", usuario.Respuesta2);
                        cmd.Parameters.AddWithValue("@Pregunta3", usuario.Pregunta3);
                        cmd.Parameters.AddWithValue("@Respuesta3", usuario.Respuesta3);
                        cmd.Parameters.AddWithValue("@Provincia", provincia);
                        cmd.Parameters.AddWithValue("@Canton", canton);
                        cmd.Parameters.AddWithValue("@Distrito", distrito);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Envío de correo de confirmación
                EnviarCorreoDeConfirmacion(usuario.Correo);
            }
            catch (Exception ex)
            {
                // Manejar la excepción aquí según sea necesario
                throw new Exception("Error al cargar los datos del usuario: " + ex.Message);
            }
        }


        //Ajax de las provincias, cantones y distritos
        [WebMethod]
        public List<string> ObtenerProvincias()
        {
            List<string> provincias = new List<string>();
            string connectionString = LibroV.connectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string procedimientoAlmacenado = "ObtenerProvinciasDesdeBD";
                    using (SqlCommand command = new SqlCommand(procedimientoAlmacenado, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            provincias.Add(reader["nombre"].ToString());
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener provincias: " + ex.Message);
            }

            return provincias;
        }

        [WebMethod]
        public List<string> ObtenerCantones(string provincia)
        {
            List<string> cantones = new List<string>();
            try
            {
                string connectionString = LibroV.connectionString; // Obtener la cadena de conexión desde la clase LibroV

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string procedimientoAlmacenado = "ObtenerCantonesDesdeBD";
                    using (SqlCommand command = new SqlCommand(procedimientoAlmacenado, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Provincia", provincia);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            cantones.Add(reader["nombre"].ToString());
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener cantones: " + ex.Message);
            }

            return cantones;
        }

        [WebMethod]
        public List<string> ObtenerDistritos(string provincia, string canton)
        {
            List<string> distritos = new List<string>();
            try
            {
                string connectionString = LibroV.connectionString; // Obtener la cadena de conexión desde la clase LibroV

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string procedimientoAlmacenado = "ObtenerDistritosDesdeBD";
                    using (SqlCommand command = new SqlCommand(procedimientoAlmacenado, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Provincia", provincia);
                        command.Parameters.AddWithValue("@Canton", canton);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            distritos.Add(reader["nombre"].ToString());
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine("Error al obtener distritos: " + ex.Message);
            }

            return distritos;
        }

        //Correo
        private void EnviarCorreoDeConfirmacion(string correoUsuario)
        {
            string asunto = "Confirmación de Registro";
            string cuerpo = $"Querido Cliente,\n\n" + $"\n\n" + $"\n\n" + $"Muchas gracias por haber completado nuestro formulario de registro. Esperamos que disfrute explorando nuestra página y encuentre los productos que necesita. Le recordamos algunas recomendaciones y sugerencias en caso de cualquier inconveniente.\n\n" + $"\n\n" + $"\n\n" + $"\n\n" + $"Después de haber ingresado su identificación y contraseña, se le enviará un token a su correo registrado para su validación. Una vez que ingrese el token, podrá acceder a nuestra página principal. Recuerde manejar con cuidado el token, ya que solo tiene 3 intentos. Después de estos intentos, se bloqueará. Lo mismo ocurrirá si ingresa incorrectamente su contraseña. Como recomendación, le pedimos que revise con atención cómo ingresa su contraseña y su token.\n\n" + $"\n\n" + $"\n\n" + $"Atentamente,\n" + $"\n\n" +
                    $"Drones Blue and White Robotics";

            using (MailMessage mensajeCorreo = new MailMessage("gbarahonaq1708@gmail.com", correoUsuario))
            {
                mensajeCorreo.Subject = asunto;
                mensajeCorreo.Body = cuerpo;
                mensajeCorreo.IsBodyHtml = true;
                using (SmtpClient clienteSmtp = new SmtpClient("smtp.gmail.com"))
                {
                    clienteSmtp.Port = 587;
                    clienteSmtp.UseDefaultCredentials = false;
                    clienteSmtp.Credentials = new NetworkCredential("gbarahonaq1708@gmail.com", "faoo mwhe weio lsnw");
                    clienteSmtp.EnableSsl = true;

                    clienteSmtp.Send(mensajeCorreo);
                }
            }
        }

        //Token
        [WebMethod]
        public void Token(string codigoIngresado, int identificacionUsuario)
        {
            try
            {
                string connectionString = LibroV.connectionString;
                string codigoAlmacenado = HttpContext.Current.Session["CodigoConfirmacion"] as string;

                if (codigoAlmacenado != null && codigoIngresado == codigoAlmacenado)
                {
                    Auditoria.RegistrarAuditoria(identificacionUsuario, "Ingreso del Token", connectionString);
                }
                else
                {
                    Auditoria.RegistrarAuditoria(identificacionUsuario, "Intento de inicio de sesión fallido en la página Token", connectionString);
                    throw new Exception("El código ingresado es incorrecto. Por favor, inténtalo nuevamente.");
                }
            }
            catch (Exception ex)
            {
                // Manejar excepciones
                throw new Exception("Error al procesar el token: " + ex.Message);
            }
        }
   
        //Mostrar Productos
        [WebMethod]
        public List<Drone> ObtenerDrones()
        {
            try
            {
                List<Drone> drones = new List<Drone>();
                string connectionString = LibroV.connectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("ObtenerProductosDrones", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Drone drone = new Drone
                                {
                                    Tipo = Convert.ToString(reader["Tipo"]),
                                    Modelo = Convert.ToString(reader["Modelo"]),
                                    Descripcion = Convert.ToString(reader["Descripcion"]),
                                    FichaTecnica = Convert.ToString(reader["Ficha_tecnica"]),
                                    Precio = Convert.ToDecimal(reader["Precio"])
                                };

                                drones.Add(drone);
                            }
                        }
                    }
                }

                return drones;
            }
            catch (Exception ex)
            {
                // Manejar excepciones
                throw new Exception("Error al obtener la lista de drones: " + ex.Message);
            }
        }
        [WebMethod]
        public List<Traktor> ObtenerTraktors()
        {
            try
            {
                List<Traktor> traktors = new List<Traktor>();
                string connectionString = LibroV.connectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("ObtenerProductosTraktor", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Traktor traktor = new Traktor
                                {
                                    Tipo = Convert.ToString(reader["Tipo"]),
                                    Modelo = Convert.ToString(reader["Modelo"]),
                                    Descripcion = Convert.ToString(reader["Descripcion"]),
                                    FichaTecnica = Convert.ToString(reader["Ficha_tecnica"]),
                                    Precio = Convert.ToDecimal(reader["Precio"])
                                };

                                traktors.Add(traktor);
                            }
                        }
                    }
                }

                return traktors;
            }
            catch (Exception ex)
            {
                // Manejar excepciones
                throw new Exception("Error al obtener la lista de traktors: " + ex.Message);
            }
        }

        //Carrito de compras
        [WebMethod]
        public List<Carrito> ObtenerItemsCarrito(int identificacionUsuario)
        {
            try
            {
                List<Carrito> carritoItems = new List<Carrito>();
                string connectionString = LibroV.connectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM Carrito WHERE Identificacion = @Identificacion";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Identificacion", identificacionUsuario);
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            Carrito item = new Carrito
                            {
                                Numero_Serie = reader["Numero_Serie"].ToString(),
                                Modelo = reader["Modelo"].ToString(),
                                Cantidad = Convert.ToInt32(reader["Cantidad"]),
                                Precio = Convert.ToDecimal(reader["Precio"]),
                                Precio_Total = Convert.ToDecimal(reader["Precio_Total"])
                            };

                            carritoItems.Add(item);
                        }
                    }
                }

                return carritoItems;
            }
            catch (Exception ex)
            {
                // Manejar excepciones
                throw new Exception("Error al obtener los items del carrito: " + ex.Message);
            }
        }
        [WebMethod]
        public decimal ActualizarCantidadCarrito(string numeroSerie, int nuevaCantidad, int identificacionUsuario)
        {
            try
            {
                decimal nuevoPrecioTotal = 0;
                string connectionString = LibroV.connectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("ActualizarCantidadCarrito", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Numero_Serie", numeroSerie);
                        command.Parameters.AddWithValue("@NuevaCantidad", nuevaCantidad);
                        command.Parameters.AddWithValue("@Identificacion", identificacionUsuario);

                        SqlParameter nuevoPrecioTotalParam = new SqlParameter("@NuevoPrecioTotal", SqlDbType.Decimal);
                        nuevoPrecioTotalParam.Precision = 18;
                        nuevoPrecioTotalParam.Scale = 2;
                        nuevoPrecioTotalParam.Direction = ParameterDirection.Output;
                        command.Parameters.Add(nuevoPrecioTotalParam);

                        command.ExecuteNonQuery();

                        nuevoPrecioTotal = (decimal)nuevoPrecioTotalParam.Value;
                    }
                }

                return nuevoPrecioTotal;
            }
            catch (Exception ex)
            {
                // Manejar excepciones
                throw new Exception("Error al actualizar la cantidad en el carrito: " + ex.Message);
            }
        }

        //Agregar al carrito
        [WebMethod]
        public void AgregarAlCarrito(int identificacionUsuario, string modelo, int precio)
        {
            try
            {
                string numeroSerie = ObtenerNumeroSerieDesdeBD(modelo);
                string connectionString = LibroV.connectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand("dbo.AgregarAlCarrito", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Identificacion", identificacionUsuario);
                        cmd.Parameters.AddWithValue("@Numero_Serie", numeroSerie);
                        cmd.Parameters.AddWithValue("@Modelo", modelo);
                        cmd.Parameters.AddWithValue("@Cantidad", 1);
                        cmd.Parameters.AddWithValue("@Precio", precio);
                        cmd.Parameters.AddWithValue("@Precio_Total", precio);

                        Auditoria.RegistrarAuditoria(identificacionUsuario, $"Agregado al carrito: {modelo}", connectionString);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar al carrito: " + ex.Message);
            }
        }
        private string ObtenerNumeroSerieDesdeBD(string modelo)
        {
            string numeroSerie = string.Empty;


            string query = "SELECT Numero_Serie FROM Producto WHERE Modelo = @Modelo";
            string connectionString = LibroV.connectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Modelo", modelo);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            numeroSerie = reader["Numero_Serie"].ToString();
                        }
                    }
                }
            }

            return numeroSerie;
        }

        //Validar Respuesta
        [WebMethod]
        public string ValidarRespuestas(string correo, string respuesta1, string respuesta2, string respuesta3)
        {
            try
            {
                string connectionString = LibroV.connectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("CompararRespuestas", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Correo", correo);
                    command.Parameters.AddWithValue("@Respuesta1", respuesta1);
                    command.Parameters.AddWithValue("@Respuesta2", respuesta2);
                    command.Parameters.AddWithValue("@Respuesta3", respuesta3);

                    SqlParameter resultadoParam = new SqlParameter("@Resultado", SqlDbType.Int);
                    resultadoParam.Direction = ParameterDirection.Output;
                    command.Parameters.Add(resultadoParam);

                    command.ExecuteNonQuery();

                    int resultado = Convert.ToInt32(resultadoParam.Value);

                    if (resultado == 1)
                    {
                        return "success"; // Cambia esto si necesitas devolver algo más específico
                    }
                    else if (resultado == 0)
                    {
                        return "Las respuestas no son correctas. Inténtalo de nuevo.";
                    }
                    else
                    {
                        return "Error al validar las respuestas.";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error al validar las respuestas: " + ex.Message;
            }
        }

        //Verficar Código
        [WebMethod]
        public bool VerificarCodigo(string codigoIngresado, string codigoAlmacenado)
        {
            if (codigoAlmacenado != null && codigoIngresado == codigoAlmacenado)
            {
                return true;
            }
            return false;
        }

        //Cambiar la Contraseña
        [WebMethod]
        public bool RestablecerContraseña(string correo, string nuevaContraseña)
        {
            try
            {
                // Lógica para actualizar la contraseña en la base de datos utilizando el correo y la nueva contraseña
                bool cambioExitoso = InsertarContraseñaEnBaseDeDatos(correo, nuevaContraseña);

                return cambioExitoso;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al intentar restablecer la contraseña: " + ex.Message);
            }
        }
        private bool InsertarContraseñaEnBaseDeDatos(string correo, string nuevaContraseña)
        {
            try
            {
                string connectionString = LibroV.connectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("InsertarContraseña", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;


                        command.Parameters.AddWithValue("@correo", correo);
                        command.Parameters.AddWithValue("@nuevaContraseña", nuevaContraseña);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
    }
}
