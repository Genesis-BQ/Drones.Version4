using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Drones.Models
{
    public class Transaccion
    {
            public string NumeroTarjeta { get; set; }
            public string FechaEx { get; set; }
            public string CVV { get; set; }
            public decimal MontoTarjeta { get; set; }

            public string NumeroCuenta { get; set; }
            public decimal MontoTransferencia { get; set; }

            public string Mensaje { get; set; }
        }
}