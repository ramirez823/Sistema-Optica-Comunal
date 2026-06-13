using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Pedido
    {
        public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();

        public int Id { get; set; }

        [Required]
        public int ProveedorId { get; set; }

        public Proveedor? Proveedor { get; set; }

        [Required]
        public DateTime FechaPedido { get; set; }
        public string Descripcion { get; set; }

        public EstadoPedido Estado { get; set; }

        [Required]
        public DateTime FechaEntregaEstimada { get; set; }

        public DateTime? FechaEntregaReal { get; set; }
        public bool Activo { get; set; } = true;
        public IndicadorEntrega Indicador { get; private set; } = IndicadorEntrega.Pendiente;

        public void CambiarEstado(EstadoPedido nuevoEstado)
        {
            Estado = nuevoEstado;

            if (nuevoEstado == EstadoPedido.Recibido && FechaEntregaReal == null)
            {
                FechaEntregaReal = DateTime.Now;

                if (FechaEntregaReal <= FechaEntregaEstimada)
                    Indicador = IndicadorEntrega.A_Tiempo;
                else
                    Indicador = IndicadorEntrega.Retrasado;
            }

            if (nuevoEstado != EstadoPedido.Recibido)
            {
                Indicador = IndicadorEntrega.Pendiente;
                FechaEntregaReal = null;
            }
        }


    }

    public enum EstadoPedido
    {
        Pendiente = 1,
        Aprobado = 2,
        Enviado = 3,
        Recibido = 4,
        Cancelado = 5
    }
    public enum IndicadorEntrega
    {
        Pendiente = 0,
        A_Tiempo = 1,
        Retrasado = 2
    }

}
