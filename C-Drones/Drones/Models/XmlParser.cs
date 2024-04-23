using System;
using System.Xml;

namespace Drones.Models
{
    public class XmlParser
    {
        private readonly string _xmlData;

        public XmlParser(string xmlData)
        {
            _xmlData = xmlData;
        }

        public string GetValue(string tagName)
        {
            try
            {
                // Verificar si el XML tiene un elemento raíz
                if (string.IsNullOrWhiteSpace(_xmlData))
                {
                    throw new Exception("El XML devuelto está vacío o no tiene un elemento raíz.");
                }

                // Crear un nuevo documento XML
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(_xmlData);

                // Verificar si el XML tiene hijos
                if (xmlDoc.DocumentElement == null)
                {
                    throw new Exception("El XML devuelto no tiene un elemento raíz válido.");
                }

                // Obtener el valor del tag especificado
                XmlNodeList nodeList = xmlDoc.GetElementsByTagName(tagName);
                if (nodeList.Count > 0)
                {
                    return nodeList[0].InnerText;
                }
                else
                {
                    throw new Exception($"El tag '{tagName}' no fue encontrado en el documento XML.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el valor del tag '{tagName}': {ex.Message}");
                throw; // Lanzar la excepción para que sea manejada en un nivel superior
            }
        }
    }
}
