using System;
using System.Collections.Generic;
using System.Text;

namespace MarcaDeTiempo.Common.Models
{
    public class Consolidado
    {
        //id unico de empleado
        public int idEmpleado { get; set; }

        //Fecha y hora de consolidado
        public DateTime dateTime { get; set; }


        //Tiempo trabajado
        public TimeSpan tiempoTrabajado { get; set; }

    }
}
