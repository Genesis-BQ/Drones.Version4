using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Drones.Models;
using System.Net.Mail;
using System.Net;
using Drones.ServidorWeb;
using System.Web.Services;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace Drones.Controllers
{
    public class HomeController : Controller
    {
        private string connectionString = LibroV.connectionString;
                private string pythonWebServiceUrl = "http://localhost:5000";
        private TipoCambioService _tipoCambioService;

        public HomeController()
        {
            _tipoCambioService = new TipoCambioService();
        }
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Home()
        {
            return View();
        }

        //Preguntas de Seguridad del registro

        public ActionResult Registrar()
        {
            try
            {
                // Crear una instancia del cliente del servicio web
                WebService servicio = new WebService();

                // Obtener las preguntas de seguridad desde el servicio web
                List<PreguntaSeguridad> preguntasSeguridad = servicio.ObtenerPreguntasSeguridad();

                // Crear un nuevo usuario con las preguntas de seguridad
                var usuario = new Usuario
                {
                    PreguntasSeguridad = preguntasSeguridad
                };

                return View(usuario);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al obtener las preguntas de seguridad: " + ex.Message;
                return View("Error");
            }
        }


        //Registrar el usuario
        public ActionResult CargarDatos()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CargarDatos(Usuario usuario, string provincia, string canton, string distrito)
        {
            try
            {
                // Crear una instancia del cliente del servicio web
                WebService servicio = new WebService();
                servicio.CargarDatos(usuario, provincia, canton, distrito);

                // Redireccionar a una vista de éxito o cualquier otra acción que desees después de registrar al usuario
                return RedirectToAction("Index");
            }
            catch (WebException ex)
            {
                ViewBag.ErrorMessage = "Error de comunicación con el servicio web: " + ex.Message;
                return View("ErrorRegistro");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("ErrorRegistro");
            }
        }


        //Ajax de las pronvincias, cantones, distritos
        public ActionResult ObtenerProvincias()
        {
            // Crear una instancia del cliente del servicio web
            WebService servicio = new WebService();

            List<string> provincias = servicio.ObtenerProvincias();

            return Json(provincias, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerCantones(string provincia)
        {
            // Crear una instancia del cliente del servicio web
            WebService servicio = new WebService();

            
            List<string> cantones = servicio.ObtenerCantones(provincia);

            return Json(cantones, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ObtenerDistritos(string provincia, string canton)
        {
            // Crear una instancia del cliente del servicio web
            WebService servicio = new WebService();

           
            List<string> distritos = servicio.ObtenerDistritos(provincia, canton);

            return Json(distritos, JsonRequestBehavior.AllowGet);
        }

        //Validar Login
        [HttpPost]
        public ActionResult Validarlogin()
        {

            string identificacionS = Request.Form["identificacion"]?.ToString();

            if (string.IsNullOrEmpty(identificacionS) || !int.TryParse(identificacionS, out int identificacion))
            {

                ViewBag.MensajeError = "Identificación inválida";
                return View("ErrorInicioSesion");
            }

            string contrasenaEncriptada = Request.Form["contrasena"]?.ToString();

            if (string.IsNullOrEmpty(contrasenaEncriptada))
            {

                ViewBag.MensajeError = "Contraseña inválida";
                return View("ErrorInicioSesion");
            }


            bool credencialesValidas = ValidarCredenciales(identificacion, contrasenaEncriptada);

            if (credencialesValidas)
            {


                EnviarCodigoDeConfirmacion(identificacion);


                Session["UsuarioIdentificacion"] = identificacion;


                Auditoria.RegistrarAuditoria(identificacion, "Inicio de sesión exitoso", connectionString);

                return View("Token");
            }
            else
            {

                IncrementarIntentosFallidos();
                string mensajeBloqueo = ObtenerMensajeUsuarioBloqueado();
                if (!string.IsNullOrEmpty(mensajeBloqueo))
                {
                    ViewBag.MensajeError = mensajeBloqueo;
                    return View("UsuarioBloqueado");
                }


                Auditoria.RegistrarAuditoria(identificacion, "Intento de inicio de sesión fallido", connectionString);
            }


            ViewBag.MensajeError = "Credenciales inválidas";
            return View("ErrorInicioSesion");
        }


        private bool ValidarCredenciales(int identificacion, string contrasenaEncriptada)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("ValidarCredenciales", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Identificacion", identificacion);
                    command.Parameters.AddWithValue("@Contraseña", contrasenaEncriptada);


                    SqlParameter resultadoParam = new SqlParameter("@Resultado", SqlDbType.Int);
                    resultadoParam.Direction = ParameterDirection.Output;
                    command.Parameters.Add(resultadoParam);


                    Auditoria.RegistrarAuditoria(identificacion, "Validación de credenciales", connectionString);


                    command.ExecuteNonQuery();


                    int resultado = Convert.ToInt32(resultadoParam.Value);

                    return resultado == 1;
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
        }

        public ActionResult ErrorInicioSesion()
        {

            return View();
        }

        //Bloque del usuario por medio de los 3 intentos
        public ActionResult UsuarioBloqueado()
        {
            ActualizarTiempoRestante();

            
            int identificacionUsuario = (int)Session["UsuarioIdentificacion"];

           
            Auditoria.RegistrarAuditoria(identificacionUsuario, "Usuario bloqueado", connectionString);

            return View();
        }

        private void IncrementarIntentosFallidos()
        {
            int intentosFallidos = (int)(Session["IntentosFallidos"] ?? 0);
            intentosFallidos++;
            Session["IntentosFallidos"] = intentosFallidos;

            if (intentosFallidos == 1)
            {

                Session["TiempoBloqueo"] = DateTime.Now.AddMinutes(1);
            }
            else if (intentosFallidos == 2)
            {

                Session["TiempoBloqueo"] = DateTime.Now.AddMinutes(20);
            }
            else if (intentosFallidos >= 3)
            {

                Session["TiempoBloqueo"] = DateTime.Now.AddHours(1);
            }
        }

        private string ObtenerMensajeUsuarioBloqueado()
        {
            DateTime? tiempoBloqueo = Session["TiempoBloqueo"] as DateTime?;

            if (tiempoBloqueo.HasValue && tiempoBloqueo > DateTime.Now)
            {
                TimeSpan tiempoRestante = tiempoBloqueo.Value - DateTime.Now;
                Session["TiempoRestante"] = tiempoRestante.ToString(@"mm\:ss");
                return $"Tu cuenta está bloqueada. Por favor, inténtalo de nuevo después de {tiempoRestante.ToString(@"mm\:ss")}.";
            }

            Session["TiempoRestante"] = null;
            return null;
        }

        private void ActualizarTiempoRestante()
        {
            DateTime? tiempoBloqueo = Session["TiempoBloqueo"] as DateTime?;

            if (tiempoBloqueo.HasValue && tiempoBloqueo > DateTime.Now)
            {
                TimeSpan tiempoRestante = tiempoBloqueo.Value - DateTime.Now;
                Session["TiempoRestante"] = tiempoRestante.ToString(@"mm\:ss");
            }
            else
            {
                Session["TiempoRestante"] = null;
            }
        }

        //Numeros aletorios
        private int GenerarCodigoAleatorio()
        {
            Random random = new Random();
            return random.Next(1000, 10000);
        }

        //Correos
        private string ObtenerCorreoPorIdentificacion(int identificacion)
        {
            string correo = null;
            string connectionString = LibroV.connectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand cmd = new SqlCommand("ObtenerCorreoPorIdentificacion", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;


                    cmd.Parameters.AddWithValue("@Identificacion", identificacion);


                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        correo = reader["Correo"].ToString();
                    }
                }
            }

            return correo;
        }
        private void EnviarCorreoDeConfirmacion(string correoUsuario)
        {
            string asunto = "Confirmación de Registro";
            string cuerpo = $"Querido Cliente,\n\n" + $"\n\n" + $"\n\n" + $"Muchas gracias por haber completado nuestro formulario de registro. Esperamos que disfrute explorando nuestra página y encuentre los productos que necesita. Le recordamos algunas recomendaciones y sugerencias en caso de cualquier inconveniente.\n\n" + $"\n\n" + $"\n\n" + $"\n\n" + $"Después de haber ingresado su identificación y contraseña, se le enviará un token a su correo registrado para su validación. Una vez que ingrese el token, podrá acceder a nuestra página principal. Recuerde manejar con cuidado el token, ya que solo tiene 3 intentos. Después de estos intentos, se bloqueará. Lo mismo ocurrirá si ingresa incorrectamente su contraseña. Como recomendación, le pedimos que revise con atención cómo ingresa su contraseña y su token.\n\n" + $"\n\n" + $"\n\n" + $"Atentamente,\n" + $"\n\n" +
                    $"Drones Blue and White Robotics";

            using (MailMessage mensajeCorreo = new MailMessage("correo de la personas", correoUsuario))
            {
                mensajeCorreo.Subject = asunto;
                mensajeCorreo.Body = cuerpo;
                mensajeCorreo.IsBodyHtml = true;
                using (SmtpClient clienteSmtp = new SmtpClient("smtp.gmail.com"))
                {
                    clienteSmtp.Port = 587;
                    clienteSmtp.UseDefaultCredentials = false;
                    clienteSmtp.Credentials = new NetworkCredential("correo de la presona", "token");
                    clienteSmtp.EnableSsl = true;

                    clienteSmtp.Send(mensajeCorreo);
                }
            }
        }

        private void EnviarCodigoDeConfirmacion(int identificacion)
        {

            string correoDestino = ObtenerCorreoPorIdentificacion(identificacion);

            if (correoDestino != null)
            {

                int codigoAleatorio = GenerarCodigoAleatorio();


                string codigo = codigoAleatorio.ToString("D4");


                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("correo de la persona", "token"),
                    EnableSsl = true,
                };


                MailMessage mensaje = new MailMessage("correo de la persona", correoDestino)
                {
                    Subject = "Código de confirmación",
                    Body = $"Querido Usuario:\n\nTu código de confirmación es: {codigo}. Úsalo para la confirmación de token.\n\nAtentamente,\nDrones Blue and White Robotics",
                };


                smtpClient.Send(mensaje);


                Session["CodigoConfirmacion"] = codigo;
            }
        }


        //Token
        public ActionResult Token(string codigoIngresado)
        {
            try
            {
               
                int identificacionUsuario = (int)Session["UsuarioIdentificacion"];
                WebService servicio = new WebService();
                servicio.Token(codigoIngresado, identificacionUsuario);

                return RedirectToAction("Home", "Home");
            }
            catch (WebException ex)
            {
                ViewBag.ErrorMessage = "Error de comunicación con el servicio web: " + ex.Message;
                return View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.MensajeError = ex.Message;
                return View("Token");
            }
        }


        //Mostrar Productos
        public ActionResult Drones()
        {
            try
            {
                // Crear una instancia del cliente del servicio web
                WebService servicio = new WebService();
                List<Drone> drones = servicio.ObtenerDrones();
                return View(drones);
            }
            catch (WebException ex)
            {
                ViewBag.ErrorMessage = "Error de comunicación con el servicio web: " + ex.Message;
                return View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        public ActionResult Traktor()
        {
            try
            {
                // Crear una instancia del cliente del servicio web
                WebService servicio = new WebService();
                List<Traktor> traktors = servicio.ObtenerTraktors();
                return View(traktors);
            }
            catch (WebException ex)
            {
                ViewBag.ErrorMessage = "Error de comunicación con el servicio web: " + ex.Message;
                return View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        //Obtener la identificacion del usuario
        private int ObtenerIdentificacionUsuario()
        {

            if (Session["UsuarioIdentificacion"] != null)
            {

                return Convert.ToInt32(Session["UsuarioIdentificacion"]);
            }
            else
            {

                return -1;
            }
        }


        //Carrito de compras
        public ActionResult Carrito()
        {
            try
            {
                // Obtener la identificación del usuario desde la sesión
                int identificacionUsuario = ObtenerIdentificacionUsuario();
                if (identificacionUsuario == -1)
                {
                    // No se ha iniciado sesión, redirigir a la página de inicio de sesión
                    return RedirectToAction("InicioSesion", "Usuario");
                }

                // Crear una instancia del cliente del servicio web
                WebService servicio = new WebService();
                List<Carrito> carritoItems = servicio.ObtenerItemsCarrito(identificacionUsuario);
                return View(carritoItems);
            }
            catch (WebException ex)
            {
                ViewBag.ErrorMessage = "Error de comunicación con el servicio web: " + ex.Message;
                return View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }



        [HttpPost]
        public ActionResult ActualizarCantidad(string numeroSerie, int nuevaCantidad, decimal? nuevoPrecio)
        {
            try
            {
                // Obtener la identificación del usuario desde la sesión
                int identificacionUsuario = ObtenerIdentificacionUsuario();
                if (identificacionUsuario == -1)
                {
                    // No se ha iniciado sesión, redirigir a la página de inicio de sesión
                    return RedirectToAction("Login");
                }

                // Crear una instancia del cliente del servicio web
                WebService servicio = new WebService();
                decimal nuevoPrecioTotal = servicio.ActualizarCantidadCarrito(numeroSerie, nuevaCantidad, identificacionUsuario);

                return RedirectToAction("Carrito");
            }
            catch (WebException ex)
            {
                ViewBag.ErrorMessage = "Error de comunicación con el servicio web: " + ex.Message;
                return View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }


        public ActionResult AgregarAlCarrito()
        {
            return View();
        }
        [HttpPost]
        public ActionResult AgregarAlCarrito(FormCollection form)
        {
            try
            {
               
                string modelo = form["modelo"];
                int precio = Convert.ToInt32(form["precio"]);
                int identificacionUsuario = Convert.ToInt32(Session["UsuarioIdentificacion"]);
                WebService servicio = new WebService();
                servicio.AgregarAlCarrito(identificacionUsuario, modelo, precio);
                return RedirectToAction("Home");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al agregar al carrito: " + ex.Message;
                return View("Error");
            }
        }

        //Formas de pago
        public ActionResult Pagar()
        {
            return View();
        }

        public ActionResult Paypal()
        {
            int identificacionUsuario = ObtenerIdentificacionUsuario();
            decimal totalAPagar = ObtenerTotalAPagar(identificacionUsuario);


            Auditoria.RegistrarAuditoria(identificacionUsuario, "Acceso a Paypal", connectionString);

            ViewBag.TotalAPagar = totalAPagar;
            ViewBag.IdentificacionUsuario = identificacionUsuario;

            return View();
        }


        // Método para procesar el pago de la tarjeta
        public async Task<ActionResult> Tarjeta(Transaccion model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var requestData = new
                        {
                            NumeroTarjeta = model.NumeroTarjeta,
                            FechaEx = model.FechaEx,
                            CVV = model.CVV,
                            Monto = model.MontoTarjeta
                        };

                        var json = JsonConvert.SerializeObject(requestData);

                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await httpClient.PostAsync($"{pythonWebServiceUrl}/validar_tarjeta", content);
                        response.EnsureSuccessStatusCode();

                        string responseBody = await response.Content.ReadAsStringAsync();

                        var responseData = JsonConvert.DeserializeObject<dynamic>(responseBody);

                        string mensaje = responseData.mensaje;

                        model.Mensaje = mensaje;

                        // Agregar otras operaciones necesarias al modelo, si las hay
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Mensaje = "Error al procesar la tarjeta: " + ex.Message;
                    return View(model);
                }
            }

            // Agregar el resto de la lógica del método original
            int identificacionUsuario = ObtenerIdentificacionUsuario();
            decimal totalAPagar = ObtenerTotalAPagar(identificacionUsuario);

            Auditoria.RegistrarAuditoria(identificacionUsuario, "Acceso a la página de pago con tarjeta", connectionString);

            ViewBag.TotalAPagar = totalAPagar;
            ViewBag.IdentificacionUsuario = identificacionUsuario;

            return View(model);
        }



        // Método para procesar la transferencia
        public async Task<ActionResult> Transferencia(Transaccion model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var requestData = new
                        {
                            NumeroCuenta = model.NumeroCuenta,
                            Monto = model.MontoTransferencia
                        };

                        var json = JsonConvert.SerializeObject(requestData);

                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await httpClient.PostAsync($"{pythonWebServiceUrl}/realizar_transaccion", content);
                        response.EnsureSuccessStatusCode();

                        string responseBody = await response.Content.ReadAsStringAsync();

                        var responseData = JsonConvert.DeserializeObject<dynamic>(responseBody);

                        string mensaje = responseData.mensaje;

                        model.Mensaje = mensaje;

                        
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Mensaje = "Error al procesar la transferencia: " + ex.Message;
                    return View(model);
                }
            }

            int identificacionUsuario = ObtenerIdentificacionUsuario();
            decimal totalAPagar = ObtenerTotalAPagar(identificacionUsuario);

            Auditoria.RegistrarAuditoria(identificacionUsuario, "Acceso a la página de pago con transferencia", connectionString);

            ViewBag.TotalAPagar = totalAPagar;
            ViewBag.IdentificacionUsuario = identificacionUsuario;

            return View(model);
        }

        // Método para crear el mensaje y pasar los datos necesarios a la vista de confirmación de pago
        private Transaccion CrearMensajeViewModel(string mensaje, Transaccion model)
        {
            var mensajeViewModel = new Transaccion
            {
                Mensaje = mensaje
            };
            return mensajeViewModel;
        }

        public ActionResult MostrarTotalAPagar()
        {
            int identificacionUsuario = ObtenerIdentificacionUsuario();

            if (identificacionUsuario == -1)
            {

                return RedirectToAction("Login");
            }

            decimal totalAPagar = ObtenerTotalAPagar(identificacionUsuario);

            ViewBag.TotalAPagar = totalAPagar;

            return View();
        }

        private decimal ObtenerTotalAPagar(int identificacionUsuario)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("ObtenerTotalAPagar", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Identificacion", identificacionUsuario);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    decimal totalAPagar = 0;

                    while (reader.Read())
                    {

                        totalAPagar += Convert.ToDecimal(reader["TotalAPagar"]);
                    }

                    return totalAPagar;
                }
            }
        }

        public ActionResult PagarYBorrar()
        {
            int identificacionUsuario = ObtenerIdentificacionUsuario();

            if (identificacionUsuario == -1)
            {

                return RedirectToAction("Login");
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("BorrarCarritoPorIdentificacion", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Identificacion", identificacionUsuario);

                    command.ExecuteNonQuery();
                }
            }



            return RedirectToAction("Home");
        }
        public ActionResult loginPaypal()
        {

            return View();
        }


        //Recuperar contraseña 
        public ActionResult RecuperarContraseña()
        {
            return View();
        }

        public ActionResult ObtenerPreguntas(string correo)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Verificar si el correo está registrado
                    int resultadoCorreoExistente = 0;
                    using (SqlCommand verificarCorreoCommand = new SqlCommand("VerificarCorreo", connection))
                    {
                        verificarCorreoCommand.CommandType = CommandType.StoredProcedure;
                        verificarCorreoCommand.Parameters.AddWithValue("@correo_verificar", correo);
                        verificarCorreoCommand.Parameters.Add("@resultado", SqlDbType.Int).Direction = ParameterDirection.Output;
                        verificarCorreoCommand.ExecuteNonQuery();
                        resultadoCorreoExistente = Convert.ToInt32(verificarCorreoCommand.Parameters["@resultado"].Value);
                    }

                    if (resultadoCorreoExistente == 0)
                    {
                        ViewBag.ErrorMessage = "El correo electrónico no está registrado.";
                        return View("RecuperarContraseña");
                    }

                    // Eliminar la contraseña asociada al correo
                    using (SqlCommand eliminarContraseñaCommand = new SqlCommand("EliminarContraseña", connection))
                    {
                        eliminarContraseñaCommand.CommandType = CommandType.StoredProcedure;
                        eliminarContraseñaCommand.Parameters.AddWithValue("@correo", correo);
                        eliminarContraseñaCommand.ExecuteNonQuery();
                    }

                    // Obtener las preguntas de seguridad si el correo está registrado
                    SqlCommand command = new SqlCommand("ObtenerPreguntas", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Correo", correo);

                    PreguntasViewModel model = new PreguntasViewModel();
                    model.Correo = correo;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.Pregunta1 = reader["Pregunta1"].ToString();
                            model.Pregunta2 = reader["Pregunta2"].ToString();
                            model.Pregunta3 = reader["Pregunta3"].ToString();
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "No se encontraron preguntas asociadas a este correo.";
                            return View("RecuperarContraseña");
                        }
                    }

                    TempData["Correo"] = correo;

                    // Enviar el código de recuperación
                    EnviarCodigoDeRecuperacion(correo);

                    ViewBag.Correo = correo;

                    return View("Recuperar", model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al recuperar las preguntas: " + ex.Message;
                return View("RecuperarContraseña");
            }
        }


        private void EnviarCodigoDeRecuperacion(string correo)
        {

            int codigoAleatorio = GenerarCodigoAleatorio();


            string codigo = codigoAleatorio.ToString("D4");


            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("correo de la persona", "token"),
                EnableSsl = true,
            };


            MailMessage mensaje = new MailMessage("correo de la persona", correo)
            {
                Subject = "Código de recuperación de contraseña",
                Body = $"Querido Usuario:\n\nTu código de recuperación es: {codigo}. Úsalo para restablecer tu contraseña.\n\nAtentamente,\nDrones Blue and White Robotics",
            };


            smtpClient.Send(mensaje);


            Session["CodigoRecuperacion"] = codigo;
        }


        //Verifcar Preguntas
        public ActionResult Recuperar(string correo)
        {
            // Crear un modelo para la vista
            PreguntasViewModel model = new PreguntasViewModel();
            model.Correo = correo;

            return View(model);
        }

        // Acción para validar las respuestas
        public ActionResult ValidarRespuestas(string correo, string respuesta1, string respuesta2, string respuesta3)
        {
            try
            {
                
                WebService servicio = new WebService();
                string resultado = servicio.ValidarRespuestas(correo, respuesta1, respuesta2, respuesta3);

                if (resultado == "success")
                {
                    return RedirectToAction("VerficarCodigo"); 
                }
                else
                {
                    ViewBag.ErrorMessage = resultado;
                    return RedirectToAction("Recuperar", new { Correo = correo });
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al validar las respuestas: " + ex.Message;
                return View("Recuperar");
            }
        }

        //Verficar Codigo
        public ActionResult VerficarCodigo()
        {
            return View();
        }

        [HttpPost]
        public ActionResult VerificarCodigo(string codigoIngresado)
        {
            try
            {
                
                string codigoAlmacenado = Session["CodigoRecuperacion"] as string;

               
                WebService servicio = new WebService();

                bool codigoValido = servicio.VerificarCodigo(codigoIngresado, codigoAlmacenado);

                if (codigoValido)
                {
                    return RedirectToAction("RestablecerContraseña", "Home");
                }
                else
                {
                    ViewBag.MensajeError = "El código ingresado es incorrecto. Por favor, inténtalo nuevamente.";
                    return View("VerficarCodigo");
                }
            }
            catch (Exception ex)
            {
                ViewBag.MensajeError = "Error al verificar el código: " + ex.Message;
                return View("VerificarCodigo");
            }
        }


        //Restaurar Constraseña
        public ActionResult RestablecerContraseña()
        {

            return View();
        }
        [HttpPost]
        public ActionResult RestablecerContraseña(string nuevaContraseña)
        {
            try
            {
                string correo = TempData["Correo"] as string;

                // Crear una instancia del cliente del servicio web
                WebService servicio = new WebService();

                // Llamar al método del servicio web para restablecer la contraseña
                bool cambioExitoso = servicio.RestablecerContraseña(correo, nuevaContraseña);

                if (cambioExitoso)
                {
                    ViewBag.Mensaje = "La contraseña se cambió exitosamente.";
                    return View("RestablecerContraseña");
                }
                else
                {
                    ViewBag.MensajeError = "No se pudo cambiar la contraseña. Inténtalo nuevamente.";
                    return View("RestablecerContraseña");
                }
            }
            catch (Exception ex)
            {
                ViewBag.MensajeError = $"Error: {ex.Message}";
                return View("RestablecerContraseña");
            }
        }

        //Cambio de Dolar
        public async Task<ActionResult> TipoCambio()
        {
            try
            {
                // Obtener el DataSet para el indicador 317 (compra)
                var dataSetCompra = await _tipoCambioService.ObtenerIndicadoresEconomicos("317", "30/03/2024", "30/03/2024", "nombre de la persona", "S", "correo de la persona", "numero de token");

                // Obtener el DataSet para el indicador 318 (venta)
                var dataSetVenta = await _tipoCambioService.ObtenerIndicadoresEconomicos("318", "30/03/2024", "30/03/2024", "nombre de la persona", "S", "correo de la personas", "numero de token");


                var compra = dataSetCompra.Tables["INGC011_CAT_INDICADORECONOMIC"].Rows[0]["NUM_VALOR"];
                var venta = dataSetVenta.Tables["INGC011_CAT_INDICADORECONOMIC"].Rows[0]["NUM_VALOR"];


                ViewBag.Compra = compra;
                ViewBag.Venta = venta;

                
                return View("TipoCambio", new { Compra = compra, Venta = venta });
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al obtener los indicadores económicos: {ex.Message}";
                return View("TipoCambio");
            }
        }

        private decimal ParseNumValorFromDataSet(DataSet dataSet)
        {
            if (dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                
                DataRow firstRow = dataSet.Tables[0].Rows[0];
                object valorObj = firstRow["Valor"];

                
                if (valorObj != null && decimal.TryParse(valorObj.ToString(), out decimal valor))
                {
                    return valor;
                }
            }

            
            return 0m;
        }

        //Servidor del Registro Civil
        public ActionResult RegistroCivil()
        {
            return View();
        }

    }

}