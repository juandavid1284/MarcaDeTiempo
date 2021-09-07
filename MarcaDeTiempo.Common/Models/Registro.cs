using System;
using System.Collections.Generic;
using System.Text;

namespace MarcaDeTiempo.Common.Models
{
    public class Registro
    {
        //id unico de empleado
        public int idEmpleado { get; set; }

        //Fecha y hora de entrada o de salida
        public DateTime dateTime { get; set; }

        // tipo es: 0: Entrada, 1:Salida
        public int tipo { get; set; }

        //Consolidado
        public bool consolidado { get; set; }
    }
}
