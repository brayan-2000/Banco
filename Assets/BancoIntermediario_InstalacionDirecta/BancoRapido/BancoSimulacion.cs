using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BancoRapido
{
    // Prototipo educativo: no se conecta a bancos ni procesa dinero real.
    public class BancoSimulacion : MonoBehaviour
    {
        public Camera camara;
        public bool controlPC = true;
        public bool seleccionPorMirada = false;
        public float segundosMirada = 2f;
        public bool modoDefensa = false;
        TextMesh detalle, avisos, cabecera, pie, reticula;
        GameObject aprobar, detener;
        readonly List<Material> materiales = new List<Material>();
        readonly List<Orden> cola = new List<Orden>();
        readonly List<bool> decisiones = new List<bool>();
        int indice;
        float yaw, pitch, permanencia, bloqueo;
        BancoBoton objetivo;
        string ultimo = "Selecciona un modo y revisa cada orden.";
        Color verde = new Color(.15f, 1f, .61f);
        Color claro = new Color(.79f, .94f, .89f);

        enum Etapa { Entrada, Revision, Resultados, Informe, Creditos }
        Etapa etapa;
        GameObject portal, entrar, botonPagina, reiniciar, manual, defensa, rotulo;
        TextMesh etiquetaPagina;
        Transform soporteJugador;
        Vector3 puesto;
        bool requiereApartar;
        AudioSource audioBanco;
        AudioClip tonoAviso, tonoDecision;
        readonly List<bool> retenidas = new List<bool>();
        [TextArea(2, 5)] public string equipoYRoles = "";
        const string instrucciones = "Mira un botón durante 2 s para elegir.";
        class Orden
        {
            public string nombre, pais; public double monto, limite; public bool nuevo;
            public Orden(string n, string p, double m, double l, bool b)
            { nombre = n; pais = p; monto = m; limite = l; nuevo = b; }
        }
        static readonly string[] nombres = { "SHALIKA FOUNDATION", "ANDES TECHNOLOGY", "LANKA EXPORTS", "PACIFIC TRADING", "GLOBAL EDUCATION", "NUEVA ESPERANZA" };
        void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // El movimiento de cabeza pertenece al Tracked Pose Driver.
            controlPC = false;
            seleccionPorMirada = true;
#endif
            segundosMirada = Mathf.Max(.25f, segundosMirada);
            ConstruirOficina();
            cola.Add(new Orden("ANDES TECHNOLOGY", "Peru", 12500, 500000, false));
            cola.Add(new Orden("SHALIKA FANDATION", "Sri Lanka", 20000000, 1000000, true));
            cola.Add(new Orden("LANKA EXPORTS", "Sri Lanka", 42000, 1000000, false));
            cola.Add(new Orden("PACIFIC TRADING", "Filipinas", 4500000, 2000000, false));
            cola.Add(new Orden("GLOBAL EDUCATION", "Reino Unido", 18000, 3000000, true));
            cola.Add(new Orden("ANDES TECHNOLOGY", "Peru", 48000, 500000, false));
            cola.Add(new Orden("NUEVA ESPERANZA", "Peru", 82000, 500000, false));
            cola.Add(new Orden("PACIFIC TRADNG", "Filipinas", 55000, 2000000, false));
            PrepararSonido();
            PrepararPortal();
            NuevaRonda();
        }
        Material Material(Color color, bool unlit)
        {
            // Resources conserva una referencia real al shader en el APK.
            string recurso = unlit ? "BancoVR/Unlit" : "BancoVR/Lit";
            Material plantilla = Resources.Load<Material>(recurso);
            if (plantilla == null)
                throw new InvalidOperationException(
                    "Faltan materiales BancoVR. Ejecuta Banco VR > Preparar materiales Android antes de compilar.");
            Material m = new Material(plantilla);
            m.SetColor("_BaseColor", color);
            materiales.Add(m);
            return m;
        }
        GameObject Cubo(string nombre, Vector3 pos, Vector3 escala, Color color, bool unlit = false)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = nombre;
            g.transform.SetParent(transform); g.transform.position = pos; g.transform.localScale = escala;
            var renderer = g.GetComponent<Renderer>();
            renderer.sharedMaterial = Material(color, unlit); return g;
        }
        TextMesh Texto(string nombre, string contenido, Vector3 pos, float tam, Color color)
        {
            GameObject g = new GameObject(nombre); g.transform.SetParent(transform); g.transform.position = pos;
            TextMesh t = g.AddComponent<TextMesh>(); t.text = contenido; t.fontSize = 64; t.characterSize = tam * 0.5f;
            t.anchor = TextAnchor.UpperLeft; t.color = color; t.richText = true;
            return t;
        }
        GameObject Boton(string nombre, string texto, float x, float y, float ancho, int accion, Color color)
        {
            var g = Cubo(nombre, new Vector3(x, y, 1.84f), new Vector3(ancho, .26f, .09f), color, true);
            var b = g.AddComponent<BancoBoton>(); b.simulacion = this; b.accion = accion;
            var t = Texto(nombre + " Texto", texto, new Vector3(x, y, 1.78f), .031f, Color.white);
            t.anchor = TextAnchor.MiddleCenter; t.transform.SetParent(g.transform, true);
            return g;
        }
        void ConstruirOficina()
        {
            Color pared = new Color(.11f, .16f, .2f), madera = new Color(.17f, .24f, .27f);
            Cubo("Piso", new Vector3(0, -.1f, 0), new Vector3(8, .2f, 9), new Color(.08f, .12f, .15f));
            Cubo("Pared izquierda", new Vector3(-4, 1.8f, 0), new Vector3(.15f, 3.6f, 9), pared);
            Cubo("Pared derecha", new Vector3(4, 1.8f, 0), new Vector3(.15f, 3.6f, 9), pared);
            Cubo("Pared posterior", new Vector3(0, 1.8f, -4.5f), new Vector3(8, 3.6f, .15f), pared);
            Cubo("Ventanal nocturno", new Vector3(0, 1.8f, 4.4f), new Vector3(8, 3.6f, .1f), new Color(.008f, .018f, .05f), true);
            for (int i = 0; i < 17; i++)
            {
                float x = -3.8f + i * .47f, h = .5f + (i * 7 % 9) * .19f;
                Cubo("Edificio", new Vector3(x, h / 2, 4.25f), new Vector3(.38f, h, .12f), new Color(.025f, .05f, .085f), true);
                for (int j = 0; j < 4; j++) Cubo("Luz ciudad", new Vector3(x, .2f + j * .32f, 4.16f), new Vector3(.08f, .07f, .02f), new Color(.2f, .55f, .65f), true);
            }
            for (int i = -1; i <= 1; i++) Cubo("Marco ventana", new Vector3(i * 2.6f, 1.8f, 4.05f), new Vector3(.06f, 3.6f, .08f), pared);
            Cubo("Escritorio", new Vector3(0, .76f, 1.3f), new Vector3(3.8f, .12f, 1.6f), madera);
            foreach (float x in new[] { -1.65f, 1.65f }) Cubo("Pata", new Vector3(x, .35f, 1.3f), new Vector3(.12f, .7f, 1.25f), pared);
            Cubo("Monitor", new Vector3(0, 1.95f, 2), new Vector3(3.55f, 2.3f, .16f), new Color(.015f, .026f, .03f), true);
            Cubo("Soporte monitor", new Vector3(0, .92f, 2), new Vector3(.18f, .3f, .15f), pared);
            Cubo("Teclado", new Vector3(0, .85f, .8f), new Vector3(1.1f, .04f, .3f), new Color(.04f, .05f, .06f));
            rotulo = Texto("Rotulo", "BANCO INTERMEDIARIO / LABORATORIO VR", new Vector3(-1.6f, 3.04f, 1.89f), .042f, verde).gameObject;
            cabecera = Texto("Cabecera", "", new Vector3(-1.6f, 2.85f, 1.89f), .035f, claro);
            manual = Boton("Modo manual", "MANUAL", -.85f, 2.57f, 1.4f, 2, new Color(.08f, .25f, .29f));
            defensa = Boton("Modo defensa", "CON DEFENSA", .85f, 2.57f, 1.4f, 3, new Color(.08f, .25f, .29f));
            detalle = Texto("Transferencia", "", new Vector3(-1.58f, 2.34f, 1.89f), .043f, claro);
            avisos = Texto("Alertas", "", new Vector3(-1.58f, 1.77f, 1.89f), .03f, new Color(1, .76f, .3f));
            aprobar = Boton("Aprobar", "APROBAR", -.85f, 1.17f, 1.4f, 0, new Color(.02f, .4f, .24f));
            detener = Boton("Detener", "DETENER", .85f, 1.17f, 1.4f, 1, new Color(.6f, .11f, .12f));
            reiniciar = Boton("Reiniciar", "REINICIAR", 0, .92f, 1.2f, 4, new Color(.12f, .17f, .23f));
            pie = Texto("Estado", "", new Vector3(-1.65f, .55f, 1.84f), .028f, claro);
            if (camara == null)
            {
                GameObject c = new GameObject("Camara jugador"); c.transform.SetParent(transform);
                camara = c.AddComponent<Camera>(); c.AddComponent<AudioListener>(); c.tag = "MainCamera";
                c.transform.position = new Vector3(0, 1.8f, -1.65f);
                camara.fieldOfView = 60; camara.nearClipPlane = .05f;
                camara.clearFlags = CameraClearFlags.SolidColor; camara.backgroundColor = new Color(.015f, .025f, .045f);
            }
            yaw = camara.transform.eulerAngles.y; pitch = camara.transform.eulerAngles.x;
            var lightObj = new GameObject("Luz oficina"); lightObj.transform.SetParent(transform);
            var luz = lightObj.AddComponent<Light>(); luz.type = LightType.Directional; luz.intensity = 1.3f;
            lightObj.transform.rotation = Quaternion.Euler(45, -25, 0);
            RenderSettings.ambientLight = new Color(.4f, .45f, .5f);
            reticula = Texto("Reticula", "+", Vector3.zero, .016f, verde);
            reticula.characterSize = .008f;
            reticula.anchor = TextAnchor.MiddleCenter; reticula.transform.SetParent(camara.transform, false);
            reticula.transform.localPosition = new Vector3(0, 0, .5f);
        }
        public static int Distancia(string a, string b)
        {
            a = a.Trim().ToUpperInvariant(); b = b.Trim().ToUpperInvariant();
            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++) for (int j = 1; j <= b.Length; j++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            return d[a.Length, b.Length];
        }
        List<string> Alertas(Orden o)
        {
            var r = new List<string>(); int min = int.MaxValue; string cercano = "";
            foreach (string n in nombres) { int d = Distancia(o.nombre, n); if (d < min) { min = d; cercano = n; } }
            if (min > 0) r.Add(min <= 3 ? "Nombre similar a: " + cercano : "Nombre no registrado");
            if (o.monto > o.limite) r.Add("Monto supera limite didactico del pais");
            if (o.nuevo) r.Add("Beneficiario nuevo: requiere revision");
            return r;
        }
        void PrepararSonido()
        {
            audioBanco = gameObject.AddComponent<AudioSource>();
            audioBanco.playOnAwake = false;
            tonoAviso = Tono("Aviso", 370, .15f);
            tonoDecision = Tono("Confirmación", 740, .12f);
        }
        AudioClip Tono(string nombre, float hz, float duracion)
        {
            const int tasa = 22050;
            float[] onda = new float[(int)(tasa * duracion)];
            for (int i = 0; i < onda.Length; i++)
            {
                float t = (float)i / tasa;
                onda[i] = .18f * Mathf.Sin(2 * Mathf.PI * hz * t) * Mathf.Sin(Mathf.PI * i / onda.Length);
            }
            var clip = AudioClip.Create(nombre, onda.Length, 1, tasa, false);
            clip.SetData(onda, 0);
            return clip;
        }
        void PrepararPortal()
        {
            // Mover el soporte del visor preserva el seguimiento local de la cabeza.
            soporteJugador = camara.transform.parent;
            if (soporteJugador == null || soporteJugador == transform) soporteJugador = camara.transform;
            puesto = soporteJugador.position;
            portal = new GameObject("Portal a operaciones");
            portal.transform.SetParent(transform);
            var fondo = Cubo("Puerta de operaciones", new Vector3(0, 1.65f, -1.25f),
                          new Vector3(2.65f, 2.10f, .10f), new Color(.018f, .055f, .07f), true);
            fondo.transform.SetParent(portal.transform, true);
            foreach (float x in new[] { -1.34f, 1.34f })
                Cubo("Luz de acceso", new Vector3(x, 1.65f, -1.27f), new Vector3(.035f, 2.1f, .07f), verde, true)
                    .transform.SetParent(portal.transform, true);
            Texto("Misión", "BANCO INTERMEDIARIO\n4 y 5 de febrero de 2016",
                  new Vector3(-1.18f, 2.52f, -1.32f), .031f, verde).transform.SetParent(portal.transform, true);
            Texto("Briefing", "Eres Nadia, analista de pagos.\nRevisa ocho órdenes en tránsito.\nCompara nombre, país y monto.\n\nMira ENTRAR durante 2 segundos.",
                  new Vector3(-1.18f, 2.00f, -1.32f), .025f, claro).transform.SetParent(portal.transform, true);
            entrar = Boton("Portal de entrada", "ENTRAR", 0, 1.03f, 1.45f, 5, new Color(.02f, .4f, .24f));
            entrar.transform.position = new Vector3(0, 1.03f, -1.40f);
            entrar.transform.SetParent(portal.transform, true);
            botonPagina = Boton("Cambiar página", "INFORME", 0, 1.16f, 1.8f, 6, new Color(.07f, .31f, .33f));
            etiquetaPagina = botonPagina.GetComponentInChildren<TextMesh>();
        }
        void NuevaRonda()
        {
            indice = 0;
            decisiones.Clear(); retenidas.Clear();
            modoDefensa = false;
            ultimo = "Turno iniciado. Revisa la orden.";
            etapa = Etapa.Entrada;
            soporteJugador.position = puesto + Vector3.back * 2f;
            Mostrar();
        }
        public void Accion(int accion)
        {
            if (Time.unscaledTime < bloqueo) return;
            bloqueo = Time.unscaledTime + .25f;
            if (etapa == Etapa.Entrada)
            {
                if (accion != 5) return;
                soporteJugador.position = puesto;
                etapa = Etapa.Revision;
            }
            else if (accion == 4) { NuevaRonda(); return; }
            else if (etapa == Etapa.Revision && (accion == 2 || accion == 3))
            {
                modoDefensa = accion == 3;
                ultimo = modoDefensa ? "Defensa: retiene órdenes con alertas." : "Manual: el pago depende de ti.";
            }
            else if (etapa == Etapa.Revision && (accion == 0 || accion == 1))
            {
                bool detenerJugador = accion == 1;
                bool alerta = Alertas(cola[indice]).Count > 0;
                bool retener = detenerJugador || (modoDefensa && alerta);
                decisiones.Add(detenerJugador);
                retenidas.Add(retener);
                string causa = detenerJugador ? "Retuviste el pago." : retener ? "Defensa retuvo el pago para verificarlo." : "Autorizaste el pago.";
                ultimo = "Orden " + (indice + 1) + ": " + causa;
                audioBanco.PlayOneShot(retener ? tonoAviso : tonoDecision);
                indice++;
                if (indice == cola.Count) etapa = Etapa.Resultados;
            }
            else if (accion == 6)
            {
                if (etapa == Etapa.Resultados) etapa = Etapa.Informe;
                else if (etapa == Etapa.Informe) etapa = Etapa.Creditos;
                else if (etapa == Etapa.Creditos) etapa = Etapa.Resultados;
            }
            Mostrar();
        }
        void Mostrar()
        {
            bool entrada = etapa == Etapa.Entrada, revision = etapa == Etapa.Revision;
            portal.SetActive(entrada); rotulo.SetActive(!entrada);
            foreach (TextMesh t in new[] { cabecera, detalle, avisos, pie }) t.gameObject.SetActive(!entrada);
            aprobar.SetActive(revision); detener.SetActive(revision);
            manual.SetActive(revision); defensa.SetActive(revision);
            reiniciar.SetActive(!entrada); botonPagina.SetActive(!entrada && !revision);
            if (entrada) return;
            detalle.characterSize = .018f; avisos.characterSize = .014f;
            if (revision)
            {
                var o = cola[indice]; var a = Alertas(o);
                cabecera.text = "ORDEN " + (indice + 1) + " / " + cola.Count + "   |   " + (modoDefensa ? "CON DEFENSA" : "MANUAL");
                detalle.text = o.nombre + "\n" + o.pais + "   |   US$ " + o.monto.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                avisos.text = modoDefensa ? (a.Count == 0 ? "Sin alertas." : string.Join("\n", a)) : (o.nuevo ? "Beneficiario nuevo." : "Beneficiario habitual.") + "\nDecide si requiere aclaración.";
                pie.text = ultimo + "\n" + instrucciones;
            }
            else if (etapa == Etapa.Resultados)
            {
                int coincide = 0, automaticas = 0, efectivas = 0;
                var filas = new StringBuilder("N.º    TU DECISIÓN    VALIDADOR    PAGO FINAL\n");
                for (int i = 0; i < cola.Count; i++)
                {
                    bool alerta = Alertas(cola[i]).Count > 0;
                    if (alerta) automaticas++;
                    if (alerta == decisiones[i]) coincide++;
                    if (retenidas[i]) efectivas++;
                    filas.AppendLine((i + 1) + "         " + (decisiones[i] ? "Detener" : "Aprobar") + "          " +
                        (alerta ? "Retener" : "Liberar") + "         " + (retenidas[i] ? "Retenido" : "Liberado"));
                }
                cabecera.text = "CIERRE DEL TURNO   |   " + coincide + " / " + cola.Count + " coincidencias";
                detalle.characterSize = .013f;
                detalle.text = filas.ToString().TrimEnd();
                avisos.text = "";
                pie.text = "Validador: " + automaticas + " alertas. Pagos retenidos: " + efectivas + ".";
                etiquetaPagina.text = "VER INFORME";
            }
            else if (etapa == Etapa.Informe)
            {
                cabecera.text = "INFORME   |   LA ORDEN A SRI LANKA";
                detalle.text = "US$20 millones a SHALIKA FANDATION.\nLa analista pidió aclaración por el nombre.\nLa revisión humana detuvo ese pago.";
                avisos.text = "Las otras órdenes no tuvieron igual revisión.\nEn el ejercicio, la defensa compara nombres,\nmontos y antigüedad antes de liberar el pago.";
                pie.text = "Las otras siete órdenes y los umbrales son didácticos.";
                etiquetaPagina.text = "CRÉDITOS";
            }
            else
            {
                cabecera.text = "CRÉDITOS   |   ESCENARIO 11";
                detalle.characterSize = .014f;
                detalle.text = string.IsNullOrWhiteSpace(equipoYRoles) ? "Equipo: completar en el Inspector." : equipoYRoles;
                avisos.text = "Caso: ficha docente, Banco intermediario.\nContexto: denuncia Park (EE. UU., 2018).\nMotor Unity y Google Cardboard XR.\nGeometría y tonos generados por código.";
                pie.text = "Prototipo educativo · Universidad Continental, Huancayo";
                etiquetaPagina.text = "RESULTADOS";
            }
        }
        void Update()
        {
            if (camara == null) return;
            Vector2 pos = new Vector2(Screen.width / 2f, Screen.height / 2f), delta = Vector2.zero;
            bool click = false, mirar = false, alternar = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) { pos = Mouse.current.position.ReadValue(); delta = Mouse.current.delta.ReadValue() * .12f; click = Mouse.current.leftButton.wasPressedThisFrame; mirar = Mouse.current.rightButton.isPressed; }
            if (Keyboard.current != null) alternar = Keyboard.current.gKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            pos=Input.mousePosition;delta=new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*2;
            click=Input.GetMouseButtonDown(0);mirar=Input.GetMouseButton(1);alternar=Input.GetKeyDown(KeyCode.G);
