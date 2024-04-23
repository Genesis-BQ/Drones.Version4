using System;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Services;
using System.Xml;

namespace Drones
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class TipoCambioService : System.Web.Services.WebService
    {
        private HttpClient _client;
        private string _xmlResponse;

        public TipoCambioService()
        {
            _client = new HttpClient();
        }

        [WebMethod]
        public async Task<DataSet> ObtenerIndicadoresEconomicos(string indicador, string fechaInicio, string fechaFinal, string nombre, string subNiveles, string correoElectronico, string token)
        {
            try
            {
                string url = $"https://gee.bccr.fi.cr/Indicadores/Suscripciones/WS/wsindicadoreseconomicos.asmx/ObtenerIndicadoresEconomicosXML?Indicador={indicador}&FechaInicio={fechaInicio}&FechaFinal={fechaFinal}&Nombre={nombre}&SubNiveles={subNiveles}&CorreoElectronico={correoElectronico}&Token={token}";

                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string xmlResponse = await response.Content.ReadAsStringAsync();

                // Decodificar la cadena XML
                string decodedXmlResponse = HttpUtility.HtmlDecode(xmlResponse);

                DataSet dataSet = ParseXmlToDataSet(decodedXmlResponse);
                return dataSet;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener los indicadores económicos: {ex.Message}");
            }
        }


        private DataSet ParseXmlToDataSet(string xmlResponse)
        {
            try
            {
                if (string.IsNullOrEmpty(xmlResponse))
                {
                    throw new ArgumentNullException(nameof(xmlResponse), "El XML de respuesta está vacío o nulo.");
                }

                DataSet dataSet = new DataSet();
                dataSet.ReadXml(new StringReader(xmlResponse));

                // Verificar si el DataSet contiene al menos una tabla
                if (dataSet.Tables.Count > 0)
                {
                    // Renombrar la tabla según el nodo de los datos
                    dataSet.Tables[0].TableName = "Datos_de_INGC011_CAT_INDICADORECONOMIC";

                    // Ajustar los nombres de las columnas según el formato del XML
                    foreach (DataColumn column in dataSet.Tables["Datos_de_INGC011_CAT_INDICADORECONOMIC"].Columns)
                    {
                        if (column.ColumnName == "INGC011_CAT_INDICADORECONOMIC")
                        {
                            column.ColumnName = "COD_INDICADORINTERNO";
                        }
                        else if (column.ColumnName == "DES_FECHA")
                        {
                            column.ColumnName = "Fecha";
                        }
                        else if (column.ColumnName == "NUM_VALOR")
                        {
                            column.ColumnName = "Valor";
                        }
                    }

                    return dataSet;
                }
                else
                {
                    throw new DataException("El XML no contiene datos válidos para procesar.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al procesar el XML: " + ex.Message);
                return null;
            }
        }
    }
}
