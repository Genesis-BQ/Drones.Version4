using System;
using System.IO;
using System.Net;
using System.Text;

namespace Drones.Models
{
    public class HttpRequestHelper
    {
        public static string GetHTML(string urlToRead)
        {
            try
            {
                StringBuilder result = new StringBuilder();
                string ip = "216.168.224.104"; // Dirección IP correspondiente al nombre de dominio indicadoreseconomicos.bccr.fi.cr

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlToRead);
                request.Host = "indicadoreseconomicos.bccr.fi.cr"; // Establecer el Host HTTP para mantener el nombre de dominio en la solicitud

                // Establecer el método de solicitud HTTP GET
                request.Method = "GET";

                // Establecer la dirección IP del servidor como la dirección de destino
                request.Headers.Add("X-Forwarded-For", ip);
                request.Headers.Add("REMOTE_ADDR", ip);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream dataStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(dataStream);
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            result.Append(line);
                        }
                    }
                }

                return result.ToString();
            }
            catch (WebException webEx)
            {
                Console.WriteLine($"Error de conexión: {webEx.Message}");
                throw; // Lanzar la excepción para que sea manejada en un nivel superior
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el HTML: {ex.Message}");
                throw; // Lanzar la excepción para que sea manejada en un nivel superior
            }
        }
    }
}