#endif
            if (alternar) seleccionPorMirada = !seleccionPorMirada;
#if UNITY_EDITOR || UNITY_STANDALONE
            if (controlPC && mirar) { yaw += delta.x; pitch = Mathf.Clamp(pitch - delta.y, -65, 65); camara.transform.rotation = Quaternion.Euler(pitch, yaw, 0); }
#endif
            reticula.gameObject.SetActive(seleccionPorMirada);
            Ray ray = seleccionPorMirada ? new Ray(camara.transform.position, camara.transform.forward) : camara.ScreenPointToRay(pos);
            RaycastHit hit; BancoBoton actual = null;
            if (Physics.Raycast(ray, out hit, 15)) actual = hit.collider.GetComponent<BancoBoton>();
            if (actual != objetivo) { objetivo = actual; permanencia = 0; requiereApartar = false; }
            if (actual != null && !requiereApartar)
            {
                if (seleccionPorMirada) { permanencia += Time.unscaledDeltaTime; reticula.text = "+ " + Mathf.RoundToInt(Mathf.Clamp01(permanencia / segundosMirada) * 100) + "%"; }
                if (click || (seleccionPorMirada && permanencia >= segundosMirada))
                { actual.Activar(); permanencia = 0; requiereApartar = seleccionPorMirada; }
            }
            else reticula.text = actual != null ? "✓" : "+";
        }
        void OnDestroy() { if (tonoAviso != null) Destroy(tonoAviso); if (tonoDecision != null) Destroy(tonoDecision); foreach (var m in materiales) if (m != null) Destroy(m); }
    }
}
