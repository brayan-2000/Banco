using UnityEngine;
namespace BancoRapido
{
    public class BancoBoton : MonoBehaviour
    {
        public BancoSimulacion simulacion;
        public int accion;
        public void Activar(){if(simulacion!=null)simulacion.Accion(accion);}
    }
}
