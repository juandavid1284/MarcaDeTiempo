using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarcaDeTiempo.Functions.Entities
{
    public class ConsolidadoEntity : TableEntity
    {
        //id unico de empleado
        public int idEmpleado { get; set; }

        //Fecha y hora de consolidado
        public DateTime dateTime { get; set; }

        //Tiempo trabajado
        public string tiempoTrabajado { get; set; }

    }
}
